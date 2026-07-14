using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.Models
{
    public static class PreprocessDatasetFilter
    {
        public static List<PreprocessDatasetModel> Apply(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            MeasurementDataFilterOptions? options)
        {
            List<PreprocessDatasetModel> result = PreprocessDatasetModel.Clone(preDatas);
            if (options == null || result.Count == 0)
            {
                return result;
            }

            if (options.MedianFilterEnabled)
            {
                result = ApplyMedianFilter(result, options);
            }

            if (options.SmoothFilterEnabled)
            {
                result = ApplySmoothFilter(result, options);
            }

            return result;
        }

        public static List<PreprocessDatasetModel> ApplyMedianFilter(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            MeasurementDataFilterOptions? options)
        {
            return ApplyWindowFilter(preDatas, options, options?.MedianFilterWindowSize ?? 3, CalculateMedian);
        }

        public static List<PreprocessDatasetModel> ApplySmoothFilter(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            MeasurementDataFilterOptions? options)
        {
            return ApplyWindowFilter(preDatas, options, options?.SmoothFilterWindowSize ?? 3, CalculateAverage);
        }

        private static List<PreprocessDatasetModel> ApplyWindowFilter(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            MeasurementDataFilterOptions? options,
            int configuredWindowSize,
            Func<IReadOnlyList<double>, double, double> calculateValue)
        {
            List<PreprocessDatasetModel> result = PreprocessDatasetModel.Clone(preDatas);
            if (options == null || result.Count == 0)
            {
                return result;
            }

            int windowSize = NormalizeWindowSize(configuredWindowSize);
            List<PreprocessDatasetModel> source = PreprocessDatasetModel.Clone(result);

            if (options.FilterUpSurface)
            {
                ApplyChannel(
                    source,
                    result,
                    windowSize,
                    data => data.UpSurface,
                    (data, value) =>
                    {
                        data.UpSurface = value;
                        SetCoreOriginalDataValue(data, options.UpSurfaceOriginalDataValueName, PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName, value);
                    },
                    calculateValue);
            }

            if (options.FilterDownSurface)
            {
                ApplyChannel(
                    source,
                    result,
                    windowSize,
                    data => data.DownSurface,
                    (data, value) =>
                    {
                        data.DownSurface = value;
                        SetCoreOriginalDataValue(data, options.DownSurfaceOriginalDataValueName, PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName, value);
                    },
                    calculateValue);
            }

            if (options.FilterThickness)
            {
                ApplyChannel(
                    source,
                    result,
                    windowSize,
                    data => data.Thickness,
                    (data, value) => data.Thickness = value,
                    calculateValue);
            }

            if (options.FilterOriginalDataValues)
            {
                foreach (string valueName in GetOriginalDataValueNames(source))
                {
                    ApplyChannel(
                        source,
                        result,
                        windowSize,
                        data => TryGetOriginalDataValue(data, valueName, out double value) ? value : double.NaN,
                        (data, value) => SetOriginalDataValue(data, valueName, value),
                        calculateValue);
                }
            }

            return result;
        }

        private static void ApplyChannel(
            IReadOnlyList<PreprocessDatasetModel> source,
            IReadOnlyList<PreprocessDatasetModel> target,
            int windowSize,
            Func<PreprocessDatasetModel, double> selector,
            Action<PreprocessDatasetModel, double> assign,
            Func<IReadOnlyList<double>, double, double> calculateValue)
        {
            if (source.Count == 0 || source.Count != target.Count)
            {
                return;
            }

            int halfWindow = windowSize / 2;
            for (int index = 0; index < source.Count; index++)
            {
                int start = Math.Max(0, index - halfWindow);
                int end = Math.Min(source.Count - 1, index + halfWindow);
                List<double> values = new List<double>(end - start + 1);

                for (int sampleIndex = start; sampleIndex <= end; sampleIndex++)
                {
                    double value = selector(source[sampleIndex]);
                    if (IsFinite(value))
                    {
                        values.Add(value);
                    }
                }

                double originalValue = selector(source[index]);
                assign(target[index], calculateValue(values, originalValue));
            }
        }

        private static double CalculateMedian(IReadOnlyList<double> values, double fallbackValue)
        {
            if (values == null || values.Count == 0)
            {
                return fallbackValue;
            }

            List<double> sortedValues = values.OrderBy(value => value).ToList();
            int middleIndex = sortedValues.Count / 2;
            if (sortedValues.Count % 2 == 1)
            {
                return sortedValues[middleIndex];
            }

            return (sortedValues[middleIndex - 1] + sortedValues[middleIndex]) / 2d;
        }

        private static double CalculateAverage(IReadOnlyList<double> values, double fallbackValue)
        {
            return values == null || values.Count == 0
                ? fallbackValue
                : values.Average();
        }

        private static int NormalizeWindowSize(int windowSize)
        {
            int normalizedWindowSize = Math.Max(3, windowSize);
            return normalizedWindowSize % 2 == 0
                ? normalizedWindowSize + 1
                : normalizedWindowSize;
        }

        private static IEnumerable<string> GetOriginalDataValueNames(IEnumerable<PreprocessDatasetModel> preDatas)
        {
            return preDatas
                .Where(data => data?.OriginalDataValues != null)
                .SelectMany(data => data.OriginalDataValues.Keys)
                .Where(valueName => !string.IsNullOrWhiteSpace(valueName))
                .Select(PreprocessDatasetModel.NormalizeOriginalDataValueName)
                .Where(valueName => !string.IsNullOrWhiteSpace(valueName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(valueName => valueName, StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryGetOriginalDataValue(
            PreprocessDatasetModel data,
            string valueName,
            out double value)
        {
            value = double.NaN;
            return data?.OriginalDataValues != null &&
                   data.OriginalDataValues.TryGetValue(valueName, out value) &&
                   IsFinite(value);
        }

        private static void SetCoreOriginalDataValue(
            PreprocessDatasetModel data,
            string? configuredValueName,
            string defaultValueName,
            double value)
        {
            SetOriginalDataValue(data, defaultValueName, value);
            SetOriginalDataValue(data, configuredValueName, value);
        }

        private static void SetOriginalDataValue(
            PreprocessDatasetModel data,
            string? valueName,
            double value)
        {
            if (data == null || !IsFinite(value))
            {
                return;
            }

            string normalizedValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(valueName);
            if (string.IsNullOrWhiteSpace(normalizedValueName))
            {
                return;
            }

            data.OriginalDataValues ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            data.OriginalDataValues[normalizedValueName] = value;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
