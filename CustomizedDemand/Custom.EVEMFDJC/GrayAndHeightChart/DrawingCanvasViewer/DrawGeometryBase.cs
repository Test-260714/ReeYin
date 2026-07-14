using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Custom.EVEMFDJC
{
    /// <summary>
    /// 画图几何图形基类
    /// </summary>
    public abstract class DrawGeometryBase : DrawingVisual, IDrawTool
    {
        public DrawGeometryBase(DrawingCanvas drawingCanvas)
        {
            this.drawingCanvas = drawingCanvas;
            this.Guid = Guid.NewGuid();
            this.PixelEquivalentX = drawingCanvas.PixelEquivalentX;
            this.PixelEquivalentY = drawingCanvas.PixelEquivalentY;
            this.UnitType = drawingCanvas.UnitType;
        }
        #region 鼠标键盘事件
        public virtual Boolean OnKeyDown(Key key) => false;

        public virtual Boolean OnKeyUp(Key key) => false;

        public virtual Boolean OnMouseDown(MouseButtonEventArgs e) => false;

        public virtual Boolean OnTouchDown(Int32 touchId, Point point) => false;

        public virtual Boolean OnTouchEnter(Point point) => false;

        public virtual Boolean OnTouchLeave(Point point) => false;

        public virtual Boolean OnTouchMove(Point point) => false;

        public virtual Boolean OnTouchUp(Point point) => false;

        public virtual Boolean OnDoubleClick(Point point) => false;

        #endregion

        #region 绘图事件

        public virtual void Draw()
        {
            DrawingContext dc = this.RenderOpen();
            dc.DrawGeometry(pen.Brush, null, geometry);
            dc.Close();
        }

        public virtual Boolean Erase(Geometry erase)
        {
            geometry = Geometry.Combine(geometry, erase, GeometryCombineMode.Exclude, null);

            if (geometry.IsEmpty())
                return true;

            Draw();

            return false;
        }

        public virtual Boolean Select(Point point)
        {
            return geometry.FillContains(point);
        }

        public virtual Boolean Select(Geometry select)
        {
            return !Geometry.Combine(geometry, select, GeometryCombineMode.Intersect, null).IsEmpty();
        }

        public virtual Rect Selected()
        {
            if (Mode == 1)
                return selectRect;

            Mode = 1;

            var dc = this.RenderOpen();

            dc.DrawGeometry(pen.Brush, null, geometry);

            selectRect = GetRenderBounds();

            dc.DrawRectangle(Brushes.Transparent, this.drawingCanvas.SelectBackgroundPen, selectRect);
            dc.DrawRectangle(null, this.drawingCanvas.SelectPen, selectRect);

            dc.Close();

            return selectRect;
        }

        public virtual void Unselected()
        {
            if (Mode == 0)
                return;

            Mode = 0;

            Draw();
        }

        public virtual Rect GetRenderBounds()
        {
            return geometry.GetRenderBounds(this.drawingCanvas.SelectPen);
        }
        /// <summary>
        /// 获取位置信息
        /// </summary>
        /// <returns></returns>
        public virtual List<Point> GetPosition()
        {
            return null;
        }

        public virtual void Move(Double dx, Double dy)
        {
            if (geometry.Transform == null)
                geometry.Transform = new TranslateTransform(dx, dy);
            else
            {
                var translate = (TranslateTransform)geometry.Transform;
                translate.X += dx;
                translate.Y += dy;
            }

            if (Mode == 1)
            {
                Mode = 0;
                Selected();
            }
            else
                Draw();
        }

        public virtual void Edit() { }

        public virtual void ReDraw(List<EditRectangle> newPosition) { }
        public virtual void DoDrawEnd() { }

        public void Cencel()
        {
            this.RenderOpen().Close();
            this.drawingCanvas.CencelWorkingDrawTool(this);

            this.IsFinish = true;

            this.CanTouchDown = false;
            this.CanTouchMove = false;
            this.CanTouchLeave = false;
            this.CanKeyDown = false;
            this.CanKeyUp = false;

            if (this.TouchId == 0 && this.drawingCanvas.IsMouseCaptured)
                this.drawingCanvas.ReleaseMouseCapture();
        }

        public void DoDrawToolEndedEvent(object sender, DrawToolEventArgs e)
        {
            DrawToolEndedEvent?.Invoke(sender, e);
        }

        public string GetUnitStr()
        {
            var str = UnitType switch
            {
                UnitType.Pixel => "Pixel",
                UnitType.Millimeter => "mm",
                _ => "Unknown"
            };

            return str;
        }
        #endregion

        #region 序列化

        //public virtual DrawGeometrySerializerBase ToSerializer() => null;

        //public virtual void DeserializeFrom(DrawGeometrySerializerBase serializer) { }

        #endregion

        #region 属性
        public Guid Guid { get; protected set; }

        /// <summary>
        /// X像素当量
        /// </summary>
        public Double PixelEquivalentX { get; set; } = 1;
        /// <summary>
        /// Y像素当量
        /// </summary>
        public Double PixelEquivalentY { get; set; } = 1;
        /// <summary>
        /// 单位
        /// </summary>
        public UnitType UnitType { get; set; }

        public Int32 TouchId { get; protected set; }

        public Boolean CanTouchEnter { get; protected set; }

        public Boolean CanTouchLeave { get; protected set; }

        public Boolean CanTouchDown { get; protected set; }

        public Boolean CanTouchMove { get; protected set; }

        public Boolean CanTouchUp { get; protected set; }

        public Boolean CanKeyDown { get; protected set; }

        public Boolean CanKeyUp { get; protected set; }

        public Boolean IsFinish { get; protected set; }

        public Boolean CanDoubleClick { get; protected set; }

        public Boolean CanMouseDown { get; protected set; }

        public DrawToolType DrawingToolType { get; protected set; }

        public Boolean CanEdit { get; protected set; }

        public Int32 Mode { get; protected set; }


        public Point? StartPoint
        {
            get => _startPoint;
            protected set => _startPoint = value;
        }

        public Point? EndPoint
        {
            get => _endPoint;
            protected set => _endPoint = value;
        }

        public Point CenterPoint
        {
            get => _centerPoint;
            protected set => _centerPoint = value;
        }

        #endregion

        #region 字段

        protected Point? _startPoint, _endPoint;
        protected Point _centerPoint;

        protected DrawingCanvas drawingCanvas;
        protected Geometry geometry;
        protected PathGeometry pathGeometry => (PathGeometry)geometry;
        protected Pen pen;
        protected Rect selectRect;

        public event DrawToolEventHandler DrawToolEndedEvent;

        #endregion
    }
}
