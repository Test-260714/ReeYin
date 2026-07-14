using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Collections.Generic;
using System.Windows;

namespace Nodify.FlowApp
{
    /// <summary>
    /// 连接器形状
    /// </summary>
    public enum ConnectorShape
    {
        Circle,
        Triangle,
        Square,
    }

    /// <summary>
    /// 连接节点
    /// </summary>
    [Serializable]
    public class ConnectorViewModel : ObservableObject
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [JsonIgnore]
        private double _value;
        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value)
                .Then(() => ValueObservers.ForEach(o => o.Value = value));
        }

        [JsonIgnore]
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        [JsonIgnore]
        private bool _isInput;
        public bool IsInput
        {
            get => _isInput;
            set => SetProperty(ref _isInput, value);
        }

        [JsonIgnore]
        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set => SetProperty(ref _anchor, value);
        }

        [JsonIgnore]
        private OperationViewModel _operation = default!;
        public OperationViewModel Operation
        {
            get => _operation;
            set => SetProperty(ref _operation, value);
        }

        [JsonIgnore]
        private NodeViewModel _node = default!;
        public NodeViewModel Node
        {
            get => _node;
            set
            {
                if (SetProperty(ref _node, value))
                {
                    OnNodeChanged();
                }
            }
        }

        [JsonIgnore]
        private ConnectorShape _shape;
        public ConnectorShape Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections { get; set; } = 20;

        public ConnectorFlow Flow { get; set; }

        public List<ConnectorViewModel> ValueObservers { get; set; } = new List<ConnectorViewModel>();

        public NodifyObservableCollection<ConnectionViewModel> Connections { get; set; } = new NodifyObservableCollection<ConnectionViewModel>();
        #endregion

        #region Constructor

        public ConnectorViewModel()
        {
            Connections.WhenAdded(c =>
            {
                if (c.Input == null)
                    c.Input = new ConnectorViewModel();
                    c.Input.IsConnected = true;
                if (c.Output == null)
                    c.Output = new ConnectorViewModel();
                    c.Output.IsConnected = true;
            }).WhenRemoved(c =>
            {
                if (c.Input.Connections.Count == 0)
                {
                    c.Input.IsConnected = false; 
                }

                if (c.Output.Connections.Count == 0)
                {
                    c.Output.IsConnected = false;
                }
            });
        }
        #endregion

        #region Methods
        protected virtual void OnNodeChanged()
        {
            if (Node is FlowNodeViewModel flow)
            {
                Flow = flow.Input.Contains(this) ? ConnectorFlow.Input : ConnectorFlow.Output;
            }
            else if (Node is KnotNodeViewModel knot)
            {
                Flow = knot.Flow;
            }
        }

        public bool IsConnectedTo(ConnectorViewModel con)
    => Connections.Any(c => c.Input == con || c.Output == con);

        public virtual bool AllowsNewConnections()
            => Connections.Count < MaxConnections;

        public void Disconnect()
            => Node.Graph.Schema.DisconnectConnector(this);
        #endregion
    }
}
