using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.UI.Controls.OpenCvImg
{
    // ShapeType.cs
    public enum ShapeType
    {
        Rectangle,
        Circle,
        Ellipse,
        Line
    }

    // DrawingObjectInfo.cs
    // 用于存储绘制在 Canvas 上的 WPF 元素和相关参数
    public class DrawingObjectInfo
    {
        public ShapeType ShapeType { get; set; }
        public System.Windows.Shapes.Shape UiElement { get; set; } // 存储 WPF 形状元素 (Rectangle, Ellipse, Line)
        public double ScaleX { get; set; } // 绘制时的缩放比例 (用于坐标还原)
        public double ScaleY { get; set; }
        // 存储原始图像坐标（例如矩形的左上角和右下角）
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }
    }
}
