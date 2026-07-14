using HandyControl.Controls;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
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
using System.Windows.Shapes;
using Window = System.Windows.Window;

namespace ReeYin_V.UI
{
    /// <summary>
    /// MessageView.xaml 的交互逻辑
    /// </summary>
    public partial class MessageView : Window
    {
        #region Single
        private static MessageView _instance;
        public static MessageView Ins
        {
            get
            {
                Application.Current.Dispatcher.Invoke(() => { _instance = new MessageView(); });
                return _instance;
            }
        }
        #endregion

        #region Fields
        private MessageBoxButton _MessageBoxButton;

        private MessageBoxResult _messageBoxResult { get; set; }
        #endregion

        #region Constructor
        public MessageView()
        {
            InitializeComponent();
            DataContext = new MessageViewModel();
        }
        #endregion

        #region Methods
        public MessageBoxResult MessageBoxShow(string msg, eMsgType msgType = eMsgType.Info, MessageBoxButton messageBoxButton = MessageBoxButton.OK, bool allowClose = true)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    Button okbutton = new Button()
                    {
                        Content = "确定",
                        Width = 80,
                        Height = 35,
                        FontSize = 18,
                        Style = (System.Windows.Style)FindResource("GeneralButtonStyle")
                    };
                    okbutton.Click += btnConfirm_Click;
                    Button cancelbutton = new Button()
                    {
                        Content = "取消",
                        Width = 80,
                        Height = 35,
                        FontSize = 18,
                        Style = (System.Windows.Style)FindResource("GeneralButtonStyle")
                    };
                    cancelbutton.Click += btnConfirm_Click;
                    Button yesbutton = new Button()
                    {
                        Content = "是",
                        Width = 80,
                        Height = 35,
                        FontSize = 18,
                        Style = (System.Windows.Style)FindResource("GeneralButtonStyle")
                    };
                    yesbutton.Click += btnConfirm_Click;
                    Button notbutton = new Button()
                    {
                        Content = "否",
                        Width = 80,
                        Height = 35,
                        FontSize = 18,
                        Style = (System.Windows.Style)FindResource("GeneralButtonStyle")
                    };
                    notbutton.Click += btnConfirm_Click;
                    var vm = DataContext as MessageViewModel;
                    if (allowClose == false)
                    {
                        vm.ConfirmVisibility = Visibility.Collapsed;
                        vm.IsCloseButtonEnabled = false;
                        vm.IsMinButtonEnabled = false;
                    }
                    _MessageBoxButton = messageBoxButton;
                    vm.Message = msg;
                    this.Topmost = true;
                    switch (messageBoxButton)
                    {
                        case MessageBoxButton.OK:
                            spContainer.Children.Add(okbutton);
                            break;
                        case MessageBoxButton.OKCancel:
                            spContainer.Children.Add(okbutton);
                            spContainer.Children.Add(new Rectangle { Width = 200 });
                            spContainer.Children.Add(cancelbutton);
                            break;
                        case MessageBoxButton.YesNoCancel:
                            spContainer.Children.Add(yesbutton);
                            spContainer.Children.Add(new Rectangle { Width = 100 });
                            spContainer.Children.Add(notbutton);
                            spContainer.Children.Add(new Rectangle { Width = 100 });
                            spContainer.Children.Add(cancelbutton);
                            break;
                        case MessageBoxButton.YesNo:
                            spContainer.Children.Add(yesbutton);
                            spContainer.Children.Add(new Rectangle { Width = 200 });
                            spContainer.Children.Add(notbutton);
                            break;
                        default:
                            break;
                    }
                    switch (msgType)
                    {
                        case eMsgType.Info:
                            vm.Icon = "\ue664";
                            tbIcon.Foreground = new SolidColorBrush(Color.FromRgb(57, 96, 196));
                            break;
                        case eMsgType.Warn:
                            vm.Icon = "\ue666";
                            tbIcon.Foreground = new SolidColorBrush(Color.FromRgb(233, 168, 0));
                            break;
                        case eMsgType.Error:
                            vm.Icon = "\ue662";
                            tbIcon.Foreground = new SolidColorBrush(Color.FromRgb(178, 9, 9));
                            break;
                        case eMsgType.Success:
                            vm.Icon = "\ue661";
                            tbIcon.Foreground = new SolidColorBrush(Color.FromRgb(79, 205, 111));
                            break;
                        default:
                            break;
                    }
                    ShowDialog();             
                }
                catch (Exception ex)
                {
                }
            }));
            return _messageBoxResult;

        }

        /// <summary>
        /// HC的通知消息框
        /// </summary>
        public static void Notification(string msg,string Type)
        {
            switch (Type)
            {
                case "Info":
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        Growl.Info(msg);
                        Logs.LogInfo(msg);
                    });
                    break;
                case "Warn":
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        Growl.Warning(msg);
                        Logs.LogWarning(msg);
                    });
                    break;
                case "Error":
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        Growl.Error($"{msg}");
                        Logs.LogError(msg);
                    });
                    break;
                case "Success":
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        Growl.Success($"{msg}");
                        Logs.LogInfo(msg);
                    });
                    break;
                default:
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        Growl.Info(msg);
                        Logs.LogInfo(msg);
                    });
                    break;
            }
        }

        /// <summary>
        /// 显示保存错误消息
        /// </summary>
        private void ShowSaveErrorMessage(string error)
        {

        }
        #endregion


        #region Commands
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            switch (_MessageBoxButton)
            {
                case MessageBoxButton.OK:
                    _messageBoxResult = MessageBoxResult.OK;
                    this.DialogResult = true;
                    break;
                case MessageBoxButton.OKCancel:
                    switch(button.Content.ToString())
                    {
                        case "确定":
                            _messageBoxResult = MessageBoxResult.OK;
                            this.DialogResult = true;
                            break;
                        case "取消":
                            _messageBoxResult = MessageBoxResult.Cancel;
                            this.DialogResult = false;
                            break;
                        default:
                            this.DialogResult = true;
                            break;
                    }
                    break;
                case MessageBoxButton.YesNoCancel:
                    switch (button.Content.ToString())
                    {
                        case "是":
                            _messageBoxResult = MessageBoxResult.Yes;
                            this.DialogResult = true;
                            break;
                        case "否":
                            _messageBoxResult = MessageBoxResult.No;
                            this.DialogResult = false;
                            break;
                        case "取消":
                            _messageBoxResult = MessageBoxResult.Cancel;
                            this.DialogResult = false;
                            break;
                        default:
                            this.DialogResult = false;
                            break;
                    }
                    break;
                case MessageBoxButton.YesNo:
                    switch (button.Content.ToString())
                    {
                        case "是":
                            _messageBoxResult = MessageBoxResult.Yes;
                            this.DialogResult = true;
                            break;
                        case "否":
                            _messageBoxResult = MessageBoxResult.No;
                            this.DialogResult = false;
                            break;
                        default:
                            this.DialogResult = false;
                            break;
                    }
                    break;
                default:
                    break;
            }

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            switch (_MessageBoxButton)
            {
                case MessageBoxButton.OK:
                    this.DialogResult = false;
                    break;
                case MessageBoxButton.OKCancel:
                    this.DialogResult = false;
                    break;
                case MessageBoxButton.YesNoCancel:
                    break;
                case MessageBoxButton.YesNo:
                    break;
                default:
                    break;
            }
        }

        private void MetroWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnConfirm_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                btnCancel_Click(null, null);
            }
        }
        #endregion

    }
}
