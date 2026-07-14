using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.MovingRelated
{
    public class LayoutLocus
    {
        /// <summary>
        /// 多线段最优执行路径
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="startPoint"></param>
        /// <returns></returns>
        public static List<LineSegment> SortByShortestPath( List<LineSegment> segments,
    Point2D? startPoint = null)
        {
            if (segments == null || segments.Count == 0)
                return new List<LineSegment>();

            var remaining = new List<LineSegment>(segments);
            var result = new List<LineSegment>();

            // 起始点（默认使用第一条线段的起点）
            Point2D current = startPoint ?? remaining[0].Start;

            while (remaining.Count > 0)
            {
                double minDist = double.MaxValue;
                LineSegment bestSegment = null;
                bool reverse = false;

                foreach (var seg in remaining)
                {
                    double dStart = current.DistanceTo(seg.Start);
                    double dEnd = current.DistanceTo(seg.End);

                    if (dStart < minDist)
                    {
                        minDist = dStart;
                        bestSegment = seg;
                        reverse = false;
                    }

                    if (dEnd < minDist)
                    {
                        minDist = dEnd;
                        bestSegment = seg;
                        reverse = true;
                    }
                }

                var chosen = reverse ? bestSegment.Reverse() : bestSegment;

                result.Add(chosen);
                current = chosen.End;
                remaining.Remove(bestSegment);
            }

            return result;
        }

    }
}
