using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    [Serializable]
    public class CoordinateCacheModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<CoordinatePos> _allPosInfo = new ObservableCollection<CoordinatePos>() 
        {

        };
        /// <summary>
        /// 所有位置信息
        /// </summary>
        public ObservableCollection<CoordinatePos> AllPosInfo
        {
            get { return _allPosInfo; }
            set { _allPosInfo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CoordinatePos _sltPosInfo;
        /// <summary>
        /// 当前选中的位置信息
        /// </summary>
        public CoordinatePos SltPosInfo
        {
            get { return _sltPosInfo; }
            set { _sltPosInfo = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public CoordinateCacheModel()
        {
            
        }
        #endregion

        #region Methods

        #endregion

    }

    public enum MovingMode
    {

    }

}
