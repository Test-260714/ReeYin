namespace ReeYin.Hardware.LightController.Rsee.Api;

public static class RseeLightControllerProtocol
{
    public static string BuildSetPulseWidthCommand(int channelIndex, int pulseWidthUs)
    {
        ValidatePulseWidth(pulseWidthUs);
        return $"S{GetChannelCode(channelIndex)}{pulseWidthUs:D4}#";
    }

    public static string BuildReadPulseWidthCommand(int channelIndex)
    {
        return $"S{GetChannelCode(channelIndex)}#";
    }

    public static bool IsPulseWidthSetResponse(int channelIndex, string? response)
    {
        return string.Equals(response?.Trim(), GetChannelCode(channelIndex).ToString(), StringComparison.Ordinal);
    }

    public static bool TryParsePulseWidthResponse(int channelIndex, string? response, out int pulseWidthUs)
    {
        pulseWidthUs = 0;
        response = response?.Trim();
        if (string.IsNullOrEmpty(response) || response.Length != 5)
        {
            return false;
        }

        char expectedChannel = char.ToLowerInvariant(GetChannelCode(channelIndex));
        if (response[0] != expectedChannel)
        {
            return false;
        }

        return int.TryParse(response[1..], out pulseWidthUs);
    }

    public static string BuildSetTriggerModeCommand(int mode, out string expectedResponse)
    {
        if (mode == 2)
        {
            expectedResponse = "H";
            return "SH#";
        }

        expectedResponse = "L";
        return "SL#";
    }

    public static string BuildSetInternalTriggerCycleCommand(int cycleMs)
    {
        if (cycleMs < 10 || cycleMs > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(cycleMs), "Internal trigger cycle must be 10-999 ms.");
        }

        return $"ST{cycleMs:D4}#";
    }

    public static string BuildReadInternalTriggerCycleCommand()
    {
        return "ST#";
    }

    public static bool TryParseInternalTriggerCycleResponse(string? response, out int cycleMs)
    {
        cycleMs = 0;
        response = response?.Trim();
        if (string.IsNullOrEmpty(response) || response.Length != 5 || response[0] != 't')
        {
            return false;
        }

        return int.TryParse(response[1..], out cycleMs);
    }

    private static char GetChannelCode(int channelIndex)
    {
        return channelIndex switch
        {
            1 => 'A',
            2 => 'B',
            3 => 'C',
            4 => 'D',
            _ => throw new ArgumentOutOfRangeException(nameof(channelIndex), "Rsee controller supports channels 1-4.")
        };
    }

    private static void ValidatePulseWidth(int pulseWidthUs)
    {
        if (pulseWidthUs < 0 || pulseWidthUs > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(pulseWidthUs), "Pulse width must be 0-999 us.");
        }
    }
}