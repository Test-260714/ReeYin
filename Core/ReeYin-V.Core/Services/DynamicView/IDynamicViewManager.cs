using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.DynamicView
{
    public interface IDynamicViewManager
    {
        /// <summary>
        /// 所有动态控件
        /// </summary>
        public IReadOnlyList<DynamicView> DynamicViews { get; }

        /// <summary>
        /// 动态控件被移除时触发
        /// </summary>
        event EventHandler<DynamicViewsRemovedEventArgs> DynamicViewsRemoved;

        bool Init();

        bool AddDynamic(DynamicView dynamicView);

        bool RemoveDynamic(DynamicView dynamicView);

        int RemoveDynamic(
            DynamicViewType type,
            string viewName = null,
            int nodeSerial = -1,
            string subjection = null
        );

        void Clear();
    }

    public enum DynamicViewType
    {
        //通用
        General,
        //定制
        Custom,
        //节点映射
        NodeMap
    }

    /// <summary>
    /// 动态控件
    /// </summary>
    public class DynamicView
    {
        private DynamicViewType _type = DynamicViewType.General;
        /// <summary>
        /// 类型
        /// </summary>
        public DynamicViewType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private int _nodeSerial = -1;
        /// <summary>
        /// 类型为节点映射时必须设置（会存在多个节点的问题，使用Serial区分）
        /// </summary>
        public int NodeSerial
        {
            get { return _nodeSerial; }
            set { _nodeSerial = value; }
        }

        private string _subjection;
        /// <summary>
        /// 隶属
        /// </summary>
        public string Subjection
        {
            get { return _subjection; }
            set { _subjection = value; }
        }

        private string _displayName;
        /// <summary>
        /// 展示名称
        /// </summary>
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        private string _viewName;
        /// <summary>
        /// 实际View的名称
        /// </summary>
        public string ViewName
        {
            get { return _viewName; }
            set { _viewName = value; }
        }

        public DynamicView Clone()
        {
            return new DynamicView
            {
                Type = Type,
                NodeSerial = NodeSerial,
                Subjection = Subjection,
                DisplayName = DisplayName,
                ViewName = ViewName
            };
        }

    }

    public class DynamicViewsRemovedEventArgs : EventArgs
    {
        public DynamicViewsRemovedEventArgs(IEnumerable<DynamicView> removedViews, bool isClearOperation = false)
        {
            RemovedViews = removedViews?
                .Where(item => item != null)
                .Select(item => item.Clone())
                .ToArray() ?? Array.Empty<DynamicView>();
            IsClearOperation = isClearOperation;
        }

        /// <summary>
        /// 本次被移除的动态控件
        /// </summary>
        public IReadOnlyList<DynamicView> RemovedViews { get; }

        /// <summary>
        /// 是否由清空操作触发
        /// </summary>
        public bool IsClearOperation { get; }
    }
}
