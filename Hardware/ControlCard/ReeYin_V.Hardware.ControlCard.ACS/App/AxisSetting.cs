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
    protected override bool DoGetAxisEnable(En_AxisNum axisType)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var state = _api.GetMotorState(ToAcsAxis(axisType));
            var enabled = (state & MotorStates.ACSC_MST_ENABLE) != 0;
            ApplyAxisState(axisType, state);
            return enabled;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoGetAxisEnable failed: {ex.Message}");
            return false;
        }
    }

    protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var axis = ToAcsAxis(axisType);
            if (v)
            {
                _api.Enable(axis);
                _api.WaitMotorEnabled(axis, 1, InternalTimeout);
            }
            else
            {
                if (!DoGetAxisStopped(axisType))
                {
                    _api.Kill(axis);
                }

                _api.Disable(axis);
            }

            UpdateAxisState(axisType);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoSetAxisEnable failed: {ex.Message}");
            return false;
        }
    }

    protected override bool DoGetAxisStopped(En_AxisNum axisType)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var state = _api.GetMotorState(ToAcsAxis(axisType));
            ApplyAxisState(axisType, state);
            return (state & MotorStates.ACSC_MST_MOVE) == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoGetAxisStopped failed: {ex.Message}");
            return false;
        }
    }

    public override bool SetAxisEnabled(short axisId, bool isEnabled)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var axis = ToZeroBasedAcsAxis(axisId);
            if (isEnabled)
            {
                _api.Enable(axis);
                _api.WaitMotorEnabled(axis, 1, InternalTimeout);
            }
            else
            {
                _api.Disable(axis);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS SetAxisEnabled failed: {ex.Message}");
            return false;
        }
    }

    private bool PrepareMotorToMove(Axis axis)
    {
        var axisNo = (int)axis;
        if (!GetFlag($"?MFLAGS({axisNo}).#BRUSHL"))
        {
            return true;
        }

        if (GetFlag($"?MFLAGS({axisNo}).#BRUSHOK"))
        {
            return true;
        }

        _api.Commut(axis);
        _api.WaitMotorCommutated(axis, 1, InternalTimeout);
        return true;
    }

    private bool GetFlag(string command)
    {
        var result = _api.Transaction(command)?.Trim();
        return result == "1";
    }

    private double GetAxisVelocityForSpeedMode(En_AxisNum axisType, EN_SpeedType speedType)
    {
        var axis = Config.AllAxis.FirstOrDefault(item => item.AxisNum == axisType);
        return Math.Abs(axis?.SpeedDict1?.FirstOrDefault(item => item.SpeedType == speedType)?.MaxSpeed
                        ?? ResolveSpeedSetting(axisType)?.MaxSpeed
                        ?? 100d);
    }

    private double GetInterpolationVelocity(IEnumerable<En_AxisNum> axisIds, EN_SpeedType speedType)
    {
        if (Config.DefaultInterpCS?.MaxSpeed > 0d)
        {
            return Math.Abs(Config.DefaultInterpCS.MaxSpeed);
        }

        return axisIds?
            .Select(axis => GetAxisVelocityForSpeedMode(axis, speedType))
            .Where(speed => speed > 0d)
            .DefaultIfEmpty(0d)
            .Min() ?? 0d;
    }

    private void ConfigureInterpolationAxes(IEnumerable<En_AxisNum> axisIds, EN_SpeedType speedType)
    {
        if (axisIds == null)
        {
            return;
        }

        var interpolationAcceleration = Math.Abs(Config.DefaultInterpCS?.AccSpeed ?? 0d);
        foreach (var axisId in axisIds.Distinct())
        {
            var axis = ToAcsAxis(axisId);
            var speed = Config.AllAxis
                .FirstOrDefault(item => item.AxisNum == axisId)?
                .SpeedDict1?
                .FirstOrDefault(item => item.SpeedType == speedType);
            var velocity = Math.Abs(speed?.MaxSpeed ?? Config.DefaultInterpCS?.MaxSpeed ?? GetAxisDefaultVelocity(axisId));
            var acceleration = Math.Abs(speed?.AccSpeed ?? interpolationAcceleration);

            if (velocity > 0d)
            {
                _api.SetVelocity(axis, velocity);
            }

            if (acceleration > 0d)
            {
                _api.SetAcceleration(axis, acceleration);
                _api.SetDeceleration(axis, acceleration);
            }
        }
    }

    private Axis ToAcsAxis(En_AxisNum axisType)
    {
        var axis = Config.AllAxis.FirstOrDefault(item => item.AxisNum == axisType);
        return axis != null ? ToConfiguredAcsAxis(axis) : ToZeroBasedAcsAxis((short)(int)axisType);
    }

    private static Axis ToConfiguredAcsAxis(SingleAxisParam axis)
    {
        return ToZeroBasedAcsAxis((short)Math.Max(0, axis.AxisNo - 1));
    }

    private static Axis ToZeroBasedAcsAxis(short zeroBasedAxisNo)
    {
        return (Axis)Math.Max(0, (int)zeroBasedAxisNo);
    }

    private static RotationDirection ToAcsRotation(DirOfRotation direction)
    {
        return (int)direction == 0
            ? RotationDirection.ACSC_CLOCKWISE
            : RotationDirection.ACSC_COUNTERCLOCKWISE;
    }

    private void UpdateAxisState(En_AxisNum axisType)
    {
        if (!IsConnected)
        {
            return;
        }

        ApplyAxisState(axisType, _api.GetMotorState(ToAcsAxis(axisType)));
    }

    private static bool HasFlagByName<TEnum>(TEnum value, string flagName) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(flagName, out var flag))
        {
            return false;
        }

        var rawValue = Convert.ToInt64(value);
        var rawFlag = Convert.ToInt64(flag);
        return rawFlag != 0 && (rawValue & rawFlag) != 0;
    }

    private void ApplyAxisState(En_AxisNum axisType, MotorStates state)
    {
        var axis = Config.AllAxis.FirstOrDefault(item => item.AxisNum == axisType);
        if (axis == null)
        {
            return;
        }

        var isEnabled = (state & MotorStates.ACSC_MST_ENABLE) != 0;
        var isMoving = (state & MotorStates.ACSC_MST_MOVE) != 0;
        var inPosition = (state & MotorStates.ACSC_MST_INPOS) != 0;
        AxisStates axisState = 0;
        try
        {
            axisState = _api.GetAxisState(ToConfiguredAcsAxis(axis));
        }
        catch
        {
            axisState = 0;
        }

        var hasFault = HasFlagByName(state, "ACSC_MST_FAULT")
                       || HasFlagByName(state, "ACSC_MST_SAFETY")
                       || HasFlagByName(state, "ACSC_MST_ERROR");
        var positiveLimit = HasFlagByName(axisState, "ACSC_AST_LPOS")
                            || HasFlagByName(axisState, "ACSC_AST_PE");
        var negativeLimit = HasFlagByName(axisState, "ACSC_AST_LNEG")
                            || HasFlagByName(axisState, "ACSC_AST_NE");

        axis.IsEnable = isEnabled;
        axis.IsMoving = isMoving;
        axis.IsPositiveLimit = positiveLimit;
        axis.IsNegativeLimit = negativeLimit;
        axis.AxisStatus = 0;
        if (isEnabled)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.MotorEnabled;
        }

        if (isMoving)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.PlanningInMotion;
        }

        if (inPosition)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.MotorInPosition;
        }

        if (hasFault)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.FollowErrorOverLimit;
        }

        if (positiveLimit)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.PositiveLimitTriggered;
        }

        if (negativeLimit)
        {
            axis.AxisStatus |= (int)AxisStatusFlags.NegativeLimitTriggered;
        }

        if (IsConnected)
        {
            try
            {
                axis.CurPos = Math.Round(_api.GetFPosition(ToConfiguredAcsAxis(axis)), 3);
            }
            catch
            {
                // Position is best-effort here; motion state should still be returned.
            }
        }
    }

    private void UpdateAxisStates(IEnumerable<En_AxisNum> axisTypes)
    {
        foreach (var axisType in axisTypes.Distinct())
        {
            UpdateAxisState(axisType);
        }
    }

}
