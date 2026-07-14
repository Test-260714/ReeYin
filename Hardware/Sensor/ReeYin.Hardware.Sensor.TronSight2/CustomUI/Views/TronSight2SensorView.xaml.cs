using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.Views
{
    /// <summary>
    /// TronSight2SensorView.xaml 的交互逻辑
    /// </summary>
    public partial class TronSight2SensorView : UserControl
    {
        public TronSight2SensorView()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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

            // 双击文本单元格时主动进入编辑态，避免默认交互半天点不进去。
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
