using ALGO.DefectPostProcess.Models;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.DefectPostProcess.Views
{
    /// <summary>
    /// 使用项目统一 HALCON 预览控件显示缺陷后处理图像和结果区域。
    /// </summary>
    public partial class DefectPostProcessPreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(DefectPostProcessModel),
                typeof(DefectPostProcessPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        private DefectPostProcessModel _subscribedModel;

        /// <summary>
        /// 当前预览绑定的缺陷后处理模型。
        /// </summary>
        public DefectPostProcessModel Model
        {
            get => (DefectPostProcessModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        public DefectPostProcessPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DefectPostProcessPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as DefectPostProcessModel);
            control.SubscribeModel(e.NewValue as DefectPostProcessModel);
            control.SyncPreviewDrawObjects();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeModel(Model);
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeModel(Model);
            ClearMirroredPreviewObjects();
        }

        private void SubscribeModel(DefectPostProcessModel model)
        {
            if (model == null || ReferenceEquals(_subscribedModel, model))
            {
                return;
            }

            UnsubscribeModel(_subscribedModel);
            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            _subscribedModel = model;
            SyncPreviewDrawObjects();
        }

        private void UnsubscribeModel(DefectPostProcessModel model)
        {
            if (model == null)
            {
                return;
            }

            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            if (ReferenceEquals(_subscribedModel, model))
            {
                _subscribedModel = null;
            }
        }
        #endregion

        #region 预览对象同步
        private void PreviewDrawObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        private void SyncPreviewDrawObjects()
        {
            if (Model == null || HalconPreview?.DrawObjectList == null)
            {
                return;
            }

            ClearMirroredPreviewObjects();
            foreach (HalconDrawingObject drawObject in Model.PreviewDrawObjects.ToList())
            {
                if (drawObject?.Hobject == null || !drawObject.Hobject.IsInitialized())
                {
                    continue;
                }

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
            {
                return;
            }

            foreach (HalconDrawingObject item in HalconPreview.DrawObjectList.ToList())
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
