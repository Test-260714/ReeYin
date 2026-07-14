using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReeYin_V.Core.Helper.ImageOP;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.RegionProcessing.Views
{
    /// <summary>
    /// 区域处理 HALCON 图像预览控件，只负责把 Model 预览对象镜像到共享预览控件。
    /// </summary>
    public partial class RegionProcessingPreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(RegionProcessingModel),
                typeof(RegionProcessingPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        private RegionProcessingModel? _subscribedModel;

        public RegionProcessingModel? Model
        {
            get => (RegionProcessingModel?)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        public RegionProcessingPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (RegionProcessingPreviewControl)d;
            control.SubscribeModel(e.NewValue as RegionProcessingModel);
            control.SyncPreviewDrawObjects();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeModel(Model);
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SubscribeModel(null);
            ClearMirroredPreviewObjects();
        }
        #endregion

        #region 预览同步
        private void SubscribeModel(RegionProcessingModel? model)
        {
            if (ReferenceEquals(_subscribedModel, model))
                return;

            if (_subscribedModel != null)
            {
                _subscribedModel.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            }

            _subscribedModel = model;

            if (_subscribedModel != null)
            {
                _subscribedModel.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            }
        }

        private void PreviewDrawObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void SyncPreviewDrawObjects()
        {
            if (Model == null || HalconPreview?.DrawObjectList == null)
                return;

            ClearMirroredPreviewObjects();
            foreach (HalconDrawingObject drawObject in Model.PreviewDrawObjects.ToList())
            {
                if (drawObject?.Hobject == null || !drawObject.Hobject.IsInitialized())
                    continue;

                HalconDotNet.HObject? mirroredObject = null;
                try
                {
                    mirroredObject = HalconImageOwnership.CopyBorrowedObjectOrNull(drawObject.Hobject);
                    if (mirroredObject == null)
                        continue;

                    HalconPreview.DrawObjectList.Add(new HalconDrawingObject
                    {
                        ShapeType = drawObject.ShapeType,
                        Hobject = mirroredObject,
                        HTuples = drawObject.HTuples,
                        Color = drawObject.Color,
                        IsFillDisplay = drawObject.IsFillDisplay
                    });
                    mirroredObject = null;
                }
                catch
                {
                    HalconImageOwnership.DisposeOwned(mirroredObject);
                }
            }
        }

        private void ClearMirroredPreviewObjects()
        {
            if (HalconPreview?.DrawObjectList == null)
                return;

            foreach (HalconDrawingObject item in HalconPreview.DrawObjectList.ToList())
            {
                try
                {
                    HalconImageOwnership.DisposeOwned(item.Hobject);
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
