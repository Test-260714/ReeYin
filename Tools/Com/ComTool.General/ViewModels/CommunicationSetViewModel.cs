using ComTool.General.Communacation;
using ComTool.General.Models;
using Microsoft.Win32;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ComTool.General.ViewModels
{
    public class CommunicationSetViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private int _Communication_SelectedIndex;

        public int Communication_SelectedIndex
        {
            get { return _Communication_SelectedIndex; }
            set
            {
                _Communication_SelectedIndex = value;
                RaisePropertyChanged();
            }
        }

        private CommunicationSetModel _modelParam = new CommunicationSetModel();
        public CommunicationSetModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private ECommunacation _CurrentCommunication = new ECommunacation();
        public ECommunacation CurrentCommunication
        {
            get { return _CurrentCommunication; }
            set
            {
                _CurrentCommunication = value;
                RaisePropertyChanged();
            }
        }
        private Array _CommunicationTypes = Enum.GetValues(typeof(eCommunicationType));

        public Array CommunicationTypes
        {
            get { return _CommunicationTypes; }
            set { _CommunicationTypes = value; }
        }
        private eCommunicationType _CommunicationType = eCommunicationType.TCP客户端;

        public eCommunicationType CommunicationType
        {
            get { return _CommunicationType; }
            set
            {
                _CommunicationType = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Constructor
        public CommunicationSetViewModel(IConfigManager configManager) 
        {
            ConfigManager = configManager;
            PrismProvider.EventAggregator.GetEvent<HardwareChangedEvent>().Subscribe(()=> {

                //ModelParam.CommunicationModels = EComManageer.GetEcomList().ToObservableCollection();
            });
        }
        #endregion

        #region Command
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "确认":
                    {
                        ModelParam.s_ECommunacationDic = EComManageer.s_ECommunacationDic;
                        PrismProvider.HardwareModuleManager.Modules[ConfigKey.ComConfig] = ModelParam;
                        ConfigManager.Write(ConfigKey.ComConfig, ModelParam);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                default:
                    break;
            }

        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    string name = "";
                    switch (CommunicationType)
                    {
                        case eCommunicationType.TCP客户端:
                            name = EComManageer.CreateECom(
                                eCommunicationType.TCP客户端
                            );
                            break;
                        case eCommunicationType.TCP服务器:
                            name = EComManageer.CreateECom(
                                eCommunicationType.TCP服务器
                            );
                            break;
                        case eCommunicationType.串口通讯:
                            name = EComManageer.CreateECom(eCommunicationType.串口通讯);
                            break;
                        case eCommunicationType.UDP通讯:
                            name = EComManageer.CreateECom(
                                eCommunicationType.UDP通讯
                            );
                            break;
                        default:
                            break;
                    }

                    ModelParam.CommunicationModels = EComManageer.GetEcomList().ToObservableCollection();
                    break;
                case "Delete":
                    if (CurrentCommunication == null)
                        return;
                    EComManageer.DisConnect(CurrentCommunication.Key);
                    EComManageer.s_ECommunacationDic.Remove(
                        CurrentCommunication.Key
                    );
                    ModelParam.CommunicationModels = EComManageer.GetEcomList().ToObservableCollection();
                    break;
                default:
                    break;
            }
            //CommunicationSetView.Ins.dg.ItemsSource = CommunicationModels;
            Communication_SelectedIndex = ModelParam.CommunicationModels.Count - 1;
            
        });

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
        #endregion

        #region Method
        //[OnSerializing()] 序列化之前
        //[OnSerialized()] 序列化之后
        //[OnDeserializing()] 反序列化之前
        [OnDeserialized()] //反序列化之后
        internal void OnDeserializedMethod(StreamingContext context)
        {

        }

        //[OnSerializing()] 序列化之前
        //[OnSerialized()] 序列化之后
        //[OnDeserializing()] 反序列化之前
        [OnDeserialized()] //反序列化之后
        internal void OnSerializedMethod(StreamingContext context)
        {

        }


        public override void InitParam()
        {
            if (EComManageer.s_ECommunacationDic.Count == 0)
            {
                ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.ComConfig] as CommunicationSetModel ?? new CommunicationSetModel();
                if (ModelParam.s_ECommunacationDic != null)
                    EComManageer.s_ECommunacationDic = ModelParam.s_ECommunacationDic.DeepClone();
            }
            else
            {
                ModelParam.s_ECommunacationDic = EComManageer.s_ECommunacationDic;
                ModelParam.CommunicationModels = EComManageer.GetEcomList().ToObservableCollection();
            }
        }

        #endregion
    }
}
