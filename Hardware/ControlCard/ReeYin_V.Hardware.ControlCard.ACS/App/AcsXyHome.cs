using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public static string BuildXyHomeBufferScript(AcsXyHomeBufferConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var builder = new StringBuilder();
        builder.AppendLine("!HOMING X Y");
        builder.AppendLine("LOCAL INT AXIS");
        builder.AppendLine("FCLEAR ALL");
        builder.AppendLine();
        AppendXHomeSequence(builder, config.XPhysicalAxis);
        builder.AppendLine();
        AppendYHomeSequence(builder, config.YPhysicalAxis);
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildXHomeBufferScript(AcsXyHomeBufferConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var builder = new StringBuilder();
        builder.AppendLine("!HOMING X");
        builder.AppendLine("LOCAL INT AXIS");
        builder.AppendLine("FCLEAR ALL");
        builder.AppendLine();
        AppendXHomeSequence(builder, config.XPhysicalAxis);
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    public static string BuildYHomeBufferScript(AcsXyHomeBufferConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var builder = new StringBuilder();
        builder.AppendLine("!HOMING Y");
        builder.AppendLine("LOCAL INT AXIS");
        builder.AppendLine("FCLEAR ALL");
        builder.AppendLine();
        AppendYHomeSequence(builder, config.YPhysicalAxis);
        builder.AppendLine("STOP");
        return builder.ToString();
    }

    private static void AppendXHomeSequence(StringBuilder builder, int axis)
    {
        builder.AppendLine($"AXIS={axis}");
        builder.AppendLine("WAIT 500");
        builder.AppendLine();
        builder.AppendLine("VEL(AXIS)= 20");
        builder.AppendLine("ACC(AXIS)= 100");
        builder.AppendLine("DEC(AXIS)= 100");
        builder.AppendLine("JERK(AXIS)= 900");
        builder.AppendLine("KDEC(AXIS)= 300");
        AppendCommonLimitDisable(builder);
        builder.AppendLine();
        AppendCommonHomeSearch(builder);
        builder.AppendLine("PTP (AXIS),-340");
        AppendCommonHomeFinish(builder);
    }

    private static void AppendYHomeSequence(StringBuilder builder, int axis)
    {
        builder.AppendLine($"AXIS={axis}");
        builder.AppendLine("WAIT 500");
        builder.AppendLine();
        builder.AppendLine("VEL(AXIS)= 10");
        builder.AppendLine("ACC(AXIS)= 50");
        builder.AppendLine("DEC(AXIS)= 50");
        builder.AppendLine("JERK(AXIS)= 500");
        builder.AppendLine("KDEC(AXIS)= 150");
        AppendCommonLimitDisable(builder);
        builder.AppendLine();
        builder.AppendLine("ENABLE (AXIS)");
        builder.AppendLine("!COMMUT (AXIS)");
        builder.AppendLine("WAIT 500");
        AppendCommonIndexSearch(builder);
        builder.AppendLine("PTP (AXIS),-48");
        AppendCommonHomeFinish(builder);
    }

    private static void AppendCommonLimitDisable(StringBuilder builder)
    {
        builder.AppendLine("FDEF(AXIS).#LL=0");
        builder.AppendLine("FDEF(AXIS).#RL=0");
    }

    private static void AppendCommonHomeSearch(StringBuilder builder)
    {
        builder.AppendLine("ENABLE (AXIS)");
        builder.AppendLine("WAIT 500");
        AppendCommonIndexSearch(builder);
    }

    private static void AppendCommonIndexSearch(StringBuilder builder)
    {
        builder.AppendLine("JOG (AXIS),-");
        builder.AppendLine("TILL FAULT(AXIS).#LL");
        builder.AppendLine("JOG (AXIS),+");
        builder.AppendLine("TILL ^FAULT(AXIS).#LL");
        builder.AppendLine("IST(AXIS).#IND=0");
        builder.AppendLine("TILL IST(AXIS).#IND");
        builder.AppendLine("SET FPOS(AXIS)=FPOS(AXIS)-IND(AXIS)");
    }

    private static void AppendCommonHomeFinish(StringBuilder builder)
    {
        builder.AppendLine("TILL MST(AXIS).#MOVE=0");
        builder.AppendLine("WAIT 500");
        builder.AppendLine("SET FPOS(AXIS)=0");
        builder.AppendLine("FDEF(AXIS).#LL=1");
        builder.AppendLine("FDEF(AXIS).#RL=1");
        builder.AppendLine("WAIT 500");
    }

    private bool TryBuildXyHomePlan(
        IReadOnlyCollection<SingleAxisParam> axes,
        out SingleAxisParam? xAxis,
        out SingleAxisParam? yAxis,
        out AcsXyHomeBufferConfig? xyHomeConfig,
        out SingleAxisParam[] remainingAxes,
        out string message)
    {
        xAxis = axes.FirstOrDefault(axis => axis.AxisNum == En_AxisNum.X);
        yAxis = axes.FirstOrDefault(axis => axis.AxisNum == En_AxisNum.Y);
        xyHomeConfig = null;
        remainingAxes = axes.ToArray();

        if (xAxis == null || yAxis == null || !Options.XyHomeBuffer.IsEnabled)
        {
            message = string.Empty;
            return true;
        }

        if (!TryGetXyHomeBufferConfig(out xyHomeConfig, out message))
        {
            return false;
        }

        remainingAxes = axes.Where(axis => !IsXyHomeAxis(axis.AxisNum)).ToArray();
        message = string.Empty;
        return true;
    }

    private bool TryGetXyHomeBufferConfig(out AcsXyHomeBufferConfig config, out string message)
    {
        EnsureOptions();
        config = Options.XyHomeBuffer;

        if (config.XBufferNo < 0 || config.XBufferNo > 64)
        {
            message = $"X/Y 合并回零 X Buffer 编号无效: XBufferNo={config.XBufferNo}。";
            return false;
        }

        if (config.YBufferNo < 0 || config.YBufferNo > 64)
        {
            message = $"X/Y 合并回零 Y Buffer 编号无效: YBufferNo={config.YBufferNo}。";
            return false;
        }

        if (config.XBufferNo == config.YBufferNo)
        {
            message = "X/Y 合并回零的 X Buffer 和 Y Buffer 编号不能相同。";
            return false;
        }

        if (config.XPhysicalAxis == config.YPhysicalAxis)
        {
            message = "X/Y 合并回零的 ACS 物理轴号不能相同。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool IsXyHomeAxis(En_AxisNum axis)
    {
        return axis == En_AxisNum.X || axis == En_AxisNum.Y;
    }

    private bool RunXyHomeBuffer(
        SingleAxisParam xAxis,
        SingleAxisParam yAxis,
        AcsXyHomeBufferConfig homeConfig,
        out string message)
    {
        var timeout = homeConfig.Timeout > 0 ? homeConfig.Timeout : Options.InternalTimeout;
        var xAcsAxis = ToZeroBasedAcsAxis((short)homeConfig.XPhysicalAxis);
        var yAcsAxis = ToZeroBasedAcsAxis((short)homeConfig.YPhysicalAxis);

        if (!TryPrepareProgramBuffer(homeConfig.XBufferNo, out var xBuffer, out message))
        {
            return false;
        }

        if (!TryPrepareProgramBuffer(homeConfig.YBufferNo, out var yBuffer, out message))
        {
            return false;
        }

        var xScript = BuildXHomeBufferScript(homeConfig);
        var yScript = BuildYHomeBufferScript(homeConfig);

        try
        {
            if (homeConfig.StopAxesBeforeRun)
            {
                StopAxis(xAcsAxis, Options.HomeBeforeRunStopMode);
                StopAxis(yAcsAxis, Options.HomeBeforeRunStopMode);
                if (!WaitUntilAcsAxesStopped(new[] { xAcsAxis, yAcsAxis }, timeout))
                {
                    message = $"X/Y 合并回零 XBuffer={homeConfig.XBufferNo}; YBuffer={homeConfig.YBufferNo}; 启动前 X/Y 轴在 {timeout}ms 内未停止。";
                    return false;
                }
            }

            TryStopProgramBufferSilently(xBuffer);
            TryStopProgramBufferSilently(yBuffer);
            _api.LoadBuffer(xBuffer, xScript);
            _api.LoadBuffer(yBuffer, yScript);
            _api.CompileBuffer(xBuffer);
            _api.CompileBuffer(yBuffer);
            _api.RunBuffer(xBuffer, null);
            _api.RunBuffer(yBuffer, null);
            _api.WaitProgramEnd(xBuffer, timeout);
            _api.WaitProgramEnd(yBuffer, timeout);

            UpdateAxisState(En_AxisNum.X);
            UpdateAxisState(En_AxisNum.Y);
            GetAllPosInfos();

            xAxis.IsResetCompleted = true;
            yAxis.IsResetCompleted = true;
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            TryStopProgramBufferSilently(xBuffer);
            TryStopProgramBufferSilently(yBuffer);
            TryStopAxisSilently(xAcsAxis);
            TryStopAxisSilently(yAcsAxis);
            message = $"X/Y 合并回零失败; XBuffer={homeConfig.XBufferNo}; YBuffer={homeConfig.YBufferNo}; 异常={ex.Message}; X={FormatProgramDiagnostics(homeConfig.XBufferNo)}; Y={FormatProgramDiagnostics(homeConfig.YBufferNo)}";
            Console.WriteLine($"ACS XY home failed: {message}");
            return false;
        }
    }

    private bool WaitUntilAcsAxesStopped(IEnumerable<Axis> axes, int timeout)
    {
        var acsAxes = axes.Distinct().ToArray();
        var elapsed = 0;
        while (elapsed < timeout)
        {
            try
            {
                if (acsAxes.All(axis => (_api.GetMotorState(axis) & MotorStates.ACSC_MST_MOVE) == 0))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ACS WaitUntilAcsAxesStopped failed: {ex.Message}");
                return false;
            }

            Thread.Sleep(20);
            elapsed += 20;
        }

        return false;
    }

    private void TryStopProgramBufferSilently(ProgramBuffer buffer)
    {
        try
        {
            _api.StopBuffer(buffer);
        }
        catch
        {
        }
    }

    private void TryStopAxisSilently(Axis axis)
    {
        try
        {
            StopAxis(axis, AxisStopMode.立即停止);
        }
        catch
        {
        }
    }
}
