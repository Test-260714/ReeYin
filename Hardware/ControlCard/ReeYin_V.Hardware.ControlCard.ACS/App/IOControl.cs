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
    public override bool GetAllInput(out bool[] Status)
    {
        Status = new bool[Options.DigitalInputCount];
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            for (var i = 0; i < Status.Length; i++)
            {
                Status[i] = _api.GetInput(GetIoPort(i), GetIoBit(i)) != 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetAllInput failed: {ex.Message}");
            return false;
        }
    }

    public override bool GetAllOutput(out bool[] Status)
    {
        Status = new bool[Options.DigitalOutputCount];
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            for (var i = 0; i < Status.Length; i++)
            {
                Status[i] = _api.GetOutput(GetIoPort(i), GetIoBit(i)) != 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetAllOutput failed: {ex.Message}");
            return false;
        }
    }

    public override bool SetSpecifiedIO(int Part, bool OnOrOff)
    {
        if (!IsConnected || Part < 0 || Part >= Options.DigitalOutputCount)
        {
            return false;
        }

        try
        {
            _api.SetOutput(GetIoPort(Part), GetIoBit(Part), OnOrOff ? 1 : 0);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS SetSpecifiedIO failed: {ex.Message}");
            return false;
        }
    }

    public override bool GetSpecifiedIO(bool InOrOut, int Part, out bool OnOrOff)
    {
        OnOrOff = false;
        var ioCount = InOrOut ? Options.DigitalInputCount : Options.DigitalOutputCount;
        if (!IsConnected || Part < 0 || Part >= ioCount)
        {
            return false;
        }

        try
        {
            OnOrOff = InOrOut
                ? _api.GetInput(GetIoPort(Part), GetIoBit(Part)) != 0
                : _api.GetOutput(GetIoPort(Part), GetIoBit(Part)) != 0;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetSpecifiedIO failed: {ex.Message}");
            return false;
        }
    }

    private static int GetIoPort(int part)
    {
        return part / 32;
    }

    private static int GetIoBit(int part)
    {
        return part % 32;
    }
}
