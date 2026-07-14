using HandyControl.Controls;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;
using ReeYin_V.Config.Models;
using ReeYin_V.Config.Services;
using ReeYin_V.Core;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ReeYin_V.Config.ViewModels
{
    public sealed class StyleConfigViewModel : DialogViewModelBase
    {
        private readonly StyleResourceService _styleResourceService = new StyleResourceService();
        private StyleThemeDocument _currentThemeDocument;
        private ObservableCollection<ThemeOption> _themes = new ObservableCollection<ThemeOption>();
        private ThemeOption _selectedTheme;
        private ObservableCollection<StyleResourceItem> _colors = new ObservableCollection<StyleResourceItem>();
        private ObservableCollection<StyleResourceItem> _fontSizes = new ObservableCollection<StyleResourceItem>();
        private string _themeDirectory = string.Empty;
        private string _currentThemeFile = string.Empty;
        private string _statusMessage = "就绪";
        private bool _hasChanges;

        public StyleConfigViewModel()
        {
            RefreshCommand = new DelegateCommand(RefreshThemes);
            OpenThemeFileCommand = new DelegateCommand(OpenThemeFile);
            ApplyCommand = new DelegateCommand(ApplyPreview);
            SaveCommand = new DelegateCommand(SaveTheme);
            ReloadCommand = new DelegateCommand(ReloadCurrentTheme);
            ResetDefaultColorsCommand = new DelegateCommand(ResetDefaultColors);

            RefreshThemes();
        }

        public ObservableCollection<ThemeOption> Themes
        {
            get => _themes;
            set => SetProperty(ref _themes, value);
        }

        public ThemeOption SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value) && value != null)
                {
                    LoadTheme(value.FilePath);
                }
            }
        }

        public ObservableCollection<StyleResourceItem> Colors
        {
            get => _colors;
            set
            {
                SetProperty(ref _colors, value);
                RaisePropertyChanged(nameof(ColorCount));
            }
        }

        public ObservableCollection<StyleResourceItem> FontSizes
        {
            get => _fontSizes;
            set
            {
                SetProperty(ref _fontSizes, value);
                RaisePropertyChanged(nameof(FontSizeCount));
            }
        }

        public string ThemeDirectory
        {
            get => _themeDirectory;
            set => SetProperty(ref _themeDirectory, value ?? string.Empty);
        }

        public string CurrentThemeFile
        {
            get => _currentThemeFile;
            set => SetProperty(ref _currentThemeFile, value ?? string.Empty);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        public int ColorCount => Colors.Count;

        public int FontSizeCount => FontSizes.Count;

        public DelegateCommand RefreshCommand { get; }

        public DelegateCommand OpenThemeFileCommand { get; }

        public DelegateCommand ApplyCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public DelegateCommand ReloadCommand { get; }

        public DelegateCommand ResetDefaultColorsCommand { get; }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "ReturnMainPage":
                case "返回主页面":
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                        PrismProvider.RegionManager.RequestNavigate("MainRegion", ViewNames.MainView);
                    });
                    break;
                case "刷新":
                    RefreshThemes();
                    break;
                case "打开文件":
                    OpenThemeFile();
                    break;
                case "重新加载":
                    ReloadCurrentTheme();
                    break;
                case "应用预览":
                    ApplyPreview();
                    break;
                case "保存":
                    SaveTheme();
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters());
                    break;
            }
        });

        private void RefreshThemes()
        {
            try
            {
                var foundDirectory = _styleResourceService.FindDefaultThemeDirectory();
                if (!string.IsNullOrWhiteSpace(foundDirectory))
                {
                    ThemeDirectory = foundDirectory;
                }

                if (string.IsNullOrWhiteSpace(ThemeDirectory) || !Directory.Exists(ThemeDirectory))
                {
                    Themes = new ObservableCollection<ThemeOption>();
                    CurrentThemeFile = string.Empty;
                    StatusMessage = "未找到 ReeYin_V.UI 主题目录，请手动打开主题文件。";
                    return;
                }

                DetachItemHandlers();

                var themes = _styleResourceService.GetAvailableThemes(ThemeDirectory);
                Themes = new ObservableCollection<ThemeOption>(themes);
                SelectedTheme = Themes.FirstOrDefault(item => item.Name == "Light") ?? Themes.FirstOrDefault();
                StatusMessage = $"已加载主题目录：{ThemeDirectory}";
            }
            catch (Exception ex)
            {
                Logs.LogError($"加载样式主题失败: {ex}");
                StatusMessage = $"加载失败: {ex.Message}";
            }
        }

        private void OpenThemeFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "WPF主题文件 (*.xaml)|*.xaml|所有文件 (*.*)|*.*",
                Title = "选择主题文件",
                InitialDirectory = Directory.Exists(ThemeDirectory) ? ThemeDirectory : Directory.GetCurrentDirectory()
            };

            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            var localFilePath = _styleResourceService.ImportThemeToLocal(dialog.FileName);
            ThemeDirectory = Path.GetDirectoryName(localFilePath) ?? ThemeDirectory;
            var option = Themes.FirstOrDefault(item => string.Equals(item.FilePath, localFilePath, StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                option = new ThemeOption(Path.GetFileNameWithoutExtension(localFilePath), localFilePath);
                Themes.Add(option);
            }

            SelectedTheme = option;
        }

        private void LoadTheme(string filePath)
        {
            try
            {
                DetachItemHandlers();

                _currentThemeDocument = _styleResourceService.LoadTheme(filePath);
                Colors = _currentThemeDocument.Colors;
                FontSizes = _currentThemeDocument.FontSizes;
                AttachItemHandlers();

                HasChanges = false;
                CurrentThemeFile = _currentThemeDocument.FilePath;
                StatusMessage = $"已加载 {Path.GetFileName(filePath)}，颜色 {ColorCount} 项，字号 {FontSizeCount} 项。";
            }
            catch (Exception ex)
            {
                Logs.LogError($"读取样式主题失败: {ex}");
                StatusMessage = $"读取失败: {ex.Message}";
            }
        }

        private void ReloadCurrentTheme()
        {
            if (_currentThemeDocument == null)
            {
                StatusMessage = "没有可重新加载的主题。";
                return;
            }

            LoadTheme(_currentThemeDocument.FilePath);
        }

        private void ApplyPreview()
        {
            if (_currentThemeDocument == null)
            {
                StatusMessage = "没有可应用的主题。";
                return;
            }

            if (!ValidateAll())
            {
                StatusMessage = "存在无效值，已停止应用。";
                return;
            }

            _styleResourceService.ApplyToApplication(_currentThemeDocument);
            StatusMessage = "已应用到当前运行界面，保存后才会写回主题文件。";
        }

        private void SaveTheme()
        {
            if (_currentThemeDocument == null)
            {
                StatusMessage = "没有可保存的主题。";
                return;
            }

            if (!ValidateAll())
            {
                StatusMessage = "存在无效值，已停止保存。";
                return;
            }

            try
            {
                _styleResourceService.SaveTheme(_currentThemeDocument);
                _styleResourceService.ApplyToApplication(_currentThemeDocument);
                HasChanges = false;
                StatusMessage = $"已保存 {_currentThemeDocument.FilePath}，部分颜色需要重启应用后才能生效。";
                MessageBox.Show(
                    $"样式配置已保存：{_currentThemeDocument.FilePath}\n\n部分颜色已应用到当前运行界面；部分由控件模板或启动时资源读取的颜色需要重启应用后才能生效。",
                    "样式配置",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存样式主题失败: {ex}");
                MessageBox.Show($"保存失败: {ex.Message}", "样式配置", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }

        private void ResetDefaultColors()
        {
            if (_currentThemeDocument == null)
            {
                StatusMessage = GetResourceText("StyleConfigNoThemeToReset", "没有可还原颜色的主题。");
                return;
            }

            var result = MessageBox.Show(
                GetResourceText("StyleConfigResetDefaultColorsConfirm", "确定要还原默认颜色吗？还原后需要点击“保存”才会写回主题文件。"),
                GetResourceText("StyleConfiguration", "样式配置"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                var changedCount = 0;
                DetachItemHandlers();
                try
                {
                    changedCount = _styleResourceService.ResetColorsToDefault(_currentThemeDocument);
                }
                finally
                {
                    AttachItemHandlers();
                }

                HasChanges = Colors.Concat(FontSizes).Any(item => item.IsChanged);
                if (changedCount == 0)
                {
                    StatusMessage = GetResourceText("StyleConfigNoDefaultColorsToRestore", "当前颜色已是默认值。");
                    return;
                }

                _styleResourceService.ApplyToApplication(_currentThemeDocument);
                StatusMessage = string.Format(
                    GetResourceText("StyleConfigDefaultColorsRestoredStatus", "已还原 {0} 个颜色，点击保存后写回主题文件。"),
                    changedCount);
            }
            catch (Exception ex)
            {
                Logs.LogError($"还原默认颜色失败: {ex}");
                MessageBox.Show(
                    string.Format(GetResourceText("StyleConfigResetDefaultColorsFailed", "还原默认颜色失败: {0}"), ex.Message),
                    GetResourceText("StyleConfiguration", "样式配置"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusMessage = string.Format(GetResourceText("StyleConfigResetDefaultColorsFailed", "还原默认颜色失败: {0}"), ex.Message);
            }
        }

        private bool ValidateAll()
        {
            var isValid = true;
            foreach (var item in Colors.Concat(FontSizes))
            {
                if (_styleResourceService.TryValidate(item, out var error))
                {
                    item.Error = string.Empty;
                }
                else
                {
                    item.Error = error;
                    isValid = false;
                }
            }

            return isValid;
        }

        private void AttachItemHandlers()
        {
            foreach (var item in Colors.Concat(FontSizes))
            {
                item.PropertyChanged += OnStyleResourceItemPropertyChanged;
            }
        }

        private void DetachItemHandlers()
        {
            foreach (var item in Colors.Concat(FontSizes))
            {
                item.PropertyChanged -= OnStyleResourceItemPropertyChanged;
            }
        }

        private void OnStyleResourceItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StyleResourceItem.Value))
            {
                HasChanges = true;
                if (sender is StyleResourceItem item)
                {
                    ApplyChangedItem(item);
                }
            }
        }

        private void ApplyChangedItem(StyleResourceItem item)
        {
            if (_currentThemeDocument == null)
            {
                return;
            }

            if (!_styleResourceService.TryValidate(item, out var error))
            {
                item.Error = error;
                StatusMessage = $"{item.Key}: {error}";
                return;
            }

            item.Error = string.Empty;
            _styleResourceService.ApplyToApplication(_currentThemeDocument);
            StatusMessage = $"已自动应用 {item.Key}，保存后写回主题文件。";
        }

        private static string GetResourceText(string key, string fallback)
        {
            if (Application.Current?.TryFindResource(key) is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return fallback;
        }
    }
}
