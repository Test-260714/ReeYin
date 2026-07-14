using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Timer = System.Windows.Forms.Timer;


namespace ReeYin_V.Initialize.ViewModels
{
    public class LoadingViewModel : DialogViewModelBase
    {
        #region Fields
        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; RaisePropertyChanged(); }
        }

        #endregion

        public ICommand LoadedCommand { get; }
        public ICommand EnterCommand { get; }
 
        public LoadingViewModel()
        {
            LoadedCommand = new DelegateCommand(Init);
            EnterCommand = new DelegateCommand(Enter);

        }

        /// <summary>
        /// 进入主界面
        /// </summary>
        private async void Enter()
        {
            //加载主界面
            await Task.Delay(100).ContinueWith(p =>
            {
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    //加载主界面
                    PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                    //导航到主区域
                    PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "RunMainView");
                });

            });

            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 异步方法
        /// </summary>
        private async void Init()
        {
            Message = "正在初始化所有组件...";

        }

        public void Monitor(object sender, EventArgs e)
        {
            if (!PrismProvider.ProjectManager.IsOpenSolution)
            {
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    Message = "初始化完成!!!";
                    CloseDialog(ButtonResult.OK);
                });

            }
        }

    }
}
