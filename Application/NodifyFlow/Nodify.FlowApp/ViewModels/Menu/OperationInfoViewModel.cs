using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.NodifyManager;
using System.Collections.Generic;

namespace Nodify.FlowApp
{
    /// <summary>
    /// 节点类型
    /// </summary>
    public enum OperationType
    {
        Normal,
        Expando,
        Expression,
        Calculator,
        Group,
        Graph
    }

    /// <summary>
    /// 目录节点信息
    /// </summary>
    [Serializable]
    public class OperationInfoViewModel
    {
        public MenuInfo? MenuInfo { get; set; }
        public string? BindingView = "CollectImageView";
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public OperationType Type { get; set; }
        public NodeStatus CurStatus { get; set; }

        [JsonIgnore]
        public IOperation? Operation { get; set; }
        public List<string?> Input { get; } = new List<string?>();
        public uint MinInput { get; set; }
        public uint MaxInput { get; set; }
    }
}
