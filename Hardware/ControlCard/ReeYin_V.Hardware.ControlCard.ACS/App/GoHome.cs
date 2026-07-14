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
    protected override bool DoGoHome(out string message)
    {
        message = string.Empty;
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        EnsureOptions();
        IsAxisHoming = true;
        IsAxisHomed = false;

        try
        {
            var axes = Config.AllAxis.Where(axis => axis.IsUsing).ToArray();
            if (axes.Length == 0)
            {
                message = "ACS 回零失败: 未配置启用轴。";
                return false;
            }

            foreach (var axis in axes)
            {
                axis.IsResetCompleted = false;
            }

            if (!TryBuildXyHomePlan(axes, out var xAxis, out var yAxis, out var xyHomeConfig, out var remainingAxes, out message))
            {
                message = $"ACS 回零失败，X/Y 合并回零执行前检查未通过: {message}";
                return false;
            }

            var homePlan = new List<(SingleAxisParam Axis, AcsAxisHomeBufferConfig HomeConfig)>(remainingAxes.Length);
            foreach (var axis in remainingAxes)
            {
                if (!TryGetHomeBufferConfig(axis.AxisNum, out var homeConfig, out message))
                {
                    message = $"ACS 回零失败，轴 {axis.AxisNum} 执行前检查未通过: {message}";
                    return false;
                }

                homePlan.Add((axis, homeConfig));
            }

            if (xyHomeConfig != null && xAxis != null && yAxis != null)
            {
                if (!RunXyHomeBuffer(xAxis, yAxis, xyHomeConfig, out message))
                {
                    xAxis.IsResetCompleted = false;
                    yAxis.IsResetCompleted = false;
                    message = $"ACS 回零失败，X/Y 合并回零: {message}";
                    return false;
                }
            }

            foreach (var (axis, homeConfig) in homePlan)
            {
                if (!RunAxisHomeBuffer(axis, homeConfig, out message))
                {
                    axis.IsResetCompleted = false;
                    message = $"ACS 回零失败，轴 {axis.AxisNum}: {message}";
                    return false;
                }
            }

            if (!EnsureDBufferLciDeclarations(out var dBufferMessage))
            {
                IsAxisHomed = false;
                message = $"ACS 回零完成，但 D-Buffer LCI 全局声明写入失败: {dBufferMessage}";
                return false;
            }

            IsAxisHomed = true;
            message = $"ACS 已完成 X/Y 合并 Buffer 及其余轴 ACSPL Buffer 回零。{Environment.NewLine}{dBufferMessage}";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            Console.WriteLine($"ACS DoGoHome failed: {ex.Message}");
            return false;
        }
        finally
        {
            IsAxisHoming = false;
        }
    }

    private bool TryGetHomeBufferConfig(En_AxisNum axisId, out AcsAxisHomeBufferConfig config, out string message)
    {
        EnsureOptions();
        var matchedConfig = Options.HomeBuffers.FirstOrDefault(item => item != null && item.Axis == axisId);
        if (matchedConfig == null)
        {
            config = null!;
            message = $"轴 {axisId} 未配置回零 Buffer。";
            return false;
        }

        config = matchedConfig;
        if (!config.IsEnabled)
        {
            message = $"轴 {axisId} 的回零 Buffer 已配置但未启用。";
            return false;
        }

        if (config.BufferNo < 0 || config.BufferNo > 64)
        {
            message = $"轴 {axisId} 的回零 Buffer 编号无效: BufferNo={config.BufferNo}。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool RunAxisHomeBuffer(SingleAxisParam axisConfig, AcsAxisHomeBufferConfig homeConfig, out string message)
    {
        var axisId = axisConfig.AxisNum;
        var acsAxis = ToConfiguredAcsAxis(axisConfig);
        var timeout = homeConfig.Timeout > 0 ? homeConfig.Timeout : Options.InternalTimeout;

        try
        {
            if (homeConfig.StopAxisBeforeRun)
            {
                StopAxis(acsAxis, Options.HomeBeforeRunStopMode);
                if (!WaitUntilAxisStopped(axisId, timeout))
                {
                    message = $"轴={axisId}; ACS轴={(int)acsAxis}; Buffer={homeConfig.BufferNo}; 回零 Buffer 启动前轴在 {timeout}ms 内未停止。";
                    return false;
                }
            }

            if (!RunProgramBuffer(homeConfig.BufferNo, out message))
            {
                message = $"轴={axisId}; ACS轴={(int)acsAxis}; Buffer={homeConfig.BufferNo}; {message}; {FormatProgramDiagnostics(homeConfig.BufferNo)}";
                return false;
            }

            if (!WaitProgramBufferEnd(homeConfig.BufferNo, timeout, out message))
            {
                var waitMessage = message;
                StopProgramBuffer(homeConfig.BufferNo, out var stopMessage);
                try
                {
                    StopAxis(acsAxis, AxisStopMode.立即停止);
                }
                catch (Exception stopAxisException)
                {
                    stopMessage = $"{stopMessage}; 停止轴失败: {stopAxisException.Message}";
                }

                message = $"轴={axisId}; ACS轴={(int)acsAxis}; Buffer={homeConfig.BufferNo}; {waitMessage}; {stopMessage}";
                return false;
            }

            UpdateAxisState(axisId);
            GetAllPosInfos();

            if (homeConfig.ResetFeedbackAfterSuccess && !ResetFeedbackPosition(axisId, homeConfig.ResetPosition))
            {
                message = $"轴={axisId}; ACS轴={(int)acsAxis}; Buffer={homeConfig.BufferNo}; 回零 Buffer 完成，但反馈位置设置失败。{FormatProgramDiagnostics(homeConfig.BufferNo)}";
                return false;
            }

            axisConfig.IsResetCompleted = true;
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"轴={axisId}; ACS轴={(int)acsAxis}; Buffer={homeConfig.BufferNo}; 异常={ex.Message}; {FormatProgramDiagnostics(homeConfig.BufferNo)}";
            Console.WriteLine($"ACS home failed: {message}");
            return false;
        }
    }

    public override bool GetAllPosInfos(short core = 2)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            EnsurePositionBuffers();

            for (var i = 0; i < Config.AllAxis.Count; i++)
            {
                var axisConfig = Config.AllAxis[i];
                var acsAxis = ToConfiguredAcsAxis(axisConfig);
                var position = Math.Round(_api.GetFPosition(acsAxis), 3);
                axisConfig.CurPos = position;

                var axisIndex = axisConfig.AxisNo - 1;
                if (axisIndex >= 0 && axisIndex < CurPos.Length)
                {
                    CurPos[axisIndex] = position;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetAllPosInfos failed: {ex.Message}");
            return false;
        }
    }

    public override bool GetAllPosInfos(ref double[] allPosInfos, short core = 2)
    {
        if (!GetAllPosInfos(core))
        {
            return false;
        }

        for (var i = 0; i < allPosInfos.Length && i < Config.AllAxis.Count; i++)
        {
            allPosInfos[i] = Config.AllAxis[i].CurPos;
        }

        return true;
    }

    public override bool GetAllSpeedInfos(short core = 2)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            EnsureSpeedBuffers();

            for (var i = 0; i < Config.AllAxis.Count; i++)
            {
                var axisConfig = Config.AllAxis[i];
                var acsAxis = ToConfiguredAcsAxis(axisConfig);
                var speed = Math.Round(_api.GetFVelocity(acsAxis), 3);
                axisConfig.CurSpeed = speed;

                var axisIndex = axisConfig.AxisNo - 1;
                if (axisIndex >= 0 && axisIndex < CurSpeed.Length)
                {
                    CurSpeed[axisIndex] = speed;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetAllSpeedInfos failed: {ex.Message}");
            return false;
        }
    }

    public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
    {
        if (!GetAllSpeedInfos(core))
        {
            return false;
        }

        for (var i = 0; i < allSpeedInfos.Length && i < Config.AllAxis.Count; i++)
        {
            allSpeedInfos[i] = Config.AllAxis[i].CurSpeed;
        }

        return true;
    }

    public bool ResetFeedbackPosition(En_AxisNum axisId, double position = 0d)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            _api.SetFPosition(ToAcsAxis(axisId), position);
            UpdateAxisState(axisId);
            GetAllPosInfos();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS ResetFeedbackPosition failed: {ex.Message}");
            return false;
        }
    }
}
