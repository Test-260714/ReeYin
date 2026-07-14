using ReeYin_V.License.Models;
using ReeYin_V.License.Services;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReeYin.LicenseTool;

internal static class LicenseGenerator
{
    private const string DefaultPrivateKeyXml =
        "<RSAKeyValue><Modulus>vi0KtsCUiZT6cXM8XHjgEfVJEAbYYbY69eyonXHS8cUJY0C723iSt9zkKvxU33yOKwVQGhqoUipe4FvOVaby5jDQgETooiD8Rw6rqq6SLRyLGgrEjh3A4Vbbm3rdANvzp/AIiqZWEOTyQbv5YGrBrwtb5VqemaiFJ8vRtKbJZdHRwejFRbXhwi3VuE97gZjX7U62wOB3tikR0fW7IRxDHaJdTKo9AVLRNM0IykE4v+8+XugitnpFPyL134mXsWJBAD5k2fSk5NeaR9FgRzmxGObOr6klDNsE5/YaR3ILr8ewXmfW7dU5ZoaVbINKwQoozvNqFB4Uhv+cGSYOAE/voQ==</Modulus><Exponent>AQAB</Exponent><P>z1oVdWN20LyQLb406MyXkv1hV0PER5Z6Et5xPrJ9EmibeafF4wxYdW0VYwLRE/BzjiWApeuuNstWIUH6AlOeFLpGkmWUnkAnE3K28rF18AOGnN+DciC+irB/Rz7PKQNMsWBRjUn1SHQhqDJLtd4F2AG8MpuBVX5o1TiqdCqge8c=</P><Q>6stXPr61oCZQBYIxwepP4AndVtCPXB0X3VHUyYs7RWKNWaswNkrd9sdV3P8z8EgKxnYkWr8UOs1pnEIcAS+M4ayyJ87X4Ib8onaMZxZylOpijdmE5+pHFNPN+iFd7OUp5IkPGnBsRIe6QQkYi81rbvCYb2obfVgPzqIwMtJ1KVc=</Q><DP>VYhZdbTz3CMMbnIZrTZICDBRKQghPU6LSKFNoYlLIn7YM5TLgl8jVj0LJ26QBGOZpzc9HDReBuhVvR5UHQWVHgPA/L6+UZExDUqywOYHOlyZ+LgSps9vChLITgFQvyBHUJvkyB2L+rk88P7eUEUnr/T3RwDyluuwHtRjK8wxqx0=</DP><DQ>MIp7bghlakehcZIaEVoMy2eer+0MRmHpZiMd19EGHvEiAfDHVeIig3twf/Du3vU17RPNrkkkuIdxFxH/0irveFSIvHwh21Rs9HWHz2QvqiPO3j6jIIMp0N99DQJK9cfm4k2HptKpP33D/uAPiA+e71+kVBxetIo5MmILjuY9vJ8=</DQ><InverseQ>XebzbGvRCqxv4Ca9dg96Ubc3kmY91GBl+F0pEYbjEPbU83V8EF/jwlrtQq1p0hNzFBicZrgwssayotrAE7X8xnPW2CikqAmjrMwuzfgYWe+fINjKz2azkwyZMgCThsQFG6jG7nhez5YIQWWf6lXwAfm1bdwyDxcRHM6MQU95QoI=</InverseQ><D>B5+AlXDEw3sQSunmmoJR4QZnBlhv5gLK/D6fR6hfX0eZp7vZi+OaZfpZvwtcT5ULkdflrRYydxCxuuaTL8XAaM++G9YnSRBvbF2/LdlPjLVfMg0KcfPpXl7/8IyNPVKTwsCRZxVhz82NlGr3hRDicKTQ6zGlt90UcVLvNgGVgxpDSAeFHFqPBrRy9cGEVQulUr+DyX7fpLbwJy4Gyqyfqy+qAzNgVppKxiglRzl6lY9GH4iaXcPhnQLPKINKhku7Fse/HmGFvZ0PY2boBcR4M8T1BJbPbGRkawlCoIqYG4Q5vkUqdDKHz8LFojZvQ8lcDF7lAAfYXT4bx0t4dp2ojQ==</D></RSAKeyValue>";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static LicenseGenerationResult Generate(LicenseGenerationRequest request)
    {
        string machineCode = request.UseCurrentMachine
            ? GetCurrentMachineCode()
            : request.MachineCode;

        var license = new LicenseDocument
        {
            MachineCode = machineCode,
            ExpireTime = request.ExpireTimeUtc,
            CustomerName = request.CustomerName,
            Modules = request.Modules,
            Version = request.Version,
            IsTrial = request.IsTrial,
            Signature = string.Empty
        };

        string canonicalPayload = LicenseService.BuildCanonicalPayload(license);
        string privateKeyXml = LoadPrivateKey(request.PrivateKeyFile);
        license.Signature = SignPayload(canonicalPayload, privateKeyXml);

        string outputPath = Path.GetFullPath(request.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(license, SerializerOptions), Encoding.UTF8);

        return new LicenseGenerationResult
        {
            OutputPath = outputPath,
            License = license
        };
    }

    public static string GetCurrentMachineCode()
    {
        return new HardwareInfoService().GetMachineCode(MachineCodeHashAlgorithm.Sha256);
    }

    public static List<string> ParseModules(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>();
        }

        return raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string LoadPrivateKey(string? privateKeyFile)
    {
        if (string.IsNullOrWhiteSpace(privateKeyFile))
        {
            return DefaultPrivateKeyXml;
        }

        return File.ReadAllText(privateKeyFile, Encoding.UTF8).Trim();
    }

    private static string SignPayload(string canonicalPayload, string privateKeyXml)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);

        using var rsa = new RSACryptoServiceProvider();
        rsa.PersistKeyInCsp = false;
        rsa.FromXmlString(privateKeyXml);
        return Convert.ToBase64String(rsa.SignData(payloadBytes, SHA256.Create()));
    }
}
