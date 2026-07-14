using ALGO.ImageOperation.ViewModels;
using ImageTool.Halcon;
using Newtonsoft.Json;
using Prism.Dialogs;
using Prism.Events;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.Camera.ViewModels;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace ALGO.ImageOperation.Views
{
    /// <summary>
    /// CollectImageView.xaml 的交互逻辑
    /// </summary>
    public partial class ImageOperationView : UserControl
    {
        #region Fields

        #endregion

        public ImageOperationView()
        {
            InitializeComponent();

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.winFormHost.Child = PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[ImageOperationModel.ModuleName] as VMHWindowControl;

            //this.winFormHost.Child = MeasureLineModel.mWindowH;
        }
    }
}
