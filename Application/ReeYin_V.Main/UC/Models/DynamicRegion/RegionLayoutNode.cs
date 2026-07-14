using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Main.UC.Models
{
    public enum SplitDirection
    {
        None,
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Layout tree node for a dynamic region page.
    /// Stores split structure and the dynamic view bound to a leaf node.
    /// </summary>
    public class RegionLayoutNode
    {
        public string RegionName { get; set; } = Guid.NewGuid().ToString();
        public SplitDirection SplitDirection { get; set; } = SplitDirection.None;
        public RegionLayoutNode? Child1 { get; set; }
        public RegionLayoutNode? Child2 { get; set; }
        public List<double> Sizes { get; set; } = [];
        public string? LoadedViewName { get; set; }
        public string? LoadedDisplayName { get; set; }
        public int Serial { get; set; } = -1;
        public string Subjection = string.Empty;
        public DynamicViewType Type { get; set; }

        public DynamicRegionViewLoadRequest CreateLoadRequest()
        {
            return new DynamicRegionViewLoadRequest
            {
                Serial = Serial,
                RegionName = RegionName,
                ViewName = LoadedViewName ?? string.Empty,
                DisplayName = LoadedDisplayName ?? string.Empty,
                Subjection = Subjection ?? string.Empty,
                Type = Type
            };
        }

        public void ApplyLoadRequest(DynamicRegionViewLoadRequest? request)
        {
            LoadedViewName = request?.ViewName;
            LoadedDisplayName = request?.DisplayName;
            Serial = request?.Serial ?? -1;
            Subjection = request?.Subjection ?? string.Empty;
            Type = request?.Type ?? DynamicViewType.General;
        }

        public void ClearLoadedView()
        {
            ApplyLoadRequest(null);
        }

        /// <summary>
        /// Split the current leaf node into two child regions.
        /// The current node stops hosting a view after the split.
        /// </summary>
        public void Split(SplitDirection direction)
        {
            if (direction == SplitDirection.None)
            {
                Merge();
                return;
            }

            SplitDirection = direction;
            Child1 = new RegionLayoutNode();
            Child2 = new RegionLayoutNode();
            Sizes = [1.0, 1.0];
            ClearLoadedView();
        }

        /// <summary>
        /// Merge child regions back into a single hostable region.
        /// </summary>
        public void Merge()
        {
            SplitDirection = SplitDirection.None;
            Child1 = null;
            Child2 = null;
            Sizes = [];
            ClearLoadedView();
        }

        public void ForEachLoadedNode(Action<RegionLayoutNode> action)
        {
            if (action == null)
            {
                return;
            }

            if (SplitDirection == SplitDirection.None && CreateLoadRequest().IsValid)
            {
                action(this);
            }

            Child1?.ForEachLoadedNode(action);
            Child2?.ForEachLoadedNode(action);
        }

        /// <summary>
        /// Collect all leaf nodes that currently have a valid bound view.
        /// </summary>
        public List<RegionLayoutNode> CollectLoadedNodes()
        {
            var nodes = new List<RegionLayoutNode>();
            ForEachLoadedNode(nodes.Add);
            return nodes;
        }

        public RegionLayoutNode? FindByRegionName(string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                return null;
            }

            if (RegionName == regionName)
            {
                return this;
            }

            return Child1?.FindByRegionName(regionName) ?? Child2?.FindByRegionName(regionName);
        }

        public bool MatchesDynamicView(DynamicView dynamicView)
        {
            if (dynamicView == null || string.IsNullOrWhiteSpace(LoadedViewName))
            {
                return false;
            }

            return Type == dynamicView.Type
                && string.Equals(LoadedViewName, dynamicView.ViewName, StringComparison.OrdinalIgnoreCase)
                && (Type != DynamicViewType.NodeMap || Serial == dynamicView.NodeSerial)
                && (Type != DynamicViewType.Custom
                    || string.Equals(Subjection ?? string.Empty, dynamicView.Subjection ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }
}
