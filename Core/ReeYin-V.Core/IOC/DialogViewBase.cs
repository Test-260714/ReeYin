using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin_V.Core.IOC
{
    public class DialogViewBase : UserControl, IDialogAware
    {
        #region Fields
        public DialogCloseListener RequestClose { get; }
        #endregion
        #region Prop
        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value;  }
        }

        private string _icon;

        public string Icon
        {
            get { return _icon; }
            set { _icon = value;  }
        }

        private object _param;

        public object Param
        {
            get { return _param; }
            set { _param = value; }
        }

        #endregion

        /// <summary>
        /// 关闭对话框前，判断是否可以关闭
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>
        /// 关闭时执行的方法
        /// </summary>
        public virtual void OnDialogClosed()
        {

        }

        /// <summary>
        /// 打开时执行的方法
        /// </summary>
        /// <param name="parameters"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("Title");

            Icon = parameters.GetValue<string>("Icon");

            Param = parameters.GetValue<object>("Param");


            if (Icon == null)
            {
                Icon = "\ue640";
            }
            Title = Icon + " " + Title;
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="buttonResult"></param>
        /// <param name="dialogParameters"></param>
        public virtual void CloseDialog(ButtonResult buttonResult, IDialogParameters dialogParameters = null)
        {

            RequestClose.Invoke(dialogParameters, buttonResult);


            //RequestClose.Invoke(new DialogResult(buttonResult));
        }
    }
}
