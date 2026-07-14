using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using System.ComponentModel;
using System.Windows;

namespace ReeYin_V.NodifyManager
{
    ///// <summary>
    ///// 节点状态
    ///// </summary>
    //public enum NodeStatus
    //{
    //    None = 0,
    //    Success = 1,
    //    Failed = 2,
    //    Running = 3,
    //    Warning = 4
    //}


    ///// <summary>
    ///// 操作节点
    ///// </summary>
    //[Serializable]
    //public class OperationViewModel : BindableBase
    //{
    //    #region Fields
    //    public bool IsReadOnly { get; set; }

    //    /// <summary>
    //    /// 节点状态
    //    /// </summary>
    //    public NodeStatus CurStatus { get; set; }

    //    public NodifyObservableCollection<ConnectorViewModel> Input { get; } = new NodifyObservableCollection<ConnectorViewModel>();
    //    #endregion

    //    #region Properties

    //    [JsonIgnore]
    //    private IModuleParam _moduleParam;

    //    public IModuleParam ModuleParam
    //    {
    //        get { return _moduleParam; }
    //        set { _moduleParam = value; }
    //    }
    //    [JsonIgnore]
    //    private IModuleInputParam _moduleInputParam;
    //    [JsonIgnore]
    //    public IModuleInputParam ModuleInputParam
    //    {
    //        get { return _moduleInputParam; }
    //        set { _moduleInputParam = value; }
    //    }
    //    [JsonIgnore]
    //    private IModuleOutputParam _moduleOutputParam;
    //    [JsonIgnore]
    //    public IModuleOutputParam ModuleOutputParam
    //    {
    //        get { return _moduleOutputParam; }
    //        set { _moduleOutputParam = value; }
    //    }

    //    [JsonIgnore]
    //    private string? _icon = "X";
    //    public string? Icon
    //    {
    //        get => _icon;
    //        set => SetProperty(ref _icon, value);
    //    }

    //    [JsonIgnore]
    //    private Point _location;
    //    public Point Location
    //    {
    //        get => _location;
    //        set => SetProperty(ref _location, value);
    //    }

    //    [JsonIgnore]
    //    private Size _size;
    //    public Size Size
    //    {
    //        get => _size;
    //        set => SetProperty(ref _size, value);
    //    }

    //    [JsonIgnore]
    //    private string? _title;
    //    public string? Title
    //    {
    //        get => _title;
    //        set => SetProperty(ref _title, value);
    //    }
    //    [JsonIgnore]
    //    private bool _isSelected;
    //    public bool IsSelected
    //    {
    //        get => _isSelected;
    //        set => SetProperty(ref _isSelected, value);
    //    }

    //    [JsonIgnore]
    //    private IOperation? _operation;
    //    [JsonIgnore]
    //    public IOperation? Operation
    //    {
    //        get => _operation;
    //        set => SetProperty(ref _operation, value)
    //            .Then(OnInputValueChanged);
    //    }

    //    [JsonIgnore]
    //    private ConnectorViewModel? _output;

    //    public ConnectorViewModel? Output
    //    {
    //        get => _output;
    //        set
    //        {
    //            if (SetProperty(ref _output, value) && _output != null)
    //            {
    //                _output.Operation = this;
    //            }
    //        }
    //    }
    //    #endregion

    //    #region Constructor 
    //    public OperationViewModel()
    //    {
    //        Input.WhenAdded(x =>
    //        {
    //            x.Operation = this;
    //            x.IsInput = true;
    //            x.PropertyChanged += OnInputValueChanged;
    //        })
    //        .WhenRemoved(x =>
    //        {
    //            x.PropertyChanged -= OnInputValueChanged;
    //        });
    //    }
    //    #endregion

    //    #region Methods
    //    private void OnInputValueChanged(object? sender, PropertyChangedEventArgs e)
    //    {
    //        if (e.PropertyName == nameof(ConnectorViewModel.Value))
    //        {
    //            OnInputValueChanged();
    //        }
    //    }

    //    protected virtual void OnInputValueChanged()
    //    {
    //        if (Output != null && Operation != null)
    //        {
    //            try
    //            {
    //                var input = Input.Select(i => i.Value).ToArray();
    //                Output.Value = Operation?.Execute(input) ?? 0;
    //            }
    //            catch
    //            {

    //            }
    //        }
    //    }
    //    #endregion
    //}
} 
