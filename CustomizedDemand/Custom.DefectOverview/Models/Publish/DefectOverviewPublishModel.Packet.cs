using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel
    {
        [JsonIgnore]
        private Dictionary<string, object> _publishedPacket = new(StringComparer.OrdinalIgnoreCase);

        [OutputParam("DefectOverviewPacket", "Defect overview publish package")]
        public Dictionary<string, object> PublishedPacket
        {
            get => _publishedPacket;
            set => SetProperty(ref _publishedPacket, value ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        }

        private void RefreshPublishedPacket()
        {
            PublishedPacket = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Results"] = PublishedResults ?? new List<Result>(),
                ["Count"] = PublishedCount,
                ["IsNg"] = PublishedCount > 0,
                ["SN"] = PublishedSN ?? string.Empty,
                ["DetectMeters"] = PublishedDetectMeters,
                ["IsRollCompleted"] = PublishedIsRollCompleted,
                ["FrameKey"] = PublishedFrameKey ?? string.Empty,
                ["StatusText"] = PublishStatusText ?? string.Empty,
                ["PublishTime"] = LastPublishTime,
            };
        }
    }
}
