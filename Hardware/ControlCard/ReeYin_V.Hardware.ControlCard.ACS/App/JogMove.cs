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
    public override bool Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message)
    {
        message = string.Empty;
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        if (double.IsNaN(um) || double.IsInfinity(um) || Math.Abs(um) <= double.Epsilon)
        {
            message = "移动距离必须大于 0。";
            return false;
        }

        var distance = moveDirection == MoveDirection.反向 ? -Math.Abs(um) : Math.Abs(um);
        try
        {
            if (!DoGetAxisEnable(axisType))
            {
                message = $"轴 {axisType} 未使能。";
                return false;
            }

            if (!DoGetAxisStopped(axisType))
            {
                message = $"轴 {axisType} 正在运动中。";
                return false;
            }

            if (!DoMoveAxis(axisType, distance))
            {
                message = $"轴 {axisType} 相对移动启动失败。";
                return false;
            }

            if (!WaitUntilAxisStopped(axisType, InternalTimeout))
            {
                message = $"轴 {axisType} 相对移动等待超时。";
                return false;
            }

            UpdateAxisState(axisType);
            message = $"轴 {axisType} 相对移动完成，方向={FormatMoveDirection(moveDirection)}，距离={Math.Abs(um):F3}。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"轴 {axisType} 相对移动失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    protected override bool DoMoveAxis(En_AxisNum axisType, double um)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisType);
            if (!PrepareMotorToMove(axis))
            {
                return false;
            }

            var speed = ResolveSpeedSetting(axisType);
            if (speed != null)
            {
                _api.SetVelocity(axis, Math.Abs(speed.MaxSpeed));
                _api.SetAcceleration(axis, Math.Abs(speed.AccSpeed));
                _api.SetDeceleration(axis, Math.Abs(speed.AccSpeed));
            }

            _api.ToPoint(MotionFlags.ACSC_AMF_RELATIVE, axis, um);
            UpdateAxisState(axisType);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoMoveAxis failed: {ex.Message}");
            return false;
        }
    }

    protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisType);
            var velocity = GetAxisDefaultVelocity(axisType);
            if (moveDirection == MoveDirection.反向)
            {
                velocity = -velocity;
            }

            _api.Jog(MotionFlags.ACSC_NONE, axis, velocity);
            UpdateAxisState(axisType);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoMoveContinue failed: {ex.Message}");
            return false;
        }
    }

    public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, float step)
    {
        if (Math.Abs(step) <= double.Epsilon)
        {
            return false;
        }

        var distance = dir == MoveDirection.反向 ? -Math.Abs(step) : Math.Abs(step);
        return DoMoveAxis(axisId, distance);
    }

    public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)
    {
        if (!isRunStop)
        {
            DoStop(axisId, AxisStopMode.减速停止);
            return true;
        }

        SetSpeedMode(spdType);
        if (!TryApplyJogSpeedProfile(axisId, spdType, out var profileMessage))
        {
            Console.WriteLine(profileMessage);
            return false;
        }

        Console.WriteLine(profileMessage);
        return DoMoveContinue(axisId, dir);
    }

    private bool TryApplyJogSpeedProfile(En_AxisNum axisId, EN_SpeedType speedType, out string message)
    {
        message = string.Empty;
        var speed = ResolveSpeedSetting(axisId, speedType);
        if (speed == null)
        {
            message = $"ACS Jog speed profile missing: axis={axisId}, speedType={speedType}.";
            return false;
        }

        var velocity = Math.Abs(speed.MaxSpeed);
        var acceleration = Math.Abs(speed.AccSpeed);
        if (!IsFiniteNonNegative(velocity) || velocity <= double.Epsilon || !IsFiniteNonNegative(acceleration))
        {
            message = $"ACS Jog speed profile invalid: axis={axisId}, speedType={speedType}, VEL={speed.MaxSpeed}, ACC={speed.AccSpeed}.";
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisId);
            _api.SetVelocity(axis, velocity);
            _api.SetAcceleration(axis, acceleration);
            _api.SetDeceleration(axis, acceleration);

            var actualVelocity = _api.GetVelocity(axis);
            var actualAcceleration = _api.GetAcceleration(axis);
            var actualDeceleration = _api.GetDeceleration(axis);
            if (!IsProfileWriteMatched(velocity, actualVelocity)
                || !IsProfileWriteMatched(acceleration, actualAcceleration)
                || !IsProfileWriteMatched(acceleration, actualDeceleration))
            {
                message = $"ACS Jog speed profile readback mismatch: axis={axisId}, speedType={speedType}, " +
                          $"expected VEL={velocity:F3}, ACC={acceleration:F3}, DEC={acceleration:F3}; " +
                          $"actual VEL={actualVelocity:F3}, ACC={actualAcceleration:F3}, DEC={actualDeceleration:F3}.";
                return false;
            }

            message = $"ACS Jog speed profile verified: axis={axisId}, speedType={speedType}, " +
                      $"VEL={actualVelocity:F3}, ACC={actualAcceleration:F3}, DEC={actualDeceleration:F3}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"ACS Jog speed profile write/readback failed: axis={axisId}, speedType={speedType}, error={ex.Message}";
            return false;
        }
    }

    private static bool IsFiniteNonNegative(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }

    private static bool IsProfileWriteMatched(double expected, double actual)
    {
        var tolerance = Math.Max(0.001d, Math.Abs(expected) * 0.001d);
        return Math.Abs(expected - actual) <= tolerance;
    }

    public override bool MoveAbsoluteAxis(En_AxisNum axisId, double fpos, bool waitforend = false)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisId);
            if (!PrepareMotorToMove(axis))
            {
                return false;
            }

            _api.ToPoint(MotionFlags.ACSC_NONE, axis, fpos);
            if (waitforend)
            {
                WaitUntilAxisStopped(axisId, InternalTimeout);
            }

            UpdateAxisState(axisId);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS MoveAbsoluteAxis failed: {ex.Message}");
            return false;
        }
    }

    private static string FormatMoveDirection(MoveDirection direction)
    {
        return direction == MoveDirection.反向 ? "反向" : "正向";
    }
}
