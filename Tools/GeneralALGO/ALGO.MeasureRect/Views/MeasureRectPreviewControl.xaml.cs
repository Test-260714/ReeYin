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
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.MeasureRect.Views
{
    public partial class MeasureRectPreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(MeasureRectModel),
                typeof(MeasureRectPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        private HSmartWindowControlWPF _smartWindow;
        private static readonly FieldInfo? HalconSuppressRefreshField = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetField("suppressRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo? HalconRefreshDisplayMethod = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetMethod("RefreshDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
        private MeasureRectPreviewHandle _dragHandle = MeasureRectPreviewHandle.None;
        private MeasureRectPreviewRect _dragStartRect;
        private MeasureRectPreviewPoint _dragStartPoint;
        private bool _suppressModelRedraw;
        private bool _refreshEditableRectPending;
        private bool _syncPreviewDrawObjectsPending;

        public MeasureRectModel Model
        {
            get => (MeasureRectModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        public MeasureRectPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MeasureRectPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as MeasureRectModel);
            control.SubscribeModel(e.NewValue as MeasureRectModel);
            control.QueueRefreshEditableRect();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(AttachSmartWindow));
            QueueRefreshEditableRect();
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachSmartWindow();
        }

        private void AttachSmartWindow()
        {
            DetachSmartWindow();
            _smartWindow = HalconPreview.getHWindowControl();
            if (_smartWindow == null)
            {
                return;
            }

            _smartWindow.HMouseDown += SmartWindow_HMouseDown;
            _smartWindow.HMouseMove += SmartWindow_HMouseMove;
            _smartWindow.HMouseUp += SmartWindow_HMouseUp;
            SyncPreviewDrawObjects();
        }

        private void DetachSmartWindow()
        {
            if (_smartWindow == null)
            {
                return;
            }

            _smartWindow.HMouseDown -= SmartWindow_HMouseDown;
            _smartWindow.HMouseMove -= SmartWindow_HMouseMove;
            _smartWindow.HMouseUp -= SmartWindow_HMouseUp;
            _smartWindow = null;
        }

        private void SubscribeModel(MeasureRectModel model)
        {
            if (model == null)
            {
                return;
            }

            model.PropertyChanged += Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            SyncPreviewDrawObjects();
        }

        private void UnsubscribeModel(MeasureRectModel model)
        {
            if (model == null)
            {
                return;
            }

            model.PropertyChanged -= Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            ClearMirroredPreviewObjects();
        }
        #endregion

        #region 矩形交互
        private void SmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model?.PreviewImageObject == null || e.Button != MouseButton.Left)
            {
                return;
            }

            var point = new MeasureRectPreviewPoint(e.Column, e.Row);
            _dragStartRect = Model.GetPreviewRect();
            _dragHandle = MeasureRectPreviewGeometry.HitTest(
                _dragStartRect,
                point.X,
                point.Y,
                GetHitToleranceImageUnits());

            if (_dragHandle == MeasureRectPreviewHandle.None)
            {
                return;
            }

            _dragStartPoint = point;
            HalconPreview.DrawModel = true;
            _smartWindow?.CaptureMouse();
        }

        private void SmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model == null)
            {
                return;
            }

            if (_dragHandle == MeasureRectPreviewHandle.None)
            {
                UpdateCursor(e.Column, e.Row);
                return;
            }

            var point = new MeasureRectPreviewPoint(e.Column, e.Row);
            MeasureRectPreviewRect rect = _dragHandle switch
            {
                MeasureRectPreviewHandle.Body or MeasureRectPreviewHandle.Center => MeasureRectPreviewGeometry.Move(
                    _dragStartRect,
                    point.X - _dragStartPoint.X,
                    point.Y - _dragStartPoint.Y),
                MeasureRectPreviewHandle.Rotate => MeasureRectPreviewGeometry.RotateToPoint(
                    _dragStartRect,
                    point.X,
                    point.Y),
                _ => MeasureRectPreviewGeometry.ResizeFromHandle(_dragStartRect, _dragHandle, point.X, point.Y)
            };

            ApplyPreviewRect(rect, false);
        }

        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_dragHandle == MeasureRectPreviewHandle.None)
            {
                return;
            }

            ApplyPreviewRect(Model.GetPreviewRect(), true);
            _dragHandle = MeasureRectPreviewHandle.None;
            HalconPreview.DrawModel = false;
            if (_smartWindow?.IsMouseCaptured == true)
            {
                _smartWindow.ReleaseMouseCapture();
            }
        }

        private void ApplyPreviewRect(MeasureRectPreviewRect rect, bool runMeasurement)
        {
            try
            {
                _suppressModelRedraw = true;
                Model.ApplyPreviewRect(rect, runMeasurement, GetImageUnitsForScreenPixels(1.0));
            }
            finally
            {
                _suppressModelRedraw = false;
            }
        }

        private void UpdateCursor(double column, double row)
        {
            if (Model?.PreviewImageObject == null || _smartWindow == null)
            {
                return;
            }

            var handle = MeasureRectPreviewGeometry.HitTest(
                Model.GetPreviewRect(),
                column,
                row,
                GetHitToleranceImageUnits());

            _smartWindow.Cursor = handle switch
            {
                MeasureRectPreviewHandle.Body or MeasureRectPreviewHandle.Center => Cursors.SizeAll,
                MeasureRectPreviewHandle.Left or MeasureRectPreviewHandle.Right => Cursors.SizeWE,
                MeasureRectPreviewHandle.Top or MeasureRectPreviewHandle.Bottom => Cursors.SizeNS,
                MeasureRectPreviewHandle.TopLeft or MeasureRectPreviewHandle.BottomRight => Cursors.SizeNWSE,
                MeasureRectPreviewHandle.TopRight or MeasureRectPreviewHandle.BottomLeft => Cursors.SizeNESW,
                MeasureRectPreviewHandle.Rotate => Cursors.Hand,
                _ => Cursors.Arrow
            };
        }
        #endregion

        #region 模型刷新
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressModelRedraw)
            {
                return;
            }

            if (e.PropertyName == nameof(MeasureRectModel.InitRectPX)
                || e.PropertyName == nameof(MeasureRectModel.InitRectPY)
                || e.PropertyName == nameof(MeasureRectModel.InitRectLen1)
                || e.PropertyName == nameof(MeasureRectModel.InitRectLen2)
                || e.PropertyName == nameof(MeasureRectModel.InitRectAngle)
                || e.PropertyName == nameof(MeasureRectModel.PreviewImageObject))
            {
                QueueRefreshEditableRect();
            }
        }

        private void QueueRefreshEditableRect()
        {
            if (_refreshEditableRectPending)
            {
                return;
            }

            _refreshEditableRectPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshEditableRectPending = false;
                RefreshEditableRect();
            }));
        }

        private void RefreshEditableRect()
        {
            Model?.RefreshEditableRectPreview(GetImageUnitsForScreenPixels(1.0));
        }

        private void PreviewDrawObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueSyncPreviewDrawObjects();
        }

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
        }

        private void ClearMirroredPreviewObjects()
        {
            if (HalconPreview == null)
            {
                return;
            }

            ReplaceMirroredPreviewObjects(Enumerable.Empty<HalconDrawingObject>());
        }

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

            if (isRefreshSuppressed && !previousSuppressRefresh)
            {
                RefreshHalconPreview();
            }
        }

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

        private void RefreshHalconPreview()
        {
            try
            {
                HalconRefreshDisplayMethod?.Invoke(HalconPreview, new object[] { false });
            }
            catch
            {
            }
        }

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

        private double GetHitToleranceImageUnits()
        {
            return GetImageUnitsForScreenPixels(MeasureRectPreviewStyle.HitToleranceScreenPixels);
        }

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
