using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Prism.Mvvm;

namespace Custom.WaferFlatnessMeasure.Models
{
    public sealed class PointTemperatureRecord
    {
        public string CaptureSessionId { get; set; } = string.Empty;

        public int PointIndex { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public Dictionary<string, double?> Temperatures { get; set; } = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
    }

    [Serializable]
    public sealed class PointTemperatureAddressModel : BindableBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim());
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim());
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public PointTemperatureAddressModel()
        {
        }

        public PointTemperatureAddressModel(string name, string address)
        {
            Name = name;
            Address = address;
        }

        public PointTemperatureAddressModel Clone()
        {
            return new PointTemperatureAddressModel(Name, Address)
            {
                IsEnabled = IsEnabled
            };
        }
    }

    public static class PointTemperatureStorageService
    {
        public const string PointTemperatureCaptureStartedEventName = "PointTemperatureCaptureStarted";

        public const string PointTemperatureRecordCapturedEventName = "PointTemperatureRecordCaptured";

        public const string PointTemperatureAddressConfigUpdatedEventName = "PointTemperatureAddressConfigUpdated";

        private static readonly object _activeTemperatureAddressesLock = new object();

        private static readonly IReadOnlyList<string> _defaultTemperatureAddresses = Array.AsReadOnly(new[]
        {
            "D160",
            "D162",
            "D164",
            "D166",
            "D170",
            "D172",
            "D174",
            "D176"
        });

        private static List<PointTemperatureAddressModel> _activeTemperatureAddresses =
            CreateDefaultTemperatureAddresses();

        public static IReadOnlyList<string> DefaultTemperatureAddresses => _defaultTemperatureAddresses;

        public static List<PointTemperatureAddressModel> CreateDefaultTemperatureAddresses()
        {
            return DefaultTemperatureAddresses
                .Select((address, index) => new PointTemperatureAddressModel($"温度{index + 1}", address))
                .ToList();
        }

        public static List<PointTemperatureAddressModel> CloneTemperatureAddresses(
            IEnumerable<PointTemperatureAddressModel>? source)
        {
            List<PointTemperatureAddressModel> result = source?
                .Where(item => item != null)
                .Select(item => item.Clone())
                .ToList() ?? new List<PointTemperatureAddressModel>();

            return result.Count > 0 ? result : CreateDefaultTemperatureAddresses();
        }

        public static void SetActiveTemperatureAddresses(IEnumerable<PointTemperatureAddressModel>? source)
        {
            List<PointTemperatureAddressModel> next = CloneTemperatureAddresses(source);
            lock (_activeTemperatureAddressesLock)
            {
                _activeTemperatureAddresses = next;
            }
        }

        public static List<string> GetActivePointTemperatureAddresses()
        {
            lock (_activeTemperatureAddressesLock)
            {
                return GetActivePointTemperatureAddresses(_activeTemperatureAddresses);
            }
        }

        public static List<string> GetActivePointTemperatureAddresses(IEnumerable<PointTemperatureAddressModel>? source)
        {
            List<string> addresses = source?
                .Where(item => item != null && item.IsEnabled && !string.IsNullOrWhiteSpace(item.Address))
                .Select(item => item.Address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            return addresses.Count > 0 ? addresses : DefaultTemperatureAddresses.ToList();
        }

        public static List<string> GetActivePointTemperatureAddresses(IEnumerable<string>? source)
        {
            List<string> addresses = source?
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .Select(address => address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            return addresses.Count > 0 ? addresses : DefaultTemperatureAddresses.ToList();
        }

        public static List<PointTemperatureRecord> BuildRecords(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            Func<int, string, double?> readTemperature)
        {
            ArgumentNullException.ThrowIfNull(readTemperature);

            List<PreprocessDatasetModel> points = preDatas?
                .Where(point => point != null)
                .ToList() ?? new List<PreprocessDatasetModel>();
            var records = new List<PointTemperatureRecord>(points.Count);

            for (int index = 0; index < points.Count; index++)
            {
                int pointIndex = index + 1;
                var temperatures = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
                foreach (string address in DefaultTemperatureAddresses)
                {
                    temperatures[address] = readTemperature(pointIndex, address);
                }

                records.Add(new PointTemperatureRecord
                {
                    PointIndex = pointIndex,
                    X = points[index].PosX,
                    Y = points[index].PosY,
                    Temperatures = temperatures
                });
            }

            return records;
        }

        public static List<PointTemperatureRecord> MergeCapturedRecords(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            IEnumerable<PointTemperatureRecord>? capturedRecords)
        {
            List<PreprocessDatasetModel> points = preDatas?
                .Where(point => point != null)
                .ToList() ?? new List<PreprocessDatasetModel>();
            Dictionary<int, PointTemperatureRecord> capturedByIndex = capturedRecords?
                .Where(record => record != null && record.PointIndex > 0)
                .GroupBy(record => record.PointIndex)
                .ToDictionary(group => group.Key, group => CloneRecord(group.Last())) ??
                    new Dictionary<int, PointTemperatureRecord>();
            var records = new List<PointTemperatureRecord>(points.Count);

            for (int index = 0; index < points.Count; index++)
            {
                int pointIndex = index + 1;
                if (capturedByIndex.TryGetValue(pointIndex, out PointTemperatureRecord? capturedRecord))
                {
                    capturedRecord.PointIndex = pointIndex;
                    capturedRecord.X = points[index].PosX;
                    capturedRecord.Y = points[index].PosY;
                    records.Add(capturedRecord);
                    continue;
                }

                records.Add(new PointTemperatureRecord
                {
                    PointIndex = pointIndex,
                    X = points[index].PosX,
                    Y = points[index].PosY,
                    Temperatures = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase)
                });
            }

            return records;
        }

        public static PointTemperatureRecord CloneRecord(PointTemperatureRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            return new PointTemperatureRecord
            {
                CaptureSessionId = record.CaptureSessionId ?? string.Empty,
                PointIndex = record.PointIndex,
                X = record.X,
                Y = record.Y,
                Temperatures = record.Temperatures != null
                    ? new Dictionary<string, double?>(record.Temperatures, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase)
            };
        }

        public static string SavePointTemperaturesCsv(
            IEnumerable<PointTemperatureRecord>? records,
            string? configuredDirectory,
            DateTime? createdAt = null,
            IEnumerable<PointTemperatureAddressModel>? temperatureAddresses = null)
        {
            List<PointTemperatureRecord> exportRecords = records?
                .Where(record => record != null)
                .ToList() ?? new List<PointTemperatureRecord>();
            if (exportRecords.Count == 0)
            {
                return string.Empty;
            }

            string directory = SensorDataStorageService.ResolvePreDatasCsvDirectory(configuredDirectory);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return string.Empty;
            }

            Directory.CreateDirectory(directory);

            string csvPath = Path.Combine(
                directory,
                $"{(createdAt ?? DateTime.Now):yyMMddHHmmss}_PointTemperatures.csv");

            using var writer = new StreamWriter(csvPath, append: false, encoding: Encoding.UTF8);
            List<string> temperatureColumns = GetActivePointTemperatureAddresses(temperatureAddresses);
            writer.WriteLine(string.Join(",", new[] { "PointIndex", "X", "Y" }
                .Concat(temperatureColumns)
                .Select(SensorDataStorageService.EscapeCsvValue)));
            foreach (PointTemperatureRecord record in exportRecords)
            {
                IEnumerable<string> rowValues = new[]
                {
                    record.PointIndex.ToString(CultureInfo.InvariantCulture),
                    FormatCsvNumber(record.X),
                    FormatCsvNumber(record.Y)
                }
                .Concat(temperatureColumns.Select(address =>
                {
                    return record.Temperatures != null &&
                           record.Temperatures.TryGetValue(address, out double? temperature)
                        ? FormatCsvNumber(temperature)
                        : string.Empty;
                }));

                writer.WriteLine(string.Join(",", rowValues.Select(SensorDataStorageService.EscapeCsvValue)));
            }

            return csvPath;
        }

        private static string FormatCsvNumber(double? value)
        {
            return value.HasValue && double.IsFinite(value.Value)
                ? value.Value.ToString("G17", CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}
