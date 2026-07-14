using System;
using System.Linq;
using ReeYin_V.Core.Services.Project;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public bool TryTransaction(string command, out string response, out string message)
    {
        response = string.Empty;
        if (string.IsNullOrWhiteSpace(command))
        {
            message = "命令不能为空。";
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        try
        {
            response = _api.Transaction(command.Trim()) ?? string.Empty;
            message = "命令交互完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"命令交互失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TrySetAxisEnabled(En_AxisNum axisId, bool isEnabled, out string message)
    {
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        var ok = DoSetAxisEnable(axisId, isEnabled);
        message = ok
            ? $"轴 {axisId} 已{(isEnabled ? "使能" : "失能")}。"
            : $"轴 {axisId} {(isEnabled ? "使能" : "失能")}失败。";
        return ok;
    }

    public bool TryGetAxisTestSnapshot(En_AxisNum axisId, out AcsAxisTestSnapshot snapshot, out string message)
    {
        snapshot = new AcsAxisTestSnapshot { Axis = axisId };
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        try
        {
            UpdateAxisState(axisId);
            var axis = Config?.AllAxis?.FirstOrDefault(item => item.AxisNum == axisId);
            if (axis == null)
            {
                message = $"轴 {axisId} 未配置。";
                return false;
            }

            snapshot = new AcsAxisTestSnapshot
            {
                Axis = axisId,
                IsEnabled = axis.IsEnable,
                IsMoving = axis.IsMoving,
                IsPositiveLimit = axis.IsPositiveLimit,
                IsNegativeLimit = axis.IsNegativeLimit,
                IsResetCompleted = axis.IsResetCompleted,
                CurrentPosition = axis.CurPos,
                AxisStatus = axis.AxisStatus
            };

            message = snapshot.Summary;
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取轴 {axisId} 状态失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryGetAxisMotionProfile(En_AxisNum axisId, out AcsAxisMotionProfile profile, out string message)
    {
        profile = new AcsAxisMotionProfile { Axis = axisId };
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            profile.Message = message;
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisId);
            profile = new AcsAxisMotionProfile
            {
                Axis = axisId,
                Velocity = _api.GetVelocity(axis),
                Acceleration = _api.GetAcceleration(axis),
                Deceleration = _api.GetDeceleration(axis),
                KillDeceleration = _api.GetKillDeceleration(axis),
                Jerk = _api.GetJerk(axis),
                Message = "读取完成。"
            };

            message = $"轴 {axisId} 运动参数读取完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取轴 {axisId} 运动参数失败: {ex.Message}";
            profile.Message = message;
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TrySetAxisMotionProfile(AcsAxisMotionProfile profile, out string message)
    {
        if (profile == null)
        {
            message = "运动参数不能为空。";
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            profile.Message = message;
            return false;
        }

        if (!IsValidMotionProfileValue(profile.Velocity)
            || !IsValidMotionProfileValue(profile.Acceleration)
            || !IsValidMotionProfileValue(profile.Deceleration)
            || !IsValidMotionProfileValue(profile.KillDeceleration)
            || !IsValidMotionProfileValue(profile.Jerk))
        {
            message = "运动参数必须为非负有限数。";
            profile.Message = message;
            return false;
        }

        try
        {
            var axis = ToAcsAxis(profile.Axis);
            _api.SetVelocity(axis, profile.Velocity);
            _api.SetAcceleration(axis, profile.Acceleration);
            _api.SetDeceleration(axis, profile.Deceleration);
            _api.SetKillDeceleration(axis, profile.KillDeceleration);
            _api.SetJerk(axis, profile.Jerk);

            message = $"轴 {profile.Axis} 运动参数已下发。";
            profile.Message = message;
            return true;
        }
        catch (Exception ex)
        {
            message = $"下发轴 {profile.Axis} 运动参数失败: {ex.Message}";
            profile.Message = message;
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private static bool IsValidMotionProfileValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
