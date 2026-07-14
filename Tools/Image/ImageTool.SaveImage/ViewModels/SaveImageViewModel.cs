using ImageTool.SaveImage.Models;
using Microsoft.Win32;
using ReeYin_V.Core.Enums;
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

namespace ImageTool.SaveImage.ViewModels
{
    [Serializable]
    public class SaveImageViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Peoperties
        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        private SaveImageModel _modelParam;

        public SaveImageModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private Array _imageTypess = Enum.GetValues(typeof(ImageType));
        public Array ImageTypess
        {
            get { return _imageTypess; }
            set { _imageTypess = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SaveImageViewModel()
        {
            
        }
        #endregion

        #region Commands
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

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "执行":

                    ModelParam.ExecuteModule();
                    break;
                case "取消":

                    CloseDialog(ButtonResult.No);

                    break;

                case "确认":

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });

                    break;
                default:
                    break;
            }

        });

        #endregion

        #region Methods
        public override void InitParam()
        {
            if (Param != null && (Param is SaveImageModel))
                ModelParam = Param as SaveImageModel;
            else
                ModelParam = new SaveImageModel();

            ModelParam.OutputParamResource.Clear();
            ModelParam.InputParams.Clear();
            ModelParam.OutputParams.Clear();

            ModelParam.TransferParam();

            if(ModelParam.TriggerModuleRun == null)
                ModelParam.TriggerModuleRun += () =>
                {
                    return ModelParam.ExecuteModule().Result;
                };
        }
        #endregion
    }

}
