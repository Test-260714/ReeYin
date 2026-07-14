using System;
using Prism.Mvvm;
using ReeYin_V.Core.Services.DataCollectRelated;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;

namespace Custom.WaferFlatnessMeasure.Models
{
    public class PreprocessDatasetModel : BindableBase
    {
        public const string DefaultUpSurfaceOriginalDataName = "Channel4.THICKNESS";
        public const string DefaultDownSurfaceOriginalDataName = "Channel5.THICKNESS";

        private static readonly string[] _originalDataSensorTypeNames =
        {
            "DIST1",
            "DIST2",
            "PEAK1_HEIGHT",
            "PEAK2_HEIGHT",
            "INTENSITY",
            "EXPTIME",
            "THICKNESS"
        };

        private static readonly string[] _originalDataControllerTypeNames =
        {
            "TIMESTAMP",
            "ENCODER1",
            "ENCODER2",
            "MATH1",
            "MATH2",
            "MATH3",
            "MATH4",
            "MATH5",
            "MATH6",
            "MATH7",
            "MATH8",
            "MULTI_MATH1",
            "MULTI_MATH2",
            "MULTI_MATH3",
            "MULTI_MATH4",
            "ANY_MATH1",
            "ANY_MATH2"
        };

        private static readonly IReadOnlyList<string> _originalDataValueNames = CreateOriginalDataValueNames();

        public static IReadOnlyList<string> OriginalDataValueNames => _originalDataValueNames;

        private double _posX;
        public double PosX
        {
            get { return _posX; }
            set { SetProperty(ref _posX, value); }
        }

        private double _posY;
        public double PosY
        {
            get { return _posY; }
            set { SetProperty(ref _posY, value); }
        }

        private double _upSurface;
        public double UpSurface
        {
            get { return _upSurface; }
            set { SetProperty(ref _upSurface, value); }
        }

        private double _downSurface;
        public double DownSurface
        {
            get { return _downSurface; }
            set { SetProperty(ref _downSurface, value); }
        }

        private double _thickness = double.NaN;
        public double Thickness
        {
            get { return _thickness; }
            set { SetProperty(ref _thickness, value); }
        }

        private Dictionary<string, double> _originalDataValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> OriginalDataValues
        {
            get { return _originalDataValues; }
            set
            {
                _originalDataValues = value != null
                    ? new Dictionary<string, double>(value, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                RaisePropertyChanged();
            }
        }

        public static PreprocessDatasetModel FromCollectPosition((double X, double Y) collectPos)
        {
            return new PreprocessDatasetModel
            {
                PosX = collectPos.X,
                PosY = collectPos.Y
            };
        }

        public static PreprocessDatasetModel FromMeasureData(MeasureData? measureData)
        {
            return FromMeasureData(
                measureData,
                DefaultUpSurfaceOriginalDataName,
                DefaultDownSurfaceOriginalDataName);
        }

        public static PreprocessDatasetModel FromMeasureData(
            MeasureData? measureData,
            string? upSurfaceOriginalDataName,
            string? downSurfaceOriginalDataName)
        {
            if (measureData == null)
            {
                return new PreprocessDatasetModel();
            }

            Dictionary<string, double> originalDataValues = GetMeasureDataOriginalDataValues(measureData);
            double upSurface = GetMeasureDataOriginalDataValue(
                measureData,
                upSurfaceOriginalDataName,
                DefaultUpSurfaceOriginalDataName);
            double downSurface = GetMeasureDataOriginalDataValue(
                measureData,
                downSurfaceOriginalDataName,
                DefaultDownSurfaceOriginalDataName);

            SetOriginalDataValue(
                originalDataValues,
                NormalizeOriginalDataValueName(upSurfaceOriginalDataName, DefaultUpSurfaceOriginalDataName),
                upSurface);
            SetOriginalDataValue(
                originalDataValues,
                NormalizeOriginalDataValueName(downSurfaceOriginalDataName, DefaultDownSurfaceOriginalDataName),
                downSurface);

            return new PreprocessDatasetModel
            {
                PosX = measureData.X,
                PosY = measureData.Y,
                UpSurface = upSurface,
                DownSurface = downSurface,
                OriginalDataValues = originalDataValues
            };
        }

        public static List<PreprocessDatasetModel> CreateFromCollectPositions(IEnumerable<(double X, double Y)>? collectPositions)
        {
            return collectPositions?.Select(FromCollectPosition).ToList() ?? new List<PreprocessDatasetModel>();
        }

        public static List<PreprocessDatasetModel> CreateFromMeasureDatas(IEnumerable<MeasureData>? measureDatas)
        {
            return measureDatas?.Select(FromMeasureData).ToList() ?? new List<PreprocessDatasetModel>();
        }

        public static List<PreprocessDatasetModel> CreateFromMeasureDatas(
            IEnumerable<MeasureData>? measureDatas,
            string? upSurfaceOriginalDataName,
            string? downSurfaceOriginalDataName)
        {
            return measureDatas?
                .Select(data => FromMeasureData(
                    data,
                    upSurfaceOriginalDataName,
                    downSurfaceOriginalDataName))
                .ToList() ?? new List<PreprocessDatasetModel>();
        }

        public static List<PreprocessDatasetModel> Clone(IEnumerable<PreprocessDatasetModel>? preDatas)
        {
            return preDatas?
                .Select(data => new PreprocessDatasetModel
                {
                    PosX = data.PosX,
                    PosY = data.PosY,
                    UpSurface = data.UpSurface,
                    DownSurface = data.DownSurface,
                    Thickness = data.Thickness,
                    OriginalDataValues = data.OriginalDataValues
                })
                .ToList() ?? new List<PreprocessDatasetModel>();
        }

        public static List<PreprocessDatasetModel> ApplyUpSurfaceCompensation(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            IReadOnlyList<double[]>? compensatedPoints)
        {
            List<PreprocessDatasetModel> compensatedPreDatas = Clone(preDatas);
            if (compensatedPoints == null || compensatedPreDatas.Count != compensatedPoints.Count)
            {
                return compensatedPreDatas;
            }

            for (int i = 0; i < compensatedPreDatas.Count; i++)
            {
                if (compensatedPoints[i] == null || compensatedPoints[i].Length < 3)
                {
                    continue;
                }

                compensatedPreDatas[i].PosX = compensatedPoints[i][0];
                compensatedPreDatas[i].PosY = compensatedPoints[i][1];
                compensatedPreDatas[i].UpSurface = compensatedPoints[i][2];
            }

            return compensatedPreDatas;
        }

        public static List<double[]> ToPointCloud(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            Func<PreprocessDatasetModel, double> selector)
        {
            if (preDatas == null)
            {
                return new List<double[]>();
            }

            return preDatas
                .Where(data =>
                    data != null &&
                    IsFiniteDouble(data.PosX) &&
                    IsFiniteDouble(data.PosY) &&
                    IsFiniteDouble(selector(data)))
                .Select(data => new[] { data.PosX, data.PosY, selector(data) })
                .ToList();
        }

        public static List<double[]> ToUpSurfacePointCloud(IEnumerable<PreprocessDatasetModel>? preDatas)
        {
            return ToPointCloud(preDatas, data => data.UpSurface);
        }

        public static List<double[]> ToDownSurfacePointCloud(IEnumerable<PreprocessDatasetModel>? preDatas)
        {
            return ToPointCloud(preDatas, data => data.DownSurface);
        }

        public static double CalculateThickness(double upSurface, double downSurface, double calibrationC)
        {
            if (!IsFiniteDouble(upSurface) ||
                !IsFiniteDouble(downSurface) ||
                !IsFiniteDouble(calibrationC))
            {
                return double.NaN;
            }

            return -upSurface - downSurface + calibrationC;
        }

        public static void UpdateThicknesses(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            double calibrationC)
        {
            if (preDatas == null)
            {
                return;
            }

            foreach (PreprocessDatasetModel data in preDatas)
            {
                if (data == null)
                {
                    continue;
                }

                data.Thickness = CalculateThickness(data.UpSurface, data.DownSurface, calibrationC);
            }
        }

        public static void ExportToCsv(IEnumerable<PreprocessDatasetModel>? preDatas, string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            List<PreprocessDatasetModel> exportDatas = Clone(preDatas);
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using StreamWriter writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
            List<string> extraOriginalDataColumns = exportDatas
                .SelectMany(data => data.OriginalDataValues?.Keys ?? Enumerable.Empty<string>())
                .Where(column => !IsCoreCsvHeader(column))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
                .ToList();

            writer.WriteLine(string.Join(
                ",",
                new[] { "PosX", "PosY", "UpSurface", "DownSurface", "Thickness" }
                    .Concat(extraOriginalDataColumns)
                    .Select(EscapeCsvValue)));

            foreach (PreprocessDatasetModel data in exportDatas)
            {
                IEnumerable<string> rowValues = new[]
                {
                    FormatDouble(data.PosX),
                    FormatDouble(data.PosY),
                    FormatDouble(data.UpSurface),
                    FormatDouble(data.DownSurface),
                    FormatDouble(data.Thickness)
                }.Concat(extraOriginalDataColumns.Select(column =>
                    data.TryGetOriginalDataValue(column, out double value)
                        ? FormatDouble(value)
                        : string.Empty));

                writer.WriteLine(string.Join(",", rowValues.Select(EscapeCsvValue)));
            }
        }

        public static List<PreprocessDatasetModel> LoadFromCsv(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV文件不存在: {filePath}", filePath);
            }

            List<PreprocessDatasetModel> preDatas = new List<PreprocessDatasetModel>();
            List<string>? headers = null;

            foreach (string rawLine in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                string line = rawLine.Trim();
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                List<string> tokens = SplitTokens(line);
                if (tokens.Count == 0)
                {
                    continue;
                }

                if (headers == null && LooksLikeHeader(tokens))
                {
                    headers = tokens;
                    continue;
                }

                if (TryParseCsvRecord(tokens, headers, out PreprocessDatasetModel? preData))
                {
                    preDatas.Add(preData);
                }
            }

            return preDatas;
        }

        private static bool IsFiniteDouble(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static string NormalizeOriginalDataValueName(string? valueName, string fallbackValueName)
        {
            string normalizedFallback = NormalizeOriginalDataValueNameCore(fallbackValueName)
                ?? DefaultUpSurfaceOriginalDataName;

            if (string.IsNullOrWhiteSpace(valueName))
            {
                return normalizedFallback;
            }

            return NormalizeOriginalDataValueNameCore(valueName) ?? normalizedFallback;
        }

        public static string NormalizeOriginalDataValueName(string? valueName)
        {
            if (string.IsNullOrWhiteSpace(valueName))
            {
                return string.Empty;
            }

            return NormalizeOriginalDataValueNameCore(valueName) ?? valueName.Trim();
        }

        public bool TryGetOriginalDataValue(string? valueName, out double value)
        {
            value = double.NaN;
            if (OriginalDataValues == null || OriginalDataValues.Count == 0)
            {
                return false;
            }

            string normalizedValueName = NormalizeOriginalDataValueName(valueName);
            if (string.IsNullOrWhiteSpace(normalizedValueName))
            {
                return false;
            }

            if (OriginalDataValues.TryGetValue(normalizedValueName, out value))
            {
                return IsFiniteDouble(value);
            }

            KeyValuePair<string, double> pair = OriginalDataValues
                .FirstOrDefault(item => string.Equals(item.Key, normalizedValueName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                value = pair.Value;
                return IsFiniteDouble(value);
            }

            return false;
        }

        public static double GetMeasureDataOriginalDataValue(
            MeasureData measureData,
            string? valueName,
            string fallbackValueName)
        {
            string normalizedValueName = NormalizeOriginalDataValueName(valueName, fallbackValueName);
            return TryGetMeasureDataOriginalDataValue(measureData, normalizedValueName, out double value)
                ? value
                : double.NaN;
        }

        internal static bool TryGetMeasureDataOriginalDataValue(
            MeasureData? measureData,
            string? valueName,
            out double value)
        {
            value = double.NaN;
            if (measureData?.OriginalDatas == null || measureData.OriginalDatas.Count == 0)
            {
                return false;
            }

            if (!TrySplitOriginalDataValueName(valueName, out string channelName, out string typeName))
            {
                return false;
            }

            KeyValuePair<string, Dictionary<string, object>> channelPair = measureData.OriginalDatas
                .FirstOrDefault(pair => string.Equals(pair.Key, channelName, StringComparison.OrdinalIgnoreCase));
            if (channelPair.Value == null)
            {
                return false;
            }

            KeyValuePair<string, object> typePair = channelPair.Value
                .FirstOrDefault(pair => string.Equals(pair.Key, typeName, StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrWhiteSpace(typePair.Key) &&
                   TryConvertOriginalDataValue(typePair.Value, out value);
        }

        internal static bool TryConvertOriginalDataValue(object? rawValue, out double value)
        {
            value = double.NaN;
            if (rawValue == null)
            {
                return false;
            }

            switch (rawValue)
            {
                case double doubleValue:
                    value = doubleValue;
                    return IsFiniteDouble(value);
                case float floatValue:
                    value = floatValue;
                    return IsFiniteDouble(value);
                case decimal decimalValue:
                    value = (double)decimalValue;
                    return IsFiniteDouble(value);
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    value = longValue;
                    return true;
                case short shortValue:
                    value = shortValue;
                    return true;
                case byte byteValue:
                    value = byteValue;
                    return true;
                case uint uintValue:
                    value = uintValue;
                    return true;
                case ulong ulongValue:
                    value = ulongValue;
                    return IsFiniteDouble(value);
                case string stringValue:
                    return (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                            double.TryParse(stringValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
                           IsFiniteDouble(value);
                default:
                    try
                    {
                        value = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
                        return IsFiniteDouble(value);
                    }
                    catch
                    {
                        return false;
                    }
            }
        }

        internal static Dictionary<string, double> GetMeasureDataOriginalDataValues(MeasureData? measureData)
        {
            Dictionary<string, double> originalDataValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (measureData?.OriginalDatas == null || measureData.OriginalDatas.Count == 0)
            {
                return originalDataValues;
            }

            foreach (KeyValuePair<string, Dictionary<string, object>> channelPair in measureData.OriginalDatas)
            {
                if (string.IsNullOrWhiteSpace(channelPair.Key) || channelPair.Value == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> typePair in channelPair.Value)
                {
                    if (string.IsNullOrWhiteSpace(typePair.Key) ||
                        !TryConvertOriginalDataValue(typePair.Value, out double value))
                    {
                        continue;
                    }

                    SetOriginalDataValue(originalDataValues, $"{channelPair.Key}.{typePair.Key}", value);
                }
            }

            return originalDataValues;
        }

        private static void SetOriginalDataValue(
            IDictionary<string, double> originalDataValues,
            string? valueName,
            double value)
        {
            if (originalDataValues == null || !IsFiniteDouble(value))
            {
                return;
            }

            string normalizedValueName = NormalizeOriginalDataValueName(valueName);
            if (string.IsNullOrWhiteSpace(normalizedValueName))
            {
                return;
            }

            originalDataValues[normalizedValueName] = value;
        }

        private static IReadOnlyList<string> CreateOriginalDataValueNames()
        {
            List<string> valueNames = new List<string>();
            for (int channel = 1; channel <= 18; channel++)
            {
                foreach (string typeName in _originalDataSensorTypeNames)
                {
                    valueNames.Add($"Channel{channel}.{typeName}");
                }
            }

            foreach (string typeName in _originalDataControllerTypeNames)
            {
                valueNames.Add($"Controller.{typeName}");
            }

            return valueNames;
        }

        private static string? NormalizeOriginalDataValueNameCore(string? valueName)
        {
            if (!TrySplitOriginalDataValueName(valueName, out string channelName, out string typeName))
            {
                return null;
            }

            string normalizedChannelName = string.Equals(channelName, "Controller", StringComparison.OrdinalIgnoreCase)
                ? "Controller"
                : NormalizeOriginalDataChannelName(channelName);

            if (string.IsNullOrWhiteSpace(normalizedChannelName))
            {
                return null;
            }

            string? normalizedTypeName = _originalDataSensorTypeNames
                .Concat(_originalDataControllerTypeNames)
                .FirstOrDefault(type => string.Equals(type, typeName, StringComparison.OrdinalIgnoreCase));

            return normalizedTypeName == null
                ? $"{normalizedChannelName}.{typeName}"
                : $"{normalizedChannelName}.{normalizedTypeName}";
        }

        private static string NormalizeOriginalDataChannelName(string channelName)
        {
            if (channelName.StartsWith("Channel", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(channelName.Substring("Channel".Length), out int channelIndex) &&
                channelIndex > 0)
            {
                return $"Channel{channelIndex}";
            }

            return string.Empty;
        }

        private static bool TrySplitOriginalDataValueName(
            string? valueName,
            out string channelName,
            out string typeName)
        {
            channelName = string.Empty;
            typeName = string.Empty;

            if (string.IsNullOrWhiteSpace(valueName))
            {
                return false;
            }

            string normalizedValueName = valueName.Trim();
            string[] tokens = normalizedValueName
                .Split(new[] { '.', '/', '\\', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 2 ||
                string.IsNullOrWhiteSpace(tokens[0]) ||
                string.IsNullOrWhiteSpace(tokens[1]))
            {
                return false;
            }

            channelName = tokens[0].Trim();
            typeName = tokens[1].Trim();
            return true;
        }

        private static bool TryParseCsvRecord(
            IReadOnlyList<string> tokens,
            IReadOnlyList<string>? headers,
            out PreprocessDatasetModel preData)
        {
            preData = new PreprocessDatasetModel();

            if (headers != null && headers.Count > 0)
            {
                if (!TryGetValueByHeader(tokens, headers, out double x, "posx", "collectposx", "x") ||
                    !TryGetValueByHeader(tokens, headers, out double y, "posy", "collectposy", "y"))
                {
                    return false;
                }

                if (!TryGetValueByHeader(tokens, headers, out double upSurface, "upsurface", "channel4", "z"))
                {
                    return false;
                }

                if (!TryGetValueByHeader(tokens, headers, out double downSurface, "downsurface", "channel5"))
                {
                    downSurface = double.NaN;
                }

                if (!TryGetValueByHeader(tokens, headers, out double thickness, "thickness"))
                {
                    thickness = double.NaN;
                }

                preData = new PreprocessDatasetModel
                {
                    PosX = x,
                    PosY = y,
                    UpSurface = upSurface,
                    DownSurface = downSurface,
                    Thickness = thickness,
                    OriginalDataValues = ExtractOriginalDataValues(tokens, headers)
                };
                SetOriginalDataValue(preData.OriginalDataValues, DefaultUpSurfaceOriginalDataName, upSurface);
                SetOriginalDataValue(preData.OriginalDataValues, DefaultDownSurfaceOriginalDataName, downSurface);
                return true;
            }

            if (tokens.Count < 3 ||
                !TryParseDouble(tokens[0], out double defaultX) ||
                !TryParseDouble(tokens[1], out double defaultY) ||
                !TryParseDouble(tokens[2], out double defaultUpSurface))
            {
                return false;
            }

            double defaultDownSurface = double.NaN;
            if (tokens.Count > 3)
            {
                TryParseDouble(tokens[3], out defaultDownSurface);
            }

            double defaultThickness = double.NaN;
            if (tokens.Count > 4)
            {
                TryParseDouble(tokens[4], out defaultThickness);
            }

            preData = new PreprocessDatasetModel
            {
                PosX = defaultX,
                PosY = defaultY,
                UpSurface = defaultUpSurface,
                DownSurface = defaultDownSurface,
                Thickness = defaultThickness
            };
            SetOriginalDataValue(preData.OriginalDataValues, DefaultUpSurfaceOriginalDataName, defaultUpSurface);
            SetOriginalDataValue(preData.OriginalDataValues, DefaultDownSurfaceOriginalDataName, defaultDownSurface);
            return true;
        }

        private static Dictionary<string, double> ExtractOriginalDataValues(
            IReadOnlyList<string> tokens,
            IReadOnlyList<string> headers)
        {
            Dictionary<string, double> originalDataValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count && i < tokens.Count; i++)
            {
                string header = headers[i];
                if (string.IsNullOrWhiteSpace(header) || IsCoreCsvHeader(header))
                {
                    continue;
                }

                if (TryParseDouble(tokens[i], out double value))
                {
                    SetOriginalDataValue(originalDataValues, header, value);
                }
            }

            return originalDataValues;
        }

        private static bool IsCoreCsvHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return false;
            }

            string normalizedHeader = NormalizeHeader(header);
            return normalizedHeader is "posx"
                or "collectposx"
                or "x"
                or "posy"
                or "collectposy"
                or "y"
                or "upsurface"
                or "downsurface"
                or "thickness";
        }

        private static bool TryGetValueByHeader(
            IReadOnlyList<string> tokens,
            IReadOnlyList<string> headers,
            out double value,
            params string[] candidates)
        {
            value = double.NaN;
            int index = FindHeaderIndex(headers, candidates);
            if (index < 0 || index >= tokens.Count)
            {
                return false;
            }

            return TryParseDouble(tokens[index], out value);
        }

        private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] candidates)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                string normalizedHeader = NormalizeHeader(headers[i]);
                if (candidates.Any(candidate => normalizedHeader == candidate))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool LooksLikeHeader(IReadOnlyList<string> tokens)
        {
            return tokens.Any(token => !TryParseDouble(token, out _));
        }

        private static string NormalizeHeader(string header)
        {
            return new string(header
                .Trim()
                .ToLowerInvariant()
                .Where(character => character != '_' && character != ' ' && character != '-')
                .ToArray());
        }

        private static List<string> SplitTokens(string line)
        {
            List<string> tokens = line.Split(new[] { ',', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .ToList();

            if (tokens.Count == 0)
            {
                tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                    .Select(token => token.Trim())
                    .Where(token => token.Length > 0)
                    .ToList();
            }

            return tokens;
        }

        private static bool TryParseDouble(string token, out double value)
        {
            return double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
