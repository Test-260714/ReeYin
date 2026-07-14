using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Prism.Mvvm;

namespace ReeYin_V.UI
{
    public class MessageViewModel : BindableBase
    {
        #region Prop

        private string _Message;
        /// <summary>
        /// 显示内容
        /// </summary>
        public string Message
        {
            get { return _Message; }
            set { SetProperty(ref _Message, value); }
        }
        private string _ToolBarMsg;
        /// <summary>
        /// 工具条内容
        /// </summary>
        public string ToolBarMsg
        {
            get { return _ToolBarMsg; }
            set { SetProperty(ref _ToolBarMsg, value); }
        }
        private string _Icon;
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon
        {
            get { return _Icon; }
            set { SetProperty(ref _Icon, value); }
        }
        private Visibility _ConfirmVisibility = Visibility.Visible;
        /// <summary>
        /// 确认按钮显示状态
        /// </summary>
        public Visibility ConfirmVisibility
        {
            get { return _ConfirmVisibility; }
            set { SetProperty(ref _ConfirmVisibility, value); }
        }
        private Visibility _CancelVisibility = Visibility.Collapsed;
        /// <summary>
        /// 取消按钮显示状态
        /// </summary>
        public Visibility CancelVisibility
        {
            get { return _CancelVisibility; }
            set { SetProperty(ref _CancelVisibility, value); }
        }
        private bool _IsCloseButtonEnabled = true;
        /// <summary>
        /// 关闭按钮使能
        /// </summary>
        public bool IsCloseButtonEnabled
        {
            get { return _IsCloseButtonEnabled; }
            set { SetProperty(ref _IsCloseButtonEnabled, value); }
        }
        private bool _IsMinButtonEnabled = true;
        /// <summary>
        /// 最小化按钮使能
        /// </summary>
        public bool IsMinButtonEnabled
        {
            get { return _IsMinButtonEnabled; }
            set { SetProperty(ref _IsMinButtonEnabled, value); }
        }

        #endregion
    }
}
