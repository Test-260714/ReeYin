using System;

namespace ReeYin_V.Core.Events
{
    /// <summary>
    /// 解决方案操作类型
    /// </summary>
    public enum SolutionOperationType
    {
        /// <summary>
        /// 新建解决方案
        /// </summary>
        New,
        /// <summary>
        /// 打开解决方案
        /// </summary>
        Open,
        /// <summary>
        /// 保存解决方案
        /// </summary>
        Save,
        /// <summary>
        /// 释放/关闭解决方案
        /// </summary>
        Release
    }

    /// <summary>
    /// 解决方案事件参数
    /// </summary>
    public class SolutionEventArgs
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public SolutionOperationType OperationType { get; set; }

        /// <summary>
        /// 解决方案文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 解决方案名称
        /// </summary>
        public string SolutionName { get; set; }

        /// <summary>
        /// 是否需要用户确认
        /// </summary>
        public bool RequireConfirmation { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object AdditionalData { get; set; }

        public SolutionEventArgs(SolutionOperationType operationType)
        {
            OperationType = operationType;
            RequireConfirmation = true;
        }
    }
}
