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
    protected override bool DoInit()
    {
        EnsureOptions();

        try
        {
            State = HardwareState.Connecting;

            lock (_syncRoot)
            {
                OpenCommunication();
                IsConnected = true;
                State = HardwareState.Connected;
                EnsurePositionBuffers();
                DoConfigure();
                EnsureDBufferLciDeclarations();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS DoInit failed: {ex.Message}");
            lock (_syncRoot)
            {
                try
                {
                    _api.CloseComm();
                }
                catch (Exception closeEx)
                {
                    Console.WriteLine($"ACS CloseComm after init failure failed: {closeEx.Message}");
                }
                finally
                {
                    _api = new Api();
                    IsConnected = false;
                    State = HardwareState.Error;
                }
            }

            return false;
        }
    }

    protected override void DoConfigure()
    {
        if (!IsConnected)
        {
            return;
        }

        foreach (var axis in Config.AllAxis.Where(axis => axis.IsUsing))
        {
            var acsAxis = ToConfiguredAcsAxis(axis);
            var speed = ResolveSpeedSetting(axis.AxisNum);
            if (speed == null)
            {
                continue;
            }

            TryExecute(() =>
            {
                _api.SetVelocity(acsAxis, Math.Abs(speed.MaxSpeed));
                _api.SetAcceleration(acsAxis, Math.Abs(speed.AccSpeed));
                _api.SetDeceleration(acsAxis, Math.Abs(speed.AccSpeed));
            }, $"configure axis {axis.AxisNo}");
        }
    }

    protected override void DoClose()
    {
        lock (_syncRoot)
        {
            if (!IsConnected)
            {
                State = HardwareState.Closed;
                return;
            }

            State = HardwareState.Disconnecting;

            try
            {
                StopConfiguredAxes(AxisStopMode.减速停止);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ACS stop axes during close failed: {ex.Message}");
            }

            try
            {
                _api.CloseComm();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ACS CloseComm during close failed: {ex.Message}");
            }
            finally
            {
                _api = new Api();
                IsConnected = false;
                State = HardwareState.Closed;
            }
        }
    }

    private void OpenCommunication()
    {
        var options = Options;

        _api = new Api();
        switch (options.ConnectionMode)
        {
            case AcsConnectionMode.Ethernet:
                var port = GetEthernetPort(options);
                if (options.UseTcp)
                {
                    _api.OpenCommEthernetTCP(options.RemoteAddress, port);
                }
                else
                {
                    _api.OpenCommEthernetUDP(options.RemoteAddress, port);
                }

                break;

            case AcsConnectionMode.Serial:
                _api.OpenCommSerial(GetSerialPort(), GetSerialBaudRate());
                break;

            case AcsConnectionMode.PCI:
                _api.OpenCommPCI(options.PciSlotNumber);
                break;

            case AcsConnectionMode.Simulator:
            default:
                _api.OpenCommSimulator();
                break;
        }
    }

    private int GetSerialPort()
    {
        if (!string.IsNullOrWhiteSpace(Config.Com))
        {
            var digits = new string(Config.Com.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var configuredPort) && configuredPort > 0)
            {
                return configuredPort;
            }
        }

        return Math.Max(1, Options.SerialPort);
    }

    private int GetSerialBaudRate()
    {
        var optionsBaudRate = Options.SerialBaudRate;
        if (optionsBaudRate != -1)
        {
            return optionsBaudRate;
        }

        if (Config.BaudRate > 0)
        {
            return Config.BaudRate;
        }

        return optionsBaudRate;
    }

    private static int GetEthernetPort(AcsControlCardOptions options)
    {
        var streamPort = (int)EthernetCommOption.ACSC_SOCKET_STREAM_PORT;
        var datagramPort = (int)EthernetCommOption.ACSC_SOCKET_DGRAM_PORT;

        if (!options.UseTcp && options.EthernetPort == streamPort)
        {
            return datagramPort;
        }

        if (options.EthernetPort > 0)
        {
            return options.EthernetPort;
        }

        return options.UseTcp ? streamPort : datagramPort;
    }
}
