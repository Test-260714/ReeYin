namespace ReeYin.LicenseTool;

internal sealed class LicenseGenerationRequest
{
    public bool UseCurrentMachine { get; init; }

    public string MachineCode { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public List<string> Modules { get; init; } = new();

    public string Version { get; init; } = "1.0";

    public DateTime? ExpireTimeUtc { get; init; }

    public bool IsTrial { get; init; }

    public string OutputPath { get; init; } = "license.json";

    public string? PrivateKeyFile { get; init; }
}
