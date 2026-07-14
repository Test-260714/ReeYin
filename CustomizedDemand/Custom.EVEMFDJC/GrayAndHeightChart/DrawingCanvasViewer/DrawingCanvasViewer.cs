using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.EVEMFDJC
{
    [ContentProperty("DrawingCanvas")]
    public sealed class DrawingCanvasViewer : Control
    {
        #region 依赖属性

        public static readonly DependencyProperty DrawingCanvasProperty = DependencyProperty.Register("DrawingCanvas", typeof(DrawingCanvas), typeof(DrawingCanvasViewer));
        /// <summary>
        /// 画板
        /// </summary>
        public DrawingCanvas DrawingCanvas { get => (DrawingCanvas)this.GetValue(DrawingCanvasProperty); set => this.SetValue(DrawingCanvasProperty, value); }
        #endregion

        #region 构造器

        static DrawingCanvasViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingCanvasViewer), new FrameworkPropertyMetadata(typeof(DrawingCanvasViewer)));
        }

        public DrawingCanvasViewer()
        {

        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 更新背景图片
        /// </summary>
        /// <param name="bitmapImage"></param>
        public void UpdateBackgroundImageAsync(ImageSource bitmapImage)
        {
            image.Source = bitmapImage;
            if (contentPresenter.Width != bitmapImage.Width || contentPresenter.Height != bitmapImage.Height)
            {
                contentPresenter.Width = bitmapImage.Width;
                contentPresenter.Height = bitmapImage.Height;
            }
        }


        /// <summary>
        /// 滚动
        /// </summary>
        /// <param name="e"></param>
        public void ScrollBy(MouseEventArgs e)
        {
            var transform = childBorder.RenderTransform as MatrixTransform;

            Point mouse = transform.Inverse.Transform(e.GetPosition(this));
            Vector delta = Point.Subtract(mouse, pressedMouse); // delta from old mouse to current mouse
            var translate = new TranslateTransform(delta.X, delta.Y);
            transform.Matrix = translate.Value * transform.Matrix;
        }

        /// <summary>
        /// 原始缩放
        /// </summary>
        public void Home()
        {
            ResetZoom();
        }
        /// <summary>
        /// 设置鼠标位置显示信息
        /// </summary>
        public Func<Point, string> DrawingCanvas_OnMouseMovedAction { get; set; } = (p) => { return $"X:{p.X} Y:{p.Y}"; };
        #endregion

        #region 私有方法

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (imageBorder == null)
            {
                label = this.Template.FindName("Part_Label", this) as Label;
                imageBorder = this.Template.FindName("Part_ImageBorder", this) as Border;
                image = this.Template.FindName("Part_Image", this) as Image;
                contentPresenter = this.Template.FindName("Part_ContentPresenter", this) as ContentPresenter;
                d3DImage = this.Template.FindName("Part_D3DImage", this) as D3DImage;
                childBorder = imageBorder.Child;
                var transform = childBorder.RenderTransform as MatrixTransform;
                var matrix = transform.Matrix;
                childBorder.RenderTransform = new MatrixTransform(matrix);

                imageBorder.MouseWheel += OnImageBorderMouseWheel;
                imageBorder.MouseDown += OnImageBorderMouseDown;

                DrawingCanvas.OnMouseLeveled += DrawingCanvas_OnMouseLeveled;
                DrawingCanvas.OnMouseMoved += DrawingCanvas_OnMouseMoved;
                DrawingCanvas.OnMouseEntered += DrawingCanvas_OnMouseEntered;
            }
        }

        private void DrawingCanvas_OnMouseEntered(object? sender, MouseEventArgs e)
        {
            label.Visibility = Visibility.Visible;
        }
        private void DrawingCanvas_OnMouseLeveled(object? sender, MouseEventArgs e)
        {
            label.Visibility = Visibility.Hidden;
        }
        private void DrawingCanvas_OnMouseMoved(object? sender, Point e)
        {
            if (e == _lastPoint) return;
            _lastPoint = e;
            var str = DrawingCanvas_OnMouseMovedAction?.Invoke(e);
            label.Content = str;
        }

        private void OnImageBorderMouseDown(object sender, MouseButtonEventArgs e)
        {
            pressedMouse = e.GetPosition(childBorder);
        }

        private void OnImageBorderMouseWheel(Object sender, MouseWheelEventArgs e)
        {
            if (DrawingCanvas.IsVideoMode) return;
            var position = e.GetPosition(childBorder);
            var transform = childBorder.RenderTransform as MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor
            matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);
            childBorder.RenderTransform = new MatrixTransform(matrix);
            // 添加渲染提示
            //RenderOptions.SetBitmapScalingMode(childBorder, BitmapScalingMode.NearestNeighbor);
        }
        private void ResetZoom()
        {
            if (childBorder != null)
            {
                var transform = childBorder.RenderTransform as MatrixTransform;
                var matrix = Matrix.Identity;
                childBorder.RenderTransform = new MatrixTransform(matrix);
            }
        }

        #endregion

        #region 属性
        /// <summary>
        /// 背景图
        /// </summary>
        public BitmapSource? BackgroundImage
        {
            get
            {
                if (image == null)
                    return null;
                return (BitmapSource)image.Source;
            }
        }
        #endregion

        #region 字段
        private Label label;
        private Border imageBorder;
        private Image image;
        private ContentPresenter contentPresenter;
        private D3DImage d3DImage;
        private Point pressedMouse;
        private UIElement childBorder;
        private Point _lastPoint;
        #endregion
    }
}
