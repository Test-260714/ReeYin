using OpenCvSharp;
using Prism.Dialogs;
using Prism.Events;
using HalconDotNet;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DialogResult = System.Windows.Forms.DialogResult;

namespace ReeYin.ChartShow.ViewModels
{
    public class LightingChart3DViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        private static readonly FieldInfo? MemoryParaMapField =
            typeof(ProcessedData).GetField("_memoryParaMap", BindingFlags.Instance | BindingFlags.NonPublic);

        private SubscriptionToken? _sensorTransferToken;
        private SubscriptionToken? _outputResultToken;
        #endregion

        #region Properties
        private ImageResultsDisplay? _displayResult;

        public ImageResultsDisplay? DisplayResult
        {
            get => _displayResult;
            set
            {
                if (ReferenceEquals(_displayResult, value))
                {
                    return;
                }

                ImageResultsDisplay? previousResult = _displayResult;
                if (SetProperty(ref _displayResult, value))
                {
                    DisposeDisplayResult(previousResult);
                }
            }
        }
        #endregion

        public LightingChart3DViewModel()
        {
            SubscribeSensorTransferData();
            SubscribeOutputResultEvent();
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            if (Param != null)
            {
                ApplyDisplayResultFromParam(Param);
            }
        }

        public override void OnDialogClosed()
        {
            UnsubscribeSensorTransferData();
            UnsubscribeOutputResultEvent();
            DisplayResult = null;
            base.OnDialogClosed();
        }

        /// <summary>
        /// 订阅传感器处理结果，将收到的数据统一转换成页面绑定使用的显示对象。
        /// </summary>
        private void SubscribeSensorTransferData()
        {
            _sensorTransferToken = PrismProvider.EventAggregator
                .GetEvent<SensorTransferData>()
                .Subscribe(OnSensorTransferDataReceived, ThreadOption.BackgroundThread);
        }

        /// <summary>
        /// 兼容老的 3D 结果广播，避免业务侧尚未完全切到 SensorTransferData 时页面收不到结果。
        /// </summary>
        private void SubscribeOutputResultEvent()
        {
            _outputResultToken = PrismProvider.EventAggregator
                .GetEvent<OutputResultEvent>()
                .Subscribe(OnOutputResultReceived, ThreadOption.BackgroundThread);
        }

        /// <summary>
        /// 页面关闭时取消订阅，避免重复打开后出现多次刷新。
        /// </summary>
        private void UnsubscribeSensorTransferData()
        {
            if (_sensorTransferToken == null)
            {
                return;
            }

            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Unsubscribe(_sensorTransferToken);
            _sensorTransferToken = null;
        }

        private void UnsubscribeOutputResultEvent()
        {
            if (_outputResultToken == null)
            {
                return;
            }

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Unsubscribe(_outputResultToken);
            _outputResultToken = null;
        }

        /// <summary>
        /// 收到采集处理结果后提取深度图，并回写到 DisplayResult 触发页面刷新。
        /// </summary>
        private void OnSensorTransferDataReceived(ProcessedData processedData)
        {
            ImageResultsDisplay? displayResult = null;

            try
            {
                displayResult = BuildDisplayResult(processedData);
                if (displayResult == null)
                {
                    return;
                }

                ImageResultsDisplay targetDisplayResult = displayResult;
                PrismProvider.Dispatcher.BeginInvoke(new Action(() => DisplayResult = targetDisplayResult));
                displayResult = null;
            }
            catch (Exception ex)
            {
                DisposeDisplayResult(displayResult);
                Console.WriteLine($"LightingChart3DViewModel 接收 SensorTransferData 失败：{ex.Message}");
            }
        }

        private void OnOutputResultReceived((string, object) resultTuple)
        {
            if (!string.Equals(resultTuple.Item1, "LC3DShow", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (resultTuple.Item2 is not ImageResultsDisplay displayResult)
            {
                return;
            }

            ImageResultsDisplay clonedDisplayResult = CloneDisplayResult(displayResult);
            PrismProvider.Dispatcher.BeginInvoke(new Action(() => DisplayResult = clonedDisplayResult));
        }

        /// <summary>
        /// 将外部参数统一映射到 DisplayResult，保持页面初始加载和运行时刷新走同一条绑定链路。
        /// </summary>
        private void ApplyDisplayResultFromParam(object? param)
        {
            switch (param)
            {
                case null:
                    DisplayResult = null;
                    break;

                case TransmitParam transmitParam:
                    ApplyDisplayResultFromParam(transmitParam.Value);
                    break;

                case ImageResultsDisplay displayResult:
                    DisplayResult = CloneDisplayResult(displayResult);
                    break;

                case Mat heightImage:
                    DisplayResult = new ImageResultsDisplay
                    {
                        HeightImage = heightImage.Clone()
                    };
                    break;

                case ProcessedData processedData:
                    DisplayResult = BuildDisplayResult(processedData);
                    break;
            }
        }

        /// <summary>
        /// 生成 3D 控件需要的显示结果，优先提取高度图，灰度图作为附加信息保留。
        /// </summary>
        private static ImageResultsDisplay? BuildDisplayResult(ProcessedData? processedData)
        {
            if (processedData == null)
            {
                return null;
            }

            Mat? heightImage = TryExtractHeightImage(processedData);
            if (heightImage == null)
            {
                return null;
            }

            return new ImageResultsDisplay
            {
                GrayImage = CloneValidMat(processedData.Gray),
                HeightImage = heightImage
            };
        }

        /// <summary>
        /// 兼容不同业务结果结构，统一提取可用于 3D 显示的深度图。
        /// </summary>
        private static Mat? TryExtractHeightImage(ProcessedData processedData)
        {
            if (TryCloneMatProperty(processedData, "HeightImage", out Mat? directHeightImage))
            {
                return directHeightImage;
            }

            foreach (object value in EnumerateMemoryParaValues(processedData))
            {
                if (TryExtractHeightImageFromObject(value, out Mat? heightImage))
                {
                    return heightImage;
                }
            }

            return null;
        }

        /// <summary>
        /// 从缓存参数对象中提取高度图，支持 ImageResultsDisplay、HeightImage 和 DepthMap 几种常见格式。
        /// </summary>
        private static bool TryExtractHeightImageFromObject(object? source, out Mat? heightImage)
        {
            heightImage = null;
            if (source == null)
            {
                return false;
            }

            if (source is ImageResultsDisplay displayResult &&
                TryCloneMat(displayResult.HeightImage, out heightImage))
            {
                return true;
            }

            if (TryCloneMatProperty(source, "HeightImage", out heightImage))
            {
                return true;
            }

            if (TryCreateMatFromDepthMap(source, out heightImage))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 枚举 ProcessedData 的内部缓存参数，避免通用图表项目直接依赖具体业务结果类型。
        /// </summary>
        private static IEnumerable<object> EnumerateMemoryParaValues(ProcessedData processedData)
        {
            if (MemoryParaMapField?.GetValue(processedData) is not Dictionary<string, object> memoryParaMap)
            {
                return Enumerable.Empty<object>();
            }

            lock (processedData)
            {
                return memoryParaMap.Values
                    .Where(value => value != null)
                    .ToArray()!;
            }
        }

        /// <summary>
        /// 从对象属性中读取 HeightImage，支持 Mat 和 Halcon HObject 两种格式。
        /// </summary>
        private static bool TryCloneMatProperty(object source, string propertyName, out Mat? clonedMat)
        {
            clonedMat = null;

            PropertyInfo? property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return false;
            }

            object? propertyValue = property.GetValue(source);
            switch (propertyValue)
            {
                case Mat mat:
                    return TryCloneMat(mat, out clonedMat);

                case HObject hObject when hObject.IsInitialized():
                    using (Mat convertedMat = Common_Algorithm.HobjectToMat(hObject, ImageType.Depth))
                    {
                        return TryCloneMat(convertedMat, out clonedMat);
                    }
            }

            return false;
        }

        /// <summary>
        /// 当业务结果只提供 DepthMap 时，转换为 Mat 后继续沿用统一显示流程。
        /// </summary>
        private static bool TryCreateMatFromDepthMap(object source, out Mat? heightImage)
        {
            heightImage = null;

            PropertyInfo? property = source.GetType().GetProperty("DepthMap", BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return false;
            }

            object? propertyValue = property.GetValue(source);
            switch (propertyValue)
            {
                case float[][] depthMap when depthMap.Length > 0:
                    return TryCreateMatFromDepthRows(depthMap.ToList(), out heightImage);

                case List<float[]> depthRows when depthRows.Count > 0:
                    return TryCreateMatFromDepthRows(depthRows, out heightImage);
            }

            return false;
        }

        /// <summary>
        /// 将深度数组转换成 OpenCV Mat，返回的新对象由调用方负责释放。
        /// </summary>
        private static bool TryCreateMatFromDepthRows(List<float[]> depthRows, out Mat? heightImage)
        {
            heightImage = null;
            if (depthRows.Count == 0)
            {
                return false;
            }

            if (Common_Algorithm.ConvertListToMat(depthRows, ImageType.Depth, out Mat convertedMat) != 0 ||
                convertedMat.Empty())
            {
                convertedMat.Dispose();
                return false;
            }

            heightImage = convertedMat;
            return true;
        }

        /// <summary>
        /// 仅在 Mat 有效时克隆，避免页面持有无效图像引用。
        /// </summary>
        private static Mat? CloneValidMat(Mat? source)
        {
            return TryCloneMat(source, out Mat? clonedMat) ? clonedMat : null;
        }

        private static bool TryCloneMat(Mat? source, out Mat? clonedMat)
        {
            clonedMat = null;
            if (source == null || source.Empty())
            {
                return false;
            }

            clonedMat = source.Clone();
            return true;
        }

        private static void DisposeDisplayResult(ImageResultsDisplay? result)
        {
            result?.GrayImage?.Dispose();
            result?.HeightImage?.Dispose();
        }

        private static ImageResultsDisplay CloneDisplayResult(ImageResultsDisplay source)
        {
            return new ImageResultsDisplay
            {
                GrayImage = source.GrayImage?.Clone(),
                HeightImage = source.HeightImage?.Clone()
            };
        }

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "加载文件":
                    {
                        string FilePath;
                        using (var ofd = new OpenFileDialog())
                        {
                            ofd.Title = "请选择文件";
                            ofd.InitialDirectory = @"\";
                            ofd.Filter = "文件|*.*";
                            if (ofd.ShowDialog() != DialogResult.OK) return;
                            FilePath = ofd.FileName;
                        }

                        Mat img = Cv2.ImRead(FilePath, ImreadModes.Unchanged);
                        DisplayResult = new ImageResultsDisplay
                        {
                            HeightImage = img
                        };
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion
    }
}
