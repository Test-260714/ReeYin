using Custom.MFDJC.Models;
using HalconDotNet;
using Prism.Commands;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using System;
using System.IO;
using System.Threading.Tasks;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace Custom.MFDJC.ViewModels
{
    public class ElectroStaticChuckPreviewViewModel : DialogViewModelBase, IDisposable
    {
        public const string DefaultImageFolder = ElectroStaticChuckImagePipeline.DefaultImageFolder;

        private readonly ElectroStaticChuck_Algorithm _algorithm;
        private string _selectedFolder = DefaultImageFolder;
        private string _loadStatus = "等待加载图片";
        private bool _isLoading;
        private ImageResultsDisplay? _depthDisplayResult;
        private HObject? _planeImage;

        public ElectroStaticChuckPreviewViewModel()
        {
            _algorithm = PrismProvider.Container.Resolve(typeof(ElectroStaticChuck_Algorithm)) as ElectroStaticChuck_Algorithm
                         ?? new ElectroStaticChuck_Algorithm();
            LoadImagesCommand = new DelegateCommand(async () => await LoadImagesAsync(), () => !IsLoading);
        }

        public string SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                RaisePropertyChanged();
            }
        }

        public string LoadStatus
        {
            get => _loadStatus;
            set
            {
                _loadStatus = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNotLoading));
                LoadImagesCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotLoading => !IsLoading;

        public ImageResultsDisplay? DepthDisplayResult
        {
            get => _depthDisplayResult;
            private set
            {
                if (ReferenceEquals(_depthDisplayResult, value))
                {
                    return;
                }

                DisposeDisplayResult(_depthDisplayResult);
                _depthDisplayResult = value;
                RaisePropertyChanged();
            }
        }

        public HObject? PlaneImage
        {
            get => _planeImage;
            private set
            {
                if (ReferenceEquals(_planeImage, value))
                {
                    return;
                }

                _planeImage?.Dispose();
                _planeImage = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand LoadImagesCommand { get; }

        private async Task LoadImagesAsync()
        {
            if (IsLoading)
            {
                return;
            }

            string? folder = Directory.Exists(SelectedFolder) ? SelectedFolder : SelectImageFolder();
            if (string.IsNullOrWhiteSpace(folder))
            {
                LoadStatus = "未选择图片文件夹";
                return;
            }

            IsLoading = true;
            LoadStatus = "正在加载，算法处理中...";

            try
            {
                (string depthImagePath, string planeImagePath) = ElectroStaticChuckImagePipeline.ResolveImageFiles(folder);
                ElectroStaticChuckAlgorithmRunResult runResult = await Task.Run(() =>
                    ElectroStaticChuckImagePipeline.RunAlgorithm(_algorithm, planeImagePath, depthImagePath));

                using (runResult)
                {
                    ApplyRunResult(runResult);
                }

                SelectedFolder = folder;
                LoadStatus = $"算法处理完成：深度图 {Path.GetFileName(depthImagePath)} / 平面图 {Path.GetFileName(planeImagePath)}";
            }
            catch (Exception ex)
            {
                LoadStatus = $"加载失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static string? SelectImageFolder()
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = "请选择静电卡盘图片文件夹",
                ShowNewFolderButton = false
            };

            return dialog.ShowDialog() == FormsDialogResult.OK ? dialog.SelectedPath : null;
        }

        private void ApplyRunResult(ElectroStaticChuckAlgorithmRunResult runResult)
        {
            PlaneImage = runResult.PlaneDisplayHObject?.Clone();
            DepthDisplayResult = new ImageResultsDisplay
            {
                GrayImage = runResult.PlaneDisplayImage.Clone(),
                HeightImage = runResult.HeightDisplayImage.Clone()
            };
        }

        private static void DisposeDisplayResult(ImageResultsDisplay? displayResult)
        {
            if (displayResult == null)
            {
                return;
            }

            if (!ReferenceEquals(displayResult.GrayImage, displayResult.HeightImage))
            {
                displayResult.GrayImage?.Dispose();
            }

            displayResult.HeightImage?.Dispose();
        }

        public override void OnDialogClosed()
        {
            Dispose();
            base.OnDialogClosed();
        }

        public void Dispose()
        {
            PlaneImage = null;
            DepthDisplayResult = null;
        }
    }
}
