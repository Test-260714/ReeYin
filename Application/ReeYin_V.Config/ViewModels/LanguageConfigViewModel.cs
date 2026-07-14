using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core;
using ReeYin_V.Core.Cache;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.Core.Services.WorkStatus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ReeYin_V.Config.ViewModels
{
    public class LanguageConfigViewModel : DialogViewModelBase
    {
        #region Fields
        private ICacheManager CacheManager { get; }
        #endregion

        #region Properties
        private ObservableCollection<LanguageItem> _languageItems = new ObservableCollection<LanguageItem>();
        /// <summary>
        /// 语言配置项集合
        /// </summary>
        public ObservableCollection<LanguageItem> LanguageItems
        {
            get { return _languageItems; }
            set { _languageItems = value; RaisePropertyChanged(); }
        }

        private LanguageItem _currentLanguage;
        /// <summary>
        /// 当前选中的语言
        /// </summary>
        public LanguageItem CurrentLanguage
        {
            get { return _currentLanguage; }
            set { _currentLanguage = value; RaisePropertyChanged(); }
        }

        private string _externalLanguagePackPath;
        /// <summary>
        /// 外部语言包路径
        /// </summary>
        public string ExternalLanguagePackPath
        {
            get { return _externalLanguagePackPath; }
            set
            {
                _externalLanguagePackPath = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasExternalPath));
            }
        }

        private ObservableCollection<LanguageItem> _scannedLanguagePacks = new ObservableCollection<LanguageItem>();
        /// <summary>
        /// 扫描到的语言包列表
        /// </summary>
        public ObservableCollection<LanguageItem> ScannedLanguagePacks
        {
            get { return _scannedLanguagePacks; }
            set
            {
                _scannedLanguagePacks = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasScannedPacks));
            }
        }

        private LanguageItem _selectedExternalPack;
        /// <summary>
        /// 选中的外部语言包
        /// </summary>
        public LanguageItem SelectedExternalPack
        {
            get { return _selectedExternalPack; }
            set { _selectedExternalPack = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否有外部路径
        /// </summary>
        public bool HasExternalPath => !string.IsNullOrEmpty(ExternalLanguagePackPath);

        private bool _hasScannedPacks ;

        public bool HasScannedPacks
        {
            get { return _hasScannedPacks; }
            set { _hasScannedPacks = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public LanguageConfigViewModel(ICacheManager cacheManager)
        {
            CacheManager = cacheManager;
            InitializeLanguages();
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
                case "切换语言":
                    {
                        if (CurrentLanguage != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"切换到语言: {CurrentLanguage.DisplayName}");
                        }
                    }
                    break;

                case "应用语言":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要执行此操作吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        if (CurrentLanguage != null && !CurrentLanguage.IsActive)
                        {
                            ApplyLanguage(CurrentLanguage);
                        }
                    }
                    break;

                case "重置语言":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要执行此操作吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                        ResetToDefaultLanguage();
                    }
                    break;

                case "刷新语言":
                    {
                        RefreshLanguages();
                    }
                    break;

                case "选择语言包文件夹":
                    {
                        SelectLanguagePackFolder();
                    }
                    break;

                case "扫描语言包":
                    {
                        ScanExternalLanguagePacks();
                    }
                    break;

                case "加载外部语言包":
                    {
                        LoadExternalLanguagePack();
                    }
                    break;

                case "返回主页面":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            //加载主界面
                            PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                            //导航到主区域
                            PrismProvider.RegionManager.RequestNavigate("MainRegion", "MainView");
                        });
                    }
                    break;

                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;

                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters());
                    break;

                default:
                    break;
            }
        });
        #endregion

        #region OverrideMethods
        public override void InitParam()
        {
            // 初始化参数
        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化语言列表
        /// </summary>
        private void InitializeLanguages()
        {
            LanguageItems = ServiceProvider.LanguageManager.Param.LanguageItems;

            // 设置默认选中项
            CurrentLanguage = ServiceProvider.LanguageManager.Param.curLanguage;

            //CurrentLanguage = LanguageItems.FirstOrDefault(l => l.IsActive) ?? LanguageItems.FirstOrDefault();
        }

        /// <summary>
        /// 应用语言
        /// </summary>
        private void ApplyLanguage(LanguageItem language)
        {
            if (language == null) return;

            try
            {
                // 取消所有语言的激活状态
                foreach (var item in LanguageItems)
                {
                    item.IsActive = false;
                }

                // 激活选中的语言
                language.IsActive = true;

                // TODO: 在这里添加实际的语言切换逻辑
                // 例如：加载对应的资源字典、更新配置文件等
                // ConfigCenter.FrameConfig.SetPara("SystemLanguage", language.LanguageCode);
                // 从文件加载语言包
                Enum.TryParse<LanguageType>(CurrentLanguage.LanguageCode, out var LanguageType);
                ServiceProvider.LanguageManager.Set(CurrentLanguage);
                MessageBox.Show($"语言已切换为：{language.DisplayName}\n\n部分界面需要重启应用后生效。",
                    "语言切换", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Debug.WriteLine($"应用语言: {language.DisplayName} ({language.LanguageCode})");
                PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Publish();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换语言时出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置为默认语言
        /// </summary>
        private void ResetToDefaultLanguage()
        {
            var defaultLanguage = LanguageItems.FirstOrDefault(l => l.LanguageCode == "CN");
            if (defaultLanguage != null)
            {
                CurrentLanguage = defaultLanguage;
                ApplyLanguage(defaultLanguage);
            }
        }

        /// <summary>
        /// 刷新语言列表
        /// </summary>
        private void RefreshLanguages()
        {
            var currentCode = CurrentLanguage?.LanguageCode;
            InitializeLanguages();

            // 恢复之前选中的语言
            if (!string.IsNullOrEmpty(currentCode))
            {
                CurrentLanguage = LanguageItems.FirstOrDefault(l => l.LanguageCode == currentCode) ?? LanguageItems.FirstOrDefault();
            }

            MessageBox.Show("语言列表已刷新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 选择语言包文件夹
        /// </summary>
        private void SelectLanguagePackFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "请选择包含语言包的文件夹",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExternalLanguagePackPath = dialog.SelectedPath;

                // 清空之前的扫描结果
                ScannedLanguagePacks.Clear();
                SelectedExternalPack = null;

                MessageBox.Show($"已选择文件夹：{ExternalLanguagePackPath}\n\n请点击“扫描语言包”按钮查找语言包文件。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 扫描外部语言包
        /// </summary>
        private void ScanExternalLanguagePacks()
        {
            if (string.IsNullOrEmpty(ExternalLanguagePackPath))
            {
                MessageBox.Show("请先选择语言包文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 扫描语言包
                var packs = ServiceProvider.LanguageManager.ScanLanguagePacks(ExternalLanguagePackPath);

                ScannedLanguagePacks.Clear();
                foreach (var pack in packs)
                {
                    ScannedLanguagePacks.Add(pack);
                }

                if (ScannedLanguagePacks.Count > 0)
                {
                    CurrentLanguage =  SelectedExternalPack = ScannedLanguagePacks.FirstOrDefault();
                    MessageBox.Show($"扫描完成！\n\n共发现 {ScannedLanguagePacks.Count} 个语言包。\n请从列表中选择要加载的语言包。",
                        "扫描结果", MessageBoxButton.OK, MessageBoxImage.Information);

                    HasScannedPacks = ScannedLanguagePacks != null && ScannedLanguagePacks.Count > 0;
                    if (!LanguageItems.Contains(SelectedExternalPack))
                        LanguageItems.Add(SelectedExternalPack);
                }
                else
                {
                    MessageBox.Show("未在该文件夹中找到有效的语言包文件（.xml格式）。\n\n请确保语言包文件格式正确。",
                        "扫描结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描语言包时出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载外部语言包
        /// </summary>
        private void LoadExternalLanguagePack()
        {
            if (SelectedExternalPack == null)
            {
                MessageBox.Show("请先选择要加载的语言包", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var items = ServiceProvider.LanguageManager.Param.LanguageItems;
                if (!items.Any(p => p.LanguageCode == SelectedExternalPack.LanguageCode))
                {
                    items.Add(SelectedExternalPack);
                }
                // 从文件加载语言包
                ServiceProvider.LanguageManager.LoadFromFile(SelectedExternalPack.FilePath);
                //CacheManager.Set(CacheKey.Language, SelectedExternalPack);
                MessageBox.Show($"成功加载语言包：{SelectedExternalPack.DisplayName} ({SelectedExternalPack.LanguageCode})\n\n" +
                    $"文件路径：{SelectedExternalPack.FilePath}\n\n" +
                    $"语言已切换，部分界面可能需要重启应用后生效。",
                    "加载成功", MessageBoxButton.OK, MessageBoxImage.Information);

                PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Publish();
                // 刷新语言列表
                //RefreshLanguages();
                CacheManager.Set(CacheKey.Language, ServiceProvider.LanguageManager.Param);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载语言包时出错：{ex.Message}\n\n请检查语言包文件格式是否正确。",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

}
