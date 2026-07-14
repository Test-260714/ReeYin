using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin.Hardware.Sensor.Truelight3D.API;
using ReeYin.Hardware.Sensor.Truelight3D.CustomUI.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.IOC;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.Truelight3D.CustomUI.ViewModels
{
    public class Truelight3DSensorViewModel : DialogViewModelBase
    {
        private static readonly string[] PointCloudViewerRequiredFiles =
        [
            "ALGO.VTKWrapperNative.dll",
            "ALGO.PCLCoreNative.dll",
            "ALGO.PCLAlgorithmsNative.dll",
        ];

        private Truelight3DSensor? _attachedSensor;
        private readonly DispatcherTimer _zPositionTimer = new() { Interval = System.TimeSpan.FromMilliseconds(300) };
        private CancellationTokenSource? _previewLoopCancellationTokenSource;
        private Task? _previewLoopTask;
        private CancellationTokenSource? _scanResultLoopCancellationTokenSource;
        private Task? _scanResultLoopTask;
        private volatile bool _scanResultPending;
        private bool _isScanTextureViewerOpen;

        private Truelight3DSensorModel _modelParam = new();
        public new Truelight3DSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public bool IsScanTextureViewerOpen
        {
            get { return _isScanTextureViewerOpen; }
            set
            {
                _isScanTextureViewerOpen = value;
                RaisePropertyChanged();
            }
        }

        public override void InitParam()
        {
            _zPositionTimer.Tick -= OnZPositionTimerTick;
            _zPositionTimer.Tick += OnZPositionTimerTick;

            if (Param is Truelight3DSensor sensor)
            {
                ModelParam.Sensor = sensor;
            }
            else
            {
                ModelParam.Sensor = new Truelight3DSensor();
            }

            AttachSensor(ModelParam.Sensor);
            ModelParam.UpdatePreviewImageFromSensor();
            ModelParam.UpdateScanTextureImageFromSensor();
            UpdateZPositionTimerState();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;
                case "Confirm":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;
                case "Connect":
                    if (ModelParam.Sensor.Init())
                    {
                        ModelParam.Sensor.ReadPreviewFrame();
                    }
                    break;
                case "Disconnect":
                    ModelParam.Sensor.Close();
                    IsScanTextureViewerOpen = false;
                    break;
                case "StartCollect":
                    if (ModelParam.Sensor.State == HardwareState.Running || _scanResultPending)
                    {
                        ModelParam.Sensor.LastApiMessage = "当前扫描结果尚未完成，请先等待完成或停止后回收结果。";
                        break;
                    }

                    float minScanStepUm = ModelParam.Sensor.ScanType == Truelight3DScanType.Confocal ? 0.01f : 0.1f;
                    if (ModelParam.Sensor.ScanStepUm < minScanStepUm)
                    {
                        string scanTypeName = ModelParam.Sensor.ScanType == Truelight3DScanType.Confocal ? "共焦" : "变焦";
                        ModelParam.Sensor.LastApiMessage =
                            $"步距必须大于等于 {minScanStepUm:F2}，当前为 {ModelParam.Sensor.ScanStepUm:F3} um，当前扫描方式为{scanTypeName}。";
                        break;
                    }

                    ModelParam.Sensor.StartCollect();
                    _scanResultPending = ModelParam.Sensor.State == HardwareState.Running;
                    if (_scanResultPending)
                    {
                        ModelParam.Sensor.LastApiMessage = "扫描已开始，实时预览会继续刷新，扫描结果返回后会自动更新。";
                    }
                    UpdateScanResultTimerState();
                    break;
                case "StopCollect":
                    ModelParam.Sensor.StopCollect();
                    _scanResultPending = true;
                    UpdateScanResultTimerState();
                    break;
                case "ApplyPreviewSettings":
                    ModelParam.Sensor.ReadPreviewFrame();
                    break;
                case "UseWhiteLightMode":
                    ModelParam.Sensor.UseWhiteLightMode = true;
                    break;
                case "UseSingleLightMode":
                    ModelParam.Sensor.UseWhiteLightMode = false;
                    break;
                case "ReadFrame":
                    ModelParam.Sensor.ReadPreviewFrame();
                    break;
                case "ShowPointCloudViewer":
                    if (_scanResultPending)
                    {
                        if (ModelParam.Sensor.TryReceiveScanResultOnce())
                        {
                            _scanResultPending = false;
                            UpdateScanResultTimerState();
                        }
                        else
                        {
                            ModelParam.Sensor.LastApiMessage = "扫描结果仍未返回，当前还不能显示点云，请稍后再试。";
                            break;
                        }
                    }

                    if (ModelParam.Sensor.TryExportLastPointCloudToTempFile(out string filePath, out string message))
                    {
                        ModelParam.Sensor.LastApiMessage = message;
                        if (!TryEnsurePointCloudViewerAvailable(out string dependencyMessage))
                        {
                            ModelParam.Sensor.LastApiMessage = $"{message}；但点云查看器依赖未就绪：{dependencyMessage}";
                            break;
                        }

                        try
                        {
                            ModelParam.Sensor.LastApiMessage = $"点云已导出，准备打开新点云查看器：{filePath}";
                            PrismProvider.DialogService.ShowDialog("PointCloudViewerView", new DialogParameters
                            {
                                { "Param", filePath },
                            }, _ =>
                            {
                                ModelParam.Sensor.LastApiMessage = $"点云查看器已关闭，导出文件仍保留在：{filePath}";
                            });
                        }
                        catch (Exception ex)
                        {
                            ModelParam.Sensor.LastApiMessage = $"{message}；打开点云窗口失败：{ex.Message}。如果刚补齐了 PointCloudViewerView 模块，请重启主程序后再试。";
                        }
                    }
                    else
                    {
                        ModelParam.Sensor.LastApiMessage = message;
                    }
                    break;
                case "SavePointCloudFile":
                    SavePointCloudFile();
                    break;
                case "ShowScanTexture2D":
                    if (_scanResultPending)
                    {
                        if (ModelParam.Sensor.TryReceiveScanResultOnce())
                        {
                            _scanResultPending = false;
                            UpdateScanResultTimerState();
                        }
                    }

                    ModelParam.UpdateScanTextureImageFromSensor();
                    if (!ModelParam.HasScanTextureImage)
                    {
                        ModelParam.Sensor.LastApiMessage = "当前扫描结果还没有可显示的纹理 2D 图，请先完成一次扫描。";
                        break;
                    }

                    IsScanTextureViewerOpen = true;
                    ModelParam.Sensor.LastApiMessage = "已打开扫描纹理 2D 图预览。";
                    break;
                case "SaveLatest2DImage":
                    SaveLatest2DImage();
                    break;
                case "HideScanTexture2D":
                    IsScanTextureViewerOpen = false;
                    break;
                case "UseScanStartMode":
                    if (ModelParam.Sensor.ScanType == Truelight3DScanType.Confocal)
                    {
                        ModelParam.Sensor.UseScanRange = true;
                        ModelParam.Sensor.LastApiMessage = "共焦扫描固定使用范围位置模式。";
                        break;
                    }

                    ModelParam.Sensor.UseScanRange = false;
                    break;
                case "UseScanRangeMode":
                    ModelParam.Sensor.UseScanRange = true;
                    break;
                case "MoveZPositive":
                    ModelParam.Sensor.MoveZPositive();
                    break;
                case "MoveZNegative":
                    ModelParam.Sensor.MoveZNegative();
                    break;
                case "MoveZRelative":
                    ModelParam.Sensor.MoveZRelative();
                    break;
                case "MoveZStepPositive":
                    ModelParam.Sensor.MoveZStepPositive();
                    break;
                case "MoveZStepNegative":
                    ModelParam.Sensor.MoveZStepNegative();
                    break;
                case "MoveZAbsolute":
                    ModelParam.Sensor.MoveZAbsolute();
                    break;
                case "MoveZHome":
                    ModelParam.Sensor.MoveZHome();
                    break;
                case "StopZ":
                    ModelParam.Sensor.StopZMotion();
                    break;
                case "ReadZPosition":
                    ModelParam.Sensor.RefreshZPosition();
                    break;
                case "UseCurrentZAsScanStart":
                    ModelParam.Sensor.ScanStartPositionMm = ModelParam.Sensor.ZAxisCurrentPositionMm;
                    ModelParam.Sensor.LastApiMessage = $"已将当前位置写入开始位置：{ModelParam.Sensor.ScanStartPositionMm:F3} mm。";
                    break;
                case "UseCurrentZAsScanEnd":
                    ModelParam.Sensor.ScanEndPositionMm = ModelParam.Sensor.ZAxisCurrentPositionMm;
                    ModelParam.Sensor.LastApiMessage = $"已将当前位置写入结束位置：{ModelParam.Sensor.ScanEndPositionMm:F3} mm。";
                    break;
                default:
                    break;
            }
        });

        private void AttachSensor(Truelight3DSensor sensor)
        {
            StopPreviewLoop();
            StopScanResultLoop();

            if (_attachedSensor != null)
            {
                _attachedSensor.PropertyChanged -= OnSensorPropertyChanged;
            }

            _attachedSensor = sensor;
            _attachedSensor.PropertyChanged += OnSensorPropertyChanged;
        }

        private void OnSensorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_zPositionTimer.Dispatcher.CheckAccess())
            {
                HandleSensorPropertyChanged(e.PropertyName);
                return;
            }

            _zPositionTimer.Dispatcher.BeginInvoke(new Action(() => HandleSensorPropertyChanged(e.PropertyName)));
        }

        private void HandleSensorPropertyChanged(string? propertyName)
        {
            if (propertyName == nameof(Truelight3DSensor.LastPreviewFrame))
            {
                ModelParam.UpdatePreviewImageFromSensor();
            }

            if (propertyName == nameof(Truelight3DSensor.LastScanTextureFrame))
            {
                ModelParam.UpdateScanTextureImageFromSensor();
            }

            if (propertyName == nameof(Truelight3DSensor.IsConnected))
            {
                UpdateZPositionTimerState();
                if (_attachedSensor?.IsConnected != true)
                {
                    IsScanTextureViewerOpen = false;
                    ModelParam.ClearScanTextureImage();
                }
            }

            if (propertyName == nameof(Truelight3DSensor.State) && _attachedSensor?.State != HardwareState.Running)
            {
                UpdateScanResultTimerState();
            }
        }

        public override void OnDialogClosed()
        {
            _zPositionTimer.Stop();
            StopPreviewLoop();
            StopScanResultLoop();
            base.OnDialogClosed();
        }

        private void OnZPositionTimerTick(object? sender, System.EventArgs e)
        {
            _attachedSensor?.RefreshZPositionSilently();
            _attachedSensor?.RefreshZSpeedSilently();
        }

        private void StartPreviewLoop()
        {
            if (_attachedSensor?.IsConnected != true)
            {
                StopPreviewLoop();
                return;
            }

            if (_previewLoopTask != null &&
                !_previewLoopTask.IsCompleted &&
                _previewLoopCancellationTokenSource is { IsCancellationRequested: false })
            {
                return;
            }

            StopPreviewLoop();
            CancellationTokenSource cancellationTokenSource = new();
            _previewLoopCancellationTokenSource = cancellationTokenSource;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            _previewLoopTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Truelight3DSensor? sensor = _attachedSensor;
                    if (sensor?.IsConnected != true)
                    {
                        break;
                    }

                    if (sensor.TryReadPreviewFrameDataOnce(out var frame) && frame != null)
                    {
                        RunOnUiThread(() =>
                        {
                            if (ReferenceEquals(_attachedSensor, sensor) && sensor.IsConnected)
                            {
                                sensor.ApplyPreviewFrameData(frame);
                            }
                        });
                    }

                    try
                    {
                        await Task.Delay(Math.Max(1, sensor.PreviewIntervalMs), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, cancellationToken);
        }

        private void StopPreviewLoop()
        {
            if (_previewLoopCancellationTokenSource == null)
            {
                return;
            }

            _previewLoopCancellationTokenSource.Cancel();
            _previewLoopCancellationTokenSource.Dispose();
            _previewLoopCancellationTokenSource = null;
            _previewLoopTask = null;
        }

        private void StartScanResultLoop()
        {
            if (_attachedSensor?.IsConnected != true || !_scanResultPending)
            {
                StopScanResultLoop();
                return;
            }

            if (_scanResultLoopTask != null &&
                !_scanResultLoopTask.IsCompleted &&
                _scanResultLoopCancellationTokenSource is { IsCancellationRequested: false })
            {
                return;
            }

            StopScanResultLoop();
            CancellationTokenSource cancellationTokenSource = new();
            _scanResultLoopCancellationTokenSource = cancellationTokenSource;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            _scanResultLoopTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Truelight3DSensor? sensor = _attachedSensor;
                    if (sensor?.IsConnected != true || !_scanResultPending)
                    {
                        break;
                    }

                    if (sensor.TryReadScanResultDataOnce(out var scanResult) && scanResult != null)
                    {
                        RunOnUiThread(() =>
                        {
                            if (!ReferenceEquals(_attachedSensor, sensor))
                            {
                                return;
                            }

                            sensor.ApplyScanResultData(scanResult);
                            _scanResultPending = false;
                            UpdateScanResultTimerState();
                        });
                        return;
                    }

                    try
                    {
                        await Task.Delay(Math.Max(1, sensor.ScanResultPollIntervalMs), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, cancellationToken);
        }

        private void StopScanResultLoop()
        {
            if (_scanResultLoopCancellationTokenSource == null)
            {
                return;
            }

            _scanResultLoopCancellationTokenSource.Cancel();
            _scanResultLoopCancellationTokenSource.Dispose();
            _scanResultLoopCancellationTokenSource = null;
            _scanResultLoopTask = null;
        }

        private void UpdateZPositionTimerState()
        {
            if (_attachedSensor?.IsConnected == true)
            {
                _zPositionTimer.Start();
                StartPreviewLoop();
                _attachedSensor.RefreshZPositionSilently();
                UpdateScanResultTimerState();
                return;
            }

            _zPositionTimer.Stop();
            StopPreviewLoop();
            _scanResultPending = false;
            StopScanResultLoop();
        }

        private void UpdateScanResultTimerState()
        {
            if (_attachedSensor?.IsConnected == true && _scanResultPending)
            {
                StartScanResultLoop();
                return;
            }

            StopScanResultLoop();
        }

        private void RunOnUiThread(Action action)
        {
            if (_zPositionTimer.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            _zPositionTimer.Dispatcher.BeginInvoke(action);
        }

        private void SavePointCloudFile()
        {
            if (_scanResultPending)
            {
                if (ModelParam.Sensor.TryReceiveScanResultOnce())
                {
                    _scanResultPending = false;
                    UpdateScanResultTimerState();
                }
                else
                {
                    ModelParam.Sensor.LastApiMessage = "扫描结果仍未返回，当前还不能保存点云，请稍后再试。";
                    return;
                }
            }

            var dialog = new SaveFileDialog
            {
                Filter = "XYZ 点云文件|*.xyz|所有文件|*.*",
                FileName = $"Truelight3D_{DateTime.Now:yyyyMMdd_HHmmss}.xyz",
                Title = "保存点云文件",
                AddExtension = true,
                DefaultExt = ".xyz",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ModelParam.Sensor.LastApiMessage = ModelParam.Sensor.TryExportLastPointCloudToFile(dialog.FileName, out string message)
                ? message
                : message;
        }

        private void SaveLatest2DImage()
        {
            if (_scanResultPending && ModelParam.Sensor.TryReceiveScanResultOnce())
            {
                _scanResultPending = false;
                UpdateScanResultTimerState();
            }

            ModelParam.UpdateScanTextureImageFromSensor();
            ModelParam.UpdatePreviewImageFromSensor();

            if (!TryGetLatest2DBitmap(out BitmapSource bitmap, out string imageName, out string suggestedFileName))
            {
                ModelParam.Sensor.LastApiMessage = "当前没有可保存的 2D 图，请先读取单帧或完成一次扫描。";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG 图像|*.png|JPEG 图像|*.jpg|BMP 图像|*.bmp",
                FileName = suggestedFileName,
                Title = $"保存{imageName}",
                AddExtension = true,
                DefaultExt = ".png",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (TrySaveBitmapToFile(bitmap, dialog.FileName, out string message))
            {
                ModelParam.Sensor.LastApiMessage = $"{imageName}已保存到：{dialog.FileName}";
                return;
            }

            ModelParam.Sensor.LastApiMessage = message;
        }

        private bool TryGetLatest2DBitmap(out BitmapSource bitmap, out string imageName, out string suggestedFileName)
        {
            if (ModelParam.ScanTextureImage is BitmapSource scanTextureBitmap)
            {
                bitmap = scanTextureBitmap;
                imageName = "扫描纹理 2D 图";
                suggestedFileName = $"Truelight3D_Texture2D_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                return true;
            }

            if (ModelParam.PreviewImage is BitmapSource previewBitmap)
            {
                bitmap = previewBitmap;
                imageName = "当前预览 2D 图";
                suggestedFileName = $"Truelight3D_Preview2D_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                return true;
            }

            bitmap = null!;
            imageName = string.Empty;
            suggestedFileName = string.Empty;
            return false;
        }

        private static bool TrySaveBitmapToFile(BitmapSource bitmap, string filePath, out string message)
        {
            message = string.Empty;

            try
            {
                string? folder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                BitmapEncoder encoder = CreateBitmapEncoder(filePath);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using FileStream stream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                encoder.Save(stream);
                return true;
            }
            catch (Exception ex)
            {
                message = $"保存 2D 图失败：{ex.Message}";
                return false;
            }
        }

        private static BitmapEncoder CreateBitmapEncoder(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;
            return extension switch
            {
                ".bmp" => new BmpBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                _ => new PngBitmapEncoder(),
            };
        }

        private static bool TryEnsurePointCloudViewerAvailable(out string message)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] missingFiles = PointCloudViewerRequiredFiles
                .Where(file => !File.Exists(Path.Combine(baseDirectory, file)))
                .ToArray();

            if (missingFiles.Length == 0)
            {
                message = string.Empty;
                return true;
            }

            message = $"缺少原生依赖 {string.Join(", ", missingFiles)}，当前运行目录：{baseDirectory}";
            return false;
        }
    }
}
