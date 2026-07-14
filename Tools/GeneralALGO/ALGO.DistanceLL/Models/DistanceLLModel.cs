using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.DistanceLL
{
    [Serializable]
    public class DistanceLLModel : ModelParamBase
    {
        #region 字段
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();

        [JsonIgnore]
        private TransmitParam _inputLine1 = new TransmitParam();

        [JsonIgnore]
        private TransmitParam _inputLine2 = new TransmitParam();

        [JsonIgnore]
        private DistanceLLSourceLine? _line1;

        [JsonIgnore]
        private DistanceLLSourceLine? _line2;
        #endregion

        #region 属性
        /// <summary>可选输入图像，输入后在图像上叠加线段。</summary>
        [InputParam]
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        /// <summary>线段1输入。</summary>
        [InputParam]
        public TransmitParam InputLine1
        {
            get { return _inputLine1; }
            set
            {
                _inputLine1 = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        /// <summary>线段2输入。</summary>
        [InputParam]
        public TransmitParam InputLine2
        {
            get { return _inputLine2; }
            set
            {
                _inputLine2 = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        /// <summary>线段1计算坐标。</summary>
        [JsonIgnore]
        public DistanceLLSourceLine? Line1
        {
            get { return _line1; }
            private set { SetProperty(ref _line1, value); }
        }

        /// <summary>线段2计算坐标。</summary>
        [JsonIgnore]
        public DistanceLLSourceLine? Line2
        {
            get { return _line2; }
            private set { SetProperty(ref _line2, value); }
        }

        [JsonIgnore]
        private bool _showLine = true;
        /// <summary>是否显示线段。</summary>
        public bool ShowLine
        {
            get { return _showLine; }
            set
            {
                if (SetProperty(ref _showLine, value))
                {
                    RefreshPreviewDisplay();
                }
            }
        }

        private double? _distance;
        /// <summary>线段最短距离（没算出时为 null，表示无结果）。</summary>
        [OutputParam("Distance", "线线距离")]
        public double? Distance
        {
            get { return _distance; }
            set { SetProperty(ref _distance, value); }
        }

        [JsonIgnore]
        private HObject _previewImageObject;
        /// <summary>HALCON 预览图像。</summary>
        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get { return _previewImageObject; }
            private set { SetProperty(ref _previewImageObject, value); }
        }

        [JsonIgnore]
        private double _previewImageWidth = DistanceLLPreviewStyle.MinCanvasWidth;
        /// <summary>预览图像宽度。</summary>
        [JsonIgnore]
        public double PreviewImageWidth
        {
            get { return _previewImageWidth; }
            private set { SetProperty(ref _previewImageWidth, Math.Max(1.0, value)); }
        }

        [JsonIgnore]
        private double _previewImageHeight = DistanceLLPreviewStyle.MinCanvasHeight;
        /// <summary>预览图像高度。</summary>
        [JsonIgnore]
        public double PreviewImageHeight
        {
            get { return _previewImageHeight; }
            private set { SetProperty(ref _previewImageHeight, Math.Max(1.0, value)); }
        }

        /// <summary>HALCON 预览覆盖层。</summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new ObservableCollection<HalconDrawingObject>();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }
        #endregion

        #region 构造
        public DistanceLLModel()
        {
            TriggerModuleRun = () => ExecuteModule().Result;
            RefreshPreviewDisplay();
        }
        #endregion

        #region 生命周期
        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                    return true;

                if (!base.OnceInit())
                    return false;

                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            ClearPreviewDisplay();
            base.Dispose();
        }
        #endregion

        #region 主流程
        /// <summary>加载输入图像和线段并刷新预览。</summary>
        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam())
                    return false;

                _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
                _inputLine1.Value = GetTransmitParam(InputParams, _inputLine1);
                _inputLine2.Value = GetTransmitParam(InputParams, _inputLine2);
                Line1 = CreateInputLine(_inputLine1.Value);
                Line2 = CreateInputLine(_inputLine2.Value);
                RefreshPreviewDisplay();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载线线距离参数异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>执行线段最短距离计算。</summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam())
                        return NodeStatus.Error;

                    if (Line1 == null || Line2 == null)
                    {
                        Distance = null;
                        RefreshOutputParams();
                        return NodeStatus.None;
                    }

                    // 几何退化算不出距离：视为无结果(None)而非错误，输出 null
                    if (!DistanceLLGeometry.TryCalculateSegmentDistance(Line1.Value, Line2.Value, out double tmpDist))
                    {
                        Distance = null;
                        RefreshOutputParams();
                        return NodeStatus.None;
                    }

                    Distance = Math.Round(tmpDist, 4);
                    RefreshPreviewDisplay();
                    RefreshOutputParams();
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：线线距离模块执行时间：{time} 毫秒");
            return await Task.FromResult(Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            });
        }
        #endregion

        #region 距离计算
        /// <summary>线段最短距离。</summary>
        public bool DistanceLL(DistanceLLSourceLine lineA, DistanceLLSourceLine lineB, out double distance)
        {
            return DistanceLLGeometry.TryCalculateSegmentDistance(lineA, lineB, out distance);
        }

        /// <summary>兼容旧的 HALCON ROI 线段调用。</summary>
        public bool DistanceLL(ROILine lineA, ROILine lineB, out double distance)
        {
            distance = -1;
            if (lineA == null || lineB == null || !lineA.Status || !lineB.Status)
                return false;

            return DistanceLL(
                new DistanceLLSourceLine(lineA.StartX, lineA.StartY, lineA.EndX, lineA.EndY),
                new DistanceLLSourceLine(lineB.StartX, lineB.StartY, lineB.EndX, lineB.EndY),
                out distance);
        }
        #endregion

        #region 预览显示
        /// <summary>有图像时叠加原始线段坐标，无图像时生成 HALCON 空白图像画布。</summary>
        public void RefreshPreviewDisplay()
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(RefreshPreviewDisplay));
                return;
            }

            ClearPreviewDrawObjects();

            if (UpdatePreviewImageObject(_inputImage?.Value))
            {
                AddImageOverlayLines();
                return;
            }

            if (!ShowLine || !DistanceLLPreviewGeometry.TryCreateCanvas(Line1, Line2, out int width, out int height, out var lines))
            {
                SetPreviewImageObject(null);
                PreviewImageWidth = DistanceLLPreviewStyle.MinCanvasWidth;
                PreviewImageHeight = DistanceLLPreviewStyle.MinCanvasHeight;
                return;
            }

            HObject blankImage = null;
            try
            {
                HOperatorSet.GenImageConst(out blankImage, "byte", width, height);
                SetPreviewImageObject(blankImage);
                blankImage = null;
                PreviewImageWidth = width;
                PreviewImageHeight = height;

                foreach (DistanceLLPreviewLine line in lines)
                {
                    AddPreviewLine(line);
                }
            }
            finally
            {
                DisposeHObject(blankImage);
            }
        }

        private bool UpdatePreviewImageObject(object imageValue)
        {
            HImage image = CreatePreviewImage(imageValue);
            if (image == null || !image.IsInitialized())
                return false;

            try
            {
                image.GetImageSize(out int width, out int height);
                SetPreviewImageObject(image);
                image = null;
                PreviewImageWidth = width;
                PreviewImageHeight = height;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                DisposeHObject(image);
            }
        }

        private static HImage CreatePreviewImage(object imageValue)
        {
            try
            {
                switch (imageValue)
                {
                    case HImage inputImage:
                        return inputImage.CopyImage();
                    case HObject inputObject:
                        using (var tempImage = new HImage(inputObject))
                        {
                            return tempImage.CopyImage();
                        }
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private void AddImageOverlayLines()
        {
            if (!ShowLine)
                return;

            AddRawPreviewLine("线段1", Line1, "green");
            AddRawPreviewLine("线段2", Line2, "cyan");
        }

        private void AddRawPreviewLine(string name, DistanceLLSourceLine? line, string color)
        {
            if (!line.HasValue || !line.Value.IsValid())
                return;

            AddPreviewLine(new DistanceLLPreviewLine(name, line.Value.StartX, line.Value.StartY, line.Value.EndX, line.Value.EndY, color));
        }

        private void AddPreviewLine(DistanceLLPreviewLine line)
        {
            HObject lineContour = null;
            HObject startHandle = null;
            HObject endHandle = null;
            HObject lineWithStartHandle = null;
            HObject overlay = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out lineContour, new HTuple(line.StartY, line.EndY), new HTuple(line.StartX, line.EndX));
                HOperatorSet.GenCircleContourXld(out startHandle, line.StartY, line.StartX, DistanceLLPreviewStyle.HandleRadius, 0, Math.PI * 2.0, "positive", 1.0);
                HOperatorSet.GenCircleContourXld(out endHandle, line.EndY, line.EndX, DistanceLLPreviewStyle.HandleRadius, 0, Math.PI * 2.0, "positive", 1.0);
                lineWithStartHandle = lineContour.ConcatObj(startHandle);
                overlay = lineWithStartHandle.ConcatObj(endHandle);
                AddXldPreviewOverlay(overlay, line.Color);
            }
            catch
            {
            }
            finally
            {
                DisposeHObject(lineContour);
                DisposeHObject(startHandle);
                DisposeHObject(endHandle);
                DisposeHObject(lineWithStartHandle);
                DisposeHObject(overlay);
            }
        }

        private void AddXldPreviewOverlay(HObject contourObject, string color)
        {
            if (contourObject == null || !contourObject.IsInitialized())
                return;

            try
            {
                PreviewDrawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = contourObject.Clone(),
                    Color = color,
                    IsFillDisplay = false
                });
            }
            catch
            {
            }
        }

        private void ClearPreviewDisplay()
        {
            ClearPreviewDrawObjects();
            SetPreviewImageObject(null);
            PreviewImageWidth = DistanceLLPreviewStyle.MinCanvasWidth;
            PreviewImageHeight = DistanceLLPreviewStyle.MinCanvasHeight;
        }

        private void ClearPreviewDrawObjects()
        {
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects.ToList())
            {
                DisposeHObject(drawObject?.Hobject);
            }

            PreviewDrawObjects.Clear();
        }

        private void SetPreviewImageObject(HObject image)
        {
            HObject oldImage = _previewImageObject;
            PreviewImageObject = image;
            DisposeHObject(oldImage);
        }

        private static void DisposeHObject(HObject hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }
        #endregion

        #region 辅助方法
        private void RefreshOutputParams()
        {
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (!string.IsNullOrWhiteSpace(item.ParamName) && values.TryGetValue(item.ParamName, out object value))
                {
                    item.Value = value;
                }
            }

            if (!UpdateParam())
            {
                Console.WriteLine($"模块_{Serial}更新参数失败");
            }
        }

        private static DistanceLLSourceLine? CreateInputLine(object lineValue)
        {
            switch (lineValue)
            {
                case Line inputLine:
                    return new DistanceLLSourceLine(
                        inputLine.ColumnBegin,
                        inputLine.RowBegin,
                        inputLine.ColumnEnd,
                        inputLine.RowEnd);
                case CoordLine inputLine:
                    return new DistanceLLSourceLine(
                        inputLine.ColumnBegin,
                        inputLine.RowBegin,
                        inputLine.ColumnEnd,
                        inputLine.RowEnd);
                case ROILine roiLine when roiLine.Status:
                    return new DistanceLLSourceLine(
                        roiLine.StartX,
                        roiLine.StartY,
                        roiLine.EndX,
                        roiLine.EndY);
                case ROICoordLine roiLine when roiLine.Status:
                    return new DistanceLLSourceLine(
                        roiLine.StartX,
                        roiLine.StartY,
                        roiLine.EndX,
                        roiLine.EndY);
                default:
                    return null;
            }
        }
        #endregion
    }
}
