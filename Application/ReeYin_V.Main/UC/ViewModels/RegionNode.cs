using ReeYin_V.Main.UC.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReeYin_V.Main.UC.ViewModels
{
    public enum SplitDirection
    {
        None,
        Horizontal,
        Vertical
    }

    public class RegionNode
    {
        public SplitDirection Direction { get; set; } = SplitDirection.None;
        public RegionNode? Parent { get; set; }
        public List<RegionNode> Children { get; } = new();
        public DynamicRegionControl? RegionControl { get; set; }
    }

    public class DynamicLayoutGrid : Grid
    {
        private RegionNode _root;

        public DynamicLayoutGrid()
        {
            _root = new RegionNode();
            Loaded += (s, e) => BuildLayout();
        }

        private void BuildLayout()
        {
            Children.Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();
            CreateRegionVisual(this, _root);
        }

        private void CreateRegionVisual(Grid container, RegionNode node)
        {
            if (node.Direction == SplitDirection.None)
            {
                var region = new DynamicRegionControl();
                //region.OnSplitRequested += (s, dir) => SplitRegion(node, dir);
                node.RegionControl = region;
                container.Children.Add(region);
                return;
            }

            container.Children.Clear();
            container.RowDefinitions.Clear();
            container.ColumnDefinitions.Clear();

            if (node.Direction == SplitDirection.Horizontal)
            {
                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var top = node.Children[0];
                var bottom = node.Children[1];

                var topGrid = new Grid();
                var bottomGrid = new Grid();
                CreateRegionVisual(topGrid, top);
                CreateRegionVisual(bottomGrid, bottom);

                container.Children.Add(topGrid);
                container.Children.Add(bottomGrid);
                Grid.SetRow(topGrid, 0);
                Grid.SetRow(bottomGrid, 2);

                var splitter = new GridSplitter
                {
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = Brushes.Transparent
                };
                container.Children.Add(splitter);
                Grid.SetRow(splitter, 1);
            }
            else if (node.Direction == SplitDirection.Vertical)
            {
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var left = node.Children[0];
                var right = node.Children[1];

                var leftGrid = new Grid();
                var rightGrid = new Grid();
                CreateRegionVisual(leftGrid, left);
                CreateRegionVisual(rightGrid, right);

                container.Children.Add(leftGrid);
                container.Children.Add(rightGrid);
                Grid.SetColumn(leftGrid, 0);
                Grid.SetColumn(rightGrid, 2);

                var splitter = new GridSplitter
                {
                    Width = 4,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = Brushes.Transparent
                };
                container.Children.Add(splitter);
                Grid.SetColumn(splitter, 1);
            }
        }

        private void SplitRegion(RegionNode node, SplitDirection direction)
        {
            if (node.Direction != SplitDirection.None)
                return;

            node.Direction = direction;
            node.Children.Clear();
            node.Children.Add(new RegionNode { Parent = node });
            node.Children.Add(new RegionNode { Parent = node });

            BuildLayout();
        }
    }
}
