using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.Views
{
    /// <summary>
    /// 折射率独立页面，省得左侧配置页再被这一坨内容挤炸。
    /// </summary>
    public partial class TronSight2RefractiveIndexWindow : Window
    {
        public TronSight2RefractiveIndexWindow()
        {
            InitializeComponent();
        }

        private void RefractiveTableDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void RefractiveTableDataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid == null || dataGrid.IsReadOnly)
            {
                return;
            }

            // 双击文本单元格时直接进编辑态，不然点半天跟挠痒痒一样。
            DependencyObject dependencyObject = e.OriginalSource as DependencyObject;
            while (dependencyObject != null && !(dependencyObject is DataGridCell))
            {
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            DataGridCell cell = dependencyObject as DataGridCell;
            if (cell == null || cell.IsReadOnly)
            {
                return;
            }

            cell.Focus();
            dataGrid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            dataGrid.BeginEdit(e);
            e.Handled = true;
        }
    }
}
