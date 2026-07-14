using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    public class RecipeManager : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<PLCOrder> _recipes = new ObservableCollection<PLCOrder>();
        /// <summary>
        /// 配方参数
        /// </summary>
        public ObservableCollection<PLCOrder> Recipes
        {
            get { return _recipes; }
            set { _recipes = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCOrder _sltRecipe;
        /// <summary>
        /// 选中配方参数
        /// </summary>
        public PLCOrder SltRecipe
        {
            get { return _sltRecipe; }
            set { _sltRecipe = value; RaisePropertyChanged(); }
        }
        #endregion


    }
}
