using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

internal static class LciSpeedSettings
{
    public static SpeedSetting CreateUnset()
    {
        return new SpeedSetting
        {
            SpeedType = EN_SpeedType.Work,
            SpeedDescribe = "Work"
        };
    }
}

public sealed class AcsLciFixedDistancePulseXsegParam
{
    private SpeedSetting _motionProfile = LciSpeedSettings.CreateUnset();

    public int BufferNo { get; set; } = 10;

    public int AxisX { get; set; }

    public int AxisY { get; set; } = 1;

    public double PulseWidth { get; set; } = 0.01d;

    public double Interval { get; set; } = 1d;

    public double StartDistance { get; set; }

    public double EndDistance { get; set; }

    public SpeedSetting MotionProfile
    {
        get => _motionProfile;
        set => _motionProfile = value ?? LciSpeedSettings.CreateUnset();
    }

    public bool RouteConfigOutput { get; set; } = true;

    public int ConfigOutputIndex { get; set; }

    public int ConfigOutputCode { get; set; } = 7;

    public int Timeout { get; set; } = 60000;

    public List<AcsPoint2D> Points { get; set; } =
    [
        new(0d, 0d),
        new(100d, 0d),
        new(100d, 100d)
    ];
}

public sealed class AcsLciSegmentCircleParam
{
    private SpeedSetting _motionProfile = LciSpeedSettings.CreateUnset();

    public int BufferNo { get; set; } = 10;

    public int AxisX { get; set; }

    public int AxisY { get; set; } = 1;

    public SpeedSetting MotionProfile
    {
        get => _motionProfile;
        set => _motionProfile = value ?? LciSpeedSettings.CreateUnset();
    }

    public double Velocity
    {
        get => MotionProfile.MaxSpeed;
        set => MotionProfile.MaxSpeed = value;
    }

    public double StartX { get; set; }

    public double StartY { get; set; }

    public double CenterX { get; set; } = 10d;

    public double CenterY { get; set; } = 5d;

    public double Radius { get; set; } = 5d;

    public int GateActiveState { get; set; } = 1;

    public int Timeout { get; set; } = 60000;
}

public sealed class AcsLciCoordinateArrayPulseParam
{
    private SpeedSetting _motionProfile = LciSpeedSettings.CreateUnset();

    public int BufferNo { get; set; } = 10;

    public int AxisX { get; set; }

    public int AxisY { get; set; } = 1;

    public double PulseWidth { get; set; } = 0.01d;

    public double MultiAxWinSize { get; set; } = 0.001d;

    public SpeedSetting MotionProfile
    {
        get => _motionProfile;
        set => _motionProfile = value ?? LciSpeedSettings.CreateUnset();
    }

    public double Velocity
    {
        get => MotionProfile.MaxSpeed;
        set => MotionProfile.MaxSpeed = value;
    }

    public bool RouteConfigOutput { get; set; } = true;

    public int ConfigOutputIndex { get; set; }

    public int ConfigOutputCode { get; set; } = 7;

    public int Timeout { get; set; } = 60000;

    public List<AcsPoint2D> Points { get; set; } =
    [
        new(0d, 0d),
        new(10d, 0d),
        new(10d, 10d)
    ];
}

public sealed class AcsLciFixedDistancePulseResult
{
    public AcsLciFixedDistancePulseResult(int channel, int pulseCount, string script)
    {
        Channel = channel;
        PulseCount = pulseCount;
        Script = script;
    }

    public int Channel { get; }

    public int PulseCount { get; }

    public string Script { get; }
}

public sealed class AcsLciSegmentCircleResult
{
    public AcsLciSegmentCircleResult(int channel, string script)
    {
        Channel = channel;
        Script = script;
    }

    public int Channel { get; }

    public string Script { get; }
}

public sealed class AcsLciCoordinateArrayPulseResult
{
    public AcsLciCoordinateArrayPulseResult(int channel, int pulseCount, int pointCount, string script)
    {
        Channel = channel;
        PulseCount = pulseCount;
        PointCount = pointCount;
        Script = script;
    }

    public int Channel { get; }

    public int PulseCount { get; }

    public int PointCount { get; }

    public string Script { get; }
}

public partial class AcsControlCard
{
    public bool TryRunLciFixedDistancePulseXseg(
        AcsLciFixedDistancePulseXsegParam? param,
        out AcsLciFixedDistancePulseResult result,
        out string message)
    {
        result = new AcsLciFixedDistancePulseResult(-1, 0, string.Empty);
        if (param == null)
        {
            message = "ACS LCI fixed-distance pulse parameter cannot be null.";
            return false;
        }

        if (!TryPrepareProgramBuffer(param.BufferNo, out var buffer, out message))
        {
            return false;
        }

        string script;
        try
        {
            ApplyLciMotionProfileFallback(param);
            script = BuildLciFixedDistancePulseXsegScript(param);
        }
        catch (Exception ex)
        {
            message = $"ACS LCI fixed-distance pulse script is invalid: {ex.Message}";
            return false;
        }

        try
        {
            TryStopLciFixedDistancePulseBuffer(buffer);
            _api.LoadBuffer(buffer, script);
            _api.CompileBuffer(buffer);
            _api.RunBuffer(buffer, null);
            _api.WaitProgramEnd(buffer, Math.Max(1000, param.Timeout));

            var channel = _api.ReadIntegerScalar("RY_LCI_CHANNEL", ProgramBuffer.ACSC_NONE);
            var pulseCount = _api.ReadIntegerScalar("RY_LCI_PULSE_COUNT", ProgramBuffer.ACSC_NONE);
            result = new AcsLciFixedDistancePulseResult(channel, pulseCount, script);
            message = $"ACS LCI fixed-distance pulse completed. Channel={channel}, PulseCount={pulseCount}.";
            return true;
        }
        catch (Exception ex)
        {
            TryCleanupLciFixedDistancePulse(buffer);
            message = $"ACS LCI fixed-distance pulse failed: {ex.Message}; {FormatProgramDiagnostics(param.BufferNo)}";
            Console.WriteLine(message);
            return false;
        }
    }

    public bool TryRunLciSegmentCircle(
        AcsLciSegmentCircleParam? param,
        out AcsLciSegmentCircleResult result,
        out string message)
    {
        result = new AcsLciSegmentCircleResult(-1, string.Empty);
        if (param == null)
        {
            message = "ACS LCI segment circle parameter cannot be null.";
            return false;
        }

        if (!TryPrepareProgramBuffer(param.BufferNo, out var buffer, out message))
        {
            return false;
        }

        string script;
        try
        {
            ApplyLciMotionProfileFallback(param);
            script = BuildLciSegmentCircleScript(param);
        }
        catch (Exception ex)
        {
            message = $"ACS LCI segment circle script is invalid: {ex.Message}";
            return false;
        }

        try
        {
            TryStopLciFixedDistancePulseBuffer(buffer);
            _api.LoadBuffer(buffer, script);
            _api.CompileBuffer(buffer);
            _api.RunBuffer(buffer, null);
            _api.WaitProgramEnd(buffer, Math.Max(1000, param.Timeout));

            var channel = _api.ReadIntegerScalar("RY_LCI_CHANNEL", ProgramBuffer.ACSC_NONE);
            result = new AcsLciSegmentCircleResult(channel, script);
            message = $"ACS LCI segment circle completed. Channel={channel}.";
            return true;
        }
        catch (Exception ex)
        {
            TryCleanupLciFixedDistancePulse(buffer);
            message = $"ACS LCI segment circle failed: {ex.Message}; {FormatProgramDiagnostics(param.BufferNo)}";
            Console.WriteLine(message);
            return false;
        }
    }

    public bool TryRunLciCoordinateArrayPulse(
        AcsLciCoordinateArrayPulseParam? param,
        out AcsLciCoordinateArrayPulseResult result,
        out string message)
    {
        result = new AcsLciCoordinateArrayPulseResult(-1, 0, 0, string.Empty);
        if (param == null)
        {
            message = "ACS LCI coordinate-array pulse parameter cannot be null.";
            return false;
        }

        if (!TryPrepareProgramBuffer(param.BufferNo, out var buffer, out message))
        {
            return false;
        }

        string script;
        try
        {
            ApplyLciMotionProfileFallback(param);
            script = BuildLciCoordinateArrayPulseScript(param);
        }
        catch (Exception ex)
        {
            message = $"ACS LCI coordinate-array pulse script is invalid: {ex.Message}";
            return false;
        }

        try
        {
            TryStopLciFixedDistancePulseBuffer(buffer);
            _api.LoadBuffer(buffer, script);
            _api.CompileBuffer(buffer);
            WriteLciCoordinateArrayPulseVariables(param);
            _api.RunBuffer(buffer, null);
            _api.WaitProgramEnd(buffer, Math.Max(1000, param.Timeout));

            var channel = _api.ReadIntegerScalar("RY_LCI_CHANNEL", ProgramBuffer.ACSC_NONE);
            var pulseCount = _api.ReadIntegerScalar("RY_LCI_PULSE_COUNT", ProgramBuffer.ACSC_NONE);
            var pointCount = param.Points?.Count ?? 0;
            result = new AcsLciCoordinateArrayPulseResult(channel, pulseCount, pointCount, script);
            message = $"ACS LCI coordinate-array pulse completed. Channel={channel}, PulseCount={pulseCount}, PointCount={pointCount}.";
            return true;
        }
        catch (Exception ex)
        {
            TryCleanupLciFixedDistancePulse(buffer);
            message = $"ACS LCI coordinate-array pulse failed: {ex.Message}; {FormatProgramDiagnostics(param.BufferNo)}";
            Console.WriteLine(message);
            return false;
        }
    }

    public static string BuildLciFixedDistancePulseXsegScript(AcsLciFixedDistancePulseXsegParam param)
    {
        ValidateLciFixedDistancePulseXsegParam(param);

        var points = param.Points;
        var startPoint = points[0];
        var stopPoint = points[^1];
        var pulseEndDistance = ResolveLciFixedDistancePulseEndDistance(param, startPoint, stopPoint);
        var builder = new StringBuilder();
        builder.AppendLine("global int RY_LCI_CHANNEL");
        builder.AppendLine("global int RY_LCI_PULSE_COUNT");
        builder.AppendLine();
        builder.AppendLine($"int AxX = {param.AxisX}");
        builder.AppendLine($"int AxY = {param.AxisY}");
        builder.AppendLine();
        builder.AppendLine("real PulseWidth");
        builder.AppendLine("real Interval");
        builder.AppendLine("real XStartPos, YStartPos");
        builder.AppendLine("real XStopPos, YStopPos");
        builder.AppendLine("real PulseStartPos, PulseEndPos");
        builder.AppendLine("int ch");
        builder.AppendLine();
        builder.AppendLine("lc.SetSafetyMasks(1, 1)");
        builder.AppendLine();
        builder.AppendLine("ENABLE (AxX, AxY)");
        builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxX", param.MotionProfile);
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxY", param.MotionProfile);
        builder.AppendLine();
        builder.AppendLine($"XStartPos = {FormatAcsNumber(startPoint.X)}");
        builder.AppendLine($"YStartPos = {FormatAcsNumber(startPoint.Y)}");
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine();
        builder.AppendLine("lc.Init()");
        builder.AppendLine();
        builder.AppendLine($"PulseWidth = {FormatAcsNumber(param.PulseWidth)}");
        builder.AppendLine($"Interval = {FormatAcsNumber(param.Interval)}");
        builder.AppendLine();
        builder.AppendLine($"XStopPos = {FormatAcsNumber(stopPoint.X)}");
        builder.AppendLine($"YStopPos = {FormatAcsNumber(stopPoint.Y)}");
        builder.AppendLine();
        builder.AppendLine($"PulseStartPos = {FormatAcsNumber(param.StartDistance)}");
        builder.AppendLine($"PulseEndPos = {FormatAcsNumber(pulseEndDistance)}");
        builder.AppendLine();
        builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
        builder.AppendLine("ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)");

        if (param.RouteConfigOutput)
        {
            builder.AppendLine($"lc.SetConfigOut({param.ConfigOutputIndex}, ch, {param.ConfigOutputCode})");
        }

        builder.AppendLine();
        builder.AppendLine("lc.LaserEnable()");
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStopPos, YStopPos");
        builder.AppendLine("lc.LaserDisable()");
        builder.AppendLine();
        builder.AppendLine("RY_LCI_CHANNEL = ch");
        builder.AppendLine("RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)");
        builder.AppendLine();
        builder.AppendLine("DISP \"Pulse count = %d\", RY_LCI_PULSE_COUNT");
        builder.AppendLine();
        builder.AppendLine("lc.Stop(ch)");
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildLciSegmentCircleScript(AcsLciSegmentCircleParam param)
    {
        ValidateLciSegmentCircleParam(param);

        var circleStartX = param.CenterX;
        var circleStartY = param.CenterY - param.Radius;
        var builder = new StringBuilder();
        builder.AppendLine("global int RY_LCI_CHANNEL");
        builder.AppendLine();
        builder.AppendLine($"int AxX = {param.AxisX}");
        builder.AppendLine($"int AxY = {param.AxisY}");
        builder.AppendLine();
        builder.AppendLine("real Velocity");
        builder.AppendLine("real XStartPos, YStartPos");
        builder.AppendLine("real XCenterPos, YCenterPos");
        builder.AppendLine("real Radius");
        builder.AppendLine("real XCircleStartPos, YCircleStartPos");
        builder.AppendLine("real pi");
        builder.AppendLine("int GateActiveState");
        builder.AppendLine("int ch");
        builder.AppendLine();
        builder.AppendLine("pi = ACOS(-1)");
        builder.AppendLine($"Velocity = {FormatAcsNumber(param.Velocity)}");
        builder.AppendLine();
        builder.AppendLine($"XStartPos = {FormatAcsNumber(param.StartX)}");
        builder.AppendLine($"YStartPos = {FormatAcsNumber(param.StartY)}");
        builder.AppendLine($"XCenterPos = {FormatAcsNumber(param.CenterX)}");
        builder.AppendLine($"YCenterPos = {FormatAcsNumber(param.CenterY)}");
        builder.AppendLine($"Radius = {FormatAcsNumber(param.Radius)}");
        builder.AppendLine($"XCircleStartPos = {FormatAcsNumber(circleStartX)}");
        builder.AppendLine($"YCircleStartPos = {FormatAcsNumber(circleStartY)}");
        builder.AppendLine($"GateActiveState = {param.GateActiveState}");
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxX", param.MotionProfile);
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxY", param.MotionProfile);
        builder.AppendLine();
        builder.AppendLine("lc.SetSafetyMasks(1, 1)");
        builder.AppendLine("lc.Init()");
        builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
        builder.AppendLine();
        builder.AppendLine("ENABLE (AxX, AxY)");
        builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
        builder.AppendLine("WAIT 200");
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine();
        builder.AppendLine("ch = lc.SegmentGate()");
        builder.AppendLine("RY_LCI_CHANNEL = ch");
        builder.AppendLine("lc.LaserEnable()");
        builder.AppendLine();
        builder.AppendLine("XSEG(AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine("LINE/p (AxX, AxY), XCircleStartPos, YCircleStartPos, 0");
        builder.AppendLine("ARC2/p (AxX, AxY), XCenterPos, YCenterPos, 2*pi, GateActiveState");
        builder.AppendLine("ENDS(AxX, AxY)");
        builder.AppendLine("TILL GSEG(AxX) = -1");
        builder.AppendLine();
        builder.AppendLine("lc.LaserDisable()");
        builder.AppendLine("lc.Stop()");
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildLciCoordinateArrayPulseScript(AcsLciCoordinateArrayPulseParam param)
    {
        ValidateLciCoordinateArrayPulseParam(param);

        var pointCount = param.Points.Count;
        var builder = new StringBuilder();
        builder.AppendLine("global int RY_LCI_CHANNEL");
        builder.AppendLine("global int RY_LCI_PULSE_COUNT");
        builder.AppendLine($"global real RY_LCI_XCOORD({pointCount})");
        builder.AppendLine($"global real RY_LCI_YCOORD({pointCount})");
        builder.AppendLine();
        builder.AppendLine($"int AxX = {param.AxisX}");
        builder.AppendLine($"int AxY = {param.AxisY}");
        builder.AppendLine();
        builder.AppendLine("real PulseWidth");
        builder.AppendLine("int PointsNum");
        builder.AppendLine("int PointIndex");
        builder.AppendLine("int ch");
        builder.AppendLine();
        builder.AppendLine($"PulseWidth = {FormatAcsNumber(param.PulseWidth)}");
        builder.AppendLine($"PointsNum = {pointCount}");
        builder.AppendLine();
        builder.AppendLine("lc.SetSafetyMasks(1, 1)");
        builder.AppendLine("lc.LaserDisable()");
        builder.AppendLine("lc.Stop()");
        builder.AppendLine("lc.Init()");
        builder.AppendLine();
        builder.AppendLine("ENABLE (AxX, AxY)");
        builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxX", param.MotionProfile);
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxY", param.MotionProfile);
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)");
        builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
        builder.AppendLine($"lc.MultiAxWinSize = {FormatAcsNumber(param.MultiAxWinSize)}");
        builder.AppendLine("ch = lc.CoordinateArrPulse(PointsNum, PulseWidth, RY_LCI_XCOORD, RY_LCI_YCOORD)");

        if (param.RouteConfigOutput)
        {
            builder.AppendLine($"lc.SetConfigOut({param.ConfigOutputIndex}, ch, {param.ConfigOutputCode})");
        }

        builder.AppendLine("lc.LaserEnable()");
        builder.AppendLine();
        builder.AppendLine("XSEG (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)");
        builder.AppendLine("PointIndex = 1");
        builder.AppendLine("while PointIndex < PointsNum");
        builder.AppendLine("block");
        builder.AppendLine("    LINE (AxX, AxY), RY_LCI_XCOORD(PointIndex), RY_LCI_YCOORD(PointIndex)");
        builder.AppendLine("    PointIndex = PointIndex + 1");
        builder.AppendLine("end");
        builder.AppendLine("end");
        builder.AppendLine("ENDS (AxX, AxY)");
        builder.AppendLine("till GSEG(AxX) = -1");
        builder.AppendLine();
        builder.AppendLine("lc.LaserDisable()");
        builder.AppendLine("RY_LCI_CHANNEL = ch");
        builder.AppendLine("RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)");
        builder.AppendLine("DISP \"Coordinate array pulse count = %d\", RY_LCI_PULSE_COUNT");
        builder.AppendLine("lc.Stop(ch)");
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    private static void AppendAxisMotionTuning(StringBuilder builder, string axisName, SpeedSetting motionProfile)
    {
        builder.AppendLine($"VEL({axisName}) = {FormatAcsNumber(motionProfile.MaxSpeed)}");
        builder.AppendLine($"ACC({axisName}) = {FormatAcsNumber(motionProfile.AccSpeed)}");
        builder.AppendLine($"DEC({axisName}) = {FormatAcsNumber(motionProfile.DecSpeed)}");
        builder.AppendLine($"KDEC({axisName}) = {FormatAcsNumber(motionProfile.KillDecSpeed)}");
        builder.AppendLine($"JERK({axisName}) = {FormatAcsNumber(motionProfile.Jerk)}");
    }

    private static double ResolveLciFixedDistancePulseEndDistance(
        AcsLciFixedDistancePulseXsegParam param,
        AcsPoint2D startPoint,
        AcsPoint2D stopPoint)
    {
        if (param.EndDistance > param.StartDistance)
        {
            return param.EndDistance;
        }

        var deltaX = stopPoint.X - startPoint.X;
        var deltaY = stopPoint.Y - startPoint.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private void WriteLciCoordinateArrayPulseVariables(AcsLciCoordinateArrayPulseParam param)
    {
        var points = param.Points;
        var xCoordinates = new double[points.Count];
        var yCoordinates = new double[points.Count];

        for (var index = 0; index < points.Count; index++)
        {
            xCoordinates[index] = points[index].X;
            yCoordinates[index] = points[index].Y;
        }

        var lastIndex = points.Count - 1;
        _api.WriteVariable(xCoordinates, "RY_LCI_XCOORD", ProgramBuffer.ACSC_NONE, 0, lastIndex, -1, -1);
        _api.WriteVariable(yCoordinates, "RY_LCI_YCOORD", ProgramBuffer.ACSC_NONE, 0, lastIndex, -1, -1);
    }

    private void ApplyLciMotionProfileFallback(AcsLciFixedDistancePulseXsegParam param)
    {
        param.MotionProfile = ResolveLciWorkMotionProfile(param.AxisX, param.MotionProfile, LciSpeedSettings.CreateUnset());
    }

    private void ApplyLciMotionProfileFallback(AcsLciSegmentCircleParam param)
    {
        param.MotionProfile = ResolveLciWorkMotionProfile(param.AxisX, param.MotionProfile, LciSpeedSettings.CreateUnset());
    }

    private void ApplyLciMotionProfileFallback(AcsLciCoordinateArrayPulseParam param)
    {
        param.MotionProfile = ResolveLciWorkMotionProfile(param.AxisX, param.MotionProfile, LciSpeedSettings.CreateUnset());
    }

    private SpeedSetting ResolveLciWorkMotionProfile(
        int acsAxisNumber,
        SpeedSetting? configured,
        SpeedSetting fallback)
    {
        var axisNum = ResolveLciAxisNum(acsAxisNumber);
        var workSpeed = axisNum.HasValue
            ? ResolveSpeedSetting(axisNum.Value, EN_SpeedType.Work)
            : null;

        return MergeConfiguredLciSpeedSetting(configured, workSpeed ?? fallback);
    }

    private En_AxisNum? ResolveLciAxisNum(int acsAxisNumber)
    {
        var axis = Config?.AllAxis?
            .FirstOrDefault(item => item != null && GetZeroBasedAxisNo(item) == acsAxisNumber);

        return axis?.AxisNum;
    }

    private static SpeedSetting MergeConfiguredLciSpeedSetting(SpeedSetting? configured, SpeedSetting fallback)
    {
        return new SpeedSetting
        {
            SpeedType = configured?.SpeedType ?? fallback.SpeedType,
            SpeedDescribe = configured?.SpeedDescribe ?? fallback.SpeedDescribe,
            StartSpeed = ResolveConfiguredLciSpeed(configured?.StartSpeed ?? 0d, fallback.StartSpeed),
            MaxSpeed = ResolveConfiguredLciSpeed(configured?.MaxSpeed ?? 0d, fallback.MaxSpeed),
            AccSpeed = ResolveConfiguredLciSpeed(configured?.AccSpeed ?? 0d, fallback.AccSpeed),
            DecSpeed = ResolveConfiguredLciSpeed(configured?.GetConfiguredDecSpeed() ?? 0d, fallback.DecSpeed),
            KillDecSpeed = ResolveConfiguredLciSpeed(configured?.GetConfiguredKillDecSpeed() ?? 0d, fallback.KillDecSpeed),
            Jerk = ResolveConfiguredLciSpeed(configured?.GetConfiguredJerk() ?? 0d, fallback.Jerk)
        };
    }

    private static double ResolveConfiguredLciSpeed(double configured, double fallback)
    {
        return IsFinite(configured) && configured > 0d ? configured : fallback;
    }

    private static void ValidateLciFixedDistancePulseXsegParam(AcsLciFixedDistancePulseXsegParam param)
    {
        ArgumentNullException.ThrowIfNull(param);
        ValidateLciSpeedSetting(param.MotionProfile);

        if (!IsFinite(param.PulseWidth) || param.PulseWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(param.PulseWidth), "Pulse width must be positive.");
        }

        if (!IsFinite(param.Interval) || param.Interval <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(param.Interval), "Interval must be positive.");
        }

        if (!IsFinite(param.StartDistance) || !IsFinite(param.EndDistance))
        {
            throw new ArgumentOutOfRangeException(nameof(param.EndDistance), "Start and end distances must be finite.");
        }

        if (param.RouteConfigOutput && !IsValidLciConfigOutputIndex(param.ConfigOutputIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(param.ConfigOutputIndex), "Config output index must be 0..7 or 10.");
        }

        if (param.Points == null || param.Points.Count < 2)
        {
            throw new ArgumentException("At least two XSEG points are required.", nameof(param.Points));
        }

        foreach (var point in param.Points)
        {
            if (!IsFinite(point.X) || !IsFinite(point.Y))
            {
                throw new ArgumentException("XSEG points must be finite.", nameof(param.Points));
            }
        }
    }

    private static void ValidateLciCoordinateArrayPulseParam(AcsLciCoordinateArrayPulseParam param)
    {
        ArgumentNullException.ThrowIfNull(param);
        ValidateLciSpeedSetting(param.MotionProfile);

        if (param.AxisX == param.AxisY)
        {
            throw new ArgumentException("Coordinate-array axes must be different.");
        }

        if (!IsFinite(param.PulseWidth) || param.PulseWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(param.PulseWidth), "Pulse width must be positive.");
        }

        if (!IsFinite(param.MultiAxWinSize) || param.MultiAxWinSize <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(param.MultiAxWinSize), "Coordinate-array trigger window must be positive.");
        }

        if (param.RouteConfigOutput && !IsValidLciConfigOutputIndex(param.ConfigOutputIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(param.ConfigOutputIndex), "Config output index must be 0..7 or 10.");
        }

        if (param.Timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(param.Timeout), "Timeout must be positive.");
        }

        if (param.Points == null || param.Points.Count < 2)
        {
            throw new ArgumentException("At least two coordinate-array points are required.", nameof(param.Points));
        }

        foreach (var point in param.Points)
        {
            if (!IsFinite(point.X) || !IsFinite(point.Y))
            {
                throw new ArgumentException("Coordinate-array points must be finite.", nameof(param.Points));
            }
        }
    }

    private static void ValidateLciSegmentCircleParam(AcsLciSegmentCircleParam param)
    {
        ArgumentNullException.ThrowIfNull(param);
        ValidateLciSpeedSetting(param.MotionProfile);

        if (param.AxisX == param.AxisY)
        {
            throw new ArgumentException("Circle axes must be different.");
        }

        if (!IsFinite(param.StartX)
            || !IsFinite(param.StartY)
            || !IsFinite(param.CenterX)
            || !IsFinite(param.CenterY)
            || !IsFinite(param.Radius))
        {
            throw new ArgumentException("Circle positions must be finite.");
        }

        if (param.Radius <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(param.Radius), "Circle radius must be positive.");
        }

        if (param.GateActiveState is not 0 and not 1)
        {
            throw new ArgumentOutOfRangeException(nameof(param.GateActiveState), "Gate active state must be 0 or 1.");
        }

        if (param.Timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(param.Timeout), "Timeout must be positive.");
        }
    }

    private static void ValidateLciSpeedSetting(SpeedSetting motionProfile)
    {
        ArgumentNullException.ThrowIfNull(motionProfile);

        if (!IsFinite(motionProfile.MaxSpeed) || motionProfile.MaxSpeed <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(motionProfile.MaxSpeed), "Velocity must be positive.");
        }

        if (!IsFinite(motionProfile.AccSpeed) || motionProfile.AccSpeed <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(motionProfile.AccSpeed), "Acceleration must be positive.");
        }

        if (!IsFinite(motionProfile.DecSpeed) || motionProfile.DecSpeed <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(motionProfile.DecSpeed), "Deceleration must be positive.");
        }

        if (!IsFinite(motionProfile.KillDecSpeed) || motionProfile.KillDecSpeed <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(motionProfile.KillDecSpeed), "Kill deceleration must be positive.");
        }

        if (!IsFinite(motionProfile.Jerk) || motionProfile.Jerk <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(motionProfile.Jerk), "Jerk must be positive.");
        }
    }

    private static bool IsValidLciConfigOutputIndex(int outputIndex)
    {
        return (outputIndex >= 0 && outputIndex <= 7) || outputIndex == 10;
    }

    private static string FormatAcsNumber(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }

    private void TryCleanupLciFixedDistancePulse(ProgramBuffer buffer)
    {
        try
        {
            _api.Transaction("lc.LaserDisable()");
        }
        catch
        {
        }

        try
        {
            _api.Transaction("lc.Stop(RY_LCI_CHANNEL)");
        }
        catch
        {
        }

        TryStopLciFixedDistancePulseBuffer(buffer);
    }

    private void TryStopLciFixedDistancePulseBuffer(ProgramBuffer buffer)
    {
        try
        {
            _api.StopBuffer(buffer);
        }
        catch
        {
        }
    }
}
