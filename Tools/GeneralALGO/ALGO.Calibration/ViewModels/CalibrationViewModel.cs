using ALGO.Calibration.Models;
using ImageTool.Halcon.Config;
using Microsoft.Win32;
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
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static ReeYin_V.Core.Calibration.CameraCalibrationSdk;


namespace ALGO.Calibration.ViewModels
{
    [Serializable]
    public class CalibrationViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        /// <summary>
        /// 输出参数资源
        /// </summary>
        private Dictionary<string, object> OutputParamResource = new Dictionary<string, object>();
        #endregion

        #region Peoperties
        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private List<string> _outputParamNames = new List<string>();

        public List<string> OutputParamNames
        {
            get { return _outputParamNames; }
            set { _outputParamNames = value; RaisePropertyChanged(); }
        }


        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _OutputParams = new ObservableCollection<TransmitParam>();

        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _OutputParams; }
            set { _OutputParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _InputParams = new ObservableCollection<TransmitParam>();

        public ObservableCollection<TransmitParam> InputParams
        {
            get { return _InputParams; }
            set { _InputParams = value; RaisePropertyChanged(); }
        }


        private CalibrationModel _modelParam;

        public CalibrationModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }


        public Array CalibBoardTypes { get; set; } = Enum.GetValues(typeof(eCalibrationBoardType));

        public Array DictionaryIds { get; set; } = Enum.GetValues(typeof(PredefinedDictionaryName));

        public Array CalibUsageModels { get; set; } = Enum.GetValues(typeof(eCalibUsageModel));


        #endregion

        #region Constructor
        public CalibrationViewModel()
        {

        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            //等待加载完成赋值
            if (ModelParam.InputImage == null)
            {

            }

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

                case "获取图片路径":
                    {
                        try
                        {
                            var dialog = new FolderBrowserDialog
                            {
                                Description = "请选择标定图片所在目录",
                                ShowNewFolderButton = true,
                                RootFolder = Environment.SpecialFolder.Desktop,
                                SelectedPath = !string.IsNullOrEmpty(ModelParam.CalibrationImageDir) ?
                                ModelParam.CalibrationImageDir :
                                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                            };

                            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                            if (result == System.Windows.Forms.DialogResult.OK)
                            {
                                ModelParam.CalibrationImageDir = dialog.SelectedPath;

                                GetAllFileNames(ModelParam.CalibrationImageDir);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 处理异常，例如显示错误消息
                            System.Windows.MessageBox.Show($"选择标定图片文件夹时出错: {ex.Message}");
                        }

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
                            // 处理异常，例如显示错误消息
                            System.Windows.MessageBox.Show($"选择保存标定结果文件夹时出错: {ex.Message}");
                        }

                    }
                    break;

                case "导出标定文件":
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
                            // 处理异常，例如显示错误消息
                            System.Windows.MessageBox.Show($"导出标定文件时出错: {ex.Message}");
                        }

                    }
                    break;
                
                case "导入标定文件":
                    {
                        try
                        {
                            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog 
                            {
                                Title = "选择标定文件",
                                Filter = "YAML 文件 (*.yaml;*.yml)|*.yaml;*.yml",
                                DefaultExt = ".yaml",
                                CheckFileExists = true,
                                Multiselect = false
                            };

                            var dialogResult = (bool)dialog.ShowDialog();
                            if (dialogResult)
                            {
                                ModelParam.LoadCalibrationFile(dialog.FileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 处理异常，例如显示错误消息
                            System.Windows.MessageBox.Show($"导入标定文件时出错: {ex.Message}");
                        }

                    }
                    break;

                default:
                    break;
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
                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
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
                                            ParentNode = Name,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            ParentNode = Name,
                                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value.DeepClone(),
                                            ResourcePath = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.ResourcePath,
                                            Serial = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name).Serial
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

        #region Methods
        public override void InitParam()
        {
            if (Param != null && (Param is CalibrationModel))
                ModelParam = Param as CalibrationModel;
            else
            {
                ModelParam = new CalibrationModel();
                ModelParam.moduleInputParam = (Param as IModuleParam).moduleInputParam;
            }
            LoadSpecificConfig(ModelParam);
            RestoreCalibrationFileOnInit();

            // 获取数据点定义
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(CalibrationModel));

            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = point.Name,
                    Type = DataType._object,
                    Resourece = ResoureceType.None,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }
            ModelParam.TransferParam();

            if (ModelParam.TriggerModuleRun == null)
                ModelParam.TriggerModuleRun += () =>
                {
                    return ModelParam.ExecuteModule().Result;
                };
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

        public void Init()
        {
            OutputParamResource.Clear();
            InputParams.Clear();
            OutputParams.Clear();
        }



        /// <summary>
        /// 获取指定路径下所有文件的文件名
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>文件名列表</returns>
        public void GetAllFileNames(string directoryPath)
        {
            // 检查目录是否存在
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定的目录不存在: {directoryPath}");
            }

            try
            {
                // 获取所有文件的完整路径
                var filePaths = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".bmp") || s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".gif") || s.EndsWith(".tif") || s.EndsWith(".tiff") || s.EndsWith(".eps"));

                if (filePaths.Any())
                {
                    var names = filePaths.ToList();
                    ModelParam.CalibImageNameList.Clear();
                    for (int i = 0; i < names.Count; i++)
                    {
                        ModelParam.CalibImageNameList.Add(new ImageNameModel() { ID = i + 1, ImagePath = names[i], ImageName = Path.GetFileName(names[i]), IsSelected = true });
                    }
                }


            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"没有权限访问目录: {directoryPath}", ex);
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException($"路径过长: {directoryPath}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"获取文件列表时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取指定路径下所有文件的完整路径
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="searchOption">搜索选项（是否包含子目录）</param>
        /// <param name="searchPattern">搜索模式（如 "*.*" 或 "*.txt"）</param>
        /// <returns>文件完整路径列表</returns>
        public static List<string> GetAllFilePaths(string directoryPath,
            SearchOption searchOption = SearchOption.TopDirectoryOnly,
            string searchPattern = "*.*")
        {
            // 检查目录是否存在
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定的目录不存在: {directoryPath}");
            }

            try
            {
                // 获取所有文件的完整路径
                string[] filePaths = Directory.GetFiles(directoryPath, searchPattern, searchOption);

                return filePaths.ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"没有权限访问目录: {directoryPath}", ex);
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException($"路径过长: {directoryPath}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"获取文件列表时发生错误: {ex.Message}", ex);
            }
        }



        #endregion
    }
}
