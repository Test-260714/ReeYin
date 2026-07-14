using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public bool RunInterpolationBufferScript(
        int? requestedBufferNo,
        string script,
        bool waitForEnd,
        int timeout,
        IEnumerable<En_AxisNum> axisIds,
        out string message)
    {
        var axisIdArray = axisIds?.ToArray() ?? Array.Empty<En_AxisNum>();
        if (!TryResolveInterpolationBufferNo(requestedBufferNo, out var bufferNo, out message))
        {
            return false;
        }

        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            message = "ACS interpolation buffer script cannot be empty.";
            return false;
        }

        try
        {
            TryStopProgramBuffer(bufferNo, out _);
            _api.LoadBuffer(buffer, script);
            _api.CompileBuffer(buffer);
            _api.RunBuffer(buffer, null);

            if (waitForEnd)
            {
                _api.WaitProgramEnd(buffer, Math.Max(1000, timeout));
                if (axisIdArray.Length > 0)
                {
                    UpdateAxisStates(axisIdArray);
                }
            }

            message = $"ACS interpolation buffer {bufferNo} completed.";
            return true;
        }
        catch (Exception ex)
        {
            TryStopProgramBuffer(bufferNo, out _);
            message = $"ACS interpolation buffer {bufferNo} failed: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine(message);
            return false;
        }
    }

    public static string BuildLineInterpolationBufferScript(Axis[] axes, double[] target, double velocity)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint: null, target, CreateInterpolationMotionProfile(velocity), pulseOutput: null);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity)
    {
        return BuildLineInterpolationBufferScript(axes, startPoint, target, velocity, pulseOutput: null);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, CreateInterpolationMotionProfile(velocity), pulseOutput);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        SpeedSetting motionProfile)
    {
        return BuildLineInterpolationBufferScript(axes, startPoint, target, motionProfile, pulseOutput: null);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        SpeedSetting motionProfile,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, motionProfile, pulseOutput);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity)
    {
        return BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity, pulseOutput: null);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, CreateInterpolationMotionProfile(velocity), pulseOutput);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        SpeedSetting motionProfile)
    {
        return BuildXsegLineInterpolationBufferScript(axes, startPoint, target, motionProfile, pulseOutput: null);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        SpeedSetting motionProfile,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, motionProfile, pulseOutput);
    }

    private static string BuildLinePtpLciInitBufferScript(
        Axis[] axes,
        double[]? startPoint,
        double[] target,
        SpeedSetting motionProfile,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var isPulseOutputEnabled = IsLineInterpolationPulseOutputEnabled(pulseOutput);
        if (isPulseOutputEnabled)
        {
            ValidateLineInterpolationPulseOutput(pulseOutput!);
        }

        var builder = new StringBuilder();
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("global int RY_LCI_CHANNEL");
            builder.AppendLine("global int RY_LCI_PULSE_COUNT");
            builder.AppendLine();
        }

        builder.AppendLine($"int AxX = {(int)axes[0]}");
        builder.AppendLine($"int AxY = {(int)axes[1]}");
        builder.AppendLine();
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("real PulseWidth");
            builder.AppendLine("real Interval");
        }

        builder.AppendLine("real XStartPos, YStartPos");
        builder.AppendLine("real XStopPos, YStopPos");
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("real PulseStartPos, PulseEndPos");
            builder.AppendLine("int ch");
        }

        builder.AppendLine();
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("lc.SetSafetyMasks(1, 1)");
            builder.AppendLine();
        }

        builder.AppendLine("ENABLE (AxX, AxY)");
        builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
        builder.AppendLine();
        var resolvedMotionProfile = ResolveLineInterpolationMotionProfile(motionProfile, fallbackVelocity: 10d);
        AppendAxisMotionTuning(builder, "AxX", resolvedMotionProfile);
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxY", resolvedMotionProfile);
        builder.AppendLine();
        AppendLinePtpStartPositionAssignments(builder, startPoint);
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine();
        builder.AppendLine("lc.Init()");
        builder.AppendLine();
        if (isPulseOutputEnabled)
        {
            AppendLineInterpolationPulseTimingAssignments(builder, pulseOutput!);
            builder.AppendLine();
        }

        AppendLinePtpStopPositionAssignments(builder, target);
        if (isPulseOutputEnabled)
        {
            builder.AppendLine();
            AppendLineInterpolationPulseWindowAndMode(builder, pulseOutput!, startPoint, target);
            builder.AppendLine();
            builder.AppendLine("lc.LaserEnable()");
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine();
        }

        builder.AppendLine("PTP/e (AxX, AxY), XStopPos, YStopPos");
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("lc.LaserDisable()");
            builder.AppendLine();
            builder.AppendLine("RY_LCI_CHANNEL = ch");
            builder.AppendLine("RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)");
            builder.AppendLine();
            builder.AppendLine("DISP \"Pulse count = %d\", RY_LCI_PULSE_COUNT");
            builder.AppendLine();
            builder.AppendLine("lc.Stop(ch)");
        }

        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildArcInterpolationBufferScript(
        Axis[] axes,
        double[] center,
        double[] finalPoint,
        DirOfRotation direction,
        double velocity)
    {
        var arcAxes = TakeArcAxes(axes);
        ValidateInterpolationAxesAndPoint(arcAxes, center, nameof(center));
        ValidateInterpolationAxesAndPoint(arcAxes, finalPoint, nameof(finalPoint));

        var builder = new StringBuilder();
        AppendAxisDeclarations(builder, arcAxes);
        builder.AppendLine();
        AppendEnableAndMotionTuning(builder, arcAxes, velocity);
        builder.AppendLine();
        builder.AppendLine($"ARC1 {FormatAxisTuple(arcAxes)}, {FormatPointList(center, arcAxes.Length)}, {FormatPointList(finalPoint, arcAxes.Length)}, {FormatRotation(direction)}");
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildXsegArcInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] center,
        double[] finalPoint,
        DirOfRotation direction,
        double velocity)
    {
        var arcAxes = TakeArcAxes(axes);
        ValidateInterpolationAxesAndPoint(arcAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(arcAxes, center, nameof(center));
        ValidateInterpolationAxesAndPoint(arcAxes, finalPoint, nameof(finalPoint));

        var builder = new StringBuilder();
        AppendAxisDeclarations(builder, arcAxes);
        builder.AppendLine();
        AppendEnableAndMotionTuning(builder, arcAxes, velocity);
        builder.AppendLine();
        builder.AppendLine($"XSEG {FormatAxisTuple(arcAxes)}, {FormatPointList(startPoint, arcAxes.Length)}");
        builder.AppendLine($"ARC1 {FormatAxisTuple(arcAxes)}, {FormatPointList(center, arcAxes.Length)}, {FormatPointList(finalPoint, arcAxes.Length)}, {FormatRotation(direction)}");
        builder.AppendLine($"ENDS {FormatAxisTuple(arcAxes)}");
        builder.AppendLine("TILL GSEG(Ax0) = -1");
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    private bool TryResolveInterpolationBufferNo(int? requestedBufferNo, out int bufferNo, out string message)
    {
        if (requestedBufferNo.HasValue)
        {
            if (requestedBufferNo.Value < 0 || requestedBufferNo.Value > 64)
            {
                bufferNo = 0;
                message = $"ACS interpolation Buffer {requestedBufferNo.Value} is invalid, valid range is 0..64.";
                return false;
            }

            bufferNo = requestedBufferNo.Value;
            message = string.Empty;
            return true;
        }

        bufferNo = Options.InterpolationBufferNo;
        message = string.Empty;
        return true;
    }

    private static void AppendLinePtpStartPositionAssignments(StringBuilder builder, double[]? startPoint)
    {
        builder.AppendLine($"XStartPos = {FormatLineStartCoordinate(startPoint, 0, "AxX")}");
        builder.AppendLine($"YStartPos = {FormatLineStartCoordinate(startPoint, 1, "AxY")}");
    }

    private static void AppendLinePtpStopPositionAssignments(StringBuilder builder, double[] target)
    {
        builder.AppendLine($"XStopPos = {FormatAcsNumber(target[0])}");
        builder.AppendLine($"YStopPos = {FormatAcsNumber(target[1])}");
    }

    private static void AppendLineInterpolationPulseTimingAssignments(
        StringBuilder builder,
        LineInterpolationPulseOutputParam pulseOutput)
    {
        builder.AppendLine($"PulseWidth = {FormatAcsNumber(pulseOutput.PulseWidth)}");
        builder.AppendLine($"Interval = {FormatAcsNumber(pulseOutput.Interval)}");
    }

    private static void AppendLineInterpolationPulseWindowAndMode(
        StringBuilder builder,
        LineInterpolationPulseOutputParam pulseOutput,
        double[]? startPoint,
        double[] target)
    {
        var pulseEndDistance = ResolveLineInterpolationPulseEndDistance(pulseOutput, startPoint, target);
        builder.AppendLine($"PulseStartPos = {FormatAcsNumber(pulseOutput.StartDistance)}");
        builder.AppendLine($"PulseEndPos = {FormatAcsNumber(pulseEndDistance)}");
        builder.AppendLine();
        builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
        builder.AppendLine("ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)");

        if (pulseOutput.RouteConfigOutput)
        {
            builder.AppendLine($"lc.SetConfigOut({pulseOutput.ConfigOutputIndex}, ch, {pulseOutput.ConfigOutputCode})");
        }
    }

    private static double ResolveLineInterpolationPulseEndDistance(
        LineInterpolationPulseOutputParam pulseOutput,
        double[]? startPoint,
        double[] target)
    {
        if (pulseOutput.EndDistance > pulseOutput.StartDistance)
        {
            return pulseOutput.EndDistance;
        }

        if (startPoint == null)
        {
            throw new ArgumentException("A start point is required when pulse end distance defaults to the move length.", nameof(startPoint));
        }

        var deltaX = target[0] - startPoint[0];
        var deltaY = target[1] - startPoint[1];
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static bool IsLineInterpolationPulseOutputEnabled(LineInterpolationPulseOutputParam? pulseOutput)
    {
        return pulseOutput?.IsEnabled == true;
    }

    private static void ValidateLineInterpolationPulseOutput(LineInterpolationPulseOutputParam pulseOutput)
    {
        if (!IsFinite(pulseOutput.PulseWidth) || pulseOutput.PulseWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.PulseWidth), "Pulse width must be positive.");
        }

        if (!IsFinite(pulseOutput.Interval) || pulseOutput.Interval <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.Interval), "Interval must be positive.");
        }

        if (!IsFinite(pulseOutput.StartDistance) || !IsFinite(pulseOutput.EndDistance))
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.EndDistance), "Start and end distances must be finite.");
        }

        if (pulseOutput.RouteConfigOutput && !IsValidLciConfigOutputIndex(pulseOutput.ConfigOutputIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.ConfigOutputIndex), "Config output index must be 0..7 or 10.");
        }
    }

    private static string FormatLineStartCoordinate(double[]? startPoint, int index, string axisName)
    {
        return startPoint == null ? $"FPOS({axisName})" : FormatAcsNumber(startPoint[index]);
    }

    private static string FormatAxisTuple(Axis[] axes)
    {
        return $"({string.Join(", ", Enumerable.Range(0, axes.Length).Select(index => $"Ax{index}"))})";
    }

    private static string FormatPointList(double[] point, int count)
    {
        return string.Join(", ", point.Take(count).Select(FormatAcsNumber));
    }

    private static string FormatRotation(DirOfRotation direction)
    {
        return (int)direction == 0 ? "CW" : "CCW";
    }

    private static void AppendAxisDeclarations(StringBuilder builder, Axis[] axes)
    {
        for (var index = 0; index < axes.Length; index++)
        {
            builder.AppendLine($"int Ax{index} = {(int)axes[index]}");
        }
    }

    private static void AppendEnableAndMotionTuning(StringBuilder builder, Axis[] axes, double velocity)
    {
        var axisTuple = FormatAxisTuple(axes);
        var resolvedVelocity = IsFinite(velocity) && velocity > 0d ? Math.Abs(velocity) : 10d;
        var motionProfile = CreateInterpolationMotionProfile(resolvedVelocity);
        builder.AppendLine($"ENABLE {axisTuple}");
        builder.AppendLine($"TILL ( {string.Join(" & ", Enumerable.Range(0, axes.Length).Select(index => $"MST(Ax{index}).#ENABLED"))} )");
        builder.AppendLine();

        for (var index = 0; index < axes.Length; index++)
        {
            AppendAxisMotionTuning(builder, $"Ax{index}", motionProfile);
            if (index < axes.Length - 1)
            {
                builder.AppendLine();
            }
        }
    }

    private SpeedSetting ResolveLineInterpolationWorkMotionProfile(IEnumerable<En_AxisNum> axisIds, double fallbackVelocity)
    {
        var workSpeed = axisIds?
            .Distinct()
            .Select(axisId => ResolveSpeedSetting(axisId, EN_SpeedType.Work))
            .FirstOrDefault(speed => speed != null);

        return ResolveLineInterpolationMotionProfile(workSpeed, fallbackVelocity);
    }

    private static SpeedSetting ResolveLineInterpolationMotionProfile(SpeedSetting? motionProfile, double fallbackVelocity)
    {
        var fallback = CreateInterpolationMotionProfile(fallbackVelocity);
        if (motionProfile == null)
        {
            return fallback;
        }

        return new SpeedSetting
        {
            SpeedType = motionProfile.SpeedType,
            SpeedDescribe = motionProfile.SpeedDescribe,
            StartSpeed = ResolveLineInterpolationSpeed(motionProfile.StartSpeed, fallback.StartSpeed),
            MaxSpeed = ResolveLineInterpolationSpeed(motionProfile.MaxSpeed, fallback.MaxSpeed),
            AccSpeed = ResolveLineInterpolationSpeed(motionProfile.AccSpeed, fallback.AccSpeed),
            DecSpeed = ResolveLineInterpolationSpeed(motionProfile.DecSpeed, fallback.DecSpeed),
            KillDecSpeed = ResolveLineInterpolationSpeed(motionProfile.KillDecSpeed, fallback.KillDecSpeed),
            Jerk = ResolveLineInterpolationSpeed(motionProfile.Jerk, fallback.Jerk)
        };
    }

    private static double ResolveLineInterpolationSpeed(double configured, double fallback)
    {
        return IsFinite(configured) && configured > 0d ? Math.Abs(configured) : fallback;
    }

    private static SpeedSetting CreateInterpolationMotionProfile(double velocity)
    {
        var resolvedVelocity = IsFinite(velocity) && velocity > 0d ? Math.Abs(velocity) : 10d;
        return new SpeedSetting
        {
            MaxSpeed = resolvedVelocity,
            AccSpeed = resolvedVelocity * 10d,
            DecSpeed = resolvedVelocity * 10d,
            KillDecSpeed = resolvedVelocity * 100d,
            Jerk = resolvedVelocity * 100d
        };
    }

    private static Axis[] TakeArcAxes(Axis[] axes)
    {
        if (axes == null || axes.Length < 2)
        {
            throw new ArgumentException("ACS arc interpolation requires at least two axes.", nameof(axes));
        }

        return axes.Take(2).ToArray();
    }

    private static Axis[] TakeLineAxes(Axis[] axes)
    {
        if (axes == null || axes.Length < 2)
        {
            throw new ArgumentException("ACS line interpolation requires at least two axes.", nameof(axes));
        }

        return axes.Take(2).ToArray();
    }

    private static void ValidateInterpolationAxesAndPoint(Axis[] axes, double[] point, string pointName)
    {
        if (axes == null || axes.Length < 2)
        {
            throw new ArgumentException("ACS interpolation requires at least two axes.", nameof(axes));
        }

        if (point == null || point.Length < axes.Length)
        {
            throw new ArgumentException("ACS interpolation point dimension is less than axis count.", pointName);
        }

        if (axes.Any(axis => axis == Axis.ACSC_NONE))
        {
            throw new ArgumentException("ACS interpolation axes cannot contain ACSC_NONE.", nameof(axes));
        }

        if (axes.Distinct().Count() != axes.Length)
        {
            throw new ArgumentException("ACS interpolation axes cannot contain duplicate physical axes.", nameof(axes));
        }

        if (point.Take(axes.Length).Any(value => !IsFinite(value)))
        {
            throw new ArgumentException("ACS interpolation points must be finite.", pointName);
        }
    }
}
