using Custom.DefectOverview.Models.Common;
using Custom.DefectOverview.Models.GroupedDualCamera;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
        [JsonIgnore]
        private TransmitParam _g01LeftResults = new();
        [InputParam("G01LeftResults", "多相机 01-L结果(兼容)", needDeepCopy: false)]
        public TransmitParam G01LeftResults
        {
            get => _g01LeftResults;
            set => SetProperty(ref _g01LeftResults, value);
        }

        [JsonIgnore]
        private TransmitParam _g01RightResults = new();
        [InputParam("G01RightResults", "多相机 01-R结果(兼容)", needDeepCopy: false)]
        public TransmitParam G01RightResults
        {
            get => _g01RightResults;
            set => SetProperty(ref _g01RightResults, value);
        }

        [JsonIgnore]
        private TransmitParam _g02LeftResults = new();
        [InputParam("G02LeftResults", "多相机 02-L结果(兼容)", needDeepCopy: false)]
        public TransmitParam G02LeftResults
        {
            get => _g02LeftResults;
            set => SetProperty(ref _g02LeftResults, value);
        }

        [JsonIgnore]
        private TransmitParam _g02RightResults = new();
        [InputParam("G02RightResults", "多相机 02-R结果(兼容)", needDeepCopy: false)]
        public TransmitParam G02RightResults
        {
            get => _g02RightResults;
            set => SetProperty(ref _g02RightResults, value);
        }

        [JsonIgnore]
        private TransmitParam _g03LeftResults = new();
        [InputParam("G03LeftResults", "多相机 03-L结果(兼容)", needDeepCopy: false)]
        public TransmitParam G03LeftResults
        {
            get => _g03LeftResults;
            set => SetProperty(ref _g03LeftResults, value);
        }

        [JsonIgnore]
        private TransmitParam _g03RightResults = new();
        [InputParam("G03RightResults", "多相机 03-R结果(兼容)", needDeepCopy: false)]
        public TransmitParam G03RightResults
        {
            get => _g03RightResults;
            set => SetProperty(ref _g03RightResults, value);
        }

        public TransmitParam GetDefaultGroupedDualCameraResultInput(GroupedDualCameraBinding binding)
        {
            return ResolveGroupedDualCameraResultPort(binding) ?? new TransmitParam();
        }

        private TransmitParam ResolveGroupedDualCameraResultInput(GroupedDualCameraBinding binding)
        {
            if (HasConfiguredInputSelection(binding?.ResultInput))
                return binding.ResultInput;

            return ResolveGroupedDualCameraResultPort(binding) ?? binding?.ResultInput ?? new TransmitParam();
        }

        private TransmitParam ResolveGroupedDualCameraResultPort(GroupedDualCameraBinding binding)
        {
            if (binding == null)
                return null;

            string groupKey = NormalizeGroupedDualCameraGroupKey(binding.GroupKey);
            return (groupKey, binding.Side) switch
            {
                ("G01", WidthSide.Left) => G01LeftResults,
                ("G01", WidthSide.Right) => G01RightResults,
                ("G02", WidthSide.Left) => G02LeftResults,
                ("G02", WidthSide.Right) => G02RightResults,
                ("G03", WidthSide.Left) => G03LeftResults,
                ("G03", WidthSide.Right) => G03RightResults,
                _ => null
            };
        }

        private IEnumerable<TransmitParam> EnumerateGroupedDualCameraResultPorts()
        {
            yield return G01LeftResults;
            yield return G01RightResults;
            yield return G02LeftResults;
            yield return G02RightResults;
            yield return G03LeftResults;
            yield return G03RightResults;
        }

        private static string NormalizeGroupedDualCameraGroupKey(string groupKey)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                return string.Empty;

            string normalized = groupKey.Trim().ToUpperInvariant();
            if (normalized.Length >= 2
                && normalized[0] == 'G'
                && int.TryParse(normalized[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gIndex))
            {
                return $"G{gIndex:D2}";
            }

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                return $"G{index:D2}";
            }

            return normalized;
        }
    }
}
