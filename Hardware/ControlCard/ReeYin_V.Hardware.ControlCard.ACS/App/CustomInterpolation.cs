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
    public override bool CustomInterpolationMoving(CustomInterPoParam param, Func<string> ConstomOrder, bool waitend = false)
    {
        if (!IsConnected || param == null || ConstomOrder == null)
        {
            return false;
        }

        try
        {
            if (param.TargetPosDic != null && !ValidateLimitPosition(param.TargetPosDic, out var limitMessage))
            {
                Console.WriteLine($"ACS CustomInterpolationMoving limit validation failed: {limitMessage}");
                return false;
            }

            ConfigureInterpolationAxes(param.InterPoAxiss, param.DefaultSpeed);
            if (ConstomOrder() != "OK")
            {
                Console.WriteLine("ACS CustomInterpolationMoving custom order failed.");
                return false;
            }

            var shouldWait = waitend || param.waitforend;
            if (shouldWait && param.InterPoAxiss != null && param.InterPoAxiss.Count > 0)
            {
                return WaitUntilAllAxesStopped(param.InterPoAxiss, InternalTimeout);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS CustomInterpolationMoving failed: {ex.Message}");
            return false;
        }
    }
}
