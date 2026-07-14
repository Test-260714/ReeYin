using ImageTool.Halcon.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Line = System.Windows.Shapes.Line;
using Point = System.Windows.Point;

namespace ReeYin_V.Hardware.ControlCard.Views
{
    public class ArcWithMovableCrossDrawer
    {
        private bool _manualMode = false;   // 标记是否手动模式
        private DispatcherTimer _timer;
        private PathFigure _figure;
        private double _current;
        private double _end;
        private double _step;
        private double _radius;
        private Point _center;
        private bool _clockwise;

        private Line _crossH;
        private Line _crossV;
        private double _crossSize;

        public void DrawArcWithCross(
            Canvas canvas,
            Point center,
            double radius,
            double startRadian,
            double endRadian,
            double step = 0.03,
            int frameMs = 16,
            double arcThickness = 2,
            Brush arcColor = null,
            double crossSize = 20,
            Brush crossColor = null,
            double crossThickness = 1)
        {
            if (canvas == null) return;

            _timer?.Stop();

            _clockwise = endRadian > startRadian;
            _step = _clockwise ? Math.Abs(step) : -Math.Abs(step);
            _center = center;
            _radius = radius;
            _end = endRadian;
            _current = startRadian;
            _crossSize = crossSize;

            // 初始化路径
            Point startPoint = PolarToCanvas(_current);
            _figure = new PathFigure { StartPoint = startPoint };
            var geometry = new PathGeometry();
            geometry.Figures.Add(_figure);

            var path = new Path
            {
                Data = geometry,
                Stroke = arcColor ?? Brushes.Red,
                StrokeThickness = arcThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent
            };
            canvas.Children.Add(path);

            // 十字线
            _crossH = new Line
            {
                Stroke = crossColor ?? Brushes.DarkGreen,
                StrokeThickness = crossThickness
            };
            _crossV = new Line
            {
                Stroke = crossColor ?? Brushes.DarkGreen,
                StrokeThickness = crossThickness
            };
            canvas.Children.Add(_crossH);
            canvas.Children.Add(_crossV);

            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(frameMs) };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
        }

        private void Tick()
        {
            _current += _step;
            bool finished = _clockwise ? _current >= _end : _current <= _end;
            if (finished)
            {
                _current = _end;
                _timer.Stop();
            }

            // 追加圆弧线段
            Point p = PolarToCanvas(_current);
            _figure.Segments.Add(new LineSegment(p, true));

            // 只有在非手动模式下才自动移动十字线
            if (!_manualMode)
                MoveCrossTo(p);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;

            Point clickPos = e.GetPosition(canvas);
            MoveCrossTo(clickPos);

            // 进入手动模式，防止下一帧立刻被动画覆盖
            _manualMode = true;

            // 可选：3 秒后自动恢复动画跟随
            var restoreTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            restoreTimer.Tick += (s, args) =>
            {
                restoreTimer.Stop();
                _manualMode = false;
            };
            restoreTimer.Start();
        }

        /// <summary>
        /// 更新十字线坐标
        /// </summary>
        private void MoveCrossTo(Point center)
        {
            double half = _crossSize / 2;

            _crossH.X1 = center.X - half;
            _crossH.Y1 = center.Y;
            _crossH.X2 = center.X + half;
            _crossH.Y2 = center.Y;

            _crossV.X1 = center.X;
            _crossV.Y1 = center.Y - half;
            _crossV.X2 = center.X;
            _crossV.Y2 = center.Y + half;
        }

        private Point PolarToCanvas(double rad)
        {
            return new Point(
                _center.X + _radius * Math.Cos(rad),
                _center.Y + _radius * Math.Sin(rad));
        }
    }


    /// <summary>
    /// MotionCurveView.xaml 的交互逻辑
    /// </summary>
    public partial class MotionCurveView : UserControl
    {
        private double Radius = 300;

        private int CacheNumber = 1000;

        private bool IsRunning = true;

        private ArcWithMovableCrossDrawer drawer = new ArcWithMovableCrossDrawer();



        // 用于跟踪当前是否有动画正在运行
        private DispatcherTimer _drawingTimer;
        // 用于标识是否正在清除操作
        private bool _isClearing = false;

        public MotionCurveView()
        {
            InitializeComponent();
            PlotXY();
            // 摆圆形
            //_ = Task.Run(() =>
            //{
            //ClearTrace();
            //    while (IsRunning)
            //    {
            //        for (int i = 0; i < 36; i++)
            //        {
            //            if (!IsRunning)
            //            {
            //                break;
            //            }
            //            PlotTrace(10 * i, 0.8 * Radius);
            //            Thread.Sleep(100);
            //        }
            //        Thread.Sleep(1000);
            //    }
            //});
            drawer.DrawArcWithCross(
                   CanvasTrace,
                   center: new Point(300, 300),
                   radius: 200,
                   startRadian: 0,
                   endRadian: Math.PI * 2,
                   step: 0.05,
                   frameMs: 16,
                   arcThickness: 3,
                   arcColor: Brushes.Blue,
                   crossSize: 10000,
                   crossColor: Brushes.Green,
                   crossThickness: 2);

            // 摆米字形
            //    _ = Task.Run(() =>
            //        {
            //            //control.ClearTrace();

            //            while (IsRunning)
            //            {
            //                // 准备坐标组
            //                // 生成100个随机点
            //                var randomPoints = Generate100RandomPoints();

            //                // 或者指定坐标范围生成100个点
            //                var customRangePoints = GenerateRandomPoints(100, -50, 50, -50, 50);

            //                //for (int i = 0; i < customRangePoints.Count; i++)
            //                //{
            //                //    if (!IsRunning)
            //                //    {
            //                //        break;
            //                //    }
            //                //    //获取实时点位坐标大于指定位置，就绘制


            //                //    PlotTrace(customRangePoints[i]);
            //                //    Thread.Sleep(100);
            //                //}

            //                //// 绘制一条慢速的蓝色直线
            //                //DrawLineAnimated(
            //                //    new Point(0, 0),
            //                //    new Point(200, 200),
            //                //    0.2,  // 慢速
            //                //    20,    // 线宽
            //                //    Brushes.Blue
            //                //);


            //                //_ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //                //{
            //                //    CanvasTrace.Children.Clear();
            //                //}));

            //                Thread.Sleep(1000);
            //            }
            //        });
        }

        #region Methods

        private static readonly Random _random = new Random();

        /// <summary>
        /// 生成指定数量的随机点
        /// </summary>
        /// <param name="count">点的数量</param>
        /// <param name="minX">X坐标最小值</param>
        /// <param name="maxX">X坐标最大值</param>
        /// <param name="minY">Y坐标最小值</param>
        /// <param name="maxY">Y坐标最大值</param>
        /// <returns>随机点集合</returns>
        public static List<Point> GenerateRandomPoints(
            int count,
            double minX = 0,
            double maxX = 100,
            double minY = 0,
            double maxY = 100)
        {
            if (count <= 0)
                throw new ArgumentException("点的数量必须大于0", nameof(count));

            if (minX >= maxX)
                throw new ArgumentException("minX必须小于maxX", nameof(minX));

            if (minY >= maxY)
                throw new ArgumentException("minY必须小于maxY", nameof(minY));

            var points = new List<Point>(count);

            for (int i = 0; i < count; i++)
            {
                // 生成X坐标（在minX和maxX之间）
                double x = minX + _random.NextDouble() * (maxX - minX);

                // 生成Y坐标（在minY和maxY之间）
                double y = minY + _random.NextDouble() * (maxY - minY);

                points.Add(new Point(x, y));
            }

            return points;
        }

        // 生成100个默认范围的随机点的快捷方法
        public static List<Point> Generate100RandomPoints()
        {
            // 可以根据需要调整坐标范围
            return GenerateRandomPoints(100, 0, 200, 0, 200);
        }

        /// <summary>
        /// 绘制坐标及其他辅助线
        /// </summary>
        public void PlotXY()
        {
            // 中心点
            Point p0 = new Point(Radius, Radius);
            // x 轴
            Point px1 = new Point(0, Radius);
            Point px2 = new Point(2 * Radius, Radius);
            Point px3 = new Point(2 * Radius - 10, Radius - 5);
            Point px4 = new Point(2 * Radius - 10, Radius + 5);
            // y 轴
            Point py1 = new Point(Radius, 2 * Radius);
            Point py2 = new Point(Radius, 0);
            Point py3 = new Point(Radius - 5, 10);
            Point py4 = new Point(Radius + 5, 10);
            // 添加元素：自定义路径
            PathFigure figure = new PathFigure
            {
                StartPoint = px1,
                IsClosed = false,
            };
            // 绘制 x 轴
            figure.Segments.Add(new LineSegment(px2, true));
            figure.Segments.Add(new LineSegment(px2, true));
            figure.Segments.Add(new LineSegment(px3, true));
            figure.Segments.Add(new LineSegment(px3, true));
            figure.Segments.Add(new LineSegment(px2, true));
            figure.Segments.Add(new LineSegment(px2, true));
            figure.Segments.Add(new LineSegment(px4, true));
            figure.Segments.Add(new LineSegment(px4, true));
            figure.Segments.Add(new LineSegment(px2, true));
            // 绘制 y 轴
            figure.Segments.Add(new LineSegment(px2, true));
            figure.Segments.Add(new LineSegment(p0, true));
            figure.Segments.Add(new LineSegment(p0, true));
            figure.Segments.Add(new LineSegment(py1, true));
            figure.Segments.Add(new LineSegment(py1, true));
            figure.Segments.Add(new LineSegment(py2, true));
            figure.Segments.Add(new LineSegment(py2, true));
            figure.Segments.Add(new LineSegment(py3, true));
            figure.Segments.Add(new LineSegment(py3, true));
            figure.Segments.Add(new LineSegment(py2, true));
            figure.Segments.Add(new LineSegment(py2, true));
            figure.Segments.Add(new LineSegment(py4, true));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            Path path = new Path
            {
                Stroke = Brushes.OrangeRed,
                StrokeThickness = 2,
                Data = geometry,
            };
            _ = CanvasTrace.Children.Add(path);
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CanvasXY.Children.Clear();
                // 画 x y 轴
                PointCollection pts1 = new PointCollection
                 {
                     new Point(0, Radius),
                     new Point(2 * Radius - 10, Radius - 5),
                     new Point(2 * Radius - 10, Radius + 5),
                     new Point(Radius, 2 * Radius),
                     new Point(Radius - 5, 10),
                     new Point(Radius + 5, 10),
                 };
                PointCollection pts2 = new PointCollection
                 {
                     new Point(2 * Radius, Radius),
                     new Point(2 * Radius, Radius),
                     new Point(2 * Radius, Radius),
                     new Point(Radius, 0),
                     new Point(Radius, 0),
                     new Point(Radius, 0),
                 };
                LineGeometry line;
                Path path;
                for (int i = 0; i < pts1.Count; i++)
                {
                    line = new LineGeometry
                    {
                        StartPoint = pts1[i],
                        EndPoint = pts2[i],
                    };
                    path = new Path
                    {
                        Stroke = Brushes.OrangeRed,
                        StrokeThickness = 2,
                        Data = line,
                    };
                    _ = CanvasXY.Children.Add(path);
                }

                // 增加几个虚拟圆
                for (int i = 1; i <= 10; i++)
                {
                    EllipseGeometry ellipse = new EllipseGeometry
                    {
                        Center = new Point(Radius, Radius),
                        RadiusX = 0.1 * i * Radius,
                        RadiusY = 0.1 * i * Radius,
                    };
                    path = new Path
                    {
                        Fill = Brushes.Transparent,
                        Stroke = Brushes.OrangeRed,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 5, 2 },
                        Data = ellipse,
                    };
                    _ = CanvasXY.Children.Add(path);
                }

                // 增加斜线
                line = new LineGeometry
                {
                    StartPoint = new Point(0, 2 * Radius),
                    EndPoint = new Point(2 * Radius, 0),
                };
                path = new Path
                {
                    Stroke = Brushes.OrangeRed,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 5, 2 },
                    Data = line,
                };
                _ = CanvasXY.Children.Add(path);

                // 增加斜线
                line = new LineGeometry
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(2 * Radius, 2 * Radius),
                };
                path = new Path
                {
                    Stroke = Brushes.OrangeRed,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 5, 2 },
                    Data = line,
                };
                _ = CanvasXY.Children.Add(path);
            }));
        }


        public void PlotTrace(double angle, double r)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 只保留预设点数
                while (CanvasTrace.Children.Count >= CacheNumber)
                {
                    CanvasTrace.Children.RemoveAt(0);
                }
                // 角度 → 弧度
                double arc = angle * Math.PI / 180;
                // 滑块位置
                double x = Radius + r * Math.Cos(arc);
                double y = Radius - r * Math.Sin(arc);
                EllipseGeometry ellipse = new EllipseGeometry
                {
                    Center = new Point(x, y),
                    RadiusX = 10,
                    RadiusY = 10,
                };
                Path path = new Path
                {
                    Fill = Brushes.DodgerBlue,
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 1,
                    Data = ellipse,
                };
                _ = CanvasTrace.Children.Add(path);

                // 前面的圆设置为不填充
                for (int i = 0; i < CanvasTrace.Children.Count - 1; i++)
                {
                    (CanvasTrace.Children[i] as Path).Fill = Brushes.Green;
                    (CanvasTrace.Children[i] as Path).Stroke = Brushes.Green;
                }
            }));
        }

        /// <summary>
        /// 绘制单个点
        /// </summary>
        /// <param name="point"></param>
        public void PlotTrace(Point point)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 只保留预设点数
                while (CanvasTrace.Children.Count >= CacheNumber)
                {
                    CanvasTrace.Children.RemoveAt(0);
                }

                // 使用传入的点坐标，保持原有的坐标转换逻辑
                double x = Radius + point.X;
                double y = Radius - point.Y;

                EllipseGeometry ellipse = new EllipseGeometry
                {
                    Center = new Point(x, y),
                    RadiusX = 10,
                    RadiusY = 10,
                };

                Path path = new Path
                {
                    Fill = Brushes.DodgerBlue,
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 1,
                    Data = ellipse,
                };

                _ = CanvasTrace.Children.Add(path);

                // 前面的圆设置为绿色
                for (int i = 0; i < CanvasTrace.Children.Count - 1; i++)
                {
                    if (CanvasTrace.Children[i] is Path pathItem)
                    {
                        pathItem.Fill = Brushes.Green;
                        pathItem.Stroke = Brushes.Green;
                    }
                }
            }));
        }

        /// <summary>
        /// 以指定速度匀速绘制直线
        /// </summary>
        /// <param name="startPoint">起始点坐标</param>
        /// <param name="endPoint">结束点坐标</param>
        /// <param name="speed">绘制速度（像素/毫秒）</param>
        /// <param name="thickness">线宽</param>
        /// <param name="color">线条颜色</param>
        public void DrawLineAnimated(Point startPoint, Point endPoint, double speed = 0.5,
                                     double thickness = 2, Brush color = null)
        {
            // 计算实际绘图坐标
            double startX = Radius + startPoint.X;
            double startY = Radius - startPoint.Y;
            double endX = Radius + endPoint.X;
            double endY = Radius - endPoint.Y;



            // 添加到Canvas并设置层级
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 创建直线元素
                Line line = new Line
                {
                    X1 = startX,
                    Y1 = startY,
                    X2 = startX,  // 初始时线长为0
                    Y2 = startY,
                    Stroke = color ?? Brushes.Red,
                    StrokeThickness = thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                CanvasTrace.Children.Add(line);
                Canvas.SetZIndex(line, -1);


                // 计算总长度和方向向量
                double deltaX = endX - startX;
                double deltaY = endY - startY;
                double totalLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (totalLength <= 0) return; // 起点终点相同，无需绘制

                // 计算单位向量
                double unitX = deltaX / totalLength;
                double unitY = deltaY / totalLength;

                // 动画计时器
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16) // 约60fps
                };

                double currentLength = 0;

                timer.Tick += (sender, e) =>
                {
                    // 计算当前帧应该增加的长度
                    double step = speed * timer.Interval.TotalMilliseconds;
                    currentLength += step;

                    // 检查是否到达终点
                    if (currentLength >= totalLength)
                    {
                        currentLength = totalLength;

                        _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            CanvasTrace.Children.Clear();
                        }));
                        timer.Stop();
                    }

                    // 更新直线终点位置
                    line.X2 = startX + unitX * currentLength;
                    line.Y2 = startY + unitY * currentLength;

                };

                // 启动动画
                timer.Start();
            });
        }


    }



    #endregion
}
