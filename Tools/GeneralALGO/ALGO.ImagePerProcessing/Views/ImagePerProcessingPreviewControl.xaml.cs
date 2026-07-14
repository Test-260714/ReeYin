#nullable disable
using ALGO.ImagePerProcessing;
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

namespace ALGO.ImagePerProcessing.Views
{
    public partial class ImagePerProcessingPreviewControl : UserControl
    {
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(ImagePerProcessingModel),
                typeof(ImagePerProcessingPreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        private HSmartWindowControlWPF _smartWindow;
        private static readonly FieldInfo HalconSuppressRefreshField = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetField("suppressRefresh", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo HalconRefreshDisplayMethod = typeof(ReeYin_V.UI.Controls.VMHWindowControl)
            .GetMethod("RefreshDisplay", BindingFlags.Instance | BindingFlags.NonPublic);

        private ImagePerProcessingModel _subscribedModel;
        private ImagePerProcessingRoiPreviewHandle _dragHandle = ImagePerProcessingRoiPreviewHandle.None;
        private ImagePerProcessingRoiPreview _dragStartRoi;
        private ImagePerProcessingRoiPreviewPoint _dragStartPoint;
        private bool _suppressModelRedraw;
        private bool _refreshEditableRoiPending;
        private bool _syncPreviewDrawObjectsPending;

        public ImagePerProcessingModel Model
        {
            get => GetValue(ModelProperty) as ImagePerProcessingModel;
            set => SetValue(ModelProperty, value);
        }

        public ImagePerProcessingPreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImagePerProcessingPreviewControl)d;
            control.UnsubscribeModel(e.OldValue as ImagePerProcessingModel);
            control.SubscribeModel(e.NewValue as ImagePerProcessingModel);
            control.QueueRefreshEditableRoi();
            control.QueueSyncPreviewDrawObjects();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeModel(Model);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AttachSmartWindow();
                Model?.RefreshPreviewDisplay();
                QueueRefreshEditableRoi();
                QueueSyncPreviewDrawObjects();
            }));
        }

        private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeModel(Model);
            DetachSmartWindow();
            ClearMirroredPreviewObjects();
        }

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

        private void SubscribeModel(ImagePerProcessingModel model)
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

        private void UnsubscribeModel(ImagePerProcessingModel model)
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

        private void SmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model?.PreviewImageObject == null
                || Model.SelectedROIType != eSelectRoiType.绘制ROI
                || e.Button != MouseButton.Left)
            {
                return;
            }

            var point = new ImagePerProcessingRoiPreviewPoint(e.Column, e.Row);
            _dragStartRoi = Model.GetPreviewRoi();
            _dragHandle = ImagePerProcessingRoiPreviewGeometry.HitTest(
                _dragStartRoi,
                point.X,
                point.Y,
                GetHitToleranceImageUnits());

            if (_dragHandle == ImagePerProcessingRoiPreviewHandle.None)
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

            if (_dragHandle == ImagePerProcessingRoiPreviewHandle.None)
            {
                UpdateCursor(e.Column, e.Row);
                return;
            }

            var point = new ImagePerProcessingRoiPreviewPoint(e.Column, e.Row);
            ImagePerProcessingRoiPreview roi = _dragHandle switch
            {
                ImagePerProcessingRoiPreviewHandle.Body or ImagePerProcessingRoiPreviewHandle.Center => ImagePerProcessingRoiPreviewGeometry.Move(
                    _dragStartRoi,
                    point.X - _dragStartPoint.X,
                    point.Y - _dragStartPoint.Y),
                ImagePerProcessingRoiPreviewHandle.Rotate => ImagePerProcessingRoiPreviewGeometry.RotateToPoint(
                    _dragStartRoi,
                    point.X,
                    point.Y),
                _ => ImagePerProcessingRoiPreviewGeometry.ResizeFromHandle(_dragStartRoi, _dragHandle, point.X, point.Y)
            };

            ApplyPreviewRoi(roi);
        }

        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_dragHandle == ImagePerProcessingRoiPreviewHandle.None)
            {
                ScheduleEditableRoiRefreshForViewChange();
                return;
            }

            ApplyPreviewRoi(Model.GetPreviewRoi());
            _dragHandle = ImagePerProcessingRoiPreviewHandle.None;
            HalconPreview.DrawModel = false;
            if (_smartWindow?.IsMouseCaptured == true)
            {
                _smartWindow.ReleaseMouseCapture();
            }
        }

        private void SmartWindow_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            ScheduleEditableRoiRefreshForViewChange();
        }

        private void ApplyPreviewRoi(ImagePerProcessingRoiPreview roi)
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

        private void UpdateCursor(double column, double row)
        {
            if (Model?.PreviewImageObject == null
                || Model.SelectedROIType != eSelectRoiType.绘制ROI
                || _smartWindow == null)
            {
                return;
            }

            var handle = ImagePerProcessingRoiPreviewGeometry.HitTest(
                Model.GetPreviewRoi(),
                column,
                row,
                GetHitToleranceImageUnits());

            _smartWindow.Cursor = handle switch
            {
                ImagePerProcessingRoiPreviewHandle.Body or ImagePerProcessingRoiPreviewHandle.Center => Cursors.SizeAll,
                ImagePerProcessingRoiPreviewHandle.Left or ImagePerProcessingRoiPreviewHandle.Right or ImagePerProcessingRoiPreviewHandle.Radius => Cursors.SizeWE,
                ImagePerProcessingRoiPreviewHandle.Top or ImagePerProcessingRoiPreviewHandle.Bottom => Cursors.SizeNS,
                ImagePerProcessingRoiPreviewHandle.TopLeft or ImagePerProcessingRoiPreviewHandle.BottomRight => Cursors.SizeNWSE,
                ImagePerProcessingRoiPreviewHandle.TopRight or ImagePerProcessingRoiPreviewHandle.BottomLeft => Cursors.SizeNESW,
                ImagePerProcessingRoiPreviewHandle.Rotate => Cursors.Hand,
                _ => Cursors.Arrow
            };
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressModelRedraw)
            {
                return;
            }

            if (e.PropertyName == nameof(ImagePerProcessingModel.SelectedROIType)
                || e.PropertyName == nameof(ImagePerProcessingModel.CreateROIType)
                || e.PropertyName == nameof(ImagePerProcessingModel.PreviewImageObject))
            {
                QueueRefreshEditableRoi();
            }
        }

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

        private void RefreshEditableRoi()
        {
            Model?.RefreshEditableRoiPreview(GetImageUnitsForScreenPixels(1.0));
        }

        private void ScheduleEditableRoiRefreshForViewChange()
        {
            if (Model?.PreviewImageObject == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(RefreshEditableRoi), DispatcherPriority.Background);
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
            RefreshHalconPreviewCurrentView();
        }

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
                    HalconRefreshDisplayMethod?.Invoke(HalconPreview, new object[] { false });
                }
                catch
                {
                }
            }), DispatcherPriority.Background);
        }

        private void ClearMirroredPreviewObjects()
        {
            if (HalconPreview == null)
            {
                return;
            }

            ReplaceMirroredPreviewObjects(Enumerable.Empty<HalconDrawingObject>());
            RefreshHalconPreviewCurrentView();
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
            return GetImageUnitsForScreenPixels(ImagePerProcessingRoiPreviewStyle.HitToleranceScreenPixels);
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
    }
}
