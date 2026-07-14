using LogicalTool.Conditional.Models;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LogicalTool.Conditional.ViewModels
{
    [Serializable]
    public class ConditionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        private const string REGION_NAME = "ConditionRegion";
        #endregion

        #region Properties
        [JsonIgnore]
        public IRegionManager? RegionManager { get; set; }

        [JsonIgnore]
        private ConditionModel _modelParam;

        public ConditionModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public ConditionViewModel()
        {

        }

        #endregion

        #region Methods

        public override void InitParam()
        {
            if (Param != null && Param is ConditionModel conditionModel)
                ModelParam = conditionModel;
            else
                ModelParam = new ConditionModel();
        }

        /// <summary>
        /// 根据数据类型导航到对应的视图
        /// </summary>
        private void NavigateToViewByDataType(DataType dataType)
        {
            string viewName = dataType switch
            {
                DataType.Object => "ObjectConditionView",
                DataType.List => "ListConditionView",
                DataType.String => "StringOperationView",
                DataType.Bool => "DefaultView",
                DataType.Int => "IntConditionView",
                _ => null
            };

            NavigateToView(viewName);
        }

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        private void NavigateToView(string viewName)
        {
            if (RegionManager == null)
            {
                Console.WriteLine("错误：RegionManager 未初始化");
                return;
            }

            if (string.IsNullOrEmpty(viewName)) return;

            var navigationParams = new NavigationParameters
            {
                { "ModelParam", ModelParam }
            };

            try
            {
                RegionManager.RequestNavigate(REGION_NAME, viewName, navigationParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航错误：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 添加新的判断条件
        /// </summary>
        private void AddNewCondition()
        {
            try
            {
                var newCondition = new JudgeCodition
                {
                    Guid = Guid.NewGuid(),
                    IsUsing = false,
                };
                ModelParam.AllJudgeCodition.Add(newCondition);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加条件失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 删除选中的判断条件
        /// </summary>
        private void DeleteSelectedCondition()
        {
            if (ModelParam.SltJudgeCodition == null)
            {
                Console.WriteLine("警告：未选中任何条件");
                return;
            }

            try
            {
                ModelParam.AllJudgeCodition.Remove(ModelParam.SltJudgeCodition);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除条件失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 关闭对话框并返回结果
        /// </summary>
        private void CloseDialogWithResult()
        {
            CloseDialog(ButtonResult.OK, new DialogParameters()
            {
                { "Param", ModelParam },
            });
        }

        #endregion

        #region Commands

        /// <summary>
        /// 加载命令
        /// </summary>
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            // 等待加载完成赋值
            ModelParam.SltJudgeCodition = null;
            // 不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialogWithResult();
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
                    // TODO: 实现执行逻辑
                    break;

                case "取消":
                    CloseDialog(ButtonResult.Cancel);
                    break;

                case "确认":
                    CloseDialogWithResult();
                    break;

                default:
                    Console.WriteLine($"未知命令：{order}");
                    break;
            }
        });

        /// <summary>
        /// 数据操作命令
        /// </summary>
        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    AddNewCondition();
                    ModelParam.SltJudgeCodition = null;
                    break;

                case "Modify":
                    // TODO: 实现修改逻辑
                    break;

                case "Delete":
                    DeleteSelectedCondition();
                    break;

                case "使能":
                    // 使能状态已通过绑定自动更新
                    break;

                default:
                    Console.WriteLine($"未知操作：{obj}");
                    break;
            }
        });

        /// <summary>
        /// 选中单元格变化命令 - 根据变量类型导航到对应视图
        /// </summary>
        public DelegateCommand SelectedCellsChanged => new DelegateCommand(() =>
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                if (ModelParam?.SltJudgeCodition?.Variable == null)
                {
                    return;
                }

                NavigateToViewByDataType(ModelParam.SltJudgeCodition.Variable.Type);
            });
        });

        #endregion
    }
}
