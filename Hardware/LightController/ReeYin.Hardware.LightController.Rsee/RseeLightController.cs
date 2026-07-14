using ReeYin.Hardware.LightController.Models;
using ReeYin.Hardware.LightController.Rsee.Api;
using System.Net.Sockets;
using System.Text;

namespace ReeYin.Hardware.LightController.Rsee;

public class RseeLightController : LightControllerBase
{
    private const int ConnectTimeoutMs = 1500;
    private const int IoTimeoutMs = 1000;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private readonly Dictionary<int, int> _lastPulseWidths = new();
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private int _triggerMode = 1;

    public RseeLightController()
    {
        VenderName = "Rsee";
        VenderType = "LightController";
        IP = "192.168.1.252";
        Port = 8234;
        ChannelCount = 4;
        ConnectionType = 0;
    }

    public override bool Init()
    {
        try
        {
            if (IsConnected)
            {
                return true;
            }

            if (ConnectionType != 0)
            {
                Console.WriteLine("Rsee light controller currently supports TCP connection only.");
                return false;
            }

            var client = new TcpClient
            {
                NoDelay = true,
                ReceiveTimeout = IoTimeoutMs,
                SendTimeout = IoTimeoutMs
            };

            var connectTask = client.ConnectAsync(IP, Port);
            if (!connectTask.Wait(ConnectTimeoutMs))
            {
                client.Dispose();
                Console.WriteLine($"Rsee light controller connect timeout: {IP}:{Port}");
                return false;
            }

            connectTask.GetAwaiter().GetResult();
            _tcpClient = client;
            _stream = client.GetStream();
            _stream.ReadTimeout = IoTimeoutMs;
            _stream.WriteTimeout = IoTimeoutMs;
            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rsee light controller connect failed: {ex.Message}");
            Close();
            return false;
        }
    }

    public override void Close()
    {
        try
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
        finally
        {
            _stream = null;
            _tcpClient = null;
            IsConnected = false;
        }
    }

    public override bool SetBrightness(int channelIndex, int value)
    {
        if (value < 0 || value > 255)
        {
            return false;
        }

        int pulseWidthUs = ScaleBrightnessToPulseWidth(value);
        return SetStrobeTime(channelIndex, pulseWidthUs);
    }

    public override int GetBrightness(int channelIndex)
    {
        int pulseWidthUs = GetStrobeTime(channelIndex);
        return pulseWidthUs < 0 ? -1 : ScalePulseWidthToBrightness(pulseWidthUs);
    }

    public override bool SetMultiBrightness(Dictionary<int, int> channelValues)
    {
        foreach (var (channelIndex, value) in channelValues)
        {
            if (!SetBrightness(channelIndex, value))
            {
                return false;
            }
        }

        return true;
    }

    public override bool SetChannelOnOff(int channelIndex, bool isOn)
    {
        if (!isOn)
        {
            return SetStrobeTime(channelIndex, 0);
        }

        int pulseWidthUs = _lastPulseWidths.TryGetValue(channelIndex, out int lastPulseWidth) && lastPulseWidth > 0
            ? lastPulseWidth
            : 999;
        return SetStrobeTime(channelIndex, pulseWidthUs);
    }

    public override bool GetChannelOnOff(int channelIndex)
    {
        int pulseWidthUs = GetStrobeTime(channelIndex);
        return pulseWidthUs > 0;
    }

    public override bool SetStrobeTime(int channelIndex, int strobeTime)
    {
        try
        {
            string command = RseeLightControllerProtocol.BuildSetPulseWidthCommand(channelIndex, strobeTime);
            string response = SendCommand(command);
            bool success = RseeLightControllerProtocol.IsPulseWidthSetResponse(channelIndex, response);
            if (success)
            {
                _lastPulseWidths[channelIndex] = strobeTime;
            }

            return success;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Rsee strobe time parameter invalid: {ex.Message}");
            return false;
        }
    }

    public override int GetStrobeTime(int channelIndex)
    {
        try
        {
            string command = RseeLightControllerProtocol.BuildReadPulseWidthCommand(channelIndex);
            string response = SendCommand(command);
            return RseeLightControllerProtocol.TryParsePulseWidthResponse(channelIndex, response, out int value) ? value : -1;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Rsee channel parameter invalid: {ex.Message}");
            return -1;
        }
    }

    public override bool SetTriggerMode(int mode)
    {
        string command = RseeLightControllerProtocol.BuildSetTriggerModeCommand(mode, out string expectedResponse);
        string response = SendCommand(command);
        bool success = string.Equals(response, expectedResponse, StringComparison.Ordinal);
        if (success)
        {
            _triggerMode = mode == 2 ? 2 : 1;
        }

        return success;
    }

    public override int GetTriggerMode()
    {
        return _triggerMode;
    }

    public bool SetInternalTriggerCycle(int cycleMs)
    {
        try
        {
            string command = RseeLightControllerProtocol.BuildSetInternalTriggerCycleCommand(cycleMs);
            return string.Equals(SendCommand(command), "T", StringComparison.Ordinal);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Rsee internal trigger cycle invalid: {ex.Message}");
            return false;
        }
    }

    public int GetInternalTriggerCycle()
    {
        string response = SendCommand(RseeLightControllerProtocol.BuildReadInternalTriggerCycleCommand());
        return RseeLightControllerProtocol.TryParseInternalTriggerCycleResponse(response, out int cycleMs) ? cycleMs : -1;
    }

    private string SendCommand(string command)
    {
        if (_stream == null || _tcpClient == null || !IsConnected)
        {
            return string.Empty;
        }

        _commandLock.Wait();
        try
        {
            byte[] request = Encoding.ASCII.GetBytes(command);
            _stream.Write(request, 0, request.Length);
            _stream.Flush();

            int expectedLength = GetExpectedResponseLength(command);
            byte[] buffer = new byte[64];
            var response = new StringBuilder(expectedLength);
            while (response.Length < expectedLength)
            {
                int readLength = Math.Min(buffer.Length, expectedLength - response.Length);
                int length = _stream.Read(buffer, 0, readLength);
                if (length <= 0)
                {
                    break;
                }

                response.Append(Encoding.ASCII.GetString(buffer, 0, length));
            }

            return response.ToString().Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rsee command failed: {command}, {ex.Message}");
            IsConnected = false;
            return string.Empty;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private static int ScaleBrightnessToPulseWidth(int brightness)
    {
        return (int)Math.Round(brightness * 999d / 255d, MidpointRounding.AwayFromZero);
    }

    private static int GetExpectedResponseLength(string command)
    {
        if (command.Length == 3 && command[0] == 'S' && command[2] == '#')
        {
            return 5;
        }

        if (string.Equals(command, RseeLightControllerProtocol.BuildReadInternalTriggerCycleCommand(), StringComparison.Ordinal))
        {
            return 5;
        }

        return 1;
    }

    private static int ScalePulseWidthToBrightness(int pulseWidthUs)
    {
        return (int)Math.Round(Math.Clamp(pulseWidthUs, 0, 999) * 255d / 999d, MidpointRounding.AwayFromZero);
    }
}
