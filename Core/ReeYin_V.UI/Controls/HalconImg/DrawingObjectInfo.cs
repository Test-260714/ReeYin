using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.UI.Controls
{
    public enum ShapeType
    {
        Rectangle,
        Ellipse,
        Circle,
        Region,
        Mask
    }

    public class DrawingObjectInfo
    {
        public ShapeType ShapeType { get; set; }

        public HObject Hobject { get; set; }

        public HTuple[] HTuples { get; set; }

        public string Color { get; set; } = "yellow";

        public bool IsFillDisplay { get; set; }
    }
}
