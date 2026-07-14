using FileTool.BRJReportOutput.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace FileTool.BRJReportOutput.Views
{
    public partial class BrjDiameterGroupSettingsDialog : Window
    {
        public BrjDiameterGroupSettingsDialog(IEnumerable<BrjReportSetting> settings)
        {
            InitializeComponent();
            Settings = new ObservableCollection<BrjReportSetting>((settings ?? Enumerable.Empty<BrjReportSetting>())
                .OrderBy(item => item.SortIndex)
                .Select(Clone));
            DataContext = this;
        }

        public ObservableCollection<BrjReportSetting> Settings { get; }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Clear();
            foreach (BrjReportSetting item in BrjReportSetting.CreateDefaultDiameterGroups())
            {
                Settings.Add(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private static BrjReportSetting Clone(BrjReportSetting item)
        {
            return new BrjReportSetting
            {
                SettingType = BrjReportSetting.DiameterGroupType,
                SortIndex = item.SortIndex,
                GroupName = item.GroupName,
                MinDiameterMm = item.MinDiameterMm,
                MaxDiameterMm = item.MaxDiameterMm,
                ColorHex = item.ColorHex,
            };
        }
    }
}
