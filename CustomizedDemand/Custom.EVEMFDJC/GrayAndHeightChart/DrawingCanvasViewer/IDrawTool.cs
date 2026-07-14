using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Custom.EVEMFDJC
{
    public delegate void DrawToolEventHandler(object sender, DrawToolEventArgs e);
    /// <summary>
    /// 绘制工具接口
    /// </summary>
    public interface IDrawTool
    {
        /// <summary>
        /// 工具绘制完成事件
        /// </summary>
        event DrawToolEventHandler DrawToolEndedEvent;
        /// <summary>
        /// 执行绘制完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DoDrawToolEndedEvent(object sender, DrawToolEventArgs e);
        /// <summary>
        /// Guid
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// 触摸Id，用于分辨多点触摸，0表示鼠标
        /// </summary>
        Int32 TouchId { get; }

        /// <summary>
        /// 是否可以处理鼠标进入事件
        /// </summary>
        Boolean CanTouchEnter { get; }

        /// <summary>
        /// 处理鼠标进入事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchEnter(Point point);

        /// <summary>
        /// 是否可以处理鼠标离开事件
        /// </summary>
        Boolean CanTouchLeave { get; }

        /// <summary>
        /// 处理鼠标离开事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchLeave(Point point);

        /// <summary>
        /// 是否可以处理鼠标按下事件
        /// </summary>
        Boolean CanTouchDown { get; }

        /// <summary>
        /// 执行鼠标按下事件
        /// </summary>
        /// <param name="touchId">触摸Id，用于分辨多点触摸，0表示鼠标</param>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchDown(Int32 touchId, Point point);

        /// <summary>
        /// 是否可以处理鼠标移动事件
        /// </summary>
        Boolean CanTouchMove { get; }

        /// <summary>
        /// 执行鼠标移动事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchMove(Point point);

        /// <summary>
        /// 是否可以处理鼠标弹起事件
        /// </summary>
        Boolean CanTouchUp { get; }

        Boolean CanDoubleClick { get; }

        Boolean CanMouseDown { get; }

        /// <summary>
        /// 执行鼠标弹起事件
        /// </summary>
        /// <param name="point">相对画布的点</param>
        /// <returns>事件是否已处理</returns>
        Boolean OnTouchUp(Point point);

        Boolean OnDoubleClick(Point point);
        Boolean OnMouseDown(MouseButtonEventArgs e);

        /// <summary>
        /// 是否可以处理键盘按下事件
        /// </summary>
        Boolean CanKeyDown { get; }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        /// <param name="key"></param>
        /// <returns>事件是否已处理</returns>
        Boolean OnKeyDown(Key key);

        /// <summary>
        /// 是否可以处理键盘弹起事件
        /// </summary>
        Boolean CanKeyUp { get; }

        /// <summary>
        /// 处理键盘弹起事件
        /// </summary>
        /// <param name="key"></param>
        /// <returns>事件是否已处理</returns>
        Boolean OnKeyUp(Key key);

        /// <summary>
        /// 是否结束
        /// </summary>
        Boolean IsFinish { get; }

        /// <summary>
        /// 画图工具类型
        /// </summary>
        DrawToolType DrawingToolType { get; }
    }

    public class DrawToolEventArgs
    {
        /// <summary>
        /// 画图工具类型
        /// </summary>
        public DrawToolType DrawingToolType { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public UnitType UnitType { get; set; }
        /// <summary>
        /// Guid
        /// </summary>
        public Guid Guid { get; set; }
        /// <summary>
        /// 起始点
        /// </summary>
        public Point StartPoint { get; set; }
        /// <summary>
        /// 结束点
        /// </summary>
        public Point EndPoint { get; set; }
        /// <summary>
        /// 中心点
        /// </summary>
        public Point CenterPoint { get; set; }
        /// <summary>
        /// X像素当量
        /// </summary>
        public Double PixelEquivalentX { get; set; }
        /// <summary>
        /// Y像素当量
        /// </summary>
        public Double PixelEquivalentY { get; set; }
        public double PixelValue1 { get; set; }
        public double PixelValue2 { get; set; }
        public double PixelValue3 { get; set; }
        /// <summary>
        /// 结果1
        /// </summary>
        public double PhysicalValue1 { get; set; }
        /// <summary>
        /// 结果2
        /// </summary>
        public double PhysicalValue2 { get; set; }
        /// <summary>
        /// 结果3
        /// </summary>
        public double PhysicalValue3 { get; set; }

        public Dictionary<string, object> DicParams = new Dictionary<string, object>();

        public DrawToolEventArgs(DrawGeometryBase geometry)
        {
            Guid = geometry.Guid;
            DrawingToolType = geometry.DrawingToolType;
            StartPoint = geometry.StartPoint ?? new Point(0, 0);
            EndPoint = geometry.EndPoint ?? new Point(0, 0);
            CenterPoint = geometry.CenterPoint;
            PixelEquivalentX = geometry.PixelEquivalentX;
            PixelEquivalentY = geometry.PixelEquivalentY;
        }

        public void SetPara<T>(string key, T value)
        {
            DicParams[key] = value;
        }

        public T GetPara<T>(string key, T defaultValue)
        {
            if (DicParams.ContainsKey(key))
            {
                if (DicParams[key] is T)
                    return (T)DicParams[key];
                else
                    return defaultValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// 画图工具类型
    /// </summary>
    public enum DrawToolType
    {
        /// <summary>
        /// 指针（拾取）
        /// </summary>
        Pointer,
        /// <summary>
        /// 编辑
        /// </summary>
        Edit,
        /// <summary>
        /// 画笔
        /// </summary>
        Pen,
        /// <summary>
        /// 橡皮擦
        /// </summary>
        Eraser,
        /// <summary>
        /// 测距
        /// </summary>
        Ranging,
        /// <summary>
        /// 直线
        /// </summary>
        Line,
        /// <summary>
        /// 箭头
        /// </summary>
        Arrow,
        /// <summary>
        /// 矩形
        /// </summary>
        Rectangle,
        /// <summary>
        /// 椭圆
        /// </summary>
        Ellipse,
        /// <summary>
        /// 角度
        /// </summary>
        Angle,
        /// <summary>
        /// 折线
        /// </summary>
        Polyline,
        /// <summary>
        /// 曲线
        /// </summary>
        Curve,
        /// <summary>
        /// 多边形
        /// </summary>
        Polygon,
        /// <summary>
        /// 闭合曲线
        /// </summary>
        ClosedCurve,
        /// <summary>
        /// 面积
        /// </summary>
        Area,
        /// <summary>
        /// 文字
        /// </summary>
        Text
    }

    /// <summary>
    /// 单位
    /// </summary>
    public enum UnitType
    {
        /// <summary>
        /// 像素
        /// </summary>
        Pixel,
        /// <summary>
        /// 毫米
        /// </summary>
        Millimeter,
    }
}
