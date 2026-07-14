using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Custom.XYHD.Models;
using Custom.XYHD.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {

        [NonSerialized]
        private ImageSource _displayImage;
        /// <summary>原图显示</summary>
        public ImageSource DisplayImage
        {
            get => _displayImage;
            set
            {
                if (ReferenceEquals(_displayImage, value))
                    return;
                _displayImage = value;
                RaisePropertyChanged();
            }
        }

        [NonSerialized]
        private ImageSource _leftDisplayImage;
        /// <summary>左路子图（含缺陷框）</summary>
        public ImageSource LeftDisplayImage
        {
            get => _leftDisplayImage;
            set
            {
                if (ReferenceEquals(_leftDisplayImage, value))
                    return;
                _leftDisplayImage = value;
                RaisePropertyChanged();
            }
        }

        [NonSerialized]
        private ImageSource _rightDisplayImage;
        /// <summary>右路子图（含缺陷框）</summary>
        public ImageSource RightDisplayImage
        {
            get => _rightDisplayImage;
            set
            {
                if (ReferenceEquals(_rightDisplayImage, value))
                    return;
                _rightDisplayImage = value;
                RaisePropertyChanged();
            }
        }

        private string _lastResult = "-";
        public string LastResult
        {
            get => _lastResult;
            set { _lastResult = value; RaisePropertyChanged(); }
        }

        private int _consecutiveNgCount;
        public int ConsecutiveNGCount
        {
            get => _consecutiveNgCount;
            set
            {
                if (_consecutiveNgCount == value)
                    return;
                _consecutiveNgCount = value;
                RaisePropertyChanged();
            }
        }

        private int _maxConsecutiveNgCount;
        public int MaxConsecutiveNGCount
        {
            get => _maxConsecutiveNgCount;
            set
            {
                if (_maxConsecutiveNgCount == value)
                    return;
                _maxConsecutiveNgCount = value;
                RaisePropertyChanged();
            }
        }

        private string _statusText = "等待数据...";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText == value)
                    return;
                _statusText = value;
                RaisePropertyChanged();
            }
        }

        private string _streamState = "Idle";
        public string StreamState
        {
            get => _streamState;
            set
            {
                if (_streamState == value)
                    return;
                _streamState = value;
                RaisePropertyChanged();
            }
        }

        private string _streamStatusText = "等待首帧";
        public string StreamStatusText
        {
            get => _streamStatusText;
            set
            {
                if (_streamStatusText == value)
                    return;
                _streamStatusText = value;
                RaisePropertyChanged();
            }
        }

        private long _lastFrameId;
        public long LastFrameId
        {
            get => _lastFrameId;
            set
            {
                if (_lastFrameId == value)
                    return;
                _lastFrameId = value;
                RaisePropertyChanged();
            }
        }

        private int _frameCount;
        public int FrameCount
        {
            get => _frameCount;
            set
            {
                if (_frameCount == value)
                    return;
                _frameCount = value;
                RaisePropertyChanged();
            }
        }

        private double _frameIntervalMs = -1;
        public double FrameIntervalMs
        {
            get => _frameIntervalMs;
            set
            {
                if (Math.Abs(_frameIntervalMs - value) < 0.1)
                    return;
                _frameIntervalMs = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FrameIntervalText));
            }
        }

        public string FrameIntervalText => FrameIntervalMs < 0 ? "-" : $"{FrameIntervalMs:F1} ms";

        private string _lastFrameTimeText = "-";
        public string LastFrameTimeText
        {
            get => _lastFrameTimeText;
            set
            {
                if (_lastFrameTimeText == value)
                    return;
                _lastFrameTimeText = value;
                RaisePropertyChanged();
            }
        }

        private bool _showNewFrameBadge;
        public bool ShowNewFrameBadge
        {
            get => _showNewFrameBadge;
            set
            {
                if (_showNewFrameBadge == value)
                    return;
                _showNewFrameBadge = value;
                RaisePropertyChanged();
            }
        }

        [NonSerialized]
        private DateTime _lastFrameUtc = DateTime.MinValue;
        [NonSerialized]
        private DateTime _newFrameBadgeUntilUtc = DateTime.MinValue;
        [NonSerialized]
        private DispatcherTimer _frameWatchdogTimer;
        [NonSerialized]
        private int _lastDisplayedStreamAgeBucket = -1;
        [NonSerialized]
        private readonly object _pendingFrameLock = new object();
        [NonSerialized]
        private Dictionary<string, PendingFrameUpdate> _pendingFrames = new();
        [NonSerialized]
        private readonly object _frameIdTextLock = new object();
        [NonSerialized]
        private Dictionary<long, string> _frameIdTextCache = new();

        // ========== 路1 (第一个收到结果的 DL 节点) ==========
        private string _path1Header = "路1 (等待...)";
        public string Path1Header
        {
            get => _path1Header;
            set
            {
                if (_path1Header == value)
                    return;
                _path1Header = value;
                RaisePropertyChanged();
            }
        }

        private string _path1Result = "-";
        public string Path1Result
        {
            get => _path1Result;
            set
            {
                if (_path1Result == value)
                    return;
                _path1Result = value;
                RaisePropertyChanged();
            }
        }

        private int _path1DefectCount;
        public int Path1DefectCount
        {
            get => _path1DefectCount;
            set
            {
                if (_path1DefectCount == value)
                    return;
                _path1DefectCount = value;
                RaisePropertyChanged();
            }
        }

        private int _path1PieceCount;
        public int Path1PieceCount
        {
            get => _path1PieceCount;
            set
            {
                if (_path1PieceCount == value)
                    return;
                _path1PieceCount = value;
                RaisePropertyChanged();
            }
        }

        private int _path1NgPieceCount;
        public int Path1NgPieceCount
        {
            get => _path1NgPieceCount;
            set
            {
                if (_path1NgPieceCount == value)
                    return;
                _path1NgPieceCount = value;
                RaisePropertyChanged();
            }
        }

        [NonSerialized]
        private ObservableCollection<DefectDetailItem> _path1DefectDetails = new ObservableCollection<DefectDetailItem>();
        public ObservableCollection<DefectDetailItem> Path1DefectDetails
        {
            get => _path1DefectDetails ??= [];
            set { _path1DefectDetails = value; RaisePropertyChanged(); }
        }

        // ========== 路2 (第二个收到结果的 DL 节点) ==========
        private string _path2Header = "路2 (等待...)";
        public string Path2Header
        {
            get => _path2Header;
            set
            {
                if (_path2Header == value)
                    return;
                _path2Header = value;
                RaisePropertyChanged();
            }
        }

        private string _path2Result = "-";
        public string Path2Result
        {
            get => _path2Result;
            set
            {
                if (_path2Result == value)
                    return;
                _path2Result = value;
                RaisePropertyChanged();
            }
        }

        private int _path2DefectCount;
        public int Path2DefectCount
        {
            get => _path2DefectCount;
            set
            {
                if (_path2DefectCount == value)
                    return;
                _path2DefectCount = value;
                RaisePropertyChanged();
            }
        }

        private int _path2PieceCount;
        public int Path2PieceCount
        {
            get => _path2PieceCount;
            set
            {
                if (_path2PieceCount == value)
                    return;
                _path2PieceCount = value;
                RaisePropertyChanged();
            }
        }

        private int _path2NgPieceCount;
        public int Path2NgPieceCount
        {
            get => _path2NgPieceCount;
            set
            {
                if (_path2NgPieceCount == value)
                    return;
                _path2NgPieceCount = value;
                RaisePropertyChanged();
            }
        }

        [NonSerialized]
        private ObservableCollection<DefectDetailItem> _path2DefectDetails = new ObservableCollection<DefectDetailItem>();
        public ObservableCollection<DefectDetailItem> Path2DefectDetails
        {
            get => _path2DefectDetails ??= new ObservableCollection<DefectDetailItem>();
            set { _path2DefectDetails = value; RaisePropertyChanged(); }
        }

        // ========== 合并统计 ==========
        [NonSerialized]
        private ObservableCollection<DefectDetailItem> _defectDetails = new ObservableCollection<DefectDetailItem>();
        public ObservableCollection<DefectDetailItem> DefectDetails
        {
            get => _defectDetails ??= new ObservableCollection<DefectDetailItem>();
            set { _defectDetails = value; RaisePropertyChanged(); }
        }

        private int _lastDefectCount;
        public int LastDefectCount
        {
            get => _lastDefectCount;
            set
            {
                if (_lastDefectCount == value)
                    return;
                _lastDefectCount = value;
                RaisePropertyChanged();
            }
        }

        private int _lastFramePieceCount;
        public int LastFramePieceCount
        {
            get => _lastFramePieceCount;
            set
            {
                if (_lastFramePieceCount == value)
                    return;
                _lastFramePieceCount = value;
                RaisePropertyChanged();
            }
        }

        private int _lastFrameNgPieceCount;
        public int LastFrameNgPieceCount
        {
            get => _lastFrameNgPieceCount;
            set
            {
                if (_lastFrameNgPieceCount == value)
                    return;
                _lastFrameNgPieceCount = value;
                RaisePropertyChanged();
            }
        }

        private string _lastDefectSummary = "无缺陷";
        public string LastDefectSummary
        {
            get => _lastDefectSummary;
            set
            {
                if (_lastDefectSummary == value)
                    return;
                _lastDefectSummary = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<LogItem> LogItems => Model.LogItems;

        private int _selectedMainTabIndex;
        public int SelectedMainTabIndex
        {
            get => _selectedMainTabIndex;
            set
            {
                value = Math.Clamp(value, 0, 1);
                if (_selectedMainTabIndex == value)
                    return;

                _selectedMainTabIndex = value;
                RaisePropertyChanged();
            }
        }

        public string CurrentModeText => "同步模式";

        public string ImageSizeText
        {
            get
            {
                var model = Model;
                return model.ImageWidth > 0 && model.ImageHeight > 0
                    ? $"{model.ImageWidth} x {model.ImageHeight}"
                    : "-";
            }
        }

        public string InputPortSummary
        {
            get
            {
                try
                {
                    var model = _model;
                    if (model == null)
                        return "暂无上游连接";

                    return string.Join(" | ", new[]
                    {
                        $"原图:{DescribeSelectedInput(model.InputOriginalImage, model.InputOriginalImageName)}",
                        $"左图:{DescribeSelectedInput(model.LeftInputImage, model.LeftInputImageName)}",
                        $"左结果:{DescribeSelectedInput(model.LeftInputResults, model.LeftInputResultsName)}",
                        $"右图:{DescribeSelectedInput(model.RightInputImage, model.RightInputImageName)}",
                        $"右结果:{DescribeSelectedInput(model.RightInputResults, model.RightInputResultsName)}"
                    });
                }
                catch
                {
                    return "暂无上游连接";
                }
            }
        }

        private static string DescribeSelectedInput(TransmitParam param, string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(param?.Name))
                return param.Name;

            if (!string.IsNullOrWhiteSpace(param?.ParamName))
                return param.ParamName;

            if (!string.IsNullOrWhiteSpace(fallbackName))
                return fallbackName;

            return "未选择";
        }

        public string OutputPortSummary
        {
            get
            {
                try
                {
                    var names = Model?.OutputParams?
                        .Select(p => p?.Name ?? p?.ParamName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .ToList();

                    if (names == null || names.Count == 0)
                        return DetectionModel.DetectionOutputParamName;

                    return string.Join(" | ", names);
                }
                catch
                {
                    return DetectionModel.DetectionOutputParamName;
                }
            }
        }

        public string FieldOrientationSummary
        {
            get
            {
                var model = Model;
                return $"现场左路 <- 图像:{DescribeFieldImageSource(model, true)}, 结果:{DescribeFieldResultSource(model, true)} ({FormatMirrorText(model.LeftPathXMirror)})；现场右路 <- 图像:{DescribeFieldImageSource(model, false)}, 结果:{DescribeFieldResultSource(model, false)} ({FormatMirrorText(model.RightPathXMirror)})";
            }
        }

        public string LeftPreviewOrientationText
        {
            get
            {
                var model = Model;
                return $"现场左路 | 图源: {DescribeFieldImageSource(model, true)} | 结果: {DescribeFieldResultSource(model, true)} | {FormatMirrorText(model.LeftPathXMirror)}";
            }
        }

        public string RightPreviewOrientationText
        {
            get
            {
                var model = Model;
                return $"现场右路 | 图源: {DescribeFieldImageSource(model, false)} | 结果: {DescribeFieldResultSource(model, false)} | {FormatMirrorText(model.RightPathXMirror)}";
            }
        }

        private static string DescribeFieldImageSource(DetectionModel model, bool fieldLeft)
        {
            if (model == null)
                return "未选择";

            bool useRightInput = fieldLeft ? model.SwapLeftRightPaths : !model.SwapLeftRightPaths;
            return useRightInput
                ? DescribeSelectedInput(model.RightInputImage, model.RightInputImageName)
                : DescribeSelectedInput(model.LeftInputImage, model.LeftInputImageName);
        }

        private static string DescribeFieldResultSource(DetectionModel model, bool fieldLeft)
        {
            if (model == null)
                return "未选择";

            bool useRightInput = fieldLeft ? model.SwapLeftRightPaths : !model.SwapLeftRightPaths;
            return useRightInput
                ? DescribeSelectedInput(model.RightInputResults, model.RightInputResultsName)
                : DescribeSelectedInput(model.LeftInputResults, model.LeftInputResultsName);
        }

        private static string FormatMirrorText(bool enabled)
        {
            return enabled ? "X坐标镜像" : "X坐标不镜像";
        }
    }
}
