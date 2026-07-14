using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.MovingRelated
{
    public struct Point2D
    {
        public double X;
        public double Y;

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class LineSegment
    {
        public Point2D Start { get; set; }
        public Point2D End { get; set; }

        public LineSegment Reverse()
        {
            return new LineSegment
            {
                Start = this.End,
                End = this.Start
            };
        }
    }

}
