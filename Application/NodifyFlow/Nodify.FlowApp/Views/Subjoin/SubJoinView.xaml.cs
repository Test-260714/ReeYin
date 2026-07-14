using Nodify.FlowApp.ViewModels.Subjoin;
using System;
using System.Collections.Generic;
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

namespace Nodify.FlowApp.Views.Subjoin
{
    /// <summary>
    /// SubJoinView.xaml 的交互逻辑
    /// </summary>
    public partial class SubJoinView : UserControl
    {
        public SubJoinView()
        {
            InitializeComponent();
            this.DataContext = new SubjoinViewModel();
        }
    }
}
