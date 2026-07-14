using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.Helper;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Helper;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class AppViewModel : ObservableObject
    {
        #region Fields
        [JsonIgnore]
        public PlaygroundSettings Settings => PlaygroundSettings.Instance;

        [JsonIgnore]
        public string ConnectNodesText => Settings.ShouldConnectNodes ? "CONNECT NODES" : "DISCONNECT NODES";

        private EditorViewModel? _selectedEditor;

        private bool _autoSelectNewEditor = true;

        #endregion

        #region Properties
        public Guid guid { get; set; }

        public NodifyEditorViewModel GraphViewModel { get; set; } =new NodifyEditorViewModel();

        public NodifyObservableCollection<EditorViewModel> Editors { get; set; } = new NodifyObservableCollection<EditorViewModel>();

        public EditorViewModel? SelectedEditor
        {
            get => _selectedEditor;
            set => SetProperty(ref _selectedEditor, value);
        }

        public bool AutoSelectNewEditor
        {
            get => _autoSelectNewEditor;
            set => SetProperty(ref _autoSelectNewEditor, value);
        }

        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand GenerateRandomNodesCommand { get; }

        [JsonIgnore]
        public ICommand PerformanceTestCommand { get; }

        [JsonIgnore]
        public ICommand ToggleConnectionsCommand { get; }

        [JsonIgnore]
        public ICommand ResetCommand { get; }

        [JsonIgnore]
        public ICommand AddEditorCommand 
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Editors.Add(new EditorViewModel
                    {
                        Name = $"Editor {Editors.Count + 1}"

                    });
                });
            }
        }

        [JsonIgnore]
        public ICommand CloseEditorCommand 
        {
            get 
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<Guid>(
                id => Editors.RemoveOne(editor => editor.Id == id),
                _ => Editors.Count > 0 && SelectedEditor != null);
            }
        }
        #endregion

        #region Constructor 
        public AppViewModel()
        {
            GenerateRandomNodesCommand = new DelegateCommand(GenerateRandomNodes);
            PerformanceTestCommand = new DelegateCommand(PerformanceTest);
            ToggleConnectionsCommand = new DelegateCommand(ToggleConnections);
            ResetCommand = new DelegateCommand(ResetGraph);
            if (GraphViewModel != null) ;
            BindingOperations.EnableCollectionSynchronization(GraphViewModel.Nodes, GraphViewModel.Nodes);
            BindingOperations.EnableCollectionSynchronization(GraphViewModel.Connections, GraphViewModel.Connections);

            Settings.PropertyChanged += OnSettingsChanged;

            Editors.WhenAdded((editor) =>
            {
                if (AutoSelectNewEditor || Editors.Count == 1)
                {
                    SelectedEditor = editor;
                }
                editor.OnOpenInnerCalculator += OnOpenInnerCalculator;
            })
            .WhenRemoved((editor) =>
            {
                editor.OnOpenInnerCalculator -= OnOpenInnerCalculator;
                var childEditors = Editors.Where(ed => ed.Parent == editor).ToList();
                childEditors.ForEach(ed => Editors.Remove(ed));
            });
            Editors.Add(new EditorViewModel
            {
                Name = $"Editor {Editors.Count + 1}"
            });
        }
        #endregion

        #region Methods 
        private void OnOpenInnerCalculator(EditorViewModel parentEditor, CalculatorViewModel calculator)
        {
            return;
            var editor = Editors.FirstOrDefault(e => e.Calculator == calculator);
            if (editor != null)
            {
                SelectedEditor = editor;
            }
            else
            {
                var childEditor = new EditorViewModel
                {
                    Parent = parentEditor,
                    Calculator = calculator,
                    Name = $"[Inner] Editor {Editors.Count + 1}"
                };
                Editors.Add(childEditor);
            }
        }

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaygroundSettings.ShouldConnectNodes))
                OnPropertyChanged(nameof(ConnectNodesText));
        }

        private void ResetGraph()
        {
            GraphViewModel.Nodes.Clear();
            EditorSettings.Instance.Location = new System.Windows.Point(0, 0);
            EditorSettings.Instance.Zoom = 1.0d;
        }

        /// <summary>
        /// 生成随机的节点
        /// </summary>
        private async void GenerateRandomNodes()
        {
            uint minNodesByType = Settings.MinNodes / 2;
            uint maxNodesByType = Settings.MaxNodes / 2;

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            var verticalNodes = RandomNodesGenerator.GenerateNodes<VerticalNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);
            await CopyToAsync(verticalNodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async void ToggleConnections()
        {
            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
            else
            {
                GraphViewModel.Connections.Clear();
            }
        }

        private async void PerformanceTest()
        {
            uint count = Settings.PerformanceTestNodes;
            int distance = 500;
            int size = (int)count / (int)Math.Sqrt(count);

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(count)
            {
                NodeLocationGenerator = (s, i) => new System.Windows.Point(i % size * distance, i / size * distance),
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async Task ConnectNodes()
        {
            var schema = new GraphSchema();
            var connections = RandomNodesGenerator.GenerateConnections(GraphViewModel.Nodes);

            if (Settings.AsyncLoading)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < connections.Count; i++)
                    {
                        var con = connections[i];
                        schema.TryAddConnection(con.Input, con.Output);
                    }
                });
            }
            else
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    var con = connections[i];
                    schema.TryAddConnection(con.Input, con.Output);
                }
            }
        }

        private async Task CopyToAsync(IList source, IList target)
        {
            if (Settings.AsyncLoading)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        target.Add(source[i]);
                    }
                });
            }
            else
            {
                for (int i = 0; i < source.Count; i++)
                {
                    target.Add(source[i]);
                }
            }
        }
        #endregion
    }
}
