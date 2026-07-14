using Microsoft.VisualBasic.Logging;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Custom.KCJC.Models.KCJC0_Algorithm;
using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Annotations;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Titles;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;

namespace Custom.KCJC.Views
{
    /// <summary>
    /// PartitionChart.xaml 的交互逻辑
    /// </summary>
    public partial class PartitionChart : UserControl
    {
        #region Fields

        #endregion

        #region Constructor
        public PartitionChart()
        {
            InitializeComponent();
            MeasureChart.ViewXY.DataCursor.ShowColorIndicator = false;
            MeasureChart.ViewXY.DataCursor.Visible = true;
            MeasureChart.ViewXY.DataCursor.ShowLabels = true;

        }
        #endregion

        #region Methods

        #endregion

        #region Commands

        #endregion

    }
}
