using LogicalTool.Conditional.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Conditional.ViewModels
{
    [Serializable]
    public class ListConditionViewModel : BindableBase,IViewModuleParam, INavigationAware
    {
        #region Fields

        #endregion

        #region Properties
        private JudgeCodition _curCodition;

        public JudgeCodition CurCodition
        {
            get { return _curCodition; }
            set { _curCodition = value; RaisePropertyChanged(); }
        }

        private ConditionModel _modelParam;

        public ConditionModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public ListConditionViewModel()
        {
            
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<ConditionModel>("ModelParam");
            CurCodition = ModelParam.SltJudgeCodition;
        }
        #endregion

        #region Commands
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "追加至输出":
                    TransmitParam outputParam = new TransmitParam();
                    if (CurCodition.IsOutputSingle)
                    {
                        if (CurCodition.Variable.Value is IList list)
                        {
                            // 注意做边界检查
                            var value = list[CurCodition.SingleIndex];   // value 类型是 object（装箱/引用）
                            outputParam = new TransmitParam
                            {
                                Value = value,

                            };
                        }
                    }


                    ModelParam.OutputParams.Add(outputParam);
                    break;
                case "取消":

                    break;

                default:
                    break;
            }
        });
        #endregion

    }
}
