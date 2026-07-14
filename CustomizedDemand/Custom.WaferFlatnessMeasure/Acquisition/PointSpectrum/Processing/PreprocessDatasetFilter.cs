using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.Models
{
    public sealed class MeasurementDataFilterOptions
    {
        public bool MedianFilterEnabled { get; set; }

        public int MedianFilterWindowSize { get; set; } = 3;

        public bool SmoothFilterEnabled { get; set; }

        public int SmoothFilterWindowSize { get; set; } = 3;

        public bool FilterOriginalDataValues { get; set; } = true;

        public MeasurementDataFilterOptions Clone()
        {
            return new MeasurementDataFilterOptions
            {
                MedianFilterEnabled = MedianFilterEnabled,
                MedianFilterWindowSize = MedianFilterWindowSize,
                SmoothFilterEnabled = SmoothFilterEnabled,
                SmoothFilterWindowSize = SmoothFilterWindowSize,
                FilterOriginalDataValues = FilterOriginalDataValues
            };
        }
    }

    public static class PreprocessDatasetFilter
    {
        public static List<PreprocessDatasetModel> Apply(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            MeasurementDataFilterOptions? options)
        {
            List<PreprocessDatasetModel> filteredDatas = PreprocessDatasetModel.Clone(preDatas);
            if (filteredDatas.Count == 0 || options == null)
            {
                return filteredDatas;
            }

            if (options.MedianFilterEnabled)
            {
                filteredDatas = ApplyWindowFilter(
                    filteredDatas,
                    NormalizeWindowSize(options.MedianFilterWindowSize),
                    CalculateMedian,
                    options.FilterOriginalDataValues);
            }

            if (options.SmoothFilterEnabled)
            {
                filteredDatas = ApplyWindowFilter(
                    filteredDatas,
                    NormalizeWindowSize(options.SmoothFilterWindowSize),
                    values => values.Average(),
                    options.FilterOriginalDataValues);
            }

            return filteredDatas;
        }

        private static List<PreprocessDatasetModel> ApplyWindowFilter(
            IReadOnlyList<PreprocessDatasetModel> source,
            int windowSize,
            Func<IReadOnlyList<double>, double> aggregate,
            bool filterOriginalDataValues)
        {
            var result = PreprocessDatasetModel.Clone(source);
            int radius = windowSize / 2;

            for (int index = 0; index < source.Count; index++)
            {
                List<PreprocessDatasetModel> window = GetWindow(source, index, radius);
                PreprocessDatasetModel target = result[index];

                target.UpSurface = Aggregate(window, data => data.UpSurface, aggregate);
                target.DownSurface = Aggregate(window, data => data.DownSurface, aggregate);
                target.Thickness = Aggregate(window, data => data.Thickness, aggregate);

                if (filterOriginalDataValues)
                {
                    target.OriginalDataValues = FilterOriginalValues(window, aggregate);
                }
                else
                {
                    target.OriginalDataValues[PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName] = target.UpSurface;
                    target.OriginalDataValues[PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName] = target.DownSurface;
                }
            }

            return result;
        }

        private static List<PreprocessDatasetModel> GetWindow(
            IReadOnlyList<PreprocessDatasetModel> source,
            int index,
            int radius)
        {
            int start = Math.Max(0, index - radius);
            int end = Math.Min(source.Count - 1, index + radius);
            var window = new List<PreprocessDatasetModel>(end - start + 1);

            for (int i = start; i <= end; i++)
            {
                window.Add(source[i]);
            }

            return window;
        }

        private static Dictionary<string, double> FilterOriginalValues(
            IReadOnlyList<PreprocessDatasetModel> window,
            Func<IReadOnlyList<double>, double> aggregate)
        {
            var valuesByName = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

            foreach (PreprocessDatasetModel data in window)
            {
                foreach (KeyValuePair<string, double> valuePair in data.OriginalDataValues)
                {
                    if (!double.IsFinite(valuePair.Value))
                    {
                        continue;
                    }

                    if (!valuesByName.TryGetValue(valuePair.Key, out List<double>? values))
                    {
                        values = new List<double>();
                        valuesByName[valuePair.Key] = values;
                    }

                    values.Add(valuePair.Value);
                }
            }

            return valuesByName.ToDictionary(
                valuePair => valuePair.Key,
                valuePair => aggregate(valuePair.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        private static double Aggregate(
            IEnumerable<PreprocessDatasetModel> window,
            Func<PreprocessDatasetModel, double> selector,
            Func<IReadOnlyList<double>, double> aggregate)
        {
            List<double> values = window
                .Select(selector)
                .Where(double.IsFinite)
                .ToList();

            return values.Count == 0 ? double.NaN : aggregate(values);
        }

        private static int NormalizeWindowSize(int windowSize)
        {
            return Math.Max(1, windowSize);
        }

        private static double CalculateMedian(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
            {
                return double.NaN;
            }

            List<double> orderedValues = values.OrderBy(value => value).ToList();
            int middle = orderedValues.Count / 2;

            if (orderedValues.Count % 2 == 1)
            {
                return orderedValues[middle];
            }

            return (orderedValues[middle - 1] + orderedValues[middle]) / 2d;
        }
    }
}
