using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Main.UC.Models
{
    public class DynamicRegionPageState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public RegionLayoutNode RootNode { get; set; } = new();
    }

    /// <summary>
    /// Persisted layout state for DynamicRegionControl.
    /// Stores page trees as well as lightweight UI state such as selection and floating panel position.
    /// </summary>
    public class DynamicRegionControlLayoutState
    {
        public List<DynamicRegionPageState> Pages { get; set; } = [];
        public string SelectedPageId { get; set; } = string.Empty;
        public bool IsOperationPanelExpanded { get; set; } = true;
        public double? FloatingButtonLeft { get; set; }
        public double? FloatingButtonTop { get; set; }

        public static DynamicRegionPageState CreateNewPageState(int index, string pageNamePrefix)
        {
            return new DynamicRegionPageState
            {
                Name = $"{pageNamePrefix} {index}",
                RootNode = new RegionLayoutNode()
            };
        }

        public static DynamicRegionControlLayoutState CreateDefault(string pageNamePrefix)
        {
            var firstPage = CreateNewPageState(1, pageNamePrefix);
            return new DynamicRegionControlLayoutState
            {
                Pages = [firstPage],
                SelectedPageId = firstPage.Id,
                IsOperationPanelExpanded = true
            };
        }

        public static DynamicRegionControlLayoutState FromLegacy(RegionLayoutNode? legacyRoot, string pageNamePrefix)
        {
            var firstPage = new DynamicRegionPageState
            {
                Name = $"{pageNamePrefix} 1",
                RootNode = legacyRoot ?? new RegionLayoutNode()
            };

            return new DynamicRegionControlLayoutState
            {
                Pages = [firstPage],
                SelectedPageId = firstPage.Id,
                IsOperationPanelExpanded = true
            };
        }

        public static DynamicRegionControlLayoutState EnsureValid(
            DynamicRegionControlLayoutState? layoutState,
            string pageNamePrefix)
        {
            // Normalize legacy or incomplete state before reading or saving.
            layoutState ??= new DynamicRegionControlLayoutState();
            layoutState.Pages ??= [];

            if (layoutState.Pages.Count == 0)
            {
                layoutState.Pages.Add(CreateNewPageState(1, pageNamePrefix));
            }

            for (int i = 0; i < layoutState.Pages.Count; i++)
            {
                var page = layoutState.Pages[i] ?? new DynamicRegionPageState();
                page.Id = string.IsNullOrWhiteSpace(page.Id) ? Guid.NewGuid().ToString() : page.Id;
                page.Name = $"{pageNamePrefix} {i + 1}";
                page.RootNode ??= new RegionLayoutNode();
                layoutState.Pages[i] = page;
            }

            if (string.IsNullOrWhiteSpace(layoutState.SelectedPageId)
                || !layoutState.Pages.Any(page => page.Id == layoutState.SelectedPageId))
            {
                layoutState.SelectedPageId = layoutState.Pages[0].Id;
            }

            return layoutState;
        }

        public DynamicRegionPageState? GetCurrentPage()
        {
            return Pages.FirstOrDefault(page => page.Id == SelectedPageId)
                ?? Pages.FirstOrDefault();
        }

        public int GetCurrentPageIndex()
        {
            var currentPage = GetCurrentPage();
            return currentPage == null ? -1 : Pages.FindIndex(page => page.Id == currentPage.Id);
        }

        public List<RegionLayoutNode> CollectLoadedNodesAcrossPages()
        {
            var nodes = new List<RegionLayoutNode>();
            foreach (var page in Pages)
            {
                nodes.AddRange(page.RootNode?.CollectLoadedNodes() ?? []);
            }

            return nodes;
        }

        public RegionLayoutNode? FindNodeByRegionNameAcrossPages(
            string regionName,
            RegionLayoutNode? currentRoot = null)
        {
            // Prefer the active page first, then fall back to the other cached pages.
            var currentNode = currentRoot?.FindByRegionName(regionName);
            if (currentNode != null)
            {
                return currentNode;
            }

            foreach (var page in Pages)
            {
                if (ReferenceEquals(page.RootNode, currentRoot))
                {
                    continue;
                }

                var node = page.RootNode?.FindByRegionName(regionName);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
