using FileTool.BRJReportOutput.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace FileTool.BRJReportOutput.Views
{
    public partial class BrjDefectRecordDialog : Window
    {
        public BrjDefectRecordDialog(BrjReportRecord record, IEnumerable<BrjDefectRecord> defects)
        {
            InitializeComponent();
            DataContext = new BrjDefectRecordDialogViewModel(record, defects);
        }

        private sealed class BrjDefectRecordDialogViewModel
        {
            public BrjDefectRecordDialogViewModel(BrjReportRecord record, IEnumerable<BrjDefectRecord> defects)
            {
                string sn = record?.SN ?? string.Empty;
                Defects = new ObservableCollection<BrjDefectRecord>((defects ?? Enumerable.Empty<BrjDefectRecord>()).ToList());
                WindowTitle = $"批次缺陷明细 - {sn}";
                BatchTitle = $"批次号：{sn}";
                CountText = $"缺陷记录：{Defects.Count} 条";
            }

            public string WindowTitle { get; }

            public string BatchTitle { get; }

            public string CountText { get; }

            public ObservableCollection<BrjDefectRecord> Defects { get; }
        }
    }
}
