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
    public override bool LineInterpoMoving(LineInterPoParam param)
    {
        if (!IsConnected || param == null)
        {
            return false;
        }

        try
        {
            if (!TryBuildInterpolationMove(param.InterPoAxiss, param.TargetPosDic, param.TargetPos, out var axisIds, out var axes, out var target))
            {
                return false;
            }

            if (!PrepareInterpolationAxes(axisIds, axes))
            {
                return false;
            }

            ConfigureInterpolationAxes(axisIds, EN_SpeedType.Work);
            if (!TryGetCurrentInterpolationPoint(axisIds, out var startPoint))
            {
                return false;
            }

            var motionProfile = ResolveLineInterpolationWorkMotionProfile(axisIds, GetInterpolationVelocity(axisIds, EN_SpeedType.Work));
            var script = BuildLineInterpolationBufferScript(axes, startPoint, target, motionProfile, param.PulseOutput);
            if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, param.Timeout ?? InternalTimeout, axisIds, out var message))
            {
                Console.WriteLine(message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS LineInterpoMoving failed: {ex.Message}");
            return false;
        }
    }

    public bool LineInterpoMovingXseg(LineInterPoParam param)
    {
        if (!IsConnected || param == null)
        {
            return false;
        }

        try
        {
            if (!TryBuildInterpolationMove(param.InterPoAxiss, param.TargetPosDic, param.TargetPos, out var axisIds, out var axes, out var target))
            {
                return false;
            }

            if (!PrepareInterpolationAxes(axisIds, axes))
            {
                return false;
            }

            ConfigureInterpolationAxes(axisIds, EN_SpeedType.Work);
            if (!TryGetCurrentInterpolationPoint(axisIds, out var startPoint))
            {
                return false;
            }

            var motionProfile = ResolveLineInterpolationWorkMotionProfile(axisIds, GetInterpolationVelocity(axisIds, EN_SpeedType.Work));
            var script = BuildXsegLineInterpolationBufferScript(axes, startPoint, target, motionProfile, param.PulseOutput);
            if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, param.Timeout ?? InternalTimeout, axisIds, out var message))
            {
                Console.WriteLine(message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS LineInterpoMovingXseg failed: {ex.Message}");
            return false;
        }
    }

    private bool TryBuildInterpolationMove(
        IReadOnlyList<En_AxisNum> requestedAxisIds,
        IReadOnlyDictionary<En_AxisNum, double>? targetPositionMap,
        IReadOnlyList<double>? targetPositions,
        out En_AxisNum[] axisIds,
        out Axis[] axes,
        out double[] points)
    {
        axisIds = Array.Empty<En_AxisNum>();
        axes = Array.Empty<Axis>();
        points = Array.Empty<double>();

        if (requestedAxisIds == null || requestedAxisIds.Count < 2)
        {
            Console.WriteLine("ACS interpolation requires at least two axes.");
            return false;
        }

        axisIds = requestedAxisIds.Distinct().ToArray();
        if (axisIds.Length != requestedAxisIds.Count)
        {
            Console.WriteLine("ACS interpolation axes cannot contain duplicates.");
            return false;
        }

        if (!TryBuildAxes(axisIds, out axes))
        {
            return false;
        }

        points = new double[axisIds.Length];
        var targetForLimitValidation = new Dictionary<En_AxisNum, double>();
        if (targetPositionMap != null)
        {
            foreach (var item in targetPositionMap)
            {
                targetForLimitValidation[item.Key] = item.Value;
            }
        }

        for (var i = 0; i < axisIds.Length; i++)
        {
            if (targetPositionMap != null && targetPositionMap.TryGetValue(axisIds[i], out var mappedPosition))
            {
                points[i] = mappedPosition;
            }
            else if (targetPositions != null && i < targetPositions.Count)
            {
                points[i] = targetPositions[i];
            }
            else
            {
                Console.WriteLine($"ACS interpolation target for axis {axisIds[i]} is missing.");
                return false;
            }

            if (!IsFinite(points[i]))
            {
                Console.WriteLine($"ACS interpolation target for axis {axisIds[i]} is invalid.");
                return false;
            }

            targetForLimitValidation[axisIds[i]] = points[i];
        }

        if (!ValidateLimitPosition(targetForLimitValidation, out var limitMessage))
        {
            Console.WriteLine($"ACS interpolation limit validation failed: {limitMessage}");
            return false;
        }

        return true;
    }

    private bool TryBuildAxes(IEnumerable<En_AxisNum> axisIds, out Axis[] axes)
    {
        var axisList = new List<Axis>();
        foreach (var axisId in axisIds)
        {
            if (!TryGetAxisConfig(axisId, out var axisConfig))
            {
                Console.WriteLine($"ACS interpolation axis {axisId} is not configured.");
                axes = Array.Empty<Axis>();
                return false;
            }

            axisList.Add(ToConfiguredAcsAxis(axisConfig));
        }

        axes = axisList.ToArray();
        return true;
    }

    private bool PrepareInterpolationAxes(IEnumerable<En_AxisNum> axisIds, IEnumerable<Axis> axes)
    {
        foreach (var axisId in axisIds)
        {
            if (!DoGetAxisEnable(axisId))
            {
                Console.WriteLine($"ACS interpolation axis {axisId} is not enabled.");
                return false;
            }

            if (!DoGetAxisStopped(axisId))
            {
                Console.WriteLine($"ACS interpolation axis {axisId} is moving.");
                return false;
            }
        }

        foreach (var axis in axes)
        {
            if (!PrepareMotorToMove(axis))
            {
                return false;
            }
        }

        return true;
    }
}
