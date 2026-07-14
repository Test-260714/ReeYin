using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ReeYin.Hardware.Sensor.Truelight3D.API;

namespace ReeYin.Hardware.Sensor.Truelight3D.CustomUI.Models
{
    public class Truelight3DSensorModel : BindableBase
    {
        [JsonIgnore]
        private Truelight3DSensor _sensor = new Truelight3DSensor();
        public Truelight3DSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ImageSource? _previewImage;
        public ImageSource? PreviewImage
        {
            get { return _previewImage; }
            set
            {
                _previewImage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasPreviewImage));
                RaisePropertyChanged(nameof(HasAny2DImage));
            }
        }

        [JsonIgnore]
        public bool HasPreviewImage => PreviewImage != null;

        [JsonIgnore]
        private ImageSource? _scanTextureImage;
        public ImageSource? ScanTextureImage
        {
            get { return _scanTextureImage; }
            set
            {
                _scanTextureImage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasScanTextureImage));
                RaisePropertyChanged(nameof(HasAny2DImage));
            }
        }

        [JsonIgnore]
        public bool HasScanTextureImage => ScanTextureImage != null;

        [JsonIgnore]
        public bool HasAny2DImage => HasScanTextureImage || HasPreviewImage;

        [JsonIgnore]
        private string _pageDescription = "当前页面已切到 TrueLight3D 正式 SDK 生命周期：初始化、连接、单帧预览、扫描启动和结果读取都从这里进入。";
        public string PageDescription
        {
            get { return _pageDescription; }
            set { _pageDescription = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<string> _reservedInterfaces =
        [
            "Initialize() / Connect() / Disconnect() / Shutdown()：对应 AMSDK 生命周期。",
            "ReadImage()：读取实时单帧预览图像。",
            "ConfigureScan() / StartScan() / StopScan()：对应正式扫描流程。",
            "ReadScanResult()：读取 DepthMap、Texture 和可选 PointCloud。",
            "SetParameter()：保留给 Z 轴和光源类即时控制。"
        ];
        public ObservableCollection<string> ReservedInterfaces
        {
            get { return _reservedInterfaces; }
            set { _reservedInterfaces = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<string> _debugSteps =
        [
            "1. 先点击“连接”，验证 AMSDK 初始化、实例获取和设备连接能否打通。",
            "2. 用“读取单帧”验证实时预览链路是否可用。",
            "3. 用“开始采集 / 停止采集”验证扫描参数下发和结果读取路径。"
        ];
        public ObservableCollection<string> DebugSteps
        {
            get { return _debugSteps; }
            set { _debugSteps = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<string> _nextMilestones =
        [
            "补齐扫描完成、掉线和图像采集回调的原生桥接。",
            "展开扫描参数和 Z 轴调试页，而不是只保留最小动作按钮。",
            "补充点云结果的调试显示和导出入口。",
            "继续对齐仓库里的正式参数保存与调试交互模式。"
        ];
        public ObservableCollection<string> NextMilestones
        {
            get { return _nextMilestones; }
            set { _nextMilestones = value; RaisePropertyChanged(); }
        }

        public void UpdatePreviewImageFromSensor()
        {
            PreviewImage = CreatePreviewImage(Sensor.LastPreviewFrame);
        }

        public void ClearPreviewImage()
        {
            PreviewImage = null;
        }

        public void UpdateScanTextureImageFromSensor()
        {
            ScanTextureImage = CreatePreviewImage(Sensor.LastScanTextureFrame);
        }

        public void ClearScanTextureImage()
        {
            ScanTextureImage = null;
        }

        private static ImageSource? CreatePreviewImage(Truelight3DFrame? frame)
        {
            if (frame == null ||
                frame.Width <= 0 ||
                frame.Height <= 0 ||
                frame.PixelData == null ||
                frame.PixelData.Length == 0)
            {
                return null;
            }

            PixelFormat pixelFormat;
            int bytesPerPixel;

            if (frame.Channel <= 1 || frame.Format == Truelight3DPixelFormat.Gray)
            {
                pixelFormat = PixelFormats.Gray8;
                bytesPerPixel = 1;
            }
            else if (frame.Channel == 3)
            {
                pixelFormat = frame.Format == Truelight3DPixelFormat.BGR
                    ? PixelFormats.Bgr24
                    : PixelFormats.Rgb24;
                bytesPerPixel = 3;
            }
            else
            {
                return null;
            }

            int expectedLength = checked(frame.Width * frame.Height * bytesPerPixel);
            if (frame.PixelData.Length < expectedLength)
            {
                return null;
            }

            BitmapSource bitmap = BitmapSource.Create(
                frame.Width,
                frame.Height,
                96,
                96,
                pixelFormat,
                null,
                frame.PixelData,
                frame.Width * bytesPerPixel);
            bitmap.Freeze();
            return bitmap;
        }
    }
}

