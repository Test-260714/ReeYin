using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FileTool.BRJReportOutput.Views
{
    public partial class BrjReportOutputView : UserControl
    {
        public BrjReportOutputView()
        {
            InitializeComponent();
            Loaded += BrjReportOutputView_Loaded;
        }

        private void BrjReportOutputView_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsHostedByRegion())
            {
                Width = double.NaN;
                Height = double.NaN;
            }
        }

        private bool IsHostedByRegion()
        {
            DependencyObject current = this;
            while (current != null)
            {
                if (IsDynamicRegionElement(current))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static bool IsDynamicRegionElement(DependencyObject element)
        {
            string typeName = element.GetType().FullName ?? element.GetType().Name;
            return typeName.IndexOf("DynamicRegion", StringComparison.Ordinal) >= 0;
        }
    }
}
