using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.DistanceLL.Views
{
    public partial class DistanceLLPreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(DistanceLLModel),
                typeof(DistanceLLPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        public DistanceLLModel Model
        {
            get => (DistanceLLModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        public DistanceLLPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DistanceLLPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as DistanceLLModel);
            control.SubscribeModel(e.NewValue as DistanceLLModel);
            control.SyncPreviewDrawObjects();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ClearMirroredPreviewObjects();
        }

        private void SubscribeModel(DistanceLLModel model)
        {
            if (model == null)
                return;

            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
        }

        private void UnsubscribeModel(DistanceLLModel model)
        {
            if (model == null)
                return;

            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            ClearMirroredPreviewObjects();
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
