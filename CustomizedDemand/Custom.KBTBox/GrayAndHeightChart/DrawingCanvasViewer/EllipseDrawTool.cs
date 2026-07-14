
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Custom.UI
{
    /// <summary>
    /// 椭圆
    /// </summary>
    public sealed class EllipseDrawTool : DrawGeometryBase
    {
        public EllipseDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Ellipse;
            this.CanEdit = true;
            // 准备要处理的事件
            this.CanTouchDown = true;

            this.CanMouseDown = true;
        }

        #region 鼠标键盘事件

        public override Boolean OnKeyDown(Key key)
        {
            if (key != Key.LeftShift || !_endPoint.HasValue)
                return false;

            return OnTouchMove(_endPoint.Value);
        }

        public override Boolean OnKeyUp(Key key)
        {
            if (key != Key.LeftShift || !_endPoint.HasValue)
                return false;

            return OnTouchMove(_endPoint.Value);
        }

        public override Boolean OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                Cencel();

                return true; // 返回 false 以指示绘制已被取消
            }
            return true;
        }
        public override Boolean OnTouchLeave(Point point)
        {
            if (!_endPoint.HasValue)
                this.drawingCanvas.DeleteVisual(this);
            else
            {

                var figure = pathGeometry.Figures[0];
                figure.StartPoint = new Point(_startPoint.Value.X, _centerPoint.Y);

                var clockwise = _endPoint.Value.X > _startPoint.Value.X ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                var arc = new ArcSegment(new Point(_endPoint.Value.X, _centerPoint.Y), new Size(radiusX, radiusY), 0, false, clockwise, true);
                figure.Segments.Add(arc);

                arc = new ArcSegment(figure.StartPoint, new Size(radiusX, radiusY), 0, false, clockwise, true);
                figure.Segments.Add(arc);

                geometry = geometry.GetWidenedPathGeometry(pen);


                var textGeometry = formattedText.BuildGeometry(textPoint);
                geometry = Geometry.Combine(geometry, textGeometry, GeometryCombineMode.Union, null);

                var textGeometryDx = formattedTextDx.BuildGeometry(textPointDx);
                geometry = Geometry.Combine(geometry, textGeometryDx, GeometryCombineMode.Union, null);

                var textGeometryDy = formattedTextDy.BuildGeometry(textPointDy);
                geometry = Geometry.Combine(geometry, textGeometryDy, GeometryCombineMode.Union, null);
                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;
            this.CanKeyDown = false;
            this.CanKeyUp = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();
            DoDrawEnd();
            return true;
        }

        public override Boolean OnTouchDown(Int32 touchId, Point point)
        {
            this.TouchId = touchId;

            if (!_startPoint.HasValue)
            {
                this.drawingCanvas.AddWorkingDrawTool(this);

                this.pen = this.drawingCanvas.Pen;
                this.fontSize = this.drawingCanvas.FontSize;
                this.typeface = new Typeface(new FontFamily("Microsoft YaHei UI,Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);


                _startPoint = point;

                this.geometry = new PathGeometry();

                var figure = new PathFigure { IsClosed = true };
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;
                this.CanKeyDown = true;
                this.CanKeyUp = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);
            }
            else
                return OnTouchLeave(point);

            return true;
        }

        public override Boolean OnTouchMove(Point point)
        {
            var dc = this.RenderOpen();

            var startPoint = _startPoint.Value;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                var len = Math.Min(Math.Abs(point.X - startPoint.X), Math.Abs(point.Y - startPoint.Y));
                point = new Point(startPoint.X + (point.X > startPoint.X ? len : -len), startPoint.Y + (point.Y > startPoint.Y ? len : -len));
            }

            if ((startPoint - point).Length <= pen.Thickness)
            {
                dc.Close();
                return true;
            }

            _endPoint = point;

            radiusX = (point.X - startPoint.X) / 2;
            radiusY = (point.Y - startPoint.Y) / 2;
            _centerPoint.X = startPoint.X + radiusX;
            _centerPoint.Y = startPoint.Y + radiusY;

            radiusX = Math.Abs(radiusX);
            radiusY = Math.Abs(radiusY);
            pixelValue1 = Math.PI * (radiusX) * (radiusY);
            pixelValue2 = 2 * radiusX;
            pixelValue3 = 2 * radiusY;

            returnValue1 = Math.PI * (radiusX * PixelEquivalentX) * (radiusY * PixelEquivalentY);
            returnValue2 = radiusX * PixelEquivalentX * 2;
            returnValue3 = radiusY * PixelEquivalentY * 2;
            var ret = GetText();
            var area = "A   " + ret.Item1;
            var dx = "Dx " + ret.Item2;
            var dy = "Dy " + ret.Item3;

            formattedText = new FormattedText(
                area,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);
            formattedTextDx = new FormattedText(
                dx,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);
            formattedTextDy = new FormattedText(
                dy,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);

            var width = area.Length * fontSize / 2;
            _centerPoint = new Point((startPoint.X + point.X) / 2, (startPoint.Y + point.Y) / 2);
            textPoint = new Point(_centerPoint.X - width / 2, _centerPoint.Y + radiusY + 2);
            textPointDx = new Point(_centerPoint.X - width / 2, textPoint.Y + fontSize * 1.2 + pen.Thickness);
            textPointDy = new Point(_centerPoint.X - width / 2, textPointDx.Y + fontSize * 1.2 + pen.Thickness);

            dc.DrawEllipse(null, pen, _centerPoint, radiusX, radiusY);
            dc.Close();

            return true;
        }
        public override void ReDraw(List<EditRectangle> newPosition)
        {
            var topCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.TopCenter);
            var rightCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.RightCenter);
            var bottomCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.BottomCenter);
            var leftCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.LeftCenter);

            this.geometry = new PathGeometry();

            var figure = new PathFigure { IsClosed = true };
            pathGeometry.Figures.Add(figure);

            var dc = this.RenderOpen();

            radiusX = Math.Abs((rightCenterRect.X - leftCenterRect.X) / 2);
            radiusY = Math.Abs((topCenterRect.Y - bottomCenterRect.Y) / 2);

            _centerPoint = new Point((leftCenterRect.X + rightCenterRect.X) / 2, (topCenterRect.Y + bottomCenterRect.Y) / 2);
            _startPoint = new Point(leftCenterRect.X, topCenterRect.Y);
            _endPoint = new Point(rightCenterRect.X, bottomCenterRect.Y);

            pixelValue1 = Math.PI * (radiusX) * (radiusY);
            pixelValue2 = 2 * radiusX;
            pixelValue3 = 2 * radiusY;

            returnValue1 = Math.PI * (radiusX * PixelEquivalentX) * (radiusY * PixelEquivalentY);
            returnValue2 = radiusX * PixelEquivalentX * 2;
            returnValue3 = radiusY * PixelEquivalentY * 2;
            var ret = GetText();
            var area = "A   " + ret.Item1;
            var dx = "Dx " + ret.Item2;
            var dy = "Dy " + ret.Item3;

            var width = area.Length * fontSize / 2;
            textPoint = new Point(_centerPoint.X - width / 2, _centerPoint.Y + radiusY + 2);
            textPointDx = new Point(_centerPoint.X - width / 2, textPoint.Y + fontSize * 1.2 + pen.Thickness);
            textPointDy = new Point(_centerPoint.X - width / 2, textPointDx.Y + fontSize * 1.2 + pen.Thickness);
            formattedText = new FormattedText(
                area,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);
            formattedTextDx = new FormattedText(
                dx,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);
            formattedTextDy = new FormattedText(
                dy,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);

            figure = pathGeometry.Figures[0];
            figure.StartPoint = new Point(_startPoint.Value.X, _centerPoint.Y);

            var clockwise = _endPoint.Value.X > _startPoint.Value.X ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
            var arc = new ArcSegment(new Point(_endPoint.Value.X, _centerPoint.Y), new Size(radiusX, radiusY), 0, false, clockwise, true);
            figure.Segments.Add(arc);

            arc = new ArcSegment(figure.StartPoint, new Size(radiusX, radiusY), 0, false, clockwise, true);
            figure.Segments.Add(arc);

            geometry = geometry.GetWidenedPathGeometry(pen);


            var textGeometry = formattedText.BuildGeometry(textPoint);
            geometry = Geometry.Combine(geometry, textGeometry, GeometryCombineMode.Union, null);

            var textGeometryDx = formattedTextDx.BuildGeometry(textPointDx);
            geometry = Geometry.Combine(geometry, textGeometryDx, GeometryCombineMode.Union, null);

            var textGeometryDy = formattedTextDy.BuildGeometry(textPointDy);
            geometry = Geometry.Combine(geometry, textGeometryDy, GeometryCombineMode.Union, null);
            Draw();
        }
        public override void DoDrawEnd()
        {
            var e = new DrawToolEventArgs(this);
            e.PixelValue1 = pixelValue1;
            e.PixelValue2 = pixelValue2;
            e.PhysicalValue1 = returnValue1;
            e.PhysicalValue2 = returnValue2;

            DoDrawToolEndedEvent(this, e);
        }
        public override void Move(Double dx, Double dy)
        {
            base.Move(dx, dy);
            _startPoint = new Point(_startPoint.Value.X + dx, _startPoint.Value.Y + dy);
            _endPoint = new Point(_endPoint.Value.X + dx, _endPoint.Value.Y + dy);
            _centerPoint = new Point(_centerPoint.X + dx, _centerPoint.Y + dy);
            textPoint = new Point(textPoint.X + dx, textPoint.Y + dy);
            textPointDx = new Point(textPointDx.X + dx, textPointDx.Y + dy);
        }
        #endregion
        private (string, string, string) GetText()
        {
            var unitStr = GetUnitStr();
            var str = UnitType switch
            {
                UnitType.Pixel => ($"{pixelValue1.ToString("0.00000")}{unitStr}²", $"{pixelValue2.ToString("0.00000")}{unitStr}", $"{pixelValue3.ToString("0.00000")}{unitStr}"),
                UnitType.Millimeter => ($"{returnValue1.ToString("0.00000")}{unitStr}²", $"{returnValue2.ToString("0.00000")}{unitStr}", $"{returnValue3.ToString("0.00000")}{unitStr}"),
                _ => ($"{pixelValue1.ToString("0.00000")}{unitStr}²", $"{pixelValue2.ToString("0.00000")}{unitStr}", $"{pixelValue3.ToString("0.00000")}{unitStr}")
            };
            return str;
        }
        #region 序列化

        //public override DrawGeometrySerializerBase ToSerializer()
        //{
        //    var serializer = new DrawEllipseSerializer
        //    {
        //        Color = ((SolidColorBrush)pen.Brush).Color,
        //        StrokeThickness = pen.Thickness,
        //        Geometry = geometry.ToString()
        //    };

        //    if (geometry.Transform != null)
        //        serializer.Matrix = geometry.Transform.Value;

        //    return serializer;
        //}

        //public override void DeserializeFrom(DrawGeometrySerializerBase serializer)
        //{
        //    this.pen = new Pen(new SolidColorBrush(serializer.Color), serializer.StrokeThickness);

        //    this.geometry = Geometry.Parse(serializer.Geometry).GetFlattenedPathGeometry();
        //    this.geometry.Transform = new TranslateTransform(serializer.Matrix.OffsetX, serializer.Matrix.OffsetY);

        //    this.IsFinish = true;

        //    this.Draw();
        //}

        #endregion

        #region 字段
        //private Point? startPoint, endPoint;
        private Double radiusX, radiusY;
        //private Point center;
        private Point textPoint;
        private Point textPointDx;
        private Point textPointDy;
        private Double fontSize;
        private Typeface typeface;
        private FormattedText formattedText;
        private FormattedText formattedTextDx;
        private FormattedText formattedTextDy;
        private double pixelValue1;
        private double pixelValue2;
        private double pixelValue3;
        private double returnValue1;
        private double returnValue2;
        private double returnValue3;
        #endregion
    }
}
