using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Custom.UI
{
    public class EditDrawTool : DrawingVisual, IDrawTool
    {
        public EditDrawTool(DrawingCanvas drawingCanvas)
        {
            this.drawingCanvas = drawingCanvas;
            this.Guid = Guid.NewGuid();
            // 准备要处理的事件
            this.CanTouchDown = true;

            this.CanKeyDown = true;

            this.CanDoubleClick = true;

            this.selectedDrawGeometries = new List<DrawGeometryBase>();
        }

        #region 公开方法
        public Boolean OnKeyDown(Key key)
        {
            if (key == Key.Delete)
            {
                foreach (var draw in selectedDrawGeometries)
                {
                    this.drawingCanvas.DeleteVisual(draw);
                }

                Delete();
            }
            return true;
        }

        public Boolean OnKeyUp(Key key)
        {
            throw new NotImplementedException();
        }

        public Boolean OnMouseDown(MouseButtonEventArgs e) => false;

        public Boolean OnTouchDown(Int32 touchId, Point point)
        {
            var hitResult = VisualTreeHelper.HitTest(drawingCanvas, point);
            // 点击到编辑点
            if (hitResult != null && hitResult.VisualHit is Rectangle targetRect && targetRect.Tag is EditRectangle targeEditRect)
            {
                mode = 2;
                this.CanTouchMove = true;
                this.CanTouchUp = true;
                drawingCanvas.AddWorkingDrawTool(this);
                selectedEditRect = targeEditRect;
                return false;
            }

            this.drawingCanvas.Children.OfType<Rectangle>().Where(m => m.Tag is EditRectangle).ToList().ForEach(m => (m.Tag as EditRectangle).Clear());

            if (this.drawingCanvas.DrawingToolType != this.DrawingToolType)
            {
                Delete();
                return false;
            }

            this.TouchId = touchId;

            this.mode = 0;

            if (this.VisualParent == null)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);
                this.drawingCanvas.Insert(0, this);
            }
            else
            {
                this.drawingCanvas.MovoToHead(this);

                try
                {
                    var visual = this.drawingCanvas.GetVisual(point);
                    if (visual == this || (visual is DrawGeometryBase draw && selectedDrawGeometries.Contains(draw)))
                    {
                        this.mode = 1;
                        this.lastPoint = point;
                    }
                }
                catch
                {

                }

            }

            this.startPoint = point;

            if (this.mode == 0)
            {
                selectRect = drawRect = null;

                this.selectedDrawGeometries.Clear();

                foreach (var draw in this.drawingCanvas.GetDrawGeometries())
                {
                    //if (draw.Select(point))
                    //{
                    //    if (selectRect.HasValue)
                    //        selectRect = Rect.Union(selectRect.Value, draw.Selected());
                    //    else
                    //        selectRect = draw.Selected();

                    //    selectedDrawGeometries.Add(draw);
                    //}
                    //else
                    draw.Unselected();
                }

                if (this.geometry == null)
                {
                    this.geometry = new PathGeometry();
                    var figure = new PathFigure { StartPoint = point, IsClosed = true, IsFilled = true };
                    geometry.Figures.Add(figure);
                }
                else
                    geometry.Figures[0].StartPoint = point;
            }

            this.CanTouchMove = true;
            this.CanTouchUp = true;

            if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                this.CanTouchLeave = true;

            return true;
        }

        public Boolean OnTouchEnter(Point point)
        {
            throw new NotImplementedException();
        }

        public Boolean OnTouchLeave(Point point)
        {
            return OnTouchUp(point);
        }

        public Boolean OnTouchMove(Point point)
        {
            if (mode == 0)
            {
                var figure = geometry.Figures[0];

                var topRight = new Point(point.X, startPoint.Y);
                var bottomLeft = new Point(startPoint.X, point.Y);

                if (figure.Segments.Count == 0)
                {
                    var line = new LineSegment(topRight, false);
                    figure.Segments.Add(line);

                    line = new LineSegment(point, false);
                    figure.Segments.Add(line);

                    line = new LineSegment(bottomLeft, false);
                    figure.Segments.Add(line);
                }
                else
                {
                    var line = (LineSegment)figure.Segments[0];
                    line.Point = topRight;

                    line = (LineSegment)figure.Segments[1];
                    line.Point = point;

                    line = (LineSegment)figure.Segments[2];
                    line.Point = bottomLeft;
                }

                selectedDrawGeometries.Clear();

                foreach (var draw in this.drawingCanvas.GetDrawGeometries())
                {
                    if (draw.Select(geometry))
                    {
                        if (selectRect.HasValue)
                            selectRect = Rect.Union(selectRect.Value, draw.Selected());
                        else
                            selectRect = draw.Selected();

                        selectedDrawGeometries.Add(draw);
                    }
                    else
                        draw.Unselected();
                }

                drawRect = new Rect(startPoint, point);

                var dc = this.RenderOpen();
                dc.DrawRectangle(null, this.drawingCanvas.SelectBackgroundPen, drawRect.Value);
                dc.DrawRectangle(null, this.drawingCanvas.SelectPen, drawRect.Value);
                dc.Close();
            }
            else if (mode == 1)
            {
                // 移动
                var dx = point.X - lastPoint.X;
                var dy = point.Y - lastPoint.Y;

                lastPoint = point;

                foreach (var draw in selectedDrawGeometries)
                {
                    draw.Move(dx, dy);
                }

                var rect = selectRect.Value;
                rect.X += dx;
                rect.Y += dy;
                selectRect = rect;

                var dc = this.RenderOpen();
                dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect.Value);
                dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect.Value);
                dc.Close();
            }
            else if (mode == 2)
            {
                if (CanTouchMove)
                {
                    if (selectedEditRect.EditPosition == EditPosition.Rotate)
                    {
                        //selectedEditRect.Ratate();
                    }
                    else
                    {
                        var offset = selectedEditRect?.Move(point);
                        targetDrawGeometry?.ReDraw(editRectangleList);
                        var oppositeEditRect = editRectangleList.FirstOrDefault(m => m.EditPosition == selectedEditRect.GetOppositePosition());

                        foreach (var item in editRectangleList)
                        {
                            if (item != selectedEditRect && item != oppositeEditRect)
                            {
                                switch (selectedEditRect.EditPosition)
                                {
                                    case EditPosition.BottomCenter:
                                    case EditPosition.TopCenter:
                                        item.MoveY(point.Y - (point.Y - oppositeEditRect.Y) / 2);
                                        break;
                                    case EditPosition.LeftCenter:
                                    case EditPosition.RightCenter:
                                        item.MoveX(point.X - (point.X - oppositeEditRect.X) / 2);
                                        break;

                                    case EditPosition.Left:
                                    case EditPosition.Right:
                                        item.MoveX(point.X - (point.X - oppositeEditRect.X) / 2);
                                        item.MoveY(point.Y - (point.Y - oppositeEditRect.Y) / 2);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public Boolean OnTouchUp(Point point)
        {
            if (mode == 0)
            {
                if (selectedDrawGeometries.Count == 0)
                {
                    Delete();
                    return true;
                }

                if (drawRect.HasValue || selectedDrawGeometries.Count > 1)
                {
                    if (drawRect.HasValue)
                        selectRect = Rect.Union(selectRect.Value, drawRect.Value);

                    var dc = this.RenderOpen();
                    dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect.Value);
                    dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect.Value);
                    dc.Close();
                }
            }
            else if (mode == 1 && startPoint == lastPoint)
            {
                var visual = this.drawingCanvas.GetVisual(point);

                if (visual is DrawGeometryBase selectDraw && selectDraw.CanEdit)
                {
                    selectDraw.Edit();

                    foreach (var draw in selectedDrawGeometries)
                    {
                        if (draw != selectDraw)
                            draw.Unselected();
                    }

                    selectedDrawGeometries.Clear();

                    Delete();
                    return true;
                }
            }
            else if (mode == 2)
            {
                targetDrawGeometry?.DoDrawEnd();
                mode = 0;
                drawingCanvas.DeleteWorkingDrawTool(this);
            }

            this.CanTouchMove = false;
            this.CanTouchUp = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();

            return true;
        }

        public Boolean OnDoubleClick(Point point)
        {
            selectedDrawGeometries.Clear();
            DrawGeometryBase selectDraw = null;
            foreach (var draw in this.drawingCanvas.GetDrawGeometries())
            {
                if (draw.DescendantBounds.Contains(point) && selectDraw == null)
                {
                    selectDraw = draw;

                    //判定shift是否按下
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        selectRect = draw.Selected();
                        selectedDrawGeometries.Add(draw);
                    }
                    else
                    {
                        editRectangleList.Clear();
                        this.drawingCanvas.Children.OfType<Rectangle>().Where(m => m.Tag is EditRectangle).ToList().ForEach(m => (m.Tag as EditRectangle).Clear());
                        if (draw.CanEdit)
                        {
                            var starPoint = draw.StartPoint;
                            var endPoint = draw.EndPoint;
                            var centerPoint = draw.CenterPoint;
                            //圆编辑框
                            if (draw is EllipseDrawTool)
                            {
                                var leftX = starPoint.Value.X;
                                var TopY = starPoint.Value.Y;
                                var bottomY = endPoint.Value.Y;
                                var rightX = endPoint.Value.X;

                                editRectangleList.Add(new EditRectangle(drawingCanvas, centerPoint.X, TopY, EditPosition.TopCenter));
                                editRectangleList.Add(new EditRectangle(drawingCanvas, centerPoint.X, bottomY, EditPosition.BottomCenter));
                                editRectangleList.Add(new EditRectangle(drawingCanvas, leftX, centerPoint.Y, EditPosition.LeftCenter));
                                editRectangleList.Add(new EditRectangle(drawingCanvas, rightX, centerPoint.Y, EditPosition.RightCenter));
                                //editRectangleList.Add(new EditRectangle(drawingCanvas, rightX, (TopY + bottomY) / 2, EditPosition.Rotate));
                                targetDrawGeometry = selectDraw;
                            }
                            else if (draw is RangingDrawTool)
                            {
                                var leftX = starPoint.Value.X;
                                var leftY = starPoint.Value.Y;
                                var rightY = endPoint.Value.Y;
                                var rightX = endPoint.Value.X;

                                editRectangleList.Add(new EditRectangle(drawingCanvas, leftX, leftY, EditPosition.Left));
                                editRectangleList.Add(new EditRectangle(drawingCanvas, rightX, rightY, EditPosition.Right));

                                targetDrawGeometry = selectDraw;
                            }
                        }
                    }
                }
                else
                {
                    draw.Unselected();
                }
            }
            this.CanTouchMove = false;
            return false;
        }

        #endregion

        #region 私有方法

        private void Delete()
        {
            foreach (var draw in selectedDrawGeometries)
            {
                draw.Unselected();
            }

            selectedDrawGeometries.Clear();

            this.drawingCanvas.DeleteVisual(this);
            this.drawingCanvas.DeleteWorkingDrawTool(this);

            IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchUp = false;
            this.CanTouchLeave = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();
        }

        public void DoDrawToolEndedEvent(object sender, DrawToolEventArgs e)
        {
            DrawToolEndedEvent?.Invoke(sender, e);
        }

        #endregion

        #region 属性
        public Guid Guid { get; protected set; }
        public Int32 TouchId { get; private set; }

        public Boolean CanTouchEnter { get; private set; }

        public Boolean CanTouchLeave { get; private set; }

        public Boolean CanTouchDown { get; private set; }

        public Boolean CanTouchMove { get; private set; }

        public Boolean CanTouchUp { get; private set; }

        public Boolean CanKeyDown { get; private set; }

        public Boolean CanKeyUp { get; private set; }

        public Boolean IsFinish { get; private set; }

        public Boolean CanDoubleClick { get; private set; }

        public Boolean CanMouseDown { get; private set; }

        public DrawToolType DrawingToolType => DrawToolType.Edit;

        #endregion

        #region 字段

        private DrawingCanvas drawingCanvas;
        private Point startPoint, lastPoint;
        private List<DrawGeometryBase> selectedDrawGeometries;
        private PathGeometry geometry;
        private Rect? selectRect, drawRect;
        /// <summary>
        /// 0:拾取|1:拖动|2:编辑
        /// </summary>
        private Int32 mode;
        private static List<EditRectangle> editRectangleList = new List<EditRectangle>();


        private static EditRectangle selectedEditRect;
        private static DrawGeometryBase targetDrawGeometry;

        public event DrawToolEventHandler DrawToolEndedEvent;
        #endregion
    }

    public enum EditPosition
    {
        LeftCenter,
        TopCenter,
        RightCenter,
        BottomCenter,
        Rotate,
        Left,
        Right
    }

    public interface EditShape
    {

    }

    public class EditRectangle : EditShape
    {
        private DrawingCanvas drawingCanvas;
        private double x;
        private double y;

        private Point centerPoint;

        /// <summary>
        /// 中心点X位置
        /// </summary>
        public double X => x + shape2Add.Width / 2;
        /// <summary>
        /// 中心点Y位置
        /// </summary>
        public double Y => y + shape2Add.Height / 2;

        public Point? lastPoint = null;
        public Shape shape2Add;
        public EditPosition EditPosition;
        public EditRectangle(DrawingCanvas drawingCanvas, double xOffset, double yOffset, EditPosition editPosition = EditPosition.TopCenter)
        {
            if (editPosition == EditPosition.Rotate)
            {
                shape2Add = new Ellipse()
                {
                    Fill = Brushes.Yellow,
                    Width = 6,
                    Height = 6
                };
            }
            else
            {
                shape2Add = new Rectangle()
                {
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    Width = 6,
                    Height = 6
                };
            }
            shape2Add.MouseMove += RectSharp_MouseMove;
            shape2Add.MouseDown += RectSharp_MouseDown;
            shape2Add.MouseUp += RectSharp_MouseUp;
            shape2Add.MouseLeave += RectSharp_MouseLeave;
            shape2Add.Tag = this;

            this.x = xOffset - shape2Add.Width / 2;
            this.y = yOffset - shape2Add.Height / 2;

            this.drawingCanvas = drawingCanvas;
            this.drawingCanvas.Children.Add(shape2Add);
            this.EditPosition = editPosition;
            SetOffset();
        }

        private void RectSharp_MouseLeave(object sender, MouseEventArgs e)
        {
            this.drawingCanvas.handleCursor = false;
            SetCursor(true);
        }

        private void RectSharp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.drawingCanvas.handleCursor = false;
            SetCursor(true);
            Mouse.Capture(drawingCanvas);
        }

        private void RectSharp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.drawingCanvas.handleCursor = true;
            SetCursor();
            Mouse.Capture(shape2Add);
        }

        private void RectSharp_MouseMove(object sender, MouseEventArgs e)
        {
            SetCursor();
        }

        private void SetCursor(bool setDefault = false)
        {
            if (setDefault)
            {
                this.drawingCanvas.Cursor = Cursors.Arrow;
                return;
            }
            Cursor tempCursor;
            switch (EditPosition)
            {
                case EditPosition.LeftCenter:
                case EditPosition.RightCenter:
                    tempCursor = Cursors.SizeWE;
                    break;
                case EditPosition.TopCenter:
                case EditPosition.BottomCenter:
                    tempCursor = Cursors.SizeNS;
                    break;
                case EditPosition.Left:
                case EditPosition.Right:
                    tempCursor = Cursors.Cross;
                    break;
                default:
                    tempCursor = Cursors.Arrow;
                    break;
            }
            this.drawingCanvas.Cursor = tempCursor;
        }
        /// <summary>
        /// 设置编辑对象中心
        /// </summary>
        /// <param name="centerPoint"></param>
        public void SetCenter(Point centerPoint)
        {
            this.centerPoint = centerPoint;
        }

        public void SetOffset()
        {
            Canvas.SetTop(shape2Add, y);
            Canvas.SetLeft(shape2Add, x);
            lastPoint = new Point(x, y);
        }

        public void SetOffset(double x, double y)
        {
            switch (EditPosition)
            {
                case EditPosition.TopCenter:
                case EditPosition.BottomCenter:
                    this.y = y;
                    break;

                case EditPosition.RightCenter:
                case EditPosition.LeftCenter:
                    this.x = x;
                    break;
            }

            Canvas.SetTop(shape2Add, y);
            Canvas.SetLeft(shape2Add, x);
            lastPoint = new Point(x, y);
        }

        public void Clear()
        {
            this.drawingCanvas.Children.Remove(shape2Add);
        }

        public EditPosition GetOppositePosition()
        {
            switch (EditPosition)
            {
                case EditPosition.LeftCenter:
                    return EditPosition.RightCenter;
                case EditPosition.TopCenter:
                    return EditPosition.BottomCenter;
                case EditPosition.RightCenter:
                    return EditPosition.LeftCenter;
                case EditPosition.BottomCenter:
                    return EditPosition.TopCenter;
                case EditPosition.Left:
                    return EditPosition.Right;
                case EditPosition.Right:
                    return EditPosition.Left;
                default:
                    return EditPosition.TopCenter;
            }
        }

        public Point Move(Point point)
        {
            switch (EditPosition)
            {
                case EditPosition.TopCenter:
                case EditPosition.BottomCenter:
                    this.y = point.Y - shape2Add.Height / 2;
                    Canvas.SetTop(shape2Add, this.y);
                    break;
                case EditPosition.RightCenter:
                case EditPosition.LeftCenter:
                    this.x = point.X - shape2Add.Width / 2;
                    Canvas.SetLeft(shape2Add, this.x);
                    break;
                case EditPosition.Left:
                case EditPosition.Right:
                    this.x = point.X - shape2Add.Width / 2;
                    this.y = point.Y - shape2Add.Height / 2;
                    Canvas.SetLeft(shape2Add, this.x);
                    Canvas.SetTop(shape2Add, this.y);
                    break;
            }
            if (lastPoint == null)
            {
                lastPoint = point;
                return new Point(this.x, this.y);
            }
            Point offset = new Point(this.x - lastPoint.Value.X, this.y - lastPoint.Value.Y);
            lastPoint = new Point(this.x, this.y);
            return offset;
        }

        public void MoveOffset(Point offset)
        {
            this.x += offset.X - shape2Add.Width / 2;
            this.y += offset.Y - shape2Add.Height / 2;
            Canvas.SetTop(shape2Add, this.y);
            Canvas.SetLeft(shape2Add, this.x);
        }

        public void MoveX(double x)
        {
            this.x = x - shape2Add.Width / 2;
            Canvas.SetLeft(shape2Add, this.x);
        }
        public void MoveY(double y)
        {
            this.y = y - shape2Add.Height / 2;
            Canvas.SetTop(shape2Add, this.y);
        }
    }
}
