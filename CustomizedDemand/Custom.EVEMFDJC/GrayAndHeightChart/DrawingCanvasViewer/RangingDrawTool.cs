using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Custom.EVEMFDJC
{
    /// <summary>
    /// 测距工具
    /// </summary>
    public sealed class RangingDrawTool : DrawGeometryBase
    {
        public RangingDrawTool(DrawingCanvas drawingCanvas) : base(drawingCanvas)
        {
            this.DrawingToolType = DrawToolType.Ranging;

            // 准备要处理的事件
            this.CanTouchDown = true;

            this.CanKeyDown = true;

            this.CanMouseDown = true;

            this.CanEdit = true;
        }

        #region 鼠标键盘事件
        public override Boolean OnMouseDown(MouseButtonEventArgs e)
        {
            // 检查是否按下了“Esc”键以取消绘制
            if (e.ChangedButton == MouseButton.Right)
            {
                Cencel();

                return true; // 返回 false 以指示绘制已被取消
            }
            return true;
        }
        public override Boolean OnTouchLeave(Point point)
        {
            if (!_endPoint.HasValue || (_startPoint.Value - _endPoint.Value).Length < pen.Thickness)
                this.drawingCanvas.DeleteVisual(this);
            else
            {
                var textGeometry = formattedText.BuildGeometry(textPoint);
                textGeometry.Transform = new RotateTransform(angle, _centerPoint.X, _centerPoint.Y);
                geometry = geometry.GetWidenedPathGeometry(pen);
                geometry = Geometry.Combine(geometry, textGeometry, GeometryCombineMode.Union, null);

                Draw();
            }

            this.drawingCanvas.DeleteWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanKeyDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;

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

                this.dpi = DpiHelper.GetDpiFromVisual(this.drawingCanvas);

                _startPoint = point;

                geometry = new PathGeometry();

                var figure = new PathFigure();
                pathGeometry.Figures.Add(figure);

                this.CanTouchMove = true;

                if (this.TouchId != 0 || !this.drawingCanvas.CaptureMouse())
                    this.CanTouchLeave = true;

                this.drawingCanvas.AddVisual(this);

                return true;
            }
            else
                return OnTouchLeave(point);
        }

        public override Boolean OnTouchMove(Point point)
        {
            var start = _startPoint.Value;

            if ((start - point).Length < this.drawingCanvas.StrokeThickness)
                return true;

            var dc = this.RenderOpen();

            _endPoint = point;

            var x = Math.Abs(point.X - start.X);
            var y = Math.Abs(point.Y - start.Y);
            var len = Math.Sqrt(x * x + y * y);
            pixelValue = len;
            returnValue = Math.Sqrt(x * x * PixelEquivalentX * PixelEquivalentX + y * y * PixelEquivalentY * PixelEquivalentY);
            var text = GetText();

            formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);  // 使用 PixelsPerDip 重载            
            _centerPoint = new Point((start.X + point.X) / 2, (start.Y + point.Y) / 2);
            var width = text.Length * fontSize / 2;     // 文字宽度
            textPoint = new Point(_centerPoint.X - width / 2, _centerPoint.Y /*+ fontSize * 1.2 + pen.Thickness*/);     // 文字左上角，1.2倍行高

            Double? k = null;       // 斜率

            if (start.X == point.X)
                angle = start.Y > point.Y ? 90 : -90;
            else
            {
                k = (point.Y - start.Y) / (point.X - start.X);
                angle = Math.Atan(k.Value) / Math.PI * 180;
            }
            dc.PushTransform(new RotateTransform(angle, _centerPoint.X, _centerPoint.Y));
            dc.DrawText(formattedText, textPoint);
            dc.Pop();
            var tangentK = k.HasValue ? (-1 / k) : 0;

            if (k.HasValue)
            {
                if (k.Value == 0)
                {
                    tangentK = null;
                }
            }

            var tangentLen = pen.Thickness + fontSize * 1.2;

            if (tangentK.HasValue)
            {
                var offsetX1 = Math.Sqrt(tangentLen * tangentLen / (1 + tangentK.Value * tangentK.Value)) * (angle > 0 ? 1 : -1);

                tangent1.X = offsetX1 + start.X;
                tangent1.Y = start.Y + offsetX1 * tangentK.Value;

                tangent2.X = offsetX1 + point.X;
                tangent2.Y = point.Y + offsetX1 * tangentK.Value;
            }
            else
            {
                tangent1.X = start.X;
                tangent1.Y = start.Y + (angle == 90 ? tangentLen : -tangentLen);

                tangent2.X = point.X;
                tangent2.Y = point.Y + (angle == 90 ? tangentLen : -tangentLen);
            }

            var figure = pathGeometry.Figures[0];
            figure.StartPoint = tangent1;
            figure.Segments.Clear();

            var line = new LineSegment(start, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);
            line = new LineSegment(point, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);
            line = new LineSegment(tangent2, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);

            dc.DrawGeometry(null, pen, geometry);
            dc.Close();

            return true;
        }
        public override void ReDraw(List<EditRectangle> newPosition)
        {
            var rightCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.Right);
            var leftCenterRect = newPosition.FirstOrDefault(m => m.EditPosition == EditPosition.Left);

            this.geometry = new PathGeometry();


            var dc = this.RenderOpen();
            _startPoint = new Point(leftCenterRect.X, leftCenterRect.Y);
            _endPoint = new Point(rightCenterRect.X, rightCenterRect.Y);
            _centerPoint = new Point((leftCenterRect.X + rightCenterRect.X) / 2, (leftCenterRect.Y + rightCenterRect.Y) / 2);

            var x = Math.Abs(rightCenterRect.X - leftCenterRect.X);
            var y = Math.Abs(rightCenterRect.Y - leftCenterRect.Y);
            var len = Math.Sqrt(x * x + y * y);

            pixelValue = len;
            returnValue = Math.Sqrt(x * x * PixelEquivalentX + y * y * PixelEquivalentY);
            var text = GetText();

            formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                this.fontSize,
                pen.Brush,
                Dpi.Cm2Wpf);

            var width = text.Length * fontSize / 2;
            textPoint = new Point(_centerPoint.X - width / 2, _centerPoint.Y /*- fontSize * 1.2 - pen.Thickness*/);     // 文字左上角，1.2倍行高

            Double? k = null;       // 斜率
            var start = _startPoint.Value;
            var point = _endPoint.Value;

            if (start.X == point.X)
                angle = start.Y > point.Y ? 90 : -90;
            else
            {
                k = (point.Y - start.Y) / (point.X - start.X);
                angle = Math.Atan(k.Value) / Math.PI * 180;
            }
            dc.PushTransform(new RotateTransform(angle, _centerPoint.X, _centerPoint.Y));
            dc.DrawText(formattedText, textPoint);
            dc.Pop();
            var tangentK = k.HasValue ? (-1 / k) : 0;
            if (k.HasValue)
            {
                if (k.Value == 0)
                {
                    tangentK = null;
                }
            }
            var tangentLen = pen.Thickness + fontSize * 1.2;

            if (tangentK.HasValue)
            {
                var offsetX1 = Math.Sqrt(tangentLen * tangentLen / (1 + tangentK.Value * tangentK.Value)) * (angle > 0 ? 1 : -1);

                tangent1.X = offsetX1 + start.X;
                tangent1.Y = start.Y + offsetX1 * tangentK.Value;

                tangent2.X = offsetX1 + point.X;
                tangent2.Y = point.Y + offsetX1 * tangentK.Value;
            }
            else
            {
                tangent1.X = start.X;
                tangent1.Y = start.Y + (angle == 90 ? tangentLen : -tangentLen);

                tangent2.X = point.X;
                tangent2.Y = point.Y + (angle == 90 ? tangentLen : -tangentLen);
            }

            var figure = new PathFigure();
            figure.StartPoint = new Point(_startPoint.Value.X, _centerPoint.Y);
            figure.StartPoint = tangent1;
            figure.Segments.Clear();
            pathGeometry.Figures.Add(figure);

            var line = new LineSegment(start, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);
            line = new LineSegment(point, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);

            line = new LineSegment(tangent2, true) { IsSmoothJoin = true };
            figure.Segments.Add(line);

            geometry = geometry.GetWidenedPathGeometry(pen);

            dc.DrawGeometry(pen.Brush, null, geometry);
            dc.Close();
        }
        public override void DoDrawEnd()
        {
            var e = new DrawToolEventArgs(this);
            e.PixelValue1 = returnValue;
            e.PhysicalValue1 = returnValue;
            DoDrawToolEndedEvent(this, e);
        }
        public override void Move(Double dx, Double dy)
        {
            base.Move(dx, dy);
            _startPoint = new Point(_startPoint.Value.X + dx, _startPoint.Value.Y + dy);
            _endPoint = new Point(_endPoint.Value.X + dx, _endPoint.Value.Y + dy);
            _centerPoint = new Point(_centerPoint.X + dx, _centerPoint.Y + dy);
            textPoint = new Point(textPoint.X + dx, textPoint.Y + dy);
            tangent1 = new Point(tangent1.X + dx, tangent1.Y + dy);
            tangent2 = new Point(tangent2.X + dx, tangent2.Y + dy);
        }
        #endregion
        private string GetText()
        {
            var unitStr = GetUnitStr();
            var str = UnitType switch
            {
                UnitType.Pixel => pixelValue.ToString("0.00000") + unitStr,
                UnitType.Millimeter => returnValue.ToString("0.00000") + unitStr,
                _ => pixelValue.ToString("0.00000")
            };
            return str;
        }
        #region 序列化

        //public override DrawGeometrySerializerBase ToSerializer()
        //{
        //    var serializer = new DrawRangingSerializer
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

        private Point textPoint, tangent1, tangent2;
        private Double fontSize;
        private Typeface typeface;
        private FormattedText formattedText;
        private Double angle;
        private Dpi dpi;
        private double pixelValue;
        private double returnValue;
        #endregion
    }
}
