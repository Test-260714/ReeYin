using ReeYin_V.Core.Services.DynamicView;
using System;

namespace ReeYin_V.Main.UC.Models
{
    [Serializable]
    public class DynamicRegionViewLoadRequest
    {
        public int Serial { get; set; } = -1;

        public string RegionName { get; set; } = string.Empty;

        public string ViewName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Subjection { get; set; } = string.Empty;

        public DynamicViewType Type { get; set; } = DynamicViewType.General;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RegionName) &&
            !string.IsNullOrWhiteSpace(ViewName);
    }
}
