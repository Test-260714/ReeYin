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
    protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            if (axisType.HasValue)
            {
                StopAxis(ToAcsAxis(axisType.Value), stopMode);
                UpdateAxisState(axisType.Value);
                return;
            }

            StopConfiguredAxes(stopMode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoStop failed: {ex.Message}");
        }
    }

    private void StopConfiguredAxes(AxisStopMode stopMode)
    {
        foreach (var axis in Config.AllAxis.Where(axis => axis.IsUsing))
        {
            StopAxis(ToConfiguredAcsAxis(axis), stopMode);
            UpdateAxisState(axis.AxisNum);
        }
    }

    private void StopAxis(Axis axis, AxisStopMode stopMode)
    {
        if (stopMode == AxisStopMode.立即停止)
        {
            _api.Kill(axis);
        }
        else
        {
            _api.Halt(axis);
        }
    }

}
