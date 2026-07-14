using Custom.DefectOverview.Models.Common;using Custom.DefectOverview.Models.GroupedDualCamera;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.DefectOverview.Services.GroupedDualCamera
{
    /// <summary>
    /// 将多个后处理结果源按绑定表组装为多组双相机总览帧。
    /// </summary>
    public sealed class GroupedDualCameraOverviewBuilder
    {
        public const string GroupKeyMetadataKey = "Overview_GroupKey";
        public const string GroupNameMetadataKey = "Overview_GroupName";
        public const string SideMetadataKey = "Overview_Side";
        public const string DisplayNameMetadataKey = "Overview_DisplayName";

        public GroupedDualCameraFrame Build(
            string frameKey,
            string frameIdText,
            IEnumerable<GroupedDualCameraBinding> bindings,
            Func<GroupedDualCameraBinding, IReadOnlyList<Result>> resolveResults)
        {
            GroupedDualCameraFrame frame = new()
            {
                FrameKey = frameKey ?? string.Empty,
                FrameIdText = frameIdText ?? string.Empty
            };

            if (bindings == null || resolveResults == null)
                return frame;

            foreach (GroupedDualCameraBinding binding in bindings.OrderBy(item => item.SortIndex))
            {
                IReadOnlyList<Result> results = resolveResults(binding) ?? Array.Empty<Result>();
                AttachOverviewMetadata(results, binding);
                string displayName = ResolveDisplayName(binding);

                frame.Channels.Add(new GroupedDualCameraChannelResult
                {
                    GroupKey = binding.GroupKey,
                    GroupName = binding.GroupName,
                    Side = binding.Side,
                    DisplayName = displayName,
                    SourceName = DescribeResultSource(binding),
                    Results = results
                });
            }

            return frame;
        }

        private static void AttachOverviewMetadata(
            IEnumerable<Result> results,
            GroupedDualCameraBinding binding)
        {
            if (results == null || binding == null)
                return;

            foreach (Result result in results.Where(item => item != null))
            {
                result.Others ??= new Dictionary<string, object>();
                result.Others[GroupKeyMetadataKey] = binding.GroupKey ?? string.Empty;
                result.Others[GroupNameMetadataKey] = binding.GroupName ?? string.Empty;
                result.Others[SideMetadataKey] = binding.Side.ToString();
                result.Others[DisplayNameMetadataKey] = ResolveDisplayName(binding);
            }
        }

        private static string ResolveDisplayName(GroupedDualCameraBinding binding)
        {
            if (!string.IsNullOrWhiteSpace(binding?.DisplayName))
                return binding.DisplayName;

            string groupKey = string.IsNullOrWhiteSpace(binding?.GroupKey) ? "G??" : binding.GroupKey;
            string sideText = binding?.Side == WidthSide.Left
                ? "L"
                : binding?.Side == WidthSide.Right ? "R" : "?";
            return $"{groupKey}-{sideText}";
        }

        private static string DescribeResultSource(GroupedDualCameraBinding binding)
        {
            return binding?.ResultInput?.Name
                ?? binding?.ResultInput?.ParamName
                ?? string.Empty;
        }
    }
}
