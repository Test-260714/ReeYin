using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HardwareTool.Motion.Models
{
    /// <summary>
    /// 路径规划
    /// </summary>
    public class RoutePlanning
    {
        /// <summary>
        /// 重新排序直线列表，确保p1为起点，p2为终点，并优化路径连续性
        /// </summary>
        /// <param name="lines">原始直线列表</param>
        /// <returns>排序后的直线列表</returns>
        public List<(Point Start, Point End)> ReorderLines(List<(Point p1, Point p2)> lines)
        {
            if (lines == null || lines.Count <= 1)
                return new List<(Point p1, Point p2)>(lines ?? Enumerable.Empty<(Point, Point)>());

            // 复制原始列表以避免修改输入
            var remainingLines = new List<(Point p1, Point p2)>(lines);
            var orderedLines = new List<(Point p1, Point p2)>();

            // 选择初始直线（取第一条）
            var currentLine = remainingLines[0];
            remainingLines.RemoveAt(0);
            orderedLines.Add(currentLine);

            // 逐步找到下一条最优直线
            while (remainingLines.Count > 0)
            {
                // 当前直线的终点（作为下一条直线的起点参考）
                var lastEndPoint = orderedLines.Last().p2;

                double minDistance = double.MaxValue;
                int bestLineIndex = -1;
                bool bestIsReversed = false;

                // 寻找距离最近的下一条直线（考虑两种方向）
                for (int i = 0; i < remainingLines.Count; i++)
                {
                    var line = remainingLines[i];

                    // 计算两种方向的距离
                    double distanceToP1 = CalculateDistance(lastEndPoint, line.p1);
                    double distanceToP2 = CalculateDistance(lastEndPoint, line.p2);

                    // 找到更优的方向和直线
                    if (distanceToP1 < minDistance)
                    {
                        minDistance = distanceToP1;
                        bestLineIndex = i;
                        bestIsReversed = false; // 使用原始方向 p1->p2
                    }

                    if (distanceToP2 < minDistance)
                    {
                        minDistance = distanceToP2;
                        bestLineIndex = i;
                        bestIsReversed = true; // 反转方向 p2->p1（即新p1=原p2，新p2=原p1）
                    }
                }

                // 处理找到的最优直线
                var selectedLine = remainingLines[bestLineIndex];
                remainingLines.RemoveAt(bestLineIndex);

                // 根据需要反转直线方向，确保p1是起点，p2是终点
                if (bestIsReversed)
                {
                    orderedLines.Add((selectedLine.p2, selectedLine.p1));
                }
                else
                {
                    orderedLines.Add(selectedLine);
                }
            }

            return orderedLines;
        }

        /// <summary>
        /// 计算两点之间的欧氏距离
        /// </summary>
        private double CalculateDistance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

    }
}
