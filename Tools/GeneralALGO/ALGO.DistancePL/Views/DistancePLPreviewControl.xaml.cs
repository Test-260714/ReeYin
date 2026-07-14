using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.DistancePL.Views
{
    public partial class DistancePLPreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(DistancePLModel),
                typeof(DistancePLPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        public DistancePLModel Model
        {
            get => (DistancePLModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        // 跟踪当前是否已订阅 Model.PreviewDrawObjects 事件，确保 Loaded/Unloaded/OnModelChanged 路径下订阅状态一致
        private bool _isSubscribed;
        #endregion

        #region 初始化
        public DistancePLPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DistancePLPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as DistancePLModel);
            control.SubscribeModel(e.NewValue as DistancePLModel);
            control.SyncPreviewDrawObjects();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 重新挂载到可视化树时恢复订阅（OnModelChanged 已订阅时由 _isSubscribed 守卫跳过）
            SubscribeModel(Model);
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 解绑事件避免 Model 长期持有控件引用造成内存泄漏
            UnsubscribeModel(Model);
        }

        private void SubscribeModel(DistancePLModel model)
        {
            if (model == null || _isSubscribed)
                return;

            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeModel(DistancePLModel model)
        {
            if (model == null || !_isSubscribed)
                return;

            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            ClearMirroredPreviewObjects();
            _isSubscribed = false;
        }
        #endregion

        #region 覆盖层同步
        private void PreviewDrawObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void SyncPreviewDrawObjects()
        {
            if (Model == null || HalconPreview?.DrawObjectList == null)
                return;

            ClearMirroredPreviewObjects();
            foreach (var drawObject in Model.PreviewDrawObjects.ToList())
            {
                if (drawObject?.Hobject == null || !drawObject.Hobject.IsInitialized())
                    continue;

                try
                {
                    HalconPreview.DrawObjectList.Add(new HalconDrawingObject
                    {
                        ShapeType = drawObject.ShapeType,
                        Hobject = drawObject.Hobject.Clone(),
                        HTuples = drawObject.HTuples,
                        Color = drawObject.Color,
                        IsFillDisplay = drawObject.IsFillDisplay
                    });
                }
                catch
                {
                }
            }
        }

        private void ClearMirroredPreviewObjects()
        {
            if (HalconPreview?.DrawObjectList == null)
                return;

            foreach (var item in HalconPreview.DrawObjectList.ToList())
            {
                try
                {
                    item.Hobject?.Dispose();
                }
                catch
                {
                }
            }

            HalconPreview.DrawObjectList.Clear();
        }
        #endregion
    }
}
