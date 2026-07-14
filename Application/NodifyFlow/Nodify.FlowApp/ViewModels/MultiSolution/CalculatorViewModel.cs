using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class CalculatorViewModel : ObservableObject
    {
        #region Fields
        [JsonIgnore]
        private NodifyObservableCollection<OperationViewModel> _operations = new NodifyObservableCollection<OperationViewModel>();

        [JsonIgnore]
        private OperationViewModel? _selectedNode;

        [JsonIgnore]
        private NodifyObservableCollection<OperationViewModel> _selectedOperations = new NodifyObservableCollection<OperationViewModel>();

        [JsonIgnore]
        private ConnectionViewModel? _selectedConnection;

        [JsonIgnore]
        private NodifyObservableCollection<ConnectionViewModel> _selectedConnections = new NodifyObservableCollection<ConnectionViewModel>();
        #endregion

        #region Properties
        public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();
        
        public PendingConnectionViewModel PendingConnection { get; set; } = new PendingConnectionViewModel();

        [JsonIgnore]
        public OperationsMenuViewModel OperationsMenu { get; set; }

        public NodifyObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            set => SetProperty(ref _operations, value);
        }

        [JsonIgnore]
        public OperationViewModel? SelectedNode
        {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        [JsonIgnore]
        public NodifyObservableCollection<OperationViewModel> SelectedOperations
        {
            get => _selectedOperations;
            set => SetProperty(ref _selectedOperations, value);
        }

        [JsonIgnore]
        public ConnectionViewModel? SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        [JsonIgnore]
        public NodifyObservableCollection<ConnectionViewModel> SelectedConnections
        {
            get => _selectedConnections;
            set => SetProperty(ref _selectedConnections, value);
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand StartConnectionCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>
                    (_ => PendingConnection.IsVisible = true, (c) => !(c.IsConnected && c.IsInput));
            }
        }

        [JsonIgnore]
        public ICommand CreateConnectionCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>(
                _ => CreateConnection(PendingConnection.Source, PendingConnection.Target),
                _ => CanCreateConnection(PendingConnection.Source, PendingConnection.Target));
            }
        }

        [JsonIgnore]
        public ICommand DisconnectConnectorCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>
                    (DisconnectConnector);
            }
        }

        [JsonIgnore]
        public ICommand DeleteSelectionCommand 
        {
            get
            {
                return new DelegateCommand(DeleteSelection);
            }
        }

        [JsonIgnore]
        public ICommand GroupSelectionCommand 
        {
            get
            {
                return new DelegateCommand(GroupSelectedOperations, () => SelectedOperations.Count > 0);
            } 
        }

        [JsonIgnore]
        public ICommand DeleteSelectionLineCommand { get; }
        #endregion

        #region Constructor
        public CalculatorViewModel()
        {
            //CreateConnectionCommand = new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>(
            //    _ => CreateConnection(PendingConnection.Source, PendingConnection.Target),
            //    _ => CanCreateConnection(PendingConnection.Source, PendingConnection.Target));
            //StartConnectionCommand = new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>(_ => PendingConnection.IsVisible = true, (c) => !(c.IsConnected && c.IsInput));
            //DisconnectConnectorCommand = new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>(DisconnectConnector);
            //DeleteSelectionCommand = new DelegateCommand(DeleteSelection);
            //GroupSelectionCommand = new DelegateCommand(GroupSelectedOperations, () => SelectedOperations.Count > 0);
            //DeleteSelectionLineCommand = new DelegateCommand(DeleteSelection, () => SelectedNodes.Count > 0 || SelectedConnections.Count > 0);
            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;

                c.Input.Value = c.Output.Value;

                c.Output.ValueObservers.Add(c.Input);
            })
            .WhenRemoved(c =>
            {
                var ic = Connections.Count(con => con.Input == c.Input || con.Output == c.Input);
                var oc = Connections.Count(con => con.Input == c.Output || con.Output == c.Output);

                if (ic == 0)
                {
                    c.Input.IsConnected = false;
                }

                if (oc == 0)
                {
                    c.Output.IsConnected = false;
                }

                c.Output.ValueObservers.Remove(c.Input);
            });

            Operations.WhenAdded(x =>
            {
                x.Input.WhenRemoved(RemoveConnection);

                if (x is CalculatorInputOperationViewModel ci)
                {
                    ci.Output.WhenRemoved(RemoveConnection);
                }

                void RemoveConnection(ConnectorViewModel i)
                {
                    var c = Connections.Where(con => con.Input == i || con.Output == i).ToArray();
                    c.ForEach(con => Connections.Remove(con));
                }
            })
            .WhenRemoved(x =>
            {
                foreach (var input in x.Input)
                {
                    DisconnectConnector(input);
                }

                if (x.Output != null)
                {
                    DisconnectConnector(x.Output);
                }
            });

            OperationsMenu = new OperationsMenuViewModel(this);
        }
        #endregion

        #region Methods
        /// <summary>
        /// ALT 鼠标点击链接端子解除连接
        /// </summary>
        /// <param name="connector"></param>
        private void DisconnectConnector(ConnectorViewModel connector)
        {
            var connections = Connections.Where(c => c.Input == connector || c.Output == connector).ToList();
            connections.ForEach(c => Connections.Remove(c));
        }

        internal bool CanCreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
            => target == null || (source != target && source.Operation != target.Operation && source.IsInput != target.IsInput);

        internal void CreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
        {
            if (target == null)
            {
                PendingConnection.IsVisible = true;
                OperationsMenu.OpenAt(PendingConnection.TargetLocation);
                OperationsMenu.Closed += OnOperationsMenuClosed;
                return;
            }

            var input = source.IsInput ? source : target;
            var output = target.IsInput ? source : target;

            PendingConnection.IsVisible = false;

            DisconnectConnector(input);

            Connections.Add(new ConnectionViewModel
            {
                Input = input,
                Output = output
            });
        }

        private void OnOperationsMenuClosed()
        {
            PendingConnection.IsVisible = false;
            OperationsMenu.Closed -= OnOperationsMenuClosed;
        }

        private void DeleteSelection()
        {
            var selected = SelectedOperations.ToList();
            selected.ForEach(o => Operations.Remove(o));
        }

        private void GroupSelectedOperations()
        {
            var selected = SelectedOperations.ToList();
            var bounding = selected.GetBoundingBox(50);

            Operations.Add(new OperationGroupViewModel
            {
                Title = "Operations",
                Icon = "X",
                Location = bounding.Location,
                GroupSize = new Size(bounding.Width, bounding.Height)
            });
        }
        #endregion

    }
}

