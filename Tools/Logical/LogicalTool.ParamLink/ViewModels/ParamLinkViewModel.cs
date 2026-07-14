using LogicalTool.ParamLink.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Extension;
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

namespace LogicalTool.ParamLink.ViewModels
{
    [Serializable]
    public class ParamLinkViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private TransmitParam _sltCustomNodeInput = new TransmitParam();

        public TransmitParam SltCustomNodeInput
        {
            get { return _sltCustomNodeInput; }
            set { _sltCustomNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltLastNodeInput = new TransmitParam();

        public TransmitParam SltLastNodeInput
        {
            get { return _sltLastNodeInput; }
            set { _sltLastNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltGlobalNodeInput = new TransmitParam();

        public TransmitParam SltGlobalNodeInput
        {
            get { return _sltGlobalNodeInput; }
            set { _sltGlobalNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltOtherNodeInput = new TransmitParam();

        public TransmitParam SltOtherNodeInput
        {
            get { return _sltOtherNodeInput; }
            set { _sltOtherNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _activeTabIndex;

        public int ActiveTabIndex
        {
            get { return _activeTabIndex; }
            set { _activeTabIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ParamLinkModel _modelParam = new ParamLinkModel();

        public ParamLinkModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public ParamLinkViewModel()
        {

        }


        ~ParamLinkViewModel()
        {
            Console.WriteLine($"{DateTime.Now.ToString("hhmmss.fff")}触发了ParamLinkViewModel释放资源。。。");
        }
        #endregion

        #region Methods

        public override void InitParam()
        {
            ModelParam.LastNodeInputParams = Param as ObservableCollection<TransmitParam>;
        }

        /// <summary>
        /// 确认选择参数，弹出确认对话框
        /// </summary>
        private bool ConfirmSelect(TransmitParam param)
        {
            if (param == null) return false;
            var msg = $"是否选择参数 [{param.Name}] ？\n类型：{param.Type}\n值：{param.Value}";
            var result = MessageBox.Show(msg, "确认选择", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 根据参数类型执行选择并关闭对话框
        /// </summary>
        private void SelectAndClose(TransmitParam param, ResoureceType resourceType)
        {
            if (param == null) return;
            if (!ConfirmSelect(param)) return;
            param.Resourece = resourceType;
            param.IsLink = true;
            CloseDialog(ButtonResult.OK, new DialogParameters()
            {
                { "Param", param },
            });
        }

        #endregion

        #region Commands

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "DoubleClickLastNode":
                    SelectAndClose(SltLastNodeInput, ResoureceType.LastInput);
                    break;
                case "DoubleClickCustomGlobalNode":
                    SelectAndClose(SltCustomNodeInput, ResoureceType.CustomGlobal);
                    break;
                case "DoubleClickGlobalNode":
                    SelectAndClose(SltGlobalNodeInput, ResoureceType.Global);
                    break;
                case "DoubleClickOtherNode":
                    SelectAndClose(SltOtherNodeInput, ResoureceType.LastInput);
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                default:
                    break;
            }
        });

        /// <summary>
        /// 选择当前参数按钮命令，根据当前 Tab 选择对应参数
        /// </summary>
        public DelegateCommand ConfirmSelectCommand => new DelegateCommand(() =>
        {
            switch (ActiveTabIndex)
            {
                case 0:
                    SelectAndClose(SltLastNodeInput, ResoureceType.LastInput);
                    break;
                case 1:
                    SelectAndClose(SltGlobalNodeInput, ResoureceType.Global);
                    break;
                case 2:
                    SelectAndClose(SltCustomNodeInput, ResoureceType.CustomGlobal);
                    break;
                case 3:
                    SelectAndClose(SltOtherNodeInput, ResoureceType.LastInput);
                    break;
            }
        });

        #endregion

    }
}
