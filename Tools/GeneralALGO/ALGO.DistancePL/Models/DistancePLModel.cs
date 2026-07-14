using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.DistancePL
{
    [Serializable]
    public class DistancePLModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        #endregion

        #region Properties

        [JsonIgnore]
        private HImage image = null;
        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double pointX = 0;
        [JsonIgnore]
        public double PointX
        {
            get { return pointX; }
            set { pointX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double pointY = 0;
        [JsonIgnore]
        public double PointY
        {
            get { return pointY; }
            set { pointY = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private ROILine line = null;
        [JsonIgnore]
        public ROILine Line
        {
            get { return line; }
            set { line = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
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

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        private bool _showROI = true;
        /// <summary>
        /// 是否显示线段
        /// </summary>
        public bool ShowROI
        {
            get { return _showROI; }
            set
            {
                if (SetProperty(ref _showROI, value))
                    RefreshPreviewDisplay();
            }
        }

        private double _Distance;
        /// <summary>
        /// 距离
        /// </summary>
        [OutputParam("Distance", "点线距离")]
        public double Distance
        {
            get { return _Distance; }
            set { SetProperty(ref _Distance, value); }
        }

        [JsonIgnore]
        private TransmitParam _inputPointX = new TransmitParam();
        /// <summary>
        /// 点X 输入链接（接收上游输出的数值）
        /// </summary>
        [InputParam]
        public TransmitParam InputPointX
        {
            get { return _inputPointX; }
            set
            {
                _inputPointX = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private TransmitParam _inputPointY = new TransmitParam();
        /// <summary>
        /// 点Y 输入链接（接收上游输出的数值）
        /// </summary>
        [InputParam]
        public TransmitParam InputPointY
        {
            get { return _inputPointY; }
            set
            {
                _inputPointY = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private TransmitParam _inputLine = new TransmitParam();
        /// <summary>
        /// 直线1信息
        /// </summary>
        [InputParam]
        public TransmitParam InputLine
        {
            get { return _inputLine; }
            set
            {
                _inputLine = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        // 运行时拥有的输入图像副本
        [JsonIgnore]
        private HImage _ownedInputImage;

        [JsonIgnore]
        private HObject _previewImageObject;
        /// <summary>预览图像对象</summary>
        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get => _previewImageObject;
            private set { SetProperty(ref _previewImageObject, value); }
        }

        /// <summary>预览覆盖层绘制对象集合</summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new();

        #endregion

        #region Constructor
        public DistancePLModel()
        {
        }
        #endregion

        #region 生命周期
        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit) return true;
                if (!base.OnceInit()) return false;
                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch { return false; }
        }

        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam()) return false;

                _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
                ReplaceOwnedInputImage(_inputImage?.Value);

                _inputLine.Value = GetTransmitParam(InputParams, _inputLine);
                if (_inputLine?.Value is Line tmpLine)
                    Line = new ROILine(tmpLine.RowBegin, tmpLine.ColumnBegin, tmpLine.RowEnd, tmpLine.ColumnEnd);
                else
                    Line = null;

                _inputPointX.Value = GetTransmitParam(InputParams, _inputPointX);
                _inputPointY.Value = GetTransmitParam(InputParams, _inputPointY);
                PointX = TryConvertToDouble(_inputPointX?.Value) ?? 0;
                PointY = TryConvertToDouble(_inputPointY?.Value) ?? 0;

                RefreshPreviewDisplay();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载点线距离参数异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>把链接值转换为 double；无法解析时返回 null（区分"值为0"与"非法/未链接输入"）</summary>
        private static double? TryConvertToDouble(object value)
        {
            if (value == null) return null;
            if (value is double d) return d;
            if (value is float f) return (double)f;
            if (value is int i) return (double)i;
            if (value is long l) return (double)l;
            if (value is string s && double.TryParse(s, out var sd)) return sd;
            try { return Convert.ToDouble(value); }
            catch { return null; }
        }

        public override void Dispose()
        {
            DisposeOwnedRuntimeObjects();
            ClearMHRoi();
            base.Dispose();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam())
                        return NodeStatus.Error;

                    ClearMHRoi();
                    if (Image == null || !Image.IsInitialized())
                        return NodeStatus.None;

                    if (TryConvertToDouble(_inputPointX?.Value) is not double ||
                        TryConvertToDouble(_inputPointY?.Value) is not double)
                        return NodeStatus.None;
                    if (Line == null)
                        return NodeStatus.None;

                    double tmpDist;
                    ImageTool.Halcon.Config.Point point = new ImageTool.Halcon.Config.Point(PointY, PointX);
                    bool status = DistancePL(point, Line, out tmpDist);
                    Distance = tmpDist;

                    if (!status)
                        return NodeStatus.Error;

                    if (ShowROI)
                    {
                        HObject hoCross, hoLine;
                        HOperatorSet.GenCrossContourXld(out hoCross, PointY, PointX, 6, new HTuple(45).TupleRad());
                        ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测点P1, "green", hoCross));
                        HOperatorSet.GenContourPolygonXld(out hoLine, new HTuple(line.StartY, line.EndY), new HTuple(line.StartX, line.EndX));
                        ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.测量直线1, "green", hoLine));
                    }

                    RefreshPreviewDisplay();
                    RefreshOutputParams();
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"点线距离模块执行异常：{ex.Message}");
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：点线距离模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }


        /// <summary>
        /// 点线距离
        /// </summary>
        public bool DistancePL(ImageTool.Halcon.Config.Point Point, ROILine line, out double distance)
        {
            try
            {
                distance = HMisc.DistancePl(Point.Row, Point.Column, line.StartY, line.StartX, line.EndY, line.EndX);

                return true;
            }
            catch
            {
                distance = -1;

                return false;
            }
        }


        public void InitImg()
        {
            ShowHRoi();
        }


        public void ShowHRoi()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(
                        mWindowH.hControl.HalconWindow,
                        roiText.size,
                        "false",
                        "false"
                    );
                    ShowTool.SetMsg(
                        mWindowH.hControl.HalconWindow,
                        roiText.text,
                        "image",
                        roiText.row,
                        roiText.col,
                        roiText.drawColor,
                        "false"
                    );
                }
                else
                {
                    mWindowH.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
                }
            }
        }


        public void ShowHRoi(HRoi ROI)
        {
            try
            {
                int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                if (ROI.fors == true)
                {
                    mHRoi.Add(ROI);
                    return;
                }
                if (index > -1)
                    mHRoi[index] = ROI;
                else
                    mHRoi.Add(ROI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion

        #region 输出参数刷新
        private void RefreshOutputParams()
        {
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (item.Resourece == ResoureceType.Inupt)
                    continue;

                var key = !string.IsNullOrWhiteSpace(item.ParamName)
                    ? item.ParamName
                    : item.Name;

                if (!string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out var value))
                {
                    // 克隆 HALCON 对象，避免暴露内部缓存给下游
                    if (value is HObject hObj && hObj.IsInitialized())
                        item.Value = hObj.Clone();
                    else
                        item.Value = value;
                }
            }

            if (!UpdateParam())
                Console.WriteLine($"点线距离模块_{Serial}更新参数失败");
        }
        #endregion

        #region 预览显示
        /// <summary>替换拥有的输入图像副本，兼容 HImage 和 HObject 输入</summary>
        private void ReplaceOwnedInputImage(object imageValue)
        {
            var oldOwned = _ownedInputImage;
            try
            {
                switch (imageValue)
                {
                    case HImage hImage when hImage.IsInitialized():
                        _ownedInputImage = hImage.CopyImage();
                        Image = _ownedInputImage;
                        break;
                    case HObject hObj when hObj.IsInitialized():
                        using (var tempImage = new HImage(hObj))
                        {
                            _ownedInputImage = tempImage.CopyImage();
                        }
                        Image = _ownedInputImage;
                        break;
                    default:
                        _ownedInputImage = null;
                        Image = null;
                        break;
                }
            }
            catch
            {
                _ownedInputImage = null;
                Image = null;
            }
            if (oldOwned != null && !ReferenceEquals(oldOwned, _ownedInputImage))
            {
                try { oldOwned.Dispose(); } catch { }
            }
        }

        /// <summary>清理 mHRoi 列表中的 HALCON 句柄</summary>
        private void ClearMHRoi()
        {
            foreach (var roi in mHRoi)
            {
                try { roi.hobject?.Dispose(); } catch { }
            }
            mHRoi.Clear();
        }

        /// <summary>刷新预览图像和覆盖层</summary>
        private void RefreshPreviewDisplay()
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(RefreshPreviewDisplay));
                return;
            }

            // 更新预览图像
            var oldPreview = _previewImageObject;
            if (_ownedInputImage != null && _ownedInputImage.IsInitialized())
                PreviewImageObject = _ownedInputImage.Clone();
            else
                PreviewImageObject = null;
            if (oldPreview != null && !ReferenceEquals(oldPreview, _previewImageObject))
            {
                try { oldPreview.Dispose(); } catch { }
            }

            // 更新覆盖层
            ClearPreviewDrawObjects();
            if (ShowROI && Line != null)
            {
                try
                {
                    HObject hoCross, hoLine;
                    HOperatorSet.GenCrossContourXld(out hoCross, PointY, PointX, 6, new HTuple(45).TupleRad());
                    PreviewDrawObjects.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = hoCross,
                        Color = "green"
                    });
                    HOperatorSet.GenContourPolygonXld(out hoLine,
                        new HTuple(Line.StartY, Line.EndY),
                        new HTuple(Line.StartX, Line.EndX));
                    PreviewDrawObjects.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = hoLine,
                        Color = "green"
                    });
                }
                catch { }
            }
        }

        /// <summary>清除预览覆盖层对象</summary>
        private void ClearPreviewDrawObjects()
        {
            foreach (var obj in PreviewDrawObjects)
            {
                try { obj.Hobject?.Dispose(); } catch { }
            }
            PreviewDrawObjects.Clear();
        }

        /// <summary>释放运行时拥有的 HALCON 对象</summary>
        private void DisposeOwnedRuntimeObjects()
        {
            ClearPreviewDrawObjects();
            if (_previewImageObject != null)
            {
                try { _previewImageObject.Dispose(); } catch { }
                _previewImageObject = null;
            }
            if (_ownedInputImage != null)
            {
                try { _ownedInputImage.Dispose(); } catch { }
                _ownedInputImage = null;
            }
        }
        #endregion
    }
}
