using ALGO.NPointCalibration.Models;
using HalconDotNet;
using OpenCvSharp;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.NPointCalibration.ViewModels
{
    [Serializable]
    public class NPointCalibrationViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new NPointCalibrationModel ModelParam
        {
            get { return base.ModelParam as NPointCalibrationModel; }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public NPointCalibrationViewModel()
        {
        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            ModelParam = InitModelParam<NPointCalibrationModel>();
        }
        #endregion

        #region Commands

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
                case "执行":
                    ModelParam.ExecuteModule();
                    break;
                case "确认":
                    {
                        ModelParam.LoadKeyParam();
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;

                case "选择标定文件导出目录":
                    {
                        try
                        {
                            var dialog = new FolderBrowserDialog
                            {
                                Description = "请选择保存标定结果目录",
                                ShowNewFolderButton = true,
                                RootFolder = Environment.SpecialFolder.Desktop,
                                SelectedPath = !string.IsNullOrEmpty(ModelParam.CalibFileOutputDir) ?
                                ModelParam.CalibFileOutputDir :
                                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                            };

                            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                            if (result == System.Windows.Forms.DialogResult.OK)
                            {
                                ModelParam.CalibFileOutputDir = dialog.SelectedPath;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"选择保存标定结果文件夹时出错: {ex.Message}");
                        }
                    }
                    break;

                case "导出标定文件":
                    {
                        try
                        {
                            string filename = ModelParam.CalibFileOutputDir + "/NPointCalib_" + ModelParam.CameraId + ".yaml";
                            using (var fsWrite = new FileStorage(filename, FileStorage.Modes.Write))
                            {
                                fsWrite.Write("cameraId", ModelParam.CameraId);

                                Mat homographyMatrix = new Mat(3, 3, MatType.CV_64FC1);
                                for (int i = 0; i < 3; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        homographyMatrix.Set<double>(i, j, ModelParam.HomMat2D[i * 3 + j].D);
                                    }
                                }
                                fsWrite.Write("homographyMatrix", homographyMatrix);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"导出标定文件时出错: {ex.Message}");
                        }
                    }
                    break;

                case "导入标定文件":
                    {
                        try
                        {
                            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                            var dialogResult = (bool)dialog.ShowDialog();
                            if (dialogResult)
                            {
                                string CalibFilePath = dialog.FileName;

                                ModelParam.CalibFileOutputDir = Path.GetDirectoryName(CalibFilePath);

                                using (FileStorage fs = new FileStorage(CalibFilePath, FileStorage.Modes.Read))
                                {
                                    ModelParam.CameraId = fs["cameraId"].ToString();

                                    Mat homographyMatrix = fs["homographyMatrix"].ToMat();
                                    ModelParam.HomMat2D = new HTuple();
                                    for (int i = 0; i < 3; i++)
                                    {
                                        for (int j = 0; j < 3; j++)
                                        {
                                            ModelParam.HomMat2D.Append(homographyMatrix.At<double>(i, j));
                                        }
                                    }
                                }
                            }

                            ModelParam.OutHomMat2D = new ObservableCollection<double>(ModelParam.HomMat2D.DArr);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"导入标定文件时出错: {ex.Message}");
                        }
                    }
                    break;

                default:
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            // 隐藏模式直接关闭
            if (Visibility == Visibility.Hidden)
            {
                ModelParam.LoadKeyParam();
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand
        {
            get
            {
                return new DelegateCommand<object>
                (
                    (obj) =>
                    {
                        switch (obj?.ToString())
                        {
                            case "Add":
                                if (string.IsNullOrEmpty(SltOutputParamName) ||
                                    ModelParam.OutputParamResource == null ||
                                    !ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
                                    return;

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;
                                if (curSltParam == null) return;

                                if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
                                {
                                    System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                                }
                                else
                                {
                                    if (curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                                        if (inputParam == null)
                                            return;

                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = inputParam.Value,
                                            ResourcePath = inputParam.ResourcePath,
                                            Serial = inputParam.Serial
                                        });
                                    }
                                }
                                break;
                            case "Delete":
                                if (CurrentOutputParam != null)
                                {
                                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                                    CurrentOutputParam = null;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                );
            }
        }

        #endregion
    }
}
