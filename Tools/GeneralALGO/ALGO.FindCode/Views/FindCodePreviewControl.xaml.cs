using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.FindCode.Views
{
    /// <summary>
    /// 扫码识别 HALCON 预览控件，只负责镜像 Model 中的预览图像和识别区域覆盖层。
    /// </summary>
    public partial class FindCodePreviewControl : UserControl
    {
        #region 参数与状态
        /// <summary>
        /// 当前预览控件绑定的扫码识别模型。
        /// </summary>
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(FindCodeModel),
                typeof(FindCodePreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        /// <summary>
        /// 预览控件的数据源模型，提供图像对象和覆盖层集合。
        /// </summary>
        public FindCodeModel Model
        {
            get => (FindCodeModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        /// <summary>
        /// 模块预览控件是否已订阅 Model.PreviewDrawObjects，避免重复订阅或卸载后泄漏。
        /// </summary>
        private bool _isSubscribed;
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化扫码识别 HALCON 预览控件。
        /// </summary>
        public FindCodePreviewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 模型切换时重建覆盖层订阅关系，并立即同步当前预览对象。
        /// </summary>
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FindCodePreviewControl)d;
            control.UnsubscribeModel(e.OldValue as FindCodeModel);
            control.SubscribeModel(e.NewValue as FindCodeModel);
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
        }

        /// <summary>
        /// 订阅模型覆盖层集合变化，模型为空或已订阅时不重复处理。
        /// </summary>
        /// <param name="model">当前绑定的扫码识别模型。</param>
        private void SubscribeModel(FindCodeModel? model)
        {
            if (model == null || _isSubscribed)
                return;

            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            _isSubscribed = true;
        }

        /// <summary>
        /// 取消覆盖层集合订阅，并释放控件内镜像出来的 HALCON 对象。
        /// </summary>
        /// <param name="model">即将解绑的扫码识别模型。</param>
        private void UnsubscribeModel(FindCodeModel? model)
        {
            if (model == null || !_isSubscribed)
                return;

            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            ClearMirroredPreviewObjects();
            _isSubscribed = false;
        }
        #endregion

        #region 覆盖层同步
        /// <summary>
        /// Model 覆盖层集合变化后延迟到 UI 线程刷新控件镜像对象。
        /// </summary>
        private void PreviewDrawObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncPreviewDrawObjects));
        }

        /// <summary>
        /// 将 Model 中的识别区域覆盖层复制到 VMHWindowControl，控件负责释放镜像副本。
        /// </summary>
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

        /// <summary>
        /// 清理控件镜像的识别区域覆盖层，避免持有旧 HALCON 句柄。
        /// </summary>
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
