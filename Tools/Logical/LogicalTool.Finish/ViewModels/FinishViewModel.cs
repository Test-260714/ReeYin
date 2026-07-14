using LogicalTool.Finish.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LogicalTool.Finish.ViewModels
{
    [Serializable]
    public class FinishViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties
        private ObservableCollection<TransmitParam> _globalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get { return _globalParams; }
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _globalParamsNames = new ObservableCollection<string>();

        public ObservableCollection<string> GlobalParamsNames
        {
            get { return _globalParamsNames; }
            set { _globalParamsNames = value; RaisePropertyChanged(); }
        }

        private TransmitParam _sltGlobalParam;

        public TransmitParam SltGlobalParam
        {
            get { return _sltGlobalParam; }
            set { _sltGlobalParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FinishModel _modelParam;

        public FinishModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; }
        }

        #endregion

        #region Constructor

        public FinishViewModel()
        {
            GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
            foreach (var item in GlobalParams)
            {
                GlobalParamsNames.Add(item.Serial.ToString("D3") + "_" + item.Name);
            }
        }

        #endregion

        #region Methods

        public override void InitParam()
        {
            if (Param != null && (Param is FinishModel))
                ModelParam = Param as FinishModel;
            else
                ModelParam = new FinishModel();
        }

        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //等待加载完成赋值


            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "执行":


                    break;
                case "取消":

                    break;
                case "确认":

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });
                    break;
                default:
                    break;
            }

        });
        #endregion

    }
}
