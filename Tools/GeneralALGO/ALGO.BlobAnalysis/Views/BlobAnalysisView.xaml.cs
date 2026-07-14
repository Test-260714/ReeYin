using ALGO.BlobAnalysis.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ALGO.BlobAnalysis.Views
{
    /// <summary>
    /// BlobAnalysisView.xaml 的交互逻辑
    /// </summary>
    public partial class BlobAnalysisView : UserControl
    {
        public BlobAnalysisView()
        {
            InitializeComponent();
            Unloaded += UserControl_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BlobAnalysisViewModel viewModel)
            {
                viewModel.ModelParam.AttachImageControl(imageHost);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BlobAnalysisViewModel viewModel)
            {
                viewModel.ModelParam.DetachImageControl(imageHost);
            }
        }
    }
}
