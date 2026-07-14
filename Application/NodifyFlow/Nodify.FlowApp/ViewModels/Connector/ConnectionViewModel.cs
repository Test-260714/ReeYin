using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Windows;
using System.Windows.Input;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class ConnectionViewModel : ObservableObject
    {
        #region Fields  
        [JsonIgnore]
        private NodifyEditorViewModel _graph = default!;

        [JsonIgnore]
        private ConnectorViewModel _input = default!;

        [JsonIgnore]
        private ConnectorViewModel _output = default!;

        [JsonIgnore]
        private bool _isSelected;

        [JsonIgnore]
        private NodeViewModel _source = default!;

        [JsonIgnore]
        private NodeViewModel _target = default!;

        [JsonIgnore]
        private bool _isVisible;

        private BlackboardItemReferenceViewModel? _conditionReference;

        /// <summary>
        /// 活动的
        /// </summary>
        private bool _isActive;

        [JsonIgnore]
        private Point _targetLocation;
        #endregion

        #region Properties
        public NodifyEditorViewModel Graph
        {
            get => _graph;
            set => SetProperty(ref _graph, value);
        }

        public ConnectorViewModel Input
        {
            get => _input;
            set => SetProperty(ref _input, value);
        }

        public ConnectorViewModel Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        public NodeViewModel Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public NodeViewModel Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public Point TargetLocation
        {
            get => _targetLocation;
            set => SetProperty(ref _targetLocation, value);
        }

        public BlackboardItemReferenceViewModel? ConditionReference
        {
            get => _conditionReference;
            set
            {
                if (SetProperty(ref _conditionReference, value))
                {
                    SetCondition(_conditionReference);
                }
            }
        }

        public BlackboardItemViewModel? Condition { get; private set; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand SplitCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<Point>(Split);
            }
        }

        [JsonIgnore]
        public ICommand DisconnectCommand 
        {
            get
            {
                return new DelegateCommand(() => Graph.DeleteConnection(this));
            }
        }
        #endregion

        #region Constructor
        public ConnectionViewModel()
        {
            //SplitCommand = new ReeYin_V.NodifyManager.DelegateCommand<Point>(Split);
            //DisconnectCommand = new DelegateCommand(Remove);
        }
        #endregion

        #region Methods
        public void Split(Point point)
    => Graph.Schema.SplitConnection(this, point);

        public void Remove()
        {
            Graph.Connections.Remove(this);
        }


        private void SetCondition(BlackboardItemReferenceViewModel? conditionRef)
        {
            Condition = BlackboardDescriptor.GetItem(conditionRef);

            OnPropertyChanged(nameof(Condition));
        }
        #endregion
    }
}
