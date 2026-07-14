using Prism.Dialogs;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI.Style.Dialogs;
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

namespace ReeYin_V.Login.Views
{
    /// <summary>
    /// LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void CmbUser_Loaded(object sender, RoutedEventArgs e)
        {
            if (cmbUser.Template.FindName("PART_EditableTextBox", cmbUser) is TextBox textBox)
            {
                textBox.IsReadOnly = true;
                textBox.Cursor = Cursors.Arrow;

                textBox.PreviewMouseLeftButtonDown += (s, args) =>
                {
                    if (textBox.IsReadOnly)
                    {
                        if (args.ClickCount >= 2)
                        {
                            // 双击：启用编辑模式，写入用户
                            textBox.IsReadOnly = false;
                            textBox.Cursor = Cursors.IBeam;
                            cmbUser.IsDropDownOpen = false;
                            args.Handled = true;
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                textBox.Focus();
                                textBox.SelectAll();
                            }), System.Windows.Threading.DispatcherPriority.Input);
                        }
                        else
                        {
                            // 单击：展开/收起下拉列表
                            cmbUser.IsDropDownOpen = !cmbUser.IsDropDownOpen;
                            args.Handled = true;
                        }
                    }
                };

                textBox.LostFocus += (s, args) =>
                {
                    textBox.IsReadOnly = true;
                    textBox.Cursor = Cursors.Arrow;
                };
            }
        }

        private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //弹窗初始化页面
            PrismProvider.DialogService.ShowDialog("RootManagerView", new DialogParameters
            {


            }, result =>
            {

            }, nameof(DialogWindowView));
        }
    }
}
