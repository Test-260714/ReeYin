using ComTool.General.Communacation;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComTool.General.Models
{
    [Serializable]
    public class CommunicationSetModel : BindableBase, IHardwareModule
    {
        #region Fields
        public Dictionary<string, ECommunacation> s_ECommunacationDic = null;
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<ECommunacation> _communicationModels = new ObservableCollection<ECommunacation>();

        public ObservableCollection<ECommunacation> CommunicationModels
        {
            get { return _communicationModels; }
            set 
            { 
                _communicationModels = value; 
                RaisePropertyChanged(); 
                
            }
        }

        [JsonIgnore]
        public List<HardwareStatus> AllHardwareStatus { get; set; }

        #endregion

        #region Constructor
        public CommunicationSetModel()
        {
            
        }
        #endregion

        #region Methods
        public InitResult Init()
        {
            if (s_ECommunacationDic != null)
                EComManageer.s_ECommunacationDic = s_ECommunacationDic;
            EComManageer.setEcomList(CommunicationModels.ToList());

            return new InitResult{ Success = true, Message = "TCP/UDP/串口初始化成功" };
        }

        public void Shutdown()
        {
            foreach (var item in CommunicationModels)
            {
                item.DisConnect();
            }
        }

        public void RefreshStatus()
        {
            foreach (var item in CommunicationModels)
            {
                
            }
        }
        #endregion


    }
}
