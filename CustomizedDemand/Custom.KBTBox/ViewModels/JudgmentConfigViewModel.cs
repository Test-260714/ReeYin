using Custom.KBTBox.Models;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.KBTBox.ViewModels
{
    public class JudgmentConfigViewModel : DialogViewModelBase, INavigationAware
    {
        #region Properties
        private JudgmentConfigModel _config = new();
        public JudgmentConfigModel Config
        {
            get { return _config; }
            set { _config = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public JudgmentConfigViewModel()
        {
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "保存":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要保存判定参数配置吗?", "操作确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                            return;
                        PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("保存");
                    }
                    break;
                default:
                    break;
            }
        });

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            int Serail = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serail = id;

            var temp = PrismProvider.ProjectManager.GetNodeParamCacheValue<SensorDataCollectionModel>($"{Serail.ToString("D3")}");
            Config = temp.JudgmentConfig;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
        #endregion
    }
}
