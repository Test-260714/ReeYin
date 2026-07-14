using ReeYin_V.Core.MovingRelated;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LineSegment = ReeYin_V.Core.MovingRelated.LineSegment;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 轨迹执行前的无状态计算入口，集中处理采样点、最短路径排序和软限位校验。
    /// </summary>
    internal static class WaferTrajectoryMotionHelper
    {
        public static List<(double X, double Y)> GenerateLineSamplePoints(
            double startX,
            double startY,
            double endX,
            double endY,
            double step)
        {
            if (step <= 0)
            {
                throw new ArgumentException("采样间距必须大于 0", nameof(step));
            }

            var points = new List<(double X, double Y)>();
            double dx = endX - startX;
            double dy = endY - startY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance == 0)
            {
                points.Add((startX, startY));
                return points;
            }

            double ux = dx / distance;
            double uy = dy / distance;
            int count = (int)Math.Floor(distance / step);

            for (int i = 0; i <= count; i++)
            {
                points.Add((startX + ux * step * i, startY + uy * step * i));
            }

            var last = points[^1];
            if (Math.Abs(last.X - endX) > 1e-6 || Math.Abs(last.Y - endY) > 1e-6)
            {
                points.Add((endX, endY));
            }

            return points;
        }

        public static List<LineSegment> BuildLineSegments(IEnumerable<LocusInfo>? locusInfos)
        {
            return (locusInfos ?? Enumerable.Empty<LocusInfo>())
                .Where(IsValidLocus)
                .Select(item => new LineSegment
                {
                    Start = new Point2D { X = item.OriginX, Y = item.OriginY },
                    End = new Point2D { X = item.TargetX, Y = item.TargetY },
                })
                .ToList();
        }

        public static List<LineSegment> BuildOrderedLineSegments(IEnumerable<LocusInfo>? locusInfos, bool isOptimalPathEnabled = true)
        {
            var rawSegments = BuildLineSegments(locusInfos);

            return isOptimalPathEnabled
                ? LayoutLocus.SortByShortestPath(rawSegments)
                : rawSegments;
        }

        public static List<LocusInfo> SortLocusInfosByShortestPath(IEnumerable<LocusInfo>? locusInfos)
        {
            var validLocusInfos = (locusInfos ?? Enumerable.Empty<LocusInfo>())
                .Where(IsValidLocus)
                .ToList();

            if (validLocusInfos.Count == 0)
            {
                return new List<LocusInfo>();
            }

            bool allPoints = validLocusInfos.All(item =>
                string.Equals(item.Type, LocusInfo.PointType, StringComparison.OrdinalIgnoreCase));

            if (allPoints)
            {
                return SortPointLocusInfosByShortestPath(validLocusInfos);
            }

            return BuildOrderedLineSegments(validLocusInfos)
                .Select(ToLineLocusInfo)
                .ToList();
        }

        public static List<LocusInfo> SortPointLocusInfosByShortestPath(IEnumerable<LocusInfo>? locusInfos)
        {
            var remaining = (locusInfos ?? Enumerable.Empty<LocusInfo>())
                .Where(IsValidLocus)
                .ToList();

            if (remaining.Count == 0)
            {
                return new List<LocusInfo>();
            }

            var ordered = new List<LocusInfo>(remaining.Count);
            Point2D current = new Point2D
            {
                X = remaining[0].TargetX,
                Y = remaining[0].TargetY
            };

            while (remaining.Count > 0)
            {
                LocusInfo? bestLocus = null;
                double minDistance = double.MaxValue;

                foreach (var locus in remaining)
                {
                    var target = new Point2D
                    {
                        X = locus.TargetX,
                        Y = locus.TargetY
                    };

                    double currentDistance = current.DistanceTo(target);
                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        bestLocus = locus;
                    }
                }

                if (bestLocus == null)
                {
                    break;
                }

                ordered.Add(bestLocus);
                current = new Point2D
                {
                    X = bestLocus.TargetX,
                    Y = bestLocus.TargetY
                };
                remaining.Remove(bestLocus);
            }

            return ordered;
        }

        public static void ReplaceLocusOrder(ObservableCollection<LocusInfo>? target, IEnumerable<LocusInfo> ordered)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            foreach (var locus in ordered)
            {
                target.Add(locus);
            }
        }

        public static List<Dictionary<En_AxisNum, double>> BuildLineTrajectoryTargets(IEnumerable<LineSegment> segments)
        {
            var targets = new List<Dictionary<En_AxisNum, double>>();
            foreach (var segment in segments)
            {
                targets.Add(new Dictionary<En_AxisNum, double>
                {
                    { En_AxisNum.X, segment.Start.X },
                    { En_AxisNum.Y, segment.Start.Y },
                    { En_AxisNum.Z2, 70 },
                });

                targets.Add(new Dictionary<En_AxisNum, double>
                {
                    { En_AxisNum.X, segment.End.X },
                    { En_AxisNum.Y, segment.End.Y },
                    { En_AxisNum.Z2, 70 },
                });
            }

            return targets;
        }

        public static List<Dictionary<En_AxisNum, double>> BuildPointTrajectoryTargets(IEnumerable<LocusInfo> locusInfos)
        {
            var targets = new List<Dictionary<En_AxisNum, double>>();
            foreach (var locusInfo in locusInfos)
            {
                targets.Add(new Dictionary<En_AxisNum, double>
                {
                    { En_AxisNum.X, locusInfo.TargetX },
                    { En_AxisNum.Y, locusInfo.TargetY },
                    { En_AxisNum.Z2, 70 },
                });
            }

            return targets;
        }

        public static bool HasAnyTrajectoryTargetOutOfSoftLimit(
            ControlCardBase controlCard,
            IEnumerable<Dictionary<En_AxisNum, double>> targets)
        {
            foreach (var target in targets)
            {
                foreach (var axisTarget in target)
                {
                    var axisConfig = controlCard.Config?.AllAxis?.FirstOrDefault(axis => axis.AxisNum == axisTarget.Key);
                    if (axisConfig == null)
                    {
                        Logs.LogWarning($"未找到{axisTarget.Key}轴配置，无法校验轨迹软限位，已取消执行。");
                        continue;
                        //return true;
                    }

                    if (axisTarget.Value < axisConfig.SoftLimitNegative || axisTarget.Value > axisConfig.SoftLimitPositive)
                    {
                        Logs.LogWarning(
                            $"轨迹目标超出{axisTarget.Key}轴软限位，目标值：{axisTarget.Value:F3}，" +
                            $"允许范围：[{axisConfig.SoftLimitNegative:F3}, {axisConfig.SoftLimitPositive:F3}]，已取消执行。");
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetMoveTarget(LocusInfo? locusInfo, out double targetX, out double targetY)
        {
            targetX = 0;
            targetY = 0;
            if (!IsValidLocus(locusInfo))
            {
                return false;
            }

            LocusInfo validLocus = locusInfo!;
            bool isPoint = string.Equals(validLocus.Type, LocusInfo.PointType, StringComparison.OrdinalIgnoreCase);
            targetX = isPoint ? validLocus.TargetX : validLocus.OriginX;
            targetY = isPoint ? validLocus.TargetY : validLocus.OriginY;
            return true;
        }

        public static ushort ConvertIoMaskToUInt16(ushort pointOperationIoMaskHex)
        {
            string ioMaskHex = pointOperationIoMaskHex.ToString("X");
            return Convert.ToUInt16(ioMaskHex, 16);
        }

        public static bool IsValidLocus(LocusInfo? locus)
        {
            return locus != null &&
                   double.IsFinite(locus.OriginX) &&
                   double.IsFinite(locus.OriginY) &&
                   double.IsFinite(locus.TargetX) &&
                   double.IsFinite(locus.TargetY);
        }

        private static LocusInfo ToLineLocusInfo(LineSegment segment)
        {
            return new LocusInfo
            {
                Type = LocusInfo.LineType,
                OriginX = segment.Start.X,
                OriginY = segment.Start.Y,
                TargetX = segment.End.X,
                TargetY = segment.End.Y
            };
        }
    }
}
