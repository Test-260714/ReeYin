using System.Globalization;
using System.Text;
using System.Windows;

namespace ReeYin.LicenseTool;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {

        if (args.Length == 0)
        {
            var application = new Application
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose
            };

            return application.Run(new MainWindow());
        }

        var options = ToolOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            Console.Error.WriteLine(options.Error);
            PrintUsage();
            return 1;
        }

        try
        {
            var result = LicenseGenerator.Generate(options.ToRequest());
            var license = result.License;

            Console.WriteLine($"License generated: {result.OutputPath}");
            Console.WriteLine($"MachineCode: {license.MachineCode}");
            Console.WriteLine($"Type: {license.Type}");
            Console.WriteLine($"ExpireTime: {(license.ExpireTime.HasValue ? license.ExpireTime.Value.ToString("O", CultureInfo.InvariantCulture) : "PERMANENT")}");
            Console.WriteLine($"Modules: {(license.NormalizedModules.Count == 0 ? "ALL" : string.Join(",", license.NormalizedModules))}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to generate license: {ex.Message}");
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ReeYin.LicenseTool --machine <code> --customer <name> --version <version> --output <file> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --current-machine            Use this computer's machine code instead of --machine.");
        Console.WriteLine("  --modules <a,b,c>            Authorized modules. Omit, empty, or ALL means all modules.");
        Console.WriteLine("  --expire <date|permanent>    Expiration time. Examples: 2026-12-31, 2026-12-31T23:59:59+08:00.");
        Console.WriteLine("  --days <number>              Expire after N days from now.");
        Console.WriteLine("  --trial                      Mark this as a trial license. Requires --expire or --days.");
        Console.WriteLine("  --private-key-file <file>    Use an external RSA private key XML file.");
        Console.WriteLine("  --help                       Show help.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ReeYin.LicenseTool --machine ABC123 --customer ACME --version 1.0 --modules ApplicatoinMainModule,Custom.WaferFlatnessMeasure --expire 2026-12-31 --output license.json");
        Console.WriteLine("  ReeYin.LicenseTool --current-machine --customer Trial --version Trial --trial --days 30 --output trial.json");
    }

    private sealed class ToolOptions
    {
        public bool ShowHelp { get; private set; }

        public string? Error { get; private set; }

        public string? MachineCode { get; private set; }

        public bool UseCurrentMachine { get; private set; }

        public string CustomerName { get; private set; } = string.Empty;

        public List<string> Modules { get; private set; } = new();

        public string Version { get; private set; } = "1.0";

        public DateTime? ExpireTimeUtc { get; private set; }

        public bool IsTrial { get; private set; }

        public string OutputPath { get; private set; } = "license.json";

        public string? PrivateKeyFile { get; private set; }

        public LicenseGenerationRequest ToRequest()
        {
            return new LicenseGenerationRequest
            {
                UseCurrentMachine = UseCurrentMachine,
                MachineCode = MachineCode ?? string.Empty,
                CustomerName = CustomerName,
                Modules = Modules,
                Version = Version,
                ExpireTimeUtc = ExpireTimeUtc,
                IsTrial = IsTrial,
                OutputPath = OutputPath,
                PrivateKeyFile = PrivateKeyFile
            };
        }

        public static ToolOptions Parse(string[] args)
        {
            var options = new ToolOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "--help":
                    case "-h":
                    case "/?":
                        options.ShowHelp = true;
                        return options;
                    case "--current-machine":
                        options.UseCurrentMachine = true;
                        break;
                    case "--machine":
                        options.MachineCode = ReadValue(args, ref i, arg, options);
                        break;
                    case "--customer":
                        options.CustomerName = ReadValue(args, ref i, arg, options);
                        break;
                    case "--modules":
                        options.Modules = LicenseGenerator.ParseModules(ReadValue(args, ref i, arg, options));
                        break;
                    case "--version":
                        options.Version = ReadValue(args, ref i, arg, options);
                        break;
                    case "--expire":
                        options.ExpireTimeUtc = ParseExpireTime(ReadValue(args, ref i, arg, options), options);
                        break;
                    case "--days":
                        options.ExpireTimeUtc = ParseDays(ReadValue(args, ref i, arg, options), options);
                        break;
                    case "--trial":
                        options.IsTrial = true;
                        break;
                    case "--output":
                        options.OutputPath = ReadValue(args, ref i, arg, options);
                        break;
                    case "--private-key-file":
                        options.PrivateKeyFile = ReadValue(args, ref i, arg, options);
                        break;
                    default:
                        options.Error = $"Unknown argument: {arg}";
                        return options;
                }

                if (!string.IsNullOrWhiteSpace(options.Error))
                {
                    return options;
                }
            }

            if (!options.UseCurrentMachine && string.IsNullOrWhiteSpace(options.MachineCode))
            {
                options.Error = "--machine is required unless --current-machine is specified.";
            }
            else if (options.IsTrial && !options.ExpireTimeUtc.HasValue)
            {
                options.Error = "--trial requires --expire or --days.";
            }

            return options;
        }

        private static string ReadValue(string[] args, ref int index, string optionName, ToolOptions options)
        {
            if (index + 1 >= args.Length)
            {
                options.Error = $"{optionName} requires a value.";
                return string.Empty;
            }

            index++;
            return args[index];
        }

        private static DateTime? ParseExpireTime(string raw, ToolOptions options)
        {
            if (raw.Equals("permanent", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("never", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset parsed))
            {
                options.Error = $"Invalid --expire value: {raw}";
                return null;
            }

            return parsed.UtcDateTime;
        }

        private static DateTime? ParseDays(string raw, ToolOptions options)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) || days <= 0)
            {
                options.Error = $"Invalid --days value: {raw}";
                return null;
            }

            return DateTime.UtcNow.AddDays(days);
        }
    }
}
