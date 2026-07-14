using Prism.Ioc;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Helper;
using ReeYin_V.Main.UC.Views;
using ReeYin_V.UI.UserControls.WaterDrop;
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
using static System.Net.Mime.MediaTypeNames;

namespace ReeYin_V.Main.Views
{
    /// <summary>
    /// RunMainView.xaml 的交互逻辑
    /// </summary>
    public partial class RunMainView : UserControl
    {
        private readonly IContainerProvider _container;
        private readonly IRegionManager _regionManager;

        public RunMainView(IContainerProvider container, IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager = regionManager;
        }
    }
}
