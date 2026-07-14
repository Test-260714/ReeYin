using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Share.Models
{
    public class MenuModel : BindableBase
    {
        public int ID;
        public int ParentID;

        private bool _isEnabled;
        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private string _name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private string _type;
        /// <summary>
        /// 类型
        /// </summary>
        public string Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged(); }
        }

        private string _event;
        /// <summary>
        /// 事件
        /// </summary>
        public string Event
        {
            get { return _event; }
            set { _event = value; RaisePropertyChanged(); }
        }

        private string _icon;

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; RaisePropertyChanged(); }
        }

        private string _description;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        private DateTime _createTime;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime
        {
            get { return _createTime; }
            set { _createTime = value; RaisePropertyChanged(); }
        }

        private DateTime _updateTime;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set { _updateTime = value; RaisePropertyChanged(); }
        }

        private bool isVisiable;
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisiable
        {
            get { return isVisiable; }
            set { isVisiable = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 子项
        /// </summary>
        public ObservableCollection<MenuModel> Children { get; set; } = new ObservableCollection<MenuModel>();

        /// <summary>
        /// 执行事件
        /// </summary>
        public DelegateCommand<object> ExecuteEventCommand => new DelegateCommand<object>((obj) =>
        {
            var Menu = (MenuModel)obj;

            switch (Menu.Event)
            {
                case "新建方案":
                    {
                        PrismProvider.DialogService.ShowDialog("SolutionManagerView", new DialogParameters
                        {
                            { "Title", "解决方案管理页面" },
                            { "Icon", "\ue652" },
                            { "Action", "新建" },
                        }, result =>
                        {
                            if (result.Result == ButtonResult.OK)
                                PrismProvider.RegionManager.RequestNavigate(RegionNames.PrimaryRegion, "AppView");
                        }, nameof(DialogWindowView));
                    }
                    break;
                case "方案列表":
                    {
                        //弹窗初始化页面
                        PrismProvider.DialogService.ShowDialog("SolutionManagerView", new DialogParameters
                        {
                            { "Title", "解决方案管理页面" },
                            { "Icon", "\ue652" },
                        }, result =>
                        {
                        }, nameof(DialogWindowView));
                    }
                    break;
                case "硬件设置":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            //加载主界面
                            PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                            //导航到主区域
                            PrismProvider.RegionManager.RequestNavigate("MainRegion", "HardwareConfigView");
                        });


                        ////弹窗初始化页面
                        //PrismProvider.DialogService.ShowDialog("HardwareConfigView", new DialogParameters
                        //{
                        //    { "Title", "硬件设置页面" },
                        //    { "Icon", "\ue68a" },
                        //}, result =>
                        //{
                        //    if (result.Result == ButtonResult.OK)
                        //    {
                        //        // 处理对话框返回结果
                        //    }
                        //}, nameof(DialogWindowView));
                    }
                    break;
                case "报警管理":
                case "报警中心":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicationAlarmCenterModule);
                            PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "AlarmWorkbenchShellView");
                        });
                    }
                    break;
                case "打开":
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog
                        {
                            Filter = "解决方案 (*.rysl)|*.rysl",
                            DefaultExt = "rysl",
                            Title = "打开解决方案"
                        };

                        if (openFileDialog.ShowDialog() != true)
                            break;

                        var filePath = openFileDialog.FileName;
                        var solutionManager = PrismProvider.ProjectManager.SolutionManager;

                        // 检查是否已在最近列表中
                        var existing = solutionManager.ProjectsBaseInfo
                            .FirstOrDefault(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                        var baseInfo = existing ?? new ProjectItemBaseInfo
                        {
                            Guid = Guid.NewGuid(),
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            FilePath = filePath,
                            ModifyTime = File.GetLastWriteTime(filePath)
                        };

                        var confirm = MessageBox.Show(
                            "打开新解决方案将释放当前解决方案的所有资源，确定继续吗？",
                            "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (confirm != MessageBoxResult.Yes)
                            break;

                        Task.Run(() =>
                        {
                            try
                            {
                                PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("释放");
                                var loaded = solutionManager.LoadProject(filePath);
                                if (!loaded)
                                {
                                    PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");
                                    PrismProvider.Dispatcher.Invoke(() =>
                                        HandyControl.Controls.MessageBox.Show("解决方案加载失败，请检查文件是否存在且内容有效", "错误",
                                            MessageBoxButton.OK, MessageBoxImage.Error));
                                    return;
                                }

                                baseInfo.ModifyTime = DateTime.Now;
                                baseInfo.IsUsing = true;
                                solutionManager.AddOrUpdateRecent(baseInfo);
                                solutionManager.DefaultBaseInfo = baseInfo;
                                PrismProvider.ProjectManager.ConfigManager.Write(ConfigKey.ProjectConfig, solutionManager);

                                PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("打开");
                                Logs.LogInfo($"打开解决方案成功: {baseInfo.Name}");
                            }
                            catch (Exception ex)
                            {
                                Logs.LogError($"打开解决方案失败: {ex.Message}");
                                PrismProvider.Dispatcher.Invoke(() =>
                                    HandyControl.Controls.MessageBox.Show($"打开失败: {ex.Message}", "错误",
                                        MessageBoxButton.OK, MessageBoxImage.Error));
                            }
                        });
                    }
                    break;
                case "保存":
                    {
                        var solutionManager = PrismProvider.ProjectManager.SolutionManager;
                        var currentBaseInfo = solutionManager.DefaultBaseInfo;

                        // 没有已加载的方案或路径为空，弹出另存为对话框
                        if (currentBaseInfo == null || string.IsNullOrEmpty(currentBaseInfo.FilePath))
                        {
                            var saveDialog = new Microsoft.Win32.SaveFileDialog
                            {
                                Title = "另存为",
                                Filter = "解决方案 (*.rysl)|*.rysl|所有文件 (*.*)|*.*",
                                DefaultExt = "rysl",
                                FileName = currentBaseInfo?.Name ?? "解决方案"
                            };

                            if (saveDialog.ShowDialog() != true)
                                break;

                            var savePath = saveDialog.FileName;
                            if (currentBaseInfo == null)
                            {
                                currentBaseInfo = new ProjectItemBaseInfo
                                {
                                    Guid = Guid.NewGuid(),
                                    Name = Path.GetFileNameWithoutExtension(savePath),
                                    FilePath = savePath,
                                    ModifyTime = DateTime.Now,
                                    IsUsing = true
                                };
                                solutionManager.DefaultBaseInfo = currentBaseInfo;
                            }
                            else
                            {
                                currentBaseInfo.FilePath = savePath;
                                currentBaseInfo.Name = Path.GetFileNameWithoutExtension(savePath);
                            }
                        }

                        var confirmResult = HandyControl.Controls.MessageBox.Show(
                            "确定要保存当前方案吗?", "操作确认",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (confirmResult != MessageBoxResult.Yes)
                            break;

                        var infoToSave = currentBaseInfo;
                        Task.Run(() =>
                        {
                            try
                            {
                                solutionManager.SaveProject(infoToSave.FilePath);
                                infoToSave.ModifyTime = DateTime.Now;
                                solutionManager.AddOrUpdateRecent(infoToSave);
                                PrismProvider.ProjectManager.ConfigManager.Write(ConfigKey.ProjectConfig, solutionManager);
                                PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("保存");
                                Logs.LogInfo($"保存解决方案成功: {infoToSave.Name}");
                            }
                            catch (Exception ex)
                            {
                                Logs.LogError($"保存解决方案失败: {ex.Message}");
                                PrismProvider.Dispatcher.Invoke(() =>
                                    HandyControl.Controls.MessageBox.Show($"保存失败: {ex.Message}", "错误",
                                        MessageBoxButton.OK, MessageBoxImage.Error));
                            }
                        });
                    }
                    break;
                case "急速模式":
                    {
                        //弹窗初始化页面
                        PrismProvider.DialogService.ShowDialog("RecipeManagerView", new DialogParameters
                        {
                            { "Title", "配方管理" },
                            { "Icon", "\ue652" },
                        }, result =>
                        {
                        }, nameof(DialogWindowView));
                    }
                    break;
                case "运行一次":
                    {

                    }
                    break;
                case "循环运行":
                    {

                    }
                    break;
                case "停止":
                    {
                        PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Stopped);
                        Logs.LogInfo($"触发停止！");
                    }
                    break;
                case "全局变量":
                    {
                        //全局变量页面
                        PrismProvider.DialogService.Show("CustomVariableListView", new DialogParameters
                        {
                            { "Title", "自定义全局变量" },
                            { "Icon", "\ue673" },
                        }, result =>
                        {

                        }, nameof(SingleInstanceDialogWindowView));
                    }
                    break;
                case "相机设置":
                    {
                        PrismProvider.DialogService.Show("CameraSetView", new DialogParameters
                        {
                            { "Title", "相机设置" },
                            { "Icon", "\ue673" },
                        }, result =>
                        {

                        }, nameof(SingleInstanceDialogWindowView));
                    }
                    break;
                case "通信设置":
                    {
                        //弹窗初始化页面
                        PrismProvider.DialogService.Show("CommunicationSetView", new DialogParameters
                        {
                            { "Title", "通信设置" },
                            { "Icon", "\ue673" },
                        }, result =>
                        {
                        }, nameof(SingleInstanceDialogWindowView));
                    }
                    break;
                case "配方管理":
                case "配方列表":
                case "RecipeManager":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            //加载主界面
                            PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                            //导航到主区域
                            PrismProvider.RegionManager.RequestNavigate("MainRegion", "RecipeManagerView");
                        });


                        //PrismProvider.DialogService.ShowDialog("RecipeManagerView", new DialogParameters
                        //{
                        //    { "Title", "配方管理" },
                        //    { "Icon", "\ue673" },
                        //}, result =>
                        //{
                        //}, nameof(DialogWindowView));
                    }
                    break;
            }
        });

    }
}
