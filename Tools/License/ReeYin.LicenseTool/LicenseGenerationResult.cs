using ReeYin_V.License.Models;

namespace ReeYin.LicenseTool;

internal sealed class LicenseGenerationResult
{
    public required string OutputPath { get; init; }

    public required LicenseDocument License { get; init; }
}
