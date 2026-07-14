using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Project
{
    /// <summary>
    /// 当前解决方案的运行时临时数据。
    /// 仅在内存中使用，不参与持久化。
    /// </summary>
    public class NodifySolutionRuntimeData
    {
        /// <summary>
        /// 图像显示控件对应。
        /// </summary>
        public Dictionary<string, object> ImgControlPair { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 流程执行结束标记。
        /// </summary>
        public Dictionary<int, Dictionary<int, bool>> IsProcessEnds { get; set; } =
            new Dictionary<int, Dictionary<int, bool>>();

        /// <summary>
        /// 节点运行时缓存。
        /// </summary>
        public object NodeCaches { get; set; }

        /// <summary>
        /// 节点参数缓存。
        /// </summary>
        public Dictionary<string, object> NodeParamCaches { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 为后续扩展保留的临时数据容器。
        /// </summary>
        public Dictionary<string, object> TemporaryItems { get; set; } = new Dictionary<string, object>();
    }
}
