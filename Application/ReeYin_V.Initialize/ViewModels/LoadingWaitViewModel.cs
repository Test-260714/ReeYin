using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Share.Prism;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReeYin_V.Initialize.ViewModels
{
    public class LoadingWaitViewModel : DialogViewModelBase
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

        public LoadingWaitViewModel()
        {
            LoadedCommand = new DelegateCommand(Init);
            EnterCommand = new DelegateCommand(Enter);
        }

        /// <summary>
        /// 进入主界面
        /// </summary>
        private async void Enter()
        {
            await Task.Delay(100).ContinueWith(p =>
            {
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                    //PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "RunMainView");
                });
            });

            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 初始化
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
