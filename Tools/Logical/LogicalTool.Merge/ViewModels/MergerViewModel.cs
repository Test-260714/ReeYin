using LogicalTool.Merge.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
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

namespace LogicalTool.Merge.ViewModels
{
    [Serializable]
    public class MergerViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties

        private MergerModel _modelParam;
        public MergerModel ModelParam
        {
            get => _modelParam;
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _selectedInputParam;
        /// <summary>
        /// 左侧InputParams中选中的参数
        /// </summary>
        public TransmitParam SelectedInputParam
        {
            get => _selectedInputParam;
            set { _selectedInputParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _selectedOutputParam;
        /// <summary>
        /// 右侧OutputParams中选中的参数
        /// </summary>
        public TransmitParam SelectedOutputParam
        {
            get => _selectedOutputParam;
            set { _selectedOutputParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public MergerViewModel()
        {
        }

        #endregion

        #region Override

        public override bool CanCloseDialog() => true;

        public override void InitParam()
        {
            ModelParam = Param is MergerModel model ? model : new MergerModel();
            ModelParam.TransferParam();
        }

        #endregion

        #region Commands

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam.LoadKeyParam();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam }
                });
            }
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.Cancel, new DialogParameters());
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam }
                    });
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>(obj =>
        {
            switch (obj?.ToString())
            {
                case "AddToOutput":
                    AddSelectedToOutput();
                    break;

                case "AddAllToOutput":
                    AddAllToOutput();
                    break;

                case "RemoveFromOutput":
                    RemoveSelectedFromOutput();
                    break;

                case "ClearOutput":
                    ModelParam.OutputParams.Clear();
                    break;
            }
        });

        #endregion

        #region Methods

        /// <summary>
        /// 将选中的输入参数加入输出列表
        /// </summary>
        private void AddSelectedToOutput()
        {
            if (SelectedInputParam == null) return;

            // 避免重复添加（按 Guid 判断）
            if (ModelParam.OutputParams.Any(p => p.Guid == SelectedInputParam.Guid))
            {
                Console.WriteLine($"参数 {SelectedInputParam.Name} 已在输出列表中");
                return;
            }

            ModelParam.OutputParams.Add(new TransmitParam
            {
                Guid = SelectedInputParam.Guid,
                Serial = SelectedInputParam.Serial,
                ParentNode = SelectedInputParam.ParentNode,
                LinkGuid = SelectedInputParam.LinkGuid,
                Name = SelectedInputParam.Name,
                ParamName = SelectedInputParam.ParamName,
                Type = SelectedInputParam.Type,
                Value = SelectedInputParam.Value,
                Describe = SelectedInputParam.Describe,
                IsGlobal = false,
                Resourece = ResoureceType.Output,
                ResourcePath = SelectedInputParam.ResourcePath,
            });
        }

        /// <summary>
        /// 将全部输入参数加入输出列表
        /// </summary>
        private void AddAllToOutput()
        {
            foreach (var param in ModelParam.InputParams)
            {
                if (ModelParam.OutputParams.Any(p => p.Guid == param.Guid)) continue;

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Guid = param.Guid,
                    Serial = param.Serial,
                    ParentNode = param.ParentNode,
                    LinkGuid = param.LinkGuid,
                    Name = param.Name,
                    ParamName = param.ParamName,
                    Type = param.Type,
                    Value = param.Value,
                    Describe = param.Describe,
                    IsGlobal = false,
                    Resourece = ResoureceType.Output,
                    ResourcePath = param.ResourcePath,
                });
            }
        }

        /// <summary>
        /// 从输出列表移除选中的参数
        /// </summary>
        private void RemoveSelectedFromOutput()
        {
            if (SelectedOutputParam == null) return;
            ModelParam.OutputParams.Remove(SelectedOutputParam);
            SelectedOutputParam = null;
        }

        #endregion
    }
}
