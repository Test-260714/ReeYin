
using ALGO.MeasureCircle.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ALGO.MeasureCircle.Views
{
    /// <summary>
    /// MeasureCircleView.xaml 的交互逻辑
    /// </summary>
    public partial class MeasureCircleView : UserControl
    {
        public MeasureCircleView()
        {
            InitializeComponent();
            Unloaded += UserControl_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MeasureCircleViewModel viewModel)
            {
                viewModel.ModelParam.AttachImageControl(imageHost);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MeasureCircleViewModel viewModel)
            {
                viewModel.ModelParam.DetachImageControl(imageHost);
            }
        }

    }
}
