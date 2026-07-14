using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Login.ViewModels
{
    public class ExitViewModel : DialogViewModelBase
    {


        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "关闭软件":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {

                        });
                    }
                    break;

                case "关机":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要执行此操作吗? 确认后设备将断电", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        PrismProvider.EventAggregator.GetEvent<ModuleRalatedEvent>().Publish(("PLC","关机"));
                        Console.WriteLine("触发关机");
                        Thread.Sleep(100);
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {

                        });
                    }
                    break;
                default:
                    break;
            }
        });
    }
}
