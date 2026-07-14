using ACS.SPiiPlusNET;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public override bool ArcInterpoMoving(ArcInterPoParam param)
    {
        if (!IsConnected || param == null)
        {
            return false;
        }

        try
        {
            var axisIds = (param.InterPoAxiss == null || param.InterPoAxiss.Count == 0)
                ? new[] { En_AxisNum.X, En_AxisNum.Y }
                : param.InterPoAxiss.Take(2).ToArray();

            if (axisIds.Length != 2 || !TryBuildAxes(axisIds, out var axes))
            {
                return false;
            }

            var targets = new Dictionary<En_AxisNum, double>
            {
                [axisIds[0]] = param.Destination.X,
                [axisIds[1]] = param.Destination.Y
            };

            if (param.FinalPosDic != null)
            {
                foreach (var targetPosition in param.FinalPosDic)
                {
                    targets[targetPosition.Key] = targetPosition.Value;
                }
            }

            if (!ValidateLimitPosition(targets, out var limitMessage))
            {
                Console.WriteLine($"ACS ArcInterpoMoving limit validation failed: {limitMessage}");
                return false;
            }

            var finalPoint = new[] { targets[axisIds[0]], targets[axisIds[1]] };
            var startPoint = new[] { param.Origin.X, param.Origin.Y };
            if (!TryResolveArcCenter(param, startPoint, finalPoint, out var center))
            {
                return false;
            }

            if (!PrepareInterpolationAxes(axisIds, axes))
            {
                return false;
            }

            ConfigureInterpolationAxes(axisIds, param.DefaultSpeed);
            var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
            var script = BuildArcInterpolationBufferScript(axes, center, finalPoint, param.Dir, velocity);
            if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, param.Timeout ?? InternalTimeout, axisIds, out var message))
            {
                Console.WriteLine(message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS ArcInterpoMoving failed: {ex.Message}");
            return false;
        }
    }

    public bool ArcInterpoMovingXseg(ArcInterPoParam param)
    {
        if (!IsConnected || param == null)
        {
            return false;
        }

        try
        {
            if (param.InterPoAxiss != null && param.InterPoAxiss.Count > 2)
            {
                Console.WriteLine("ACS ArcInterpoMovingXseg requires at most two interpolation axes.");
                return false;
            }

            var axisIds = (param.InterPoAxiss == null || param.InterPoAxiss.Count == 0)
                ? new[] { En_AxisNum.X, En_AxisNum.Y }
                : param.InterPoAxiss.Take(2).ToArray();

            if (axisIds.Length != 2 || axisIds.Distinct().Count() != 2)
            {
                Console.WriteLine("ACS ArcInterpoMovingXseg requires two distinct interpolation axes.");
                return false;
            }

            if (!TryBuildAxes(axisIds, out var axes))
            {
                return false;
            }

            var targets = new Dictionary<En_AxisNum, double>
            {
                [axisIds[0]] = param.Destination.X,
                [axisIds[1]] = param.Destination.Y
            };

            if (param.FinalPosDic != null)
            {
                foreach (var targetPosition in param.FinalPosDic)
                {
                    targets[targetPosition.Key] = targetPosition.Value;
                }
            }

            if (!ValidateLimitPosition(targets, out var limitMessage))
            {
                Console.WriteLine($"ACS ArcInterpoMovingXseg limit validation failed: {limitMessage}");
                return false;
            }

            var finalPoint = new[] { targets[axisIds[0]], targets[axisIds[1]] };
            if (!TryGetCurrentInterpolationPoint(axisIds, out var startPoint))
            {
                return false;
            }

            if (!TryResolveArcCenter(param, startPoint, finalPoint, out var center))
            {
                return false;
            }

            if (!PrepareInterpolationAxes(axisIds, axes))
            {
                return false;
            }

            ConfigureInterpolationAxes(axisIds, param.DefaultSpeed);
            var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
            var script = BuildXsegArcInterpolationBufferScript(axes, startPoint, center, finalPoint, param.Dir, velocity);
            if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, param.Timeout ?? InternalTimeout, axisIds, out var message))
            {
                Console.WriteLine(message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS ArcInterpoMovingXseg failed: {ex.Message}");
            return false;
        }
    }

    private static bool TryResolveArcCenter(ArcInterPoParam param, double[] startPoint, double[] finalPoint, out double[] center)
    {
        center = Array.Empty<double>();
        if (startPoint.Length < 2 || finalPoint.Length < 2)
        {
            return false;
        }

        switch (param.DrawArcMethod)
        {
            case DrawArc.Center:
                center = new[] { param.Center.X, param.Center.Y };
                return IsFinite(center[0]) && IsFinite(center[1]);

            case DrawArc.Radius:
                return TryResolveCenterFromRadius(
                    startPoint[0],
                    startPoint[1],
                    finalPoint[0],
                    finalPoint[1],
                    param.Radius,
                    (int)param.Dir == 0,
                    false,
                    out center);

            case DrawArc.Angle:
                var angle = Math.Abs(param.Angle);
                if (angle <= 0d || angle >= 360d)
                {
                    return false;
                }

                var chordLength = GetDistance(startPoint[0], startPoint[1], finalPoint[0], finalPoint[1]);
                var radius = chordLength / (2d * Math.Sin(angle * Math.PI / 360d));
                return TryResolveCenterFromRadius(
                    startPoint[0],
                    startPoint[1],
                    finalPoint[0],
                    finalPoint[1],
                    radius,
                    (int)param.Dir == 0,
                    angle > 180d,
                    out center);

            default:
                return false;
        }
    }

    private static bool TryResolveCenterFromRadius(
        double startX,
        double startY,
        double endX,
        double endY,
        double radius,
        bool clockwise,
        bool useMajorArc,
        out double[] center)
    {
        center = Array.Empty<double>();
        var chordX = endX - startX;
        var chordY = endY - startY;
        var chordLength = Math.Sqrt((chordX * chordX) + (chordY * chordY));
        var radiusMagnitude = Math.Abs(radius);
        if (chordLength <= double.Epsilon || radiusMagnitude <= double.Epsilon || chordLength > (2d * radiusMagnitude))
        {
            return false;
        }

        var midX = (startX + endX) / 2d;
        var midY = (startY + endY) / 2d;
        var halfChord = chordLength / 2d;
        var centerOffset = Math.Sqrt(Math.Max(0d, (radiusMagnitude * radiusMagnitude) - (halfChord * halfChord)));
        var normalX = -chordY / chordLength;
        var normalY = chordX / chordLength;
        var side = clockwise ? -1d : 1d;
        if (useMajorArc)
        {
            side = -side;
        }

        center = new[]
        {
            midX + (normalX * centerOffset * side),
            midY + (normalY * centerOffset * side)
        };
        return IsFinite(center[0]) && IsFinite(center[1]);
    }

    private static double GetDistance(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
