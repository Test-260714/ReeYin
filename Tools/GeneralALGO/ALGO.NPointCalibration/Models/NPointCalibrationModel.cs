using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ALGO.NPointCalibration.Models
{
    [Serializable]
    public class NPointCalibrationModel : ModelParamBase
    {
        #region Properties

        [JsonIgnore]
        private string _cameraId = null;
        /// <summary>
        /// 相机ID
        /// </summary>
        public string CameraId
        {
            get { return _cameraId; }
            set { _cameraId = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _calibFileOutputDir = null;
        /// <summary>
        /// 标定结果保存目录
        /// </summary>
        public string CalibFileOutputDir
        {
            get { return _calibFileOutputDir; }
            set { _calibFileOutputDir = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTriggerIndex = 0;
        /// <summary>
        /// 执行模式（0：标定，1：使用标定结果）
        /// </summary>
        public int SltTriggerIndex
        {
            get { return _sltTriggerIndex; }
            set { _sltTriggerIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PointSet _sourcePoints = null;
        /// <summary>
        /// 源坐标系点集
        /// </summary>
        public PointSet SourcePoints
        {
            get { return _sourcePoints; }
            set { _sourcePoints = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sourcePointSet = new TransmitParam();
        /// <summary>
        /// 输入源坐标系点集
        /// </summary>
        [InputParam]
        public TransmitParam SourcePointSet
        {
            get { return _sourcePointSet; }
            set
            {
                _sourcePointSet = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private PointSet _targetPoints = null;
        /// <summary>
        /// 目标坐标系点集
        /// </summary>
        public PointSet TargetPoints
        {
            get { return _targetPoints; }
            set { _targetPoints = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _targetPointSet = new TransmitParam();
        /// <summary>
        /// 输入目标坐标系点集
        /// </summary>
        [InputParam]
        public TransmitParam TargetPointSet
        {
            get { return _targetPointSet; }
            set
            {
                _targetPointSet = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private PointSet _outputPointSet = new PointSet();
        /// <summary>
        /// 已转换点集
        /// </summary>
        [OutputParam("OutputPointSet", "转换后的点集")]
        public PointSet OutputPointSet
        {
            get { return _outputPointSet; }
            set { SetProperty(ref _outputPointSet, value); }
        }

        [JsonIgnore]
        private ObservableCollection<NPointCalParam> _NPointParams = new ObservableCollection<NPointCalParam>();
        public ObservableCollection<NPointCalParam> NPointParams
        {
            get { return _NPointParams; }
            set { _NPointParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HTuple _homMat2D = new HTuple(new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 });
        /// <summary>
        /// 坐标转换用的坐标映射矩阵
        /// </summary>
        public HTuple HomMat2D
        {
            get { return _homMat2D; }
            set { _homMat2D = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<double> _outhomMat2D = null;
        /// <summary>
        /// 显示与输出用的坐标映射矩阵
        /// </summary>
        [OutputParam("OutHomMat2D", "坐标映射矩阵")]
        public ObservableCollection<double> OutHomMat2D
        {
            get { return _outhomMat2D; }
            set { _outhomMat2D = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        #endregion

        #region Constructor
        public NPointCalibrationModel()
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

                _sourcePointSet.Value = GetTransmitParam(InputParams, _sourcePointSet);
                if (_sourcePointSet?.Value is PointSet srcPts)
                    SourcePoints = srcPts;
                else
                    SourcePoints = null!;

                _targetPointSet.Value = GetTransmitParam(InputParams, _targetPointSet);
                if (_targetPointSet?.Value is PointSet tgtPts)
                    TargetPoints = tgtPts;
                else
                    TargetPoints = null!;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载N点标定参数异常：{ex.Message}");
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam())
                        return NodeStatus.Error;

                    switch (SltTriggerIndex)
                    {
                        // 标定流程
                        case 0:
                            return ExecuteCalibration();

                        // 使用标定
                        case 1:
                            return ExecuteTransform();

                        default:
                            return NodeStatus.Error;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"N点标定模块执行异常：{ex.Message}");
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：N点标定模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>标定流程</summary>
        private NodeStatus ExecuteCalibration()
        {
            if (CameraId == null)
            {
                Console.WriteLine($"{ModuleName}_{Serial}：请填写相机ID");
                return NodeStatus.Error;
            }

            // 未链接点集时使用原始代码的默认 4 点集（保持向后兼容行为）
            if (SourcePoints == null || TargetPoints == null)
            {
                double[] srcX = { 25, 25, 50, 50 };
                double[] srcY = { 150, 100, 100, 150 };
                double[] dstX = { 0, 100, 100, 0 };
                double[] dstY = { 0, 0, 50, 50 };
                SourcePoints = new PointSet(srcX, srcY);
                TargetPoints = new PointSet(dstX, dstY);
            }

            if (SourcePoints.Length != TargetPoints.Length)
                return NodeStatus.Error;

            List<double> LSourceX = new List<double>();
            List<double> LSourceY = new List<double>();
            List<double> LTargetX = new List<double>();
            List<double> LTargetY = new List<double>();
            // 用本地列表在工作线程上构建，完成后统一切换到 UI 线程赋给 ObservableCollection
            var localNPointParams = new List<NPointCalParam>();
            for (int i = 0; i < SourcePoints.Length; i++)
            {
                double sourceX = SourcePoints.Columns[i];
                double sourceY = SourcePoints.Rows[i];
                double targetX = TargetPoints.Columns[i];
                double targetY = TargetPoints.Rows[i];

                localNPointParams.Add(new NPointCalParam()
                {
                    ID = localNPointParams.Count + 1,
                    SourceX = sourceX,
                    SourceY = sourceY,
                    TargetX = targetX,
                    TargetY = targetY
                });
                LSourceX.Add(sourceX);
                LSourceY.Add(sourceY);
                LTargetX.Add(targetX);
                LTargetY.Add(targetY);
            }

            if (localNPointParams.Count < 3)
                return NodeStatus.Error;

            HTuple hvHomMat2D;
            HOperatorSet.VectorToHomMat2d(new HTuple(LSourceX.ToArray()), new HTuple(LSourceY.ToArray()),
                                          new HTuple(LTargetX.ToArray()), new HTuple(LTargetY.ToArray()), out hvHomMat2D);
            hvHomMat2D.Append(new HTuple(0.0, 0.0, 1.0));

            HomMat2D = new HTuple(hvHomMat2D);
            var homMatArr = HomMat2D.ToDArr();

            // 切换到 UI 线程更新 ObservableCollection，避免跨线程绑定异常
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(() =>
                {
                    NPointParams = new ObservableCollection<NPointCalParam>(localNPointParams);
                    OutHomMat2D = new ObservableCollection<double>(homMatArr);
                });
            else
            {
                NPointParams = new ObservableCollection<NPointCalParam>(localNPointParams);
                OutHomMat2D = new ObservableCollection<double>(homMatArr);
            }

            RefreshOutputParams();
            return NodeStatus.Success;
        }

        /// <summary>使用标定矩阵转换点集</summary>
        private NodeStatus ExecuteTransform()
        {
            if (SourcePoints != null && SourcePoints.Length > 0)
            {
                int pointNum = SourcePoints.Length;
                double[] X = new double[pointNum];
                double[] Y = new double[pointNum];
                var localNPointParams = new List<NPointCalParam>();

                for (int i = 0; i < SourcePoints.Length; i++)
                {
                    double sourceX = SourcePoints.Columns[i];
                    double sourceY = SourcePoints.Rows[i];
                    HOperatorSet.AffineTransPoint2d(HomMat2D, new HTuple(sourceX), new HTuple(sourceY), out HTuple targetX, out HTuple targetY);

                    localNPointParams.Add(new NPointCalParam()
                    {
                        ID = localNPointParams.Count + 1,
                        SourceX = sourceX,
                        SourceY = sourceY,
                        TargetX = targetX.D,
                        TargetY = targetY.D
                    });

                    X[i] = targetX.D;
                    Y[i] = targetY.D;
                }
                OutputPointSet = new PointSet(X, Y);

                // 切换到 UI 线程更新 ObservableCollection，避免跨线程绑定异常
                var dispatcher = PrismProvider.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                    dispatcher.Invoke(() => NPointParams = new ObservableCollection<NPointCalParam>(localNPointParams));
                else
                    NPointParams = new ObservableCollection<NPointCalParam>(localNPointParams);
            }
            else
            {
                return NodeStatus.None;
            }

            RefreshOutputParams();
            return NodeStatus.Success;
        }

        /// <summary>刷新输出参数</summary>
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
                    item.Value = value;
            }

            if (!UpdateParam())
                Console.WriteLine($"N点标定模块_{Serial}更新参数失败");
        }

        #endregion
    }


    [Serializable]
    public class NPointCalParam : BindableBase
    {
        public int ID { get; set; }
        public double SourceX { get; set; }
        public double SourceY { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }
}
