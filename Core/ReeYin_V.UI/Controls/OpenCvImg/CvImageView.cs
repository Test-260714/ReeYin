using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReeYin_V.UI.Controls.OpenCvImg
{
    public class CvImageView : UserControl
    {
        // 模板部件名称 (必须与 XAML ResourceDictionary 中的 x:Name 匹配)
        private const string PartImageDisplay = "PART_ImageDisplay";
        private const string PartOverlayCanvas = "PART_OverlayCanvas";
        private const string PartMainGrid = "PART_MainGrid";

        // 内部 WPF 控件引用
        private Image _imageDisplay;
        private Canvas _overlayCanvas;
        private Grid _mainGrid;

        // 变换控制
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;
        private ScaleTransform _scaleTransform;

        // 平移状态
        private System.Windows.Point _moveStartPoint; // 鼠标按下时的 Canvas 坐标
        private System.Windows.Point _moveOffset;     // 当前的平移量 (TranslateTransform 的值)
        private bool _isMoving = false;              // 左键移动状态

        // 当前显示的图像 (作为 Mat)
        private Mat _currentMat;

        // 存储绘制的图形
        public ObservableCollection<DrawingObjectInfo> DrawObjects { get; } = new ObservableCollection<DrawingObjectInfo>();

        #region ❖ 依赖属性 (Dependency Properties)

        // ... (ImageMat 属性定义保持不变) ...
        public Mat ImageMat
        {
            get { return (Mat)GetValue(ImageMatProperty); }
            set { SetValue(ImageMatProperty, value); }
        }

        public static readonly DependencyProperty ImageMatProperty =
            DependencyProperty.Register(nameof(ImageMat), typeof(Mat), typeof(CvImageView),
                new PropertyMetadata(null, OnImageMatChanged));

        private static void OnImageMatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CvImageView view)
            {
                view.UpdateImageDisplay(e.NewValue as Mat);
            }
        }

        public double ZoomScale
        {
            get { return (double)GetValue(ZoomScaleProperty); }
            set { SetValue(ZoomScaleProperty, value); }
        }

        public static readonly DependencyProperty ZoomScaleProperty =
            DependencyProperty.Register(nameof(ZoomScale), typeof(double), typeof(CvImageView),
                new PropertyMetadata(1.0, OnZoomScaleChanged));

        private static void OnZoomScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CvImageView view)
            {
                // 注意：ApplyZoom 不再负责平移，只负责 Scale 和 RedrawOverlay
                view.ApplyZoom((double)e.NewValue);
            }
        }

        #endregion

        #region ❖ 构造函数与模板应用

        public CvImageView()
        {
            this.ClipToBounds = true;

            DrawObjects.CollectionChanged += (s, e) => Dispatcher.Invoke(RedrawOverlay);
            this.SizeChanged += (s, e) => RedrawOverlay();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _mainGrid = GetTemplateChild(PartMainGrid) as Grid;
            _imageDisplay = GetTemplateChild(PartImageDisplay) as Image;
            _overlayCanvas = GetTemplateChild(PartOverlayCanvas) as Canvas;

            if (_mainGrid != null)
            {
                _translateTransform = new TranslateTransform();
                _scaleTransform = new ScaleTransform(1.0, 1.0);

                _transformGroup = new TransformGroup();
                _transformGroup.Children.Add(_scaleTransform);
                _transformGroup.Children.Add(_translateTransform);

                _mainGrid.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                _mainGrid.RenderTransform = _transformGroup;
            }

            // 重新注册事件
            this.MouseWheel += CvImageView_MouseWheel;

            if (_overlayCanvas != null)
            {
                // *** 关键变更 2：左键用于移动 (平移) ***
                _overlayCanvas.MouseDown += OverlayCanvas_MouseDown;
                _overlayCanvas.MouseMove += OverlayCanvas_MouseMove;
                _overlayCanvas.MouseUp += OverlayCanvas_MouseUp;

                // 移除右键平移/绘制逻辑 (如果 ContextMenu 仍需要，请使用)
                // _overlayCanvas.MouseRightButtonDown += OverlayCanvas_MouseRightButtonDown;
                // _overlayCanvas.MouseRightButtonUp += OverlayCanvas_MouseRightButtonUp;
            }
        }

        #endregion

        #region ❖ 图像显示与 Mat 转换

        private void UpdateImageDisplay(Mat newMat)
        {
            _currentMat?.Dispose();
            _currentMat = newMat;

            if (_imageDisplay == null || _overlayCanvas == null) return;

            if (_currentMat != null && !_currentMat.Empty())
            {
                BitmapSource source = _currentMat.ToWriteableBitmap();
                _imageDisplay.Source = source;

                _imageDisplay.Width = _currentMat.Cols;
                _imageDisplay.Height = _currentMat.Rows;
                _overlayCanvas.Width = _currentMat.Cols;
                _overlayCanvas.Height = _currentMat.Rows;

                ZoomScale = 1.0;
                _translateTransform.X = 0;
                _translateTransform.Y = 0;
            }
            else
            {
                _imageDisplay.Source = null;
                _imageDisplay.Width = 0;
                _imageDisplay.Height = 0;
                _overlayCanvas.Width = 0;
                _overlayCanvas.Height = 0;
            }
            RedrawOverlay();
        }

        #endregion

        #region ❖ 缩放与平移控制 (MouseWheel, Panning)

        private void CvImageView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_translateTransform == null || _scaleTransform == null) return;

            // 1. 获取缩放前鼠标在 Canvas 上的坐标 (原始图像坐标)
            // e.GetPosition(_overlayCanvas) 已经在当前 RenderTransform 下提供了原始坐标
            System.Windows.Point zoomCenterPoint = e.GetPosition(_overlayCanvas);

            double zoomFactor = (e.Delta > 0) ? 1.1 : 1.0 / 1.1;
            double newScale = Math.Max(0.1, Math.Min(10.0, ZoomScale * zoomFactor));

            // *** 关键变更 3：定点缩放计算 ***

            // 2. 计算缩放比例变化率
            double scaleFactorChange = newScale / ZoomScale;

            // 3. 计算新的平移量 (使 zoomCenterPoint 在缩放后保持在相同位置)
            // newTranslate = oldTranslate - (zoomCenterPoint * oldScale) * (scaleFactorChange - 1)
            // 简化公式，基于 RenderTransformOrigin(0.5, 0.5) 的坐标系：

            double oldTranslateX = _translateTransform.X;
            double oldTranslateY = _translateTransform.Y;

            // 图像坐标 (相对于原点 (0,0))
            double imageX = zoomCenterPoint.X;
            double imageY = zoomCenterPoint.Y;

            // 屏幕坐标 (相对于 Canvas 左上角) = (ImageCoord * CurrentScale) + CurrentTranslate

            // 计算新的平移量 (使鼠标点在屏幕上的位置不变)
            double newTranslateX = oldTranslateX - (imageX * ZoomScale * (scaleFactorChange - 1));
            double newTranslateY = oldTranslateY - (imageY * ZoomScale * (scaleFactorChange - 1));


            // 4. 更新 ZoomScale (触发 ApplyZoom)
            ZoomScale = newScale;

            // 5. 应用新的平移量 (高性能更新)
            _translateTransform.X = newTranslateX;
            _translateTransform.Y = newTranslateY;

            e.Handled = true;
        }

        private void ApplyZoom(double scale)
        {
            if (_scaleTransform == null) return;

            _scaleTransform.ScaleX = scale;
            _scaleTransform.ScaleY = scale;

            // 缩放改变，必须调用 RedrawOverlay 来更新线宽
            RedrawOverlay();
        }

        // --- 左键平移逻辑 ---

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _currentMat != null)
            {
                // *** 关键变更 4：左键用于移动 (平移) ***
                _moveStartPoint = e.GetPosition(_overlayCanvas);
                _moveOffset = new System.Windows.Point(_translateTransform.X, _translateTransform.Y);
                _isMoving = true;

                _overlayCanvas.CaptureMouse();
                e.Handled = true;
            }
            // 移除绘制启动逻辑
        }

        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMoving)
            {
                _isMoving = false;
                _overlayCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
            // 移除绘制结束逻辑
        }

        #endregion

        #region ❖ 交互式绘制 (Drawing) - 仅保留辅助方法和数据结构

        // 绘制状态 (不再使用)
        private System.Windows.Shapes.Shape _currentDrawingShape;
        // private bool _isDrawing = false; // 已废弃

        // 移除 OverlayCanvas_MouseMove 中的绘制逻辑，只保留平移
        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                // --- 左键平移逻辑 --- (高性能更新，不触发 RedrawOverlay)
                System.Windows.Point currentPoint = e.GetPosition(_overlayCanvas);

                // 计算鼠标移动的 Canvas 距离 (原始像素坐标系中)
                double deltaX = currentPoint.X - _moveStartPoint.X;
                double deltaY = currentPoint.Y - _moveStartPoint.Y;

                // 更新 TranslateTransform 的值
                _translateTransform.X = _moveOffset.X + deltaX;
                _translateTransform.Y = _moveOffset.Y + deltaY;

                e.Handled = true;
            }
            // 移除绘制逻辑
        }

        // 移除 OverlayCanvas_MouseUp 中的绘制逻辑
        // 移除 OverlayCanvas_MouseDown 中的绘制逻辑

        // 辅助方法：创建 WPF 形状 (如果 ContextMenu 仍然允许绘制)
        private System.Windows.Shapes.Shape CreateWpfShape(ShapeType type)
        {
            // ... (保持不变) ...
            System.Windows.Shapes.Shape shape = type switch
            {
                ShapeType.Rectangle => new System.Windows.Shapes.Rectangle(),
                ShapeType.Circle or ShapeType.Ellipse => new System.Windows.Shapes.Ellipse(),
                ShapeType.Line => new System.Windows.Shapes.Line(),
                _ => new System.Windows.Shapes.Rectangle()
            };

            shape.Stroke = Brushes.Red;
            shape.StrokeThickness = 2.0 / ZoomScale;
            shape.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 0, 0));

            return shape;
        }

        private void RedrawOverlay()
        {
            // ... (保持不变) ...
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RedrawOverlay);
                return;
            }

            if (_overlayCanvas == null) return;

            _overlayCanvas.Children.Clear();

            if (_currentMat == null || _currentMat.Empty()) return;

            foreach (var item in DrawObjects)
            {
                item.UiElement.StrokeThickness = 2.0 / ZoomScale;
                _overlayCanvas.Children.Add(item.UiElement);
            }
        }

        public void ClearDrawings()
        {
            DrawObjects.Clear();
            RedrawOverlay();
        }

        #endregion
    }
}
