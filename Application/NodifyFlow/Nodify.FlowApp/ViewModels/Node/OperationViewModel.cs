using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.NodifyManager;
using System.ComponentModel;
using System.Windows;

namespace Nodify.FlowApp
{
    /// <summary>
    /// 节点
    /// </summary>
    [Serializable]
    public class OperationViewModel : BindableBase
    {
        #region Fields
        [JsonIgnore]
        private IModuleParam _moduleParam;

        //[JsonIgnore]
        //private IModuleInputParam _moduleInputParam;

        //[JsonIgnore]
        //private IModuleOutputParam _moduleOutputParam;

        [JsonIgnore]
        private string? _icon = "X";

        [JsonIgnore]
        private Point _location;

        [JsonIgnore]
        private Size _size;

        [JsonIgnore]
        private string? _title;

        [JsonIgnore]
        private ConnectorViewModel? _output;

        [JsonIgnore]
        private IOperation? _operation;

        [JsonIgnore]
        private bool _isSelected;

        #endregion

        #region Properties
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 节点状态
        /// </summary>
        public NodeStatus CurStatus { get; set; }

        public NodifyObservableCollection<ConnectorViewModel> Input { get; set; } = new NodifyObservableCollection<ConnectorViewModel>();

        public IModuleParam ModuleParam
        {
            get { return _moduleParam; }
            set { _moduleParam = value; }
        }

        //[JsonIgnore]
        //public IModuleInputParam ModuleInputParam
        //{
        //    get { return _moduleInputParam; }
        //    set { _moduleInputParam = value; }
        //}

        //[JsonIgnore]
        //public IModuleOutputParam ModuleOutputParam
        //{
        //    get { return _moduleOutputParam; }
        //    set { _moduleOutputParam = value; }
        //}

        public string? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IOperation? Operation
        {
            get => _operation;
            set => SetProperty(ref _operation, value)
                .Then(OnInputValueChanged);
        }

        public ConnectorViewModel? Output
        {
            get => _output;
            set
            {
                if (SetProperty(ref _output, value) && _output != null)
                {
                    _output.Operation = this;
                }
            }
        }
        #endregion

        #region Constructor 
        public OperationViewModel()
        {
            Input.WhenAdded(x =>
            {
                x.Operation = this;
                x.IsInput = true;
                x.PropertyChanged += OnInputValueChanged;
            })
            .WhenRemoved(x =>
            {
                x.PropertyChanged -= OnInputValueChanged;
            });
        }
        #endregion

        #region Methods
        private void OnInputValueChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectorViewModel.Value))
            {
                OnInputValueChanged();
            }
        }

        protected virtual void OnInputValueChanged()
        {
            if (Output != null && Operation != null)
            {
                try
                {
                    var input = Input.Select(i => i.Value).ToArray();
                    Output.Value = Operation?.Execute(input) ?? 0;
                }
                catch
                {

                }
            }
        }
        #endregion
    }
} 
