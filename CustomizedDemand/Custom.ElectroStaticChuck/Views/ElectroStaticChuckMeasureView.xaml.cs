using ImageTool.Halcon;
using ReeYin_V.Core.IOC;
using System.Windows;
using System.Windows.Controls;

namespace Custom.ElectroStaticChuckMeasure.Views
{
    /// <summary>
    /// ElectroStaticChuckMeasure.xaml 的交互逻辑
    /// </summary>
    public partial class ElectroStaticChuckMeasureView : UserControl
    {
        public ElectroStaticChuckMeasureView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution?.ImgControlPair == null)
            {
                return;
            }

            if (solution.ImgControlPair.TryGetValue(ElectroStaticChuckMeasureModel.ModuleName, out object? control))
            {
                winFormHost.Child = control as VMHWindowControl;
            }
        }
    }
}
