using ALGO.MultiCameraCalibration.Models;
using OpenCvSharp.Aruco;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace ALGO.MultiCameraCalibration.ViewModels
{
    [Serializable]
    public class MultiCameraCalibrationViewModel : DialogViewModelBase, IViewModuleParam
    {
        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { SetProperty(ref _sltOutputParamName, value); }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { SetProperty(ref _currentOutputParam, value); }
        }

        public MultiCameraCalibrationModel ModelParam
        {
            get { return base.ModelParam as MultiCameraCalibrationModel; }
            set { base.ModelParam = value; }
        }

        public Array CalibBoardTypes { get; set; } = Enum.GetValues(typeof(eCalibrationBoardType));
        public Array DictionaryIds { get; set; } = Enum.GetValues(typeof(PredefinedDictionaryName));
        public Array CalibUsageModels { get; set; } = Enum.GetValues(typeof(MultiCameraUsageMode));
        public Array StitchBlendModes { get; set; } = Enum.GetValues(typeof(ReeYin_V.Core.Calibration.MultiCameraCalibrationSdk.MultiCameraBlendMode));

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
            {
                ModelParam.Serial = Serial;
            }

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

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
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
                    break;
                case "获取图片路径":
                    SelectCalibrationImageFolder();
                    break;
                case "删除标定图片":
                    DeleteSelectedCalibrationImage();
                    break;
                case "添加拼接输入":
                    ModelParam.AddStitchInputImage();
                    break;
                case "删除拼接输入":
                    ModelParam.RemoveSelectedStitchInputImage();
                    break;
                case "选择标定文件导出目录":
                    SelectCalibrationOutputFolder();
                    break;
                case "导出标定文件":
                    ExportCalibrationFile();
                    break;
                case "导入标定文件":
                    ImportCalibrationFile();
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    AddOutputParam();
                    break;
                case "Delete":
                    DeleteOutputParam();
                    break;
            }
        });

        public DelegateCommand<object> SelectStitchImageCommand => new DelegateCommand<object>((obj) =>
        {
            if (obj is not StitchInputImageModel row)
            {
                return;
            }

            SelectStitchImageFile(row);
        });

        public override void InitParam()
        {
            InitModelParam<MultiCameraCalibrationModel>();
            RestoreCalibrationFileOnInit();

        }

        public void Init()
        {
            ModelParam?.OutputParamResource.Clear();
            ModelParam?.InputParams.Clear();
            ModelParam?.OutputParams.Clear();
        }

        private void SelectCalibrationImageFolder()
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "请选择标定图片所在目录",
                    ShowNewFolderButton = true,
                    RootFolder = Environment.SpecialFolder.Desktop,
                    SelectedPath = !string.IsNullOrEmpty(ModelParam.CalibrationImageDir)
                        ? ModelParam.CalibrationImageDir
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                ModelParam.CalibrationImageDir = dialog.SelectedPath;
                foreach (string filePath in GetImageFilePaths(ModelParam.CalibrationImageDir))
                {
                    ModelParam.AddCalibrationImage(ModelParam.CameraId, filePath);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"选择标定图片文件夹时出错: {ex.Message}");
            }
        }

        private void SelectCalibrationOutputFolder()
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "请选择保存标定结果目录",
                    ShowNewFolderButton = true,
                    RootFolder = Environment.SpecialFolder.Desktop,
                    SelectedPath = !string.IsNullOrEmpty(ModelParam.CalibFileOutputDir)
                        ? ModelParam.CalibFileOutputDir
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ModelParam.CalibFileOutputDir = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"选择保存标定结果文件夹时出错: {ex.Message}");
            }
        }

        private void ExportCalibrationFile()
        {
            try
            {
                string outputFile = ModelParam.ResolveCalibrationOutputFile();
                if (File.Exists(outputFile))
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show(
                        $"标定文件已存在，是否覆盖？\r\n{outputFile}",
                        "覆盖标定文件",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                ModelParam.SaveCalibrationResults(outputFile);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出标定文件时出错: {ex.Message}");
            }
        }

        private void ImportCalibrationFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择标定文件",
                    Filter = "YAML 文件 (*.yaml;*.yml)|*.yaml;*.yml",
                    DefaultExt = ".yaml",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                ModelParam.LoadCalibrationFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导入标定文件时出错: {ex.Message}");
            }
        }

        private void RestoreCalibrationFileOnInit()
        {
            if (string.IsNullOrWhiteSpace(ModelParam?.CalibrationFilePath))
            {
                return;
            }

            if (!File.Exists(ModelParam.CalibrationFilePath))
            {
                return;
            }

            if (!ModelParam.TryRestoreCalibrationFromFile(out bool displayCacheOverwritten, out string errorMessage))
            {
                System.Windows.MessageBox.Show($"加载标定文件失败: {errorMessage}");
                return;
            }

            if (displayCacheOverwritten)
            {
                System.Windows.MessageBox.Show("标定文件与当前显示参数不一致，将以标定文件为准覆盖显示参数。");
            }
        }

        private void DeleteSelectedCalibrationImage()
        {
            if (ModelParam.SelectedCalibrationImage == null)
            {
                return;
            }

            ModelParam.CalibImageNameList.Remove(ModelParam.SelectedCalibrationImage);
            ModelParam.SelectedCalibrationImage = null;
        }

        private void AddOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName)
                || !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object selected)
                || selected is not TransmitParam curSltParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                return;
            }

            if (curSltParam.Resourece == ResoureceType.None)
            {
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    ParamName = curSltParam.Name,
                    Serial = ModelParam.Serial,
                    ParentNode = Name,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                    ResourcePath = curSltParam.ResourcePath,
                });
            }
            else if (curSltParam.Resourece == ResoureceType.Inupt)
            {
                TransmitParam inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                if (inputParam == null)
                {
                    return;
                }

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    ParentNode = Name,
                    Value = inputParam.Value.DeepClone(),
                    ResourcePath = inputParam.ResourcePath,
                    Serial = inputParam.Serial
                });
            }
        }

        private void DeleteOutputParam()
        {
            if (CurrentOutputParam == null)
            {
                return;
            }

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }

        private void SelectStitchImageFile(StitchInputImageModel row)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择拼接输入图片",
                    Filter = "图片文件 (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff;*.eps)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff;*.eps|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (!string.IsNullOrWhiteSpace(row.ImagePath))
                {
                    string directory = Path.GetDirectoryName(row.ImagePath);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                    }
                }

                if (dialog.ShowDialog() == true)
                {
                    row.ImagePath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"选择拼接输入图片时出错: {ex.Message}");
            }
        }

        public static List<string> GetImageFilePaths(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定的目录不存在: {directoryPath}");
            }

            string[] extensions = { ".bmp", ".png", ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".eps" };
            return Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
