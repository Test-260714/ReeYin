using ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models;
using ReeYin.Hardware.Sensor.TronSight2.CustomUI.ViewModels;
using ReeYin_V.Core.Enums;
using ReeYin_V.UI;
using System.Windows;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.Views
{
    /// <summary>
    /// 厚度修正独立页面，单层 1 行，多层 5 行，别再往主页面里硬塞。
    /// </summary>
    public partial class TronSight2ThicknessCorrectionWindow : Window
    {
        public TronSight2ThicknessCorrectionWindow()
        {
            InitializeComponent();
            Loaded += TronSight2ThicknessCorrectionWindow_Loaded;
        }

        private void TronSight2ThicknessCorrectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ThicknessViewModel?.LoadItems();
        }

        private TronSight2ThicknessCorrectionViewModel ThicknessViewModel
        {
            get { return DataContext as TronSight2ThicknessCorrectionViewModel; }
        }

        private void SelectRefractiveButton_Click(object sender, RoutedEventArgs e)
        {
            ThicknessCorrectionItem item = (sender as FrameworkElement)?.DataContext as ThicknessCorrectionItem;
            if (item == null || ThicknessViewModel == null)
            {
                return;
            }

            ThicknessViewModel.PrepareSelectLayer(item.LayerIndex);
            TronSight2RefractiveIndexWindow window = new TronSight2RefractiveIndexWindow
            {
                Owner = this,
                DataContext = ThicknessViewModel.ParentViewModel
            };

            window.ShowDialog();

            if (!ThicknessViewModel.ApplySelectedRefractiveToLayer(item.LayerIndex))
            {
                MessageView.Ins.MessageBoxShow("应用折射率失败，请查看日志", eMsgType.Error, MessageBoxButton.OK);
            }
        }

        private void CorrectionFactor_LostFocus(object sender, RoutedEventArgs e)
        {
            ThicknessCorrectionItem item = (sender as FrameworkElement)?.DataContext as ThicknessCorrectionItem;
            if (item == null || ThicknessViewModel == null)
            {
                return;
            }

            if (!ThicknessViewModel.SaveCorrectionFactor(item))
            {
                MessageView.Ins.MessageBoxShow("保存厚度修正失败，请查看日志", eMsgType.Error, MessageBoxButton.OK);
            }
        }
    }
}
