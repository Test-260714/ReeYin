#nullable disable
using ALGO.ShapeMatching;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.ShapeMatching.Views
{
    /// <summary>
    /// 形状匹配的 HALCON WPF 预览控件，负责覆盖层镜像显示和 ROI 鼠标拖拽交互。
    /// </summary>
    public partial class ShapeMatchingPreviewControl : UserControl
    {
        #region 依赖属性与状态
        /// <summary>
        /// 当前预览控件绑定的形状匹配模型。
        /// </summary>
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(ShapeMatchingModel),
                typeof(ShapeMatchingPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        /// <summary>VMHWindowControl 内部的 HALCON WPF 窗口，用于接收图像坐标鼠标事件。</summary>
        private HSmartWindowControlWPF _smartWindow;

        private static readonly FieldInfo HalconSuppressRefreshField = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetField("suppressRefresh", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo HalconRefreshDisplayMethod = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetMethod("RefreshDisplay", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>当前已订阅事件的模型实例，避免 Loaded 和绑定切换重复订阅。</summary>
        private ShapeMatchingModel _subscribedModel;

        /// <summary>当前正在拖拽的 ROI 手柄类型。</summary>
        private ShapeMatchingRoiPreviewHandle _dragHandle = ShapeMatchingRoiPreviewHandle.None;

        /// <summary>拖拽开始时的 ROI 图像坐标状态。</summary>
        private ShapeMatchingRoiPreview _dragStartRoi;

        /// <summary>拖拽开始时的鼠标图像坐标。</summary>
        private ShapeMatchingRoiPreviewPoint _dragStartPoint;

        /// <summary>拖拽写回模型时抑制属性变化触发的重复重绘。</summary>
        private bool _suppressModelRedraw;

        /// <summary>合并同一 UI 调度周期内的 ROI 编辑层刷新。</summary>
        private bool _refreshEditableRoiPending;

        /// <summary>合并同一 UI 调度周期内的预览覆盖层镜像同步。</summary>
        private bool _syncPreviewDrawObjectsPending;

        /// <summary>
        /// 当前预览控件绑定的形状匹配模型。
        /// </summary>
        public ShapeMatchingModel Model
        {
            get => GetValue(ModelProperty) as ShapeMatchingModel;
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化形状匹配预览控件。
        /// </summary>
        public ShapeMatchingPreviewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 切换绑定模型时同步订阅预览覆盖层集合和模型属性变化。
        /// </summary>
        /// <param name="d">触发变更的预览控件。</param>
        /// <param name="e">模型依赖属性变更数据。</param>
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ShapeMatchingPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as ShapeMatchingModel);
            control.SubscribeModel(e.NewValue as ShapeMatchingModel);
            control.QueueRefreshEditableRoi();
            control.QueueSyncPreviewDrawObjects();
        }

        /// <summary>
        /// 控件加载后挂接 HALCON 鼠标事件并同步当前覆盖层。
        /// </summary>
        /// <param name="sender">事件源控件。</param>
        /// <param name="e">加载事件参数。</param>
        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeModel(Model);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AttachSmartWindow();
                QueueRefreshEditableRoi();
                QueueSyncPreviewDrawObjects();
            }));
        }

        /// <summary>
        /// 控件卸载时解除模型订阅、鼠标事件并清理镜像覆盖层。
        /// </summary>
        /// <param name="sender">事件源控件。</param>
        /// <param name="e">卸载事件参数。</param>
        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeModel(Model);
            DetachSmartWindow();
            ClearMirroredPreviewObjects();
        }

        /// <summary>
        /// 挂接 VMHWindowControl 内部 HSmartWindowControlWPF 的鼠标事件。
        /// </summary>
        private void AttachSmartWindow()
        {
            DetachSmartWindow();
            _smartWindow = HalconPreview?.getHWindowControl();
            if (_smartWindow == null)
            {
                return;
            }

            _smartWindow.HMouseDown += SmartWindow_HMouseDown;
            _smartWindow.HMouseMove += SmartWindow_HMouseMove;
            _smartWindow.HMouseUp += SmartWindow_HMouseUp;
            _smartWindow.HMouseWheel += SmartWindow_HMouseWheel;
        }

        /// <summary>
        /// 解除 HALCON 鼠标事件，避免对话框关闭后事件持有控件实例。
        /// </summary>
        private void DetachSmartWindow()
        {
            if (_smartWindow == null)
            {
                return;
            }

            _smartWindow.HMouseDown -= SmartWindow_HMouseDown;
            _smartWindow.HMouseMove -= SmartWindow_HMouseMove;
            _smartWindow.HMouseUp -= SmartWindow_HMouseUp;
            _smartWindow.HMouseWheel -= SmartWindow_HMouseWheel;
            _smartWindow = null;
        }

        /// <summary>
        /// 订阅模型的属性和覆盖层集合变化。
        /// </summary>
        /// <param name="model">需要订阅的形状匹配模型，可为空。</param>
        private void SubscribeModel(ShapeMatchingModel model)
        {
            if (model == null || ReferenceEquals(_subscribedModel, model))
            {
                return;
            }

            if (_subscribedModel != null)
            {
                UnsubscribeModel(_subscribedModel);
            }

            model.PropertyChanged += Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            _subscribedModel = model;
        }

        /// <summary>
        /// 解除模型订阅，并释放预览控件镜像持有的 HALCON 对象。
        /// </summary>
        /// <param name="model">需要解除订阅的形状匹配模型，可为空。</param>
        private void UnsubscribeModel(ShapeMatchingModel model)
        {
            if (model == null || !ReferenceEquals(_subscribedModel, model))
            {
                return;
            }

            model.PropertyChanged -= Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            _subscribedModel = null;
            ClearMirroredPreviewObjects();
        }
        #endregion

        #region ROI 鼠标交互
        /// <summary>
        /// 鼠标按下时根据图像坐标命中 ROI 手柄并开始拖拽。
        /// </summary>
        /// <param name="sender">HALCON 预览窗口。</param>
        /// <param name="e">HALCON 鼠标事件参数，Row/Column 为图像坐标。</param>
        private void SmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model?.PreviewImageObject == null || e.Button != MouseButton.Left)
            {
                return;
            }

            var point = new ShapeMatchingRoiPreviewPoint(e.Column, e.Row);
            _dragStartRoi = Model.GetPreviewRoi();
            _dragHandle = ShapeMatchingRoiPreviewGeometry.HitTest(
                _dragStartRoi,
                point.X,
                point.Y,
                GetHitToleranceImageUnits());

            if (_dragHandle == ShapeMatchingRoiPreviewHandle.None)
            {
                return;
            }

            _dragStartPoint = point;
            HalconPreview.DrawModel = true;
            _smartWindow?.CaptureMouse();
        }

        /// <summary>
        /// 鼠标移动时更新 ROI 拖拽结果或鼠标指针样式。
        /// </summary>
        /// <param name="sender">HALCON 预览窗口。</param>
        /// <param name="e">HALCON 鼠标事件参数，Row/Column 为图像坐标。</param>
        private void SmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model == null)
            {
                return;
            }

            if (_dragHandle == ShapeMatchingRoiPreviewHandle.None)
            {
                UpdateCursor(e.Column, e.Row);
                return;
            }

            var point = new ShapeMatchingRoiPreviewPoint(e.Column, e.Row);
            ShapeMatchingRoiPreview roi = _dragHandle switch
            {
                ShapeMatchingRoiPreviewHandle.Body or ShapeMatchingRoiPreviewHandle.Center => ShapeMatchingRoiPreviewGeometry.Move(
                    _dragStartRoi,
                    point.X - _dragStartPoint.X,
                    point.Y - _dragStartPoint.Y),
                ShapeMatchingRoiPreviewHandle.Rotate => ShapeMatchingRoiPreviewGeometry.RotateToPoint(
                    _dragStartRoi,
                    point.X,
                    point.Y),
                _ => ShapeMatchingRoiPreviewGeometry.ResizeFromHandle(_dragStartRoi, _dragHandle, point.X, point.Y)
            };

            ApplyPreviewRoi(roi);
        }

        /// <summary>
        /// 鼠标释放时结束 ROI 拖拽并恢复预览控件普通浏览状态。
        /// </summary>
        /// <param name="sender">HALCON 预览窗口。</param>
        /// <param name="e">HALCON 鼠标事件参数。</param>
        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_dragHandle == ShapeMatchingRoiPreviewHandle.None)
            {
                ScheduleEditableRoiRefreshForViewChange();
                return;
            }

            ApplyPreviewRoi(Model.GetPreviewRoi());
            _dragHandle = ShapeMatchingRoiPreviewHandle.None;
            HalconPreview.DrawModel = false;
            if (_smartWindow?.IsMouseCaptured == true)
            {
                _smartWindow.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// 缩放后按最新窗口视野重算蓝色编辑层的屏幕像素尺寸。
        /// </summary>
        /// <param name="sender">HALCON 预览窗口。</param>
        /// <param name="e">HALCON 鼠标滚轮事件参数。</param>
        private void SmartWindow_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            ScheduleEditableRoiRefreshForViewChange();
        }

        /// <summary>
        /// 将拖拽后的 ROI 状态写回模型，并按当前缩放刷新手柄尺寸。
        /// </summary>
        /// <param name="roi">拖拽后的 ROI 图像坐标状态。</param>
        private void ApplyPreviewRoi(ShapeMatchingRoiPreview roi)
        {
            try
            {
                _suppressModelRedraw = true;
                Model.ApplyPreviewRoi(roi, GetImageUnitsForScreenPixels(1.0));
            }
            finally
            {
                _suppressModelRedraw = false;
            }
        }

        /// <summary>
        /// 根据当前鼠标所在 ROI 手柄切换光标提示。
        /// </summary>
        /// <param name="column">鼠标列坐标。</param>
        /// <param name="row">鼠标行坐标。</param>
        private void UpdateCursor(double column, double row)
        {
            if (Model?.PreviewImageObject == null || _smartWindow == null)
            {
                return;
            }

            var handle = ShapeMatchingRoiPreviewGeometry.HitTest(
                Model.GetPreviewRoi(),
                column,
                row,
                GetHitToleranceImageUnits());

            _smartWindow.Cursor = handle switch
            {
                ShapeMatchingRoiPreviewHandle.Body or ShapeMatchingRoiPreviewHandle.Center => Cursors.SizeAll,
                ShapeMatchingRoiPreviewHandle.Left or ShapeMatchingRoiPreviewHandle.Right or ShapeMatchingRoiPreviewHandle.Radius => Cursors.SizeWE,
                ShapeMatchingRoiPreviewHandle.Top or ShapeMatchingRoiPreviewHandle.Bottom => Cursors.SizeNS,
                ShapeMatchingRoiPreviewHandle.TopLeft or ShapeMatchingRoiPreviewHandle.BottomRight => Cursors.SizeNWSE,
                ShapeMatchingRoiPreviewHandle.TopRight or ShapeMatchingRoiPreviewHandle.BottomLeft => Cursors.SizeNESW,
                ShapeMatchingRoiPreviewHandle.Rotate => Cursors.Hand,
                _ => Cursors.Arrow
            };
        }
        #endregion

        #region 覆盖层同步
        /// <summary>
        /// 模型属性变化时刷新可编辑 ROI 覆盖层。
        /// </summary>
        /// <param name="sender">事件源模型。</param>
        /// <param name="e">属性变化参数。</param>
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressModelRedraw)
            {
                return;
            }

            if (e.PropertyName == nameof(ShapeMatchingModel.RegionCreatMode)
                || e.PropertyName == nameof(ShapeMatchingModel.PreviewImageObject))
            {
                QueueRefreshEditableRoi();
            }
        }

        /// <summary>
        /// 将多次属性变化合并为一次 ROI 编辑层刷新，避免大图拖拽时重复触发覆盖层重绘。
        /// </summary>
        private void QueueRefreshEditableRoi()
        {
            if (_refreshEditableRoiPending)
            {
                return;
            }

            _refreshEditableRoiPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshEditableRoiPending = false;
                RefreshEditableRoi();
            }));
        }

        /// <summary>
        /// 触发模型刷新可编辑 ROI 蓝色覆盖层。
        /// </summary>
        private void RefreshEditableRoi()
        {
            Model?.RefreshEditableRoiPreview(GetImageUnitsForScreenPixels(1.0));
        }

        /// <summary>
        /// 视野变化由 VMHWindowControl 先处理，这里延迟刷新以读取更新后的 GetPart。
        /// </summary>
        private void ScheduleEditableRoiRefreshForViewChange()
        {
            if (Model?.PreviewImageObject == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(RefreshEditableRoi), DispatcherPriority.Background);
        }

        /// <summary>
        /// 覆盖层集合变化时异步刷新预览控件中的镜像对象。
        /// </summary>
        /// <param name="sender">事件源集合。</param>
        /// <param name="e">集合变化事件参数。</param>
        private void PreviewDrawObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueSyncPreviewDrawObjects();
        }

        /// <summary>
        /// 将连续集合变化合并为一次镜像同步，降低拖拽时 VMHWindowControl 的重绘频率。
        /// </summary>
        private void QueueSyncPreviewDrawObjects()
        {
            if (_syncPreviewDrawObjectsPending)
            {
                return;
            }

            _syncPreviewDrawObjectsPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _syncPreviewDrawObjectsPending = false;
                SyncPreviewDrawObjects();
            }));
        }

        /// <summary>
        /// 将模型覆盖层中的 HALCON 对象克隆到预览控件 DrawObjectList。
        /// </summary>
        private void SyncPreviewDrawObjects()
        {
            if (Model == null || HalconPreview == null)
            {
                return;
            }

            var mirroredDrawObjects = new List<HalconDrawingObject>();
            foreach (var drawObject in Model.PreviewDrawObjects.ToList())
            {
                if (drawObject?.Hobject == null || !drawObject.Hobject.IsInitialized())
                {
                    continue;
                }

                try
                {
                    mirroredDrawObjects.Add(new HalconDrawingObject
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

            ReplaceMirroredPreviewObjects(mirroredDrawObjects);
            RefreshHalconPreviewCurrentView();
        }

        /// <summary>
        /// 同步覆盖层后触发一次保留当前缩放视野的重绘。
        /// </summary>
        private void RefreshHalconPreviewCurrentView()
        {
            if (HalconPreview == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // 共享控件暂未公开“保留当前视野刷新”的入口，这里只在本模块复用现有绘制路径。
                    HalconRefreshDisplayMethod?.Invoke(HalconPreview, new object[] { false });
                }
                catch
                {
                }
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// 释放预览控件镜像覆盖层持有的 HALCON 对象。
        /// </summary>
        private void ClearMirroredPreviewObjects()
        {
            if (HalconPreview == null)
            {
                return;
            }

            ReplaceMirroredPreviewObjects(Enumerable.Empty<HalconDrawingObject>());
            RefreshHalconPreviewCurrentView();
        }

        /// <summary>
        /// 批量替换共享预览控件中的镜像覆盖层，避免 DrawObjectList 每次 Clear/Add 都触发重绘。
        /// </summary>
        /// <param name="mirroredDrawObjects">已经克隆好的镜像覆盖层对象。</param>
        private void ReplaceMirroredPreviewObjects(IEnumerable<HalconDrawingObject> mirroredDrawObjects)
        {
            var pendingObjects = mirroredDrawObjects?.ToList() ?? new List<HalconDrawingObject>();
            if (HalconPreview?.DrawObjectList == null)
            {
                DisposeMirroredObjects(pendingObjects);
                return;
            }

            bool isRefreshSuppressed = TryBeginBatchedHalconRefresh(out bool previousSuppressRefresh);
            try
            {
                ClearMirroredPreviewObjectsCore();
                foreach (var drawObject in pendingObjects.ToList())
                {
                    HalconPreview.DrawObjectList.Add(drawObject);
                    pendingObjects.Remove(drawObject);
                }
            }
            finally
            {
                if (isRefreshSuppressed)
                {
                    RestoreBatchedHalconRefresh(previousSuppressRefresh);
                }

                DisposeMirroredObjects(pendingObjects);
            }
        }

        /// <summary>
        /// 临时打开 VMHWindowControl 内部刷新抑制，批量更新完成后再恢复。
        /// </summary>
        private bool TryBeginBatchedHalconRefresh(out bool previousSuppressRefresh)
        {
            previousSuppressRefresh = false;
            if (HalconSuppressRefreshField == null || HalconRefreshDisplayMethod == null)
            {
                return false;
            }

            try
            {
                previousSuppressRefresh = HalconSuppressRefreshField.GetValue(HalconPreview) as bool? ?? false;
                HalconSuppressRefreshField.SetValue(HalconPreview, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 恢复 VMHWindowControl 批量更新前的刷新抑制状态。
        /// </summary>
        private void RestoreBatchedHalconRefresh(bool previousSuppressRefresh)
        {
            try
            {
                HalconSuppressRefreshField?.SetValue(HalconPreview, previousSuppressRefresh);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 清空当前镜像覆盖层并释放本控件持有的 HALCON 句柄。
        /// </summary>
        private void ClearMirroredPreviewObjectsCore()
        {
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

        /// <summary>
        /// 释放尚未移交给 DrawObjectList 的镜像对象。
        /// </summary>
        /// <param name="drawObjects">需要释放的镜像对象集合。</param>
        private static void DisposeMirroredObjects(IEnumerable<HalconDrawingObject> drawObjects)
        {
            foreach (var item in drawObjects.ToList())
            {
                try
                {
                    item.Hobject?.Dispose();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 获取 ROI 命中测试在当前缩放下的图像像素容差。
        /// </summary>
        /// <returns>图像坐标系下的命中容差。</returns>
        private double GetHitToleranceImageUnits()
        {
            return GetImageUnitsForScreenPixels(ShapeMatchingRoiPreviewStyle.HitToleranceScreenPixels);
        }

        /// <summary>
        /// 将屏幕像素换算为当前 HALCON 显示区域中的图像像素。
        /// </summary>
        /// <param name="screenPixels">屏幕像素数量。</param>
        /// <returns>图像坐标单位数量。</returns>
        private double GetImageUnitsForScreenPixels(double screenPixels)
        {
            try
            {
                var hWindow = HalconPreview.HWindow;
                if (hWindow == null)
                {
                    return screenPixels;
                }

                hWindow.GetPart(out int row1, out int col1, out int row2, out int col2);
                double width = Math.Max(1.0, _smartWindow?.ActualWidth ?? HalconPreview.ActualWidth);
                double height = Math.Max(1.0, _smartWindow?.ActualHeight ?? HalconPreview.ActualHeight);
                double unitX = (Math.Abs(col2 - col1) + 1.0) / width;
                double unitY = (Math.Abs(row2 - row1) + 1.0) / height;
                return Math.Max(0.1, screenPixels * Math.Max(unitX, unitY));
            }
            catch
            {
                return screenPixels;
            }
        }
        #endregion
    }
}
