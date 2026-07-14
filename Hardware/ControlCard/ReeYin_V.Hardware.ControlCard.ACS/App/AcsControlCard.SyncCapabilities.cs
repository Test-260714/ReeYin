using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard : ICoordinatedMotionCard, IBufferedMotionCard, ISynchronizedTriggerCard
{
    public bool SupportsCoordinatedMotion => true;

    public bool SupportsBufferedMotion => true;

    public SynchronizedTriggerCapabilities TriggerCapabilities =>
        SynchronizedTriggerCapabilities.FixedDistancePulse |
        SynchronizedTriggerCapabilities.CoordinateArrayPulse |
        SynchronizedTriggerCapabilities.DataCollection;

    public bool MoveCoordinated(CoordinatedMotionRequest request, out string message)
    {
        message = string.Empty;
        if (request == null)
        {
            message = "Synchronized motion request cannot be null.";
            return false;
        }

        try
        {
            return request.Kind switch
            {
                CoordinatedMotionKind.Line => LineInterpoMoving(BuildLineParam(request)),
                CoordinatedMotionKind.XsegLine or CoordinatedMotionKind.Polyline => LineInterpoMovingXseg(BuildLineParam(request)),
                CoordinatedMotionKind.Arc when request.ArcParam != null => MoveCoordinatedArc(request),
                _ => FailUnsupportedCoordinatedMotion(request.Kind, out message)
            };
        }
        catch (Exception ex)
        {
            message = $"ACS synchronized motion failed: {ex.Message}";
            return false;
        }
    }

    public bool ClearMotionBuffer(short coordinateOrBuffer, out string message)
    {
        return TryClearProgramBuffer(coordinateOrBuffer, out message);
    }

    public bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message)
    {
        if (!TryRunProgramBuffer(coordinateOrBuffer, null, out message))
        {
            return false;
        }

        return !waitForEnd || WaitProgramBufferEnd(coordinateOrBuffer, MotionTimeoutMs, out message);
    }

    public bool RunSynchronizedTrigger(
        SynchronizedTriggerRequest request,
        out SynchronizedTriggerResult result,
        out string message)
    {
        result = new SynchronizedTriggerResult();
        message = string.Empty;
        if (request == null)
        {
            message = "Synchronized trigger request cannot be null.";
            result.Message = message;
            return false;
        }

        var success = request.Mode switch
        {
            SynchronizedTriggerMode.FixedDistancePulse => RunFixedDistancePulse(request, result, out message),
            SynchronizedTriggerMode.CoordinateArrayPulse => RunCoordinateArrayPulse(request, result, out message),
            SynchronizedTriggerMode.DataCollection => RunDataCollectionTrigger(request, result, out message),
            _ => FailUnsupportedTrigger(request.Mode, result, out message)
        };

        result.Success = success;
        result.Message = message;
        return success;
    }

    private LineInterPoParam BuildLineParam(CoordinatedMotionRequest request)
    {
        if (request.LineParam != null)
        {
            request.LineParam.waitforend = request.WaitForEnd;
            request.LineParam.BufferNo ??= request.BufferNo;
            request.LineParam.Timeout ??= request.Timeout;
            return request.LineParam;
        }

        return new LineInterPoParam
        {
            InterPoAxiss = request.Axes.ToList(),
            TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
            TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
            DefaultSpeed = request.SpeedType,
            waitforend = request.WaitForEnd,
            BufferNo = request.BufferNo,
            Timeout = request.Timeout
        };
    }

    private bool MoveCoordinatedArc(CoordinatedMotionRequest request)
    {
        request.ArcParam!.BufferNo ??= request.BufferNo;
        request.ArcParam!.Timeout ??= request.Timeout;
        request.ArcParam.waitforend = request.WaitForEnd;
        return ArcInterpoMoving(request.ArcParam);
    }

    private bool RunFixedDistancePulse(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        var param = new AcsLciFixedDistancePulseXsegParam
        {
            BufferNo = request.BufferNo,
            AxisX = ResolveAcsAxisNumber(request, En_AxisNum.X),
            AxisY = ResolveAcsAxisNumber(request, En_AxisNum.Y),
            PulseWidth = request.PulseWidth,
            Interval = request.Interval,
            StartDistance = request.StartDistance,
            EndDistance = request.EndDistance,
            MotionProfile = Options?.LciFixedDistancePulse?.ToMotionProfile() ?? LciSpeedSettings.CreateUnset(),
            RouteConfigOutput = request.RouteConfigOutput,
            ConfigOutputIndex = request.ConfigOutputIndex,
            ConfigOutputCode = request.ConfigOutputCode,
            Timeout = request.Timeout,
            Points = request.Points.Select(point => new AcsPoint2D(point.X, point.Y)).ToList()
        };

        if (!TryRunLciFixedDistancePulseXseg(param, out var pulseResult, out message))
        {
            return false;
        }

        result.PulseCount = pulseResult.PulseCount;
        result.PointCount = param.Points.Count;
        result.Data["Channel"] = pulseResult.Channel;
        result.Data["Script"] = pulseResult.Script;
        return true;
    }

    private bool RunCoordinateArrayPulse(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        var param = new AcsLciCoordinateArrayPulseParam
        {
            BufferNo = request.BufferNo,
            AxisX = ResolveAcsAxisNumber(request, En_AxisNum.X),
            AxisY = ResolveAcsAxisNumber(request, En_AxisNum.Y),
            PulseWidth = request.PulseWidth,
            MultiAxWinSize = request.TriggerWindow,
            MotionProfile = BuildCoordinateArrayMotionProfile(request),
            Velocity = request.Velocity,
            RouteConfigOutput = request.RouteConfigOutput,
            ConfigOutputIndex = request.ConfigOutputIndex,
            ConfigOutputCode = request.ConfigOutputCode,
            Timeout = request.Timeout,
            Points = request.Points.Select(point => new AcsPoint2D(point.X, point.Y)).ToList()
        };

        if (!TryRunLciCoordinateArrayPulse(param, out var pulseResult, out message))
        {
            return false;
        }

        result.PulseCount = pulseResult.PulseCount;
        result.PointCount = pulseResult.PointCount;
        result.Data["Channel"] = pulseResult.Channel;
        result.Data["Script"] = pulseResult.Script;
        return true;
    }

    private SpeedSetting BuildCoordinateArrayMotionProfile(SynchronizedTriggerRequest request)
    {
        var motionProfile = Options?.LciFixedDistancePulse?.ToMotionProfile() ?? LciSpeedSettings.CreateUnset();
        if (request.Velocity > 0d)
        {
            motionProfile.MaxSpeed = request.Velocity;
        }

        return motionProfile;
    }

    private bool RunDataCollectionTrigger(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        if (request.VendorRequest is not AcsDataCollectionRequest dataCollectionRequest)
        {
            message = "ACS DataCollection requires an AcsDataCollectionRequest vendor request.";
            return false;
        }

        if (!RunDataCollection(dataCollectionRequest, out var dataCollectionResult))
        {
            message = dataCollectionResult.Message;
            return false;
        }

        message = dataCollectionResult.Message;
        result.Data["DataCollection"] = dataCollectionResult;
        return true;
    }

    private int ResolveAcsAxisNumber(SynchronizedTriggerRequest request, En_AxisNum fallbackAxis)
    {
        var axisNum = request.Axes.FirstOrDefault(axis => axis == fallbackAxis);
        if (EqualityComparer<En_AxisNum>.Default.Equals(axisNum, default) && request.Axes.Count > 0)
        {
            axisNum = request.Axes[Math.Min(request.Axes.Count - 1, fallbackAxis == En_AxisNum.Y ? 1 : 0)];
        }

        return TryGetAxisConfig(axisNum, out var axis)
            ? GetZeroBasedAxisNo(axis)
            : fallbackAxis == En_AxisNum.Y ? 1 : 0;
    }

    private static bool FailUnsupportedCoordinatedMotion(CoordinatedMotionKind kind, out string message)
    {
        message = $"ACS does not support synchronized motion kind: {kind}.";
        return false;
    }

    private static bool FailUnsupportedTrigger(
        SynchronizedTriggerMode mode,
        SynchronizedTriggerResult result,
        out string message)
    {
        message = $"ACS does not support synchronized trigger mode: {mode}.";
        result.Message = message;
        return false;
    }
}
