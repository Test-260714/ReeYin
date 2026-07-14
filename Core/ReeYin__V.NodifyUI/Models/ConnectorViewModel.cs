using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Collections.Generic;
using System.Windows;

namespace ReeYin_V.NodifyManager
{
    //public enum ConnectorFlow
    //{
    //    Input,
    //    Output
    //}

    //public enum ConnectorShape
    //{
    //    Circle,
    //    Triangle,
    //    Square,
    //}

    ///// <summary>
    ///// 连接节点
    ///// </summary>
    //[Serializable]
    //public class ConnectorViewModel : ObservableObject
    //{

    //    [JsonIgnore]
    //    private string? _title;
    //    public string? Title
    //    {
    //        get => _title;
    //        set => SetProperty(ref _title, value);
    //    }
    //    [JsonIgnore]
    //    private double _value;
    //    public double Value
    //    {
    //        get => _value;
    //        set => SetProperty(ref _value, value)
    //            .Then(() => ValueObservers.ForEach(o => o.Value = value));
    //    }
    //    [JsonIgnore]
    //    private bool _isConnected;
    //    public bool IsConnected
    //    {
    //        get => _isConnected;
    //        set => SetProperty(ref _isConnected, value);
    //    }
    //    [JsonIgnore]
    //    private bool _isInput;
    //    [JsonIgnore]
    //    public bool IsInput
    //    {
    //        get => _isInput;
    //        set => SetProperty(ref _isInput, value);
    //    }
    //    [JsonIgnore]
    //    private Point _anchor;
    //    public Point Anchor
    //    {
    //        get => _anchor;
    //        set => SetProperty(ref _anchor, value);
    //    }
    //    [JsonIgnore]
    //    private OperationViewModel _operation = default!;
    //    public OperationViewModel Operation
    //    {
    //        get => _operation;
    //        set => SetProperty(ref _operation, value);
    //    }

    //    private NodeViewModel _node = default!;
    //    public NodeViewModel Node
    //    {
    //        get => _node;
    //        internal set
    //        {
    //            if (SetProperty(ref _node, value))
    //            {
    //                OnNodeChanged();
    //            }
    //        }
    //    }

    //    private ConnectorShape _shape;
    //    public ConnectorShape Shape
    //    {
    //        get => _shape;
    //        set => SetProperty(ref _shape, value);
    //    }

    //    public int MaxConnections { get; set; } = 2;

    //    public ConnectorFlow Flow { get; private set; }

    //    public List<ConnectorViewModel> ValueObservers { get; } = new List<ConnectorViewModel>();

    //    public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();

    //    protected virtual void OnNodeChanged()
    //    {
    //        if (Node is FlowNodeViewModel flow)
    //        {
    //            Flow = flow.Input.Contains(this) ? ConnectorFlow.Input : ConnectorFlow.Output;
    //        }
    //        else if (Node is KnotNodeViewModel knot)
    //        {
    //            Flow = knot.Flow;
    //        }
    //    }

    //    public bool IsConnectedTo(ConnectorViewModel con)
    //=> Connections.Any(c => c.Input == con || c.Output == con);

    //    public virtual bool AllowsNewConnections()
    //        => Connections.Count < MaxConnections;

    //    public void Disconnect()
    //        => Node.Graph.Schema.DisconnectConnector(this);
    //}
}
