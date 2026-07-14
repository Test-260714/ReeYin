using ImageTool.VTKPCDisplay;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.ChartShow.Models
{
    [Serializable]
    public class VTKPointCloudModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public VTKPCDisplay mVTKPCDisplay { set; get; } = new VTKPCDisplay();


        #endregion

        #region Properties

        #endregion

        #region Constructor
        public VTKPointCloudModel()
        {
            
        }
        #endregion

        #region Methods

        #endregion
    }
}
