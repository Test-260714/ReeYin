using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Events;
using System.Diagnostics;
using System.Windows.Controls;

namespace Nodify.FlowApp
{
    public partial class OperationsMenuView : UserControl
    {
        public OperationsMenuView()
        {
            InitializeComponent();

            //订阅切换样式事件
            PrismProvider.EventAggregator.GetEvent<SwitchStyleEvent>().Subscribe(Switch, ThreadOption.UIThread);
        }

        /// 切换资源
        /// </summary>
        /// <param name="style"></param>
        private void Switch(string style)
        {
            switch (style)
            {
                case "Dark":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Dark.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Dark.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
                case "Light":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Light.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Light.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
                case "Nodify":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Nodify.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Nodify.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
            }
        }
    }
}
