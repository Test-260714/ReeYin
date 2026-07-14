using HalconDotNet;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.MeasureLine.Views
{
    public partial class MeasureLinePreviewControl : UserControl
    {
        #region 参数与状态
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
                nameof(Model),
                typeof(MeasureLineModel),
                typeof(MeasureLinePreviewControl),
                new PropertyMetadata(null, OnModelChanged));

        private HSmartWindowControlWPF _smartWindow;
        private MeasureLinePreviewHandle _dragHandle = MeasureLinePreviewHandle.None;
        private MeasureLinePreviewLine _dragStartLine;
        private MeasureLinePreviewPoint _dragStartPoint;
        private bool _suppressModelRedraw;
        private bool _refreshEditableLinePending;
        private bool _syncPreviewDrawObjectsPending;

        public MeasureLineModel Model
        {
            get => (MeasureLineModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        #endregion

        #region 初始化
        public MeasureLinePreviewControl()
        {
            InitializeComponent();
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MeasureLinePreviewControl)d;
            control.UnsubscribeModel(e.OldValue as MeasureLineModel);
            control.SubscribeModel(e.NewValue as MeasureLineModel);
            control.QueueRefreshEditableLine();
        }

        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(AttachSmartWindow));
            QueueRefreshEditableLine();
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
                return;

            _smartWindow.HMouseDown += SmartWindow_HMouseDown;
            _smartWindow.HMouseMove += SmartWindow_HMouseMove;
            _smartWindow.HMouseUp += SmartWindow_HMouseUp;
            SyncPreviewDrawObjects();
        }

        private void DetachSmartWindow()
        {
            if (_smartWindow == null)
                return;

            _smartWindow.HMouseDown -= SmartWindow_HMouseDown;
            _smartWindow.HMouseMove -= SmartWindow_HMouseMove;
            _smartWindow.HMouseUp -= SmartWindow_HMouseUp;
            _smartWindow = null;
        }

        private void SubscribeModel(MeasureLineModel model)
        {
            if (model == null)
                return;

            model.PropertyChanged += Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
            SyncPreviewDrawObjects();
        }

        private void UnsubscribeModel(MeasureLineModel model)
        {
            if (model == null)
                return;

            model.PropertyChanged -= Model_PropertyChanged;
            model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
            ClearMirroredPreviewObjects();
        }
        #endregion

        #region 直线交互
        private void SmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model?.PreviewImageObject == null || e.Button != MouseButton.Left)
                return;

            var point = new MeasureLinePreviewPoint(e.Column, e.Row);
            _dragStartLine = Model.GetPreviewLine();
            _dragHandle = MeasureLinePreviewGeometry.HitTest(_dragStartLine, point.X, point.Y, GetHitToleranceImageUnits());
            if (_dragHandle == MeasureLinePreviewHandle.None)
                return;

            _dragStartPoint = point;
            HalconPreview.DrawModel = true;
            _smartWindow?.CaptureMouse();
        }

        private void SmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Model == null)
                return;

            if (_dragHandle == MeasureLinePreviewHandle.None)
            {
                UpdateCursor(e.Column, e.Row);
                return;
            }

            var point = new MeasureLinePreviewPoint(e.Column, e.Row);
            MeasureLinePreviewLine line = _dragHandle switch
            {
                MeasureLinePreviewHandle.Start => MeasureLinePreviewGeometry.MoveStart(_dragStartLine, point.X, point.Y),
                MeasureLinePreviewHandle.End => MeasureLinePreviewGeometry.MoveEnd(_dragStartLine, point.X, point.Y),
                MeasureLinePreviewHandle.Body => MeasureLinePreviewGeometry.Move(
                    _dragStartLine,
                    point.X - _dragStartPoint.X,
                    point.Y - _dragStartPoint.Y),
                _ => _dragStartLine
            };

            ApplyPreviewLine(line, false);
        }

        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_dragHandle == MeasureLinePreviewHandle.None)
                return;

            ApplyPreviewLine(Model.GetPreviewLine(), true);
            _dragHandle = MeasureLinePreviewHandle.None;
            HalconPreview.DrawModel = false;
            if (_smartWindow?.IsMouseCaptured == true)
            {
                _smartWindow.ReleaseMouseCapture();
            }
        }

        private void ApplyPreviewLine(MeasureLinePreviewLine line, bool runMeasurement)
        {
            try
            {
                _suppressModelRedraw = true;
                Model.ApplyPreviewLine(line, runMeasurement, GetImageUnitsForScreenPixels(1.0));
            }
            finally
            {
                _suppressModelRedraw = false;
            }
        }

        private void UpdateCursor(double column, double row)
        {
            if (Model?.PreviewImageObject == null || _smartWindow == null)
                return;

            var handle = MeasureLinePreviewGeometry.HitTest(Model.GetPreviewLine(), column, row, GetHitToleranceImageUnits());
            _smartWindow.Cursor = handle switch
            {
                MeasureLinePreviewHandle.Start or MeasureLinePreviewHandle.End => Cursors.Hand,
                MeasureLinePreviewHandle.Body => Cursors.SizeAll,
                _ => Cursors.Arrow
            };
        }
        #endregion

        #region 模型刷新
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressModelRedraw)
                return;

            if (e.PropertyName == nameof(MeasureLineModel.InitLineStartX)
                || e.PropertyName == nameof(MeasureLineModel.InitLineStartY)
                || e.PropertyName == nameof(MeasureLineModel.InitLineEndX)
                || e.PropertyName == nameof(MeasureLineModel.InitLineEndY)
                || e.PropertyName == nameof(MeasureLineModel.PreviewImageObject))
            {
                QueueRefreshEditableLine();
            }
        }

        private void QueueRefreshEditableLine()
        {
            if (_refreshEditableLinePending)
                return;

            _refreshEditableLinePending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshEditableLinePending = false;
                RefreshEditableLine();
            }));
        }

        private void RefreshEditableLine()
        {
            Model?.RefreshEditableLinePreview(GetImageUnitsForScreenPixels(1.0));
        }

        private void PreviewDrawObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueSyncPreviewDrawObjects();
        }

        private void QueueSyncPreviewDrawObjects()
        {
            if (_syncPreviewDrawObjectsPending)
                return;

            _syncPreviewDrawObjectsPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _syncPreviewDrawObjectsPending = false;
                SyncPreviewDrawObjects();
            }));
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

        private double GetHitToleranceImageUnits()
        {
            return GetImageUnitsForScreenPixels(MeasureLinePreviewStyle.HitToleranceScreenPixels);
        }

        private double GetImageUnitsForScreenPixels(double screenPixels)
        {
            try
            {
                var hWindow = HalconPreview.HWindow;
                if (hWindow == null)
                    return screenPixels;

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
