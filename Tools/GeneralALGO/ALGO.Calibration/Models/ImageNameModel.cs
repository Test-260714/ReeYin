using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALGO.Calibration.Models
{
    [Serializable]
    public class ImageNameModel : BindableBase
    {
        private int _ID;
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        private bool _IsSelected;
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }
        private string _ImageName;
        /// <summary>
        /// 图片名称
        /// </summary>
        public string ImageName
        {
            get { return _ImageName; }
            set { SetProperty(ref _ImageName, value); }
        }
        private string _ImagePath;
        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePath
        {
            get { return _ImagePath; }
            set { SetProperty(ref _ImagePath, value); }
        }

    }
}
