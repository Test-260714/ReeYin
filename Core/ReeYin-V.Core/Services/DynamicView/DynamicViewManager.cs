using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Core.Services.DynamicView
{
    [ExposedService(Lifetime.Singleton, 4, typeof(IDynamicViewManager))]
    public class DynamicViewManager : IDynamicViewManager
    {
        #region Fields
        private readonly object _syncRoot = new object();
        private List<DynamicView> _dynamicViews = new List<DynamicView>();
        private IReadOnlyList<DynamicView> _dynamicViewsSnapshot = Array.Empty<DynamicView>();
        #endregion

        #region Properties
        public IReadOnlyList<DynamicView> DynamicViews => _dynamicViewsSnapshot;

        public event EventHandler<DynamicViewsRemovedEventArgs> DynamicViewsRemoved;
        #endregion

        #region Constructor
        public DynamicViewManager()
        {
            Init();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            try
            {
                IReadOnlyList<DynamicView> removedViews = Array.Empty<DynamicView>();

                lock (_syncRoot)
                {
                    removedViews = _dynamicViews
                        .Select(item => item.Clone())
                        .ToArray();
                    _dynamicViews.Clear();
                    RefreshSnapshot();
                }

                RaiseDynamicViewsRemoved(removedViews, isClearOperation: true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); 
                return false;
            }
        }

        public bool AddDynamic(DynamicView dynamicView)
        {
            if (!TryNormalizeDynamicView(dynamicView, out DynamicView normalizedView))
            {
                return false;
            }

            lock (_syncRoot)
            {
                int index = _dynamicViews.FindIndex(item => HasSameIdentity(item, normalizedView));
                if (index >= 0)
                {
                    _dynamicViews[index] = normalizedView;
                }
                else
                {
                    _dynamicViews.Add(normalizedView);
                }

                RefreshSnapshot();
                return true;
            }
        }

        public bool RemoveDynamic(DynamicView dynamicView)
        {
            if (!TryNormalizeDynamicView(dynamicView, out DynamicView normalizedView))
            {
                return false;
            }

            IReadOnlyList<DynamicView> removedViews = Array.Empty<DynamicView>();
            lock (_syncRoot)
            {
                removedViews = _dynamicViews
                    .Where(item => HasSameIdentity(item, normalizedView))
                    .Select(item => item.Clone())
                    .ToArray();
                if (removedViews.Count <= 0)
                {
                    return false;
                }

                _dynamicViews.RemoveAll(item => HasSameIdentity(item, normalizedView));
                RefreshSnapshot();
            }

            RaiseDynamicViewsRemoved(removedViews);
            return true;
        }

        public int RemoveDynamic(
            DynamicViewType type,
            string viewName = null,
            int nodeSerial = -1,
            string subjection = null
        )
        {
            string normalizedViewName = NormalizeText(viewName);
            string normalizedSubjection = NormalizeText(subjection);
            IReadOnlyList<DynamicView> removedViews = Array.Empty<DynamicView>();

            lock (_syncRoot)
            {
                removedViews = _dynamicViews
                    .Where(item =>
                        item != null
                        && item.Type == type
                        && (string.IsNullOrWhiteSpace(normalizedViewName)
                            || string.Equals(item.ViewName, normalizedViewName, StringComparison.OrdinalIgnoreCase))
                        && (type != DynamicViewType.NodeMap || nodeSerial < 0 || item.NodeSerial == nodeSerial)
                        && (type != DynamicViewType.Custom
                            || string.IsNullOrWhiteSpace(normalizedSubjection)
                            || string.Equals(item.Subjection, normalizedSubjection, StringComparison.OrdinalIgnoreCase)))
                    .Select(item => item.Clone())
                    .ToArray();

                if (removedViews.Count <= 0)
                {
                    return 0;
                }

                _dynamicViews.RemoveAll(item =>
                    item != null
                    && item.Type == type
                    && (string.IsNullOrWhiteSpace(normalizedViewName)
                        || string.Equals(item.ViewName, normalizedViewName, StringComparison.OrdinalIgnoreCase))
                    && (type != DynamicViewType.NodeMap || nodeSerial < 0 || item.NodeSerial == nodeSerial)
                    && (type != DynamicViewType.Custom
                        || string.IsNullOrWhiteSpace(normalizedSubjection)
                        || string.Equals(item.Subjection, normalizedSubjection, StringComparison.OrdinalIgnoreCase)));
                RefreshSnapshot();
            }

            RaiseDynamicViewsRemoved(removedViews);
            return removedViews.Count;
        }

        public void Clear()
        {
            IReadOnlyList<DynamicView> removedViews = Array.Empty<DynamicView>();
            lock (_syncRoot)
            {
                if (_dynamicViews.Count == 0)
                {
                    return;
                }

                removedViews = _dynamicViews
                    .Select(item => item.Clone())
                    .ToArray();
                _dynamicViews.Clear();
                RefreshSnapshot();
            }

            RaiseDynamicViewsRemoved(removedViews, isClearOperation: true);
        }

        private bool TryNormalizeDynamicView(DynamicView dynamicView, out DynamicView normalizedView)
        {
            normalizedView = null;
            if (dynamicView == null)
            {
                return false;
            }

            string normalizedViewName = NormalizeText(dynamicView.ViewName);
            if (string.IsNullOrWhiteSpace(normalizedViewName))
            {
                return false;
            }

            int normalizedNodeSerial = dynamicView.Type == DynamicViewType.NodeMap ? dynamicView.NodeSerial : -1;
            if (dynamicView.Type == DynamicViewType.NodeMap && normalizedNodeSerial < 0)
            {
                return false;
            }

            normalizedView = dynamicView.Clone();
            normalizedView.ViewName = normalizedViewName;
            normalizedView.DisplayName = NormalizeText(dynamicView.DisplayName);
            if (string.IsNullOrWhiteSpace(normalizedView.DisplayName))
            {
                normalizedView.DisplayName = normalizedViewName;
            }

            normalizedView.Subjection = dynamicView.Type == DynamicViewType.Custom
                ? NormalizeText(dynamicView.Subjection)
                : string.Empty;
            normalizedView.NodeSerial = normalizedNodeSerial;
            return true;
        }

        private static bool HasSameIdentity(DynamicView left, DynamicView right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.Type == right.Type
                && string.Equals(left.ViewName, right.ViewName, StringComparison.OrdinalIgnoreCase)
                && left.NodeSerial == right.NodeSerial
                && string.Equals(left.Subjection ?? string.Empty, right.Subjection ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshSnapshot()
        {
            _dynamicViewsSnapshot = _dynamicViews
                .Select(item => item.Clone())
                .ToArray();
        }

        private void RaiseDynamicViewsRemoved(
            IReadOnlyList<DynamicView> removedViews,
            bool isClearOperation = false
        )
        {
            if (removedViews == null || removedViews.Count == 0)
            {
                return;
            }

            DynamicViewsRemoved?.Invoke(
                this,
                new DynamicViewsRemovedEventArgs(removedViews, isClearOperation));
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        #endregion
    }
}
