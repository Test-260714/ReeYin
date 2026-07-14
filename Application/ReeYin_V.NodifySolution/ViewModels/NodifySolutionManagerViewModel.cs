using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;


namespace ReeYin_V.NodifySolution.ViewModels
{
    public class NodifySolutionManagerViewModel : DialogViewModelBase
    {
        #region Fields
        public IConfigManager ConfigManager = null;


        #endregion

        #region Properties
        private NodifySolutionManagerModel _modelParam;

        public NodifySolutionManagerModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private NodifySolutionItem _sltCurSolutionItem;
        /// <summary>
        /// 选中的解决方案项目
        /// </summary>
        public NodifySolutionItem SltCurSolutionItem
        {
            get { return _sltCurSolutionItem; }
            set { _sltCurSolutionItem = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsCurrentDefault)); }
        }

        /// <summary>
        /// 当前选中项是否为默认启动方案
        /// </summary>
        public string IsCurrentDefault
        {
            get
            {
                if (SltCurSolutionItem == null) return "否";
                return SltCurSolutionItem == ModelParam?.InitiateSolution ? "是" : "否";
            }
        }
        #endregion


        #region Constructor
        public NodifySolutionManagerViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            ModelParam = PrismProvider.ProjectManager.NodifySolutionManager;
        }
        #endregion


        #region Commands
        /// <summary>
        /// 页面加载指令
        /// </summary>
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam = PrismProvider.ProjectManager.NodifySolutionManager;
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "打开":
                    {
                        if(SltCurSolutionItem == null)
                        {
                            HandyControl.Controls.MessageBox.Show("请选择一个方案项目！");
                            return;
                        }
                        PrismProvider.ProjectManager.SltCurSolutionItem = SltCurSolutionItem;

                        MessageBoxResult result = System.Windows.MessageBox.Show("确定要舍弃当前方案并打开新方案吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("打开");

                            //重新赋值
                            PrismProvider.ProjectManager.NodifySolutionManager = ModelParam;
                            ConfigManager.Write(ConfigKey.SolutionConfig, ModelParam);
                        
                            //关闭当前窗口
                            CloseDialog(ButtonResult.OK, new DialogParameters()
                            {
                                { "Param", ModelParam },
                            });
                        }
                        else
                        {

                        }
                    }
                    break;
                case "添加新解决方案":
                    {
                        ModelParam.SolutionItems.Add(new NodifySolutionItem()
                        {   
                            Guid = Guid.NewGuid(),
                            ID = UniversalMethods.FindMissingNumber(ModelParam.SolutionItems.Select(x => x.ID).ToList()),
                            FilePath = "NULL",
                            ModifyTime = DateTime.Now,
                        });
                    }
                    break;
                case "删除选中方案":
                    {
                        ModelParam.SolutionItems.Remove(SltCurSolutionItem);
                        SltCurSolutionItem = null;
                    }
                    break;

                case "设为默认启动":
                    {
                        PrismProvider.ProjectManager.NodifySolutionManager.InitiateSolution = SltCurSolutionItem;
                        ModelParam.InitiateSolution = SltCurSolutionItem;
                        RaisePropertyChanged(nameof(IsCurrentDefault));
                    }
                    break;
                case "还原备份":
                    {
                        if (SltCurSolutionItem == null)
                        {
                            HandyControl.Controls.MessageBox.Show("请先选择一个方案！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        MessageBoxResult bakResult = HandyControl.Controls.MessageBox.Show(
                            "确定要还原备份吗？此操作将使用备份文件替换当前的项目文件，当前文件将被覆盖。",
                            "确认还原",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (bakResult == MessageBoxResult.Yes)
                        {
                            try
                            {
                                string configFilePath = Path.Combine("Config",
                                    typeof(ConfigKey).FullName + "." + ConfigKey.SolutionConfig.ToString() + ".json");
                                string bakFilePath = Path.ChangeExtension(configFilePath, ".bak");

                                if (!File.Exists(bakFilePath))
                                {
                                    HandyControl.Controls.MessageBox.Show($"未找到备份文件：{Path.GetFullPath(bakFilePath)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                File.Copy(bakFilePath, configFilePath, true);

                                // 重新加载配置
                                var restored = ConfigManager.Read<NodifySolutionManagerModel>(ConfigKey.SolutionConfig);
                                if (restored != null)
                                {
                                    ModelParam = restored;
                                    PrismProvider.ProjectManager.NodifySolutionManager = ModelParam;
                                    SltCurSolutionItem = null;
                                    HandyControl.Controls.MessageBox.Show("还原备份成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    HandyControl.Controls.MessageBox.Show("备份文件读取失败，请检查备份文件是否完整。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                HandyControl.Controls.MessageBox.Show($"还原备份失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    break;
                case "选择路径":
                    {
                        // 创建文件夹选择对话框
                        using (var folderDialog = new FolderBrowserDialog())
                        {
                            // 设置对话框标题
                            folderDialog.Description = "请选择文件夹路径";

                            // 设置初始目录（可选）
                            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                            // 允许在对话框中创建新文件夹
                            folderDialog.ShowNewFolderButton = true;

                            // 显示对话框，用户点击"确定"则返回 DialogResult.OK
                            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                // 返回选中的文件夹路径
                                SltCurSolutionItem.FilePath = folderDialog.SelectedPath;
                            }
                            else
                            {
                                // 用户取消选择

                            }
                        }
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {

                    });
                    break;
                case "确认":
                    PrismProvider.ProjectManager.NodifySolutionManager = ModelParam;

                    ConfigManager.Write(ConfigKey.SolutionConfig, ModelParam);

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



        #endregion
    }
}
