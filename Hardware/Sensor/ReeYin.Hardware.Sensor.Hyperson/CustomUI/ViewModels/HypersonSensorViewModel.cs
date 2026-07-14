using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Views.ViewXY;
using Dm.util;
using OpenCvSharp.Aruco;
using OpenCvSharp.Detail;
using ReeYin.Hardware.Sensor.Hyperson.API;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Models;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Views;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.Hyperson.CustomUI.ViewModels
{
    public class HypersonSensorViewModel : DialogViewModelBase
    {
        #region Fields
        /// <summary>
        /// 刷新图表定时器
        /// </summary>
        private DispatcherTimer _RefreshChartTimer;

        /// <summary>
        /// 单点视图(折线图)
        /// </summary>
        private LightningChart _singlePointChart = new LightningChart();

        /// <summary>
        /// 轮廓视图(散点图)
        /// </summary>
        private LightningChart _outlineChart = new LightningChart();

        public delegate void bshowInfoDelegate(string info);
        public delegate void exportCacheDataDelegate(UInt32 max_countour_count);

        /// <summary>
        /// 连接触发标志位
        /// </summary>
        //private bool capture_flag = true;
        private bool connect_flag = true;
        private bool setsignal_flag = true;
        private bool ExternalTrigger_flag = false;
        private bool outlines_flag = true;
        private bool RoiSet_flag = false;

        /// <summary>
        /// 单点视图索引发生改变
        /// </summary>
        bool _signalControlChange;

        bool[] Success_conver = new bool[8];
        LCF_Result_t lcf_result;
        public float[] intenSity;

        /// <summary>
        /// 存放回调函数接收数据
        /// </summary>
        int[] g_Height_Buf;
        short[] g_Gray_Buf;
        /// <summary>
        /// 回调函数数据索引
        /// </summary>
        int g_CallbackIndex = 0;
        int g_GetDataMethod = 0;
        /// <summary>
        /// 接受存放的分辨率
        /// </summary>
        int signal_len = 0;
        int xValue = 0;


        /// <summary>
        /// 数据回放全局变量
        /// </summary>
        private static int[] Distance = new int[ShareObjects.FRAMESIZE];
        public static object[] ReplayDataArgs = new object[5];

        //Bitmap Height_pic_bit;
        //Bitmap Gray_pic_bit;
        float y_interval = 2;
        int encoderDivision = 1;
        int AdjustRoiForFps_Mode = 0;

        private int _hdrindex = 0;
        #endregion

        #region Properties
        private Grid _singlePointChartGrid = new Grid();

        public Grid SinglePointChartGrid
        {
            get { return _singlePointChartGrid; }
            set { _singlePointChartGrid = value; RaisePropertyChanged(); }
        }

        private Grid _outlineChartGrid = new Grid();

        public Grid OutlineChartGrid
        {
            get { return _outlineChartGrid; }
            set { _outlineChartGrid = value; RaisePropertyChanged(); }
        }

        private HypersonSensorModel _modelParam = new HypersonSensorModel();

        public HypersonSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public HypersonSensorViewModel()
        {
            SinglePointChartGrid.Children.Add(_singlePointChart);
            OutlineChartGrid.Children.Add(_outlineChart);
        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化图表样式
        /// </summary>
        private void InitChartStyle()
        {
            _singlePointChart.ChartName = "PointLineSeries chart";
            _singlePointChart.ViewXY.LegendBoxes[0].Visible = false;
            _singlePointChart.ViewXY.XAxes[0].SetRange(0, 2048);
            _singlePointChart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
            _singlePointChart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;

            _outlineChart.ChartName = "PointLineSeries chart";
            _outlineChart.ViewXY.LegendBoxes[0].Visible = false;
            _outlineChart.ViewXY.XAxes[0].SetRange(0, 2048);
            _outlineChart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
            _outlineChart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            if (Param != null && (Param is HypersenSensor))
                ModelParam.HypersenSensor = Param as HypersenSensor ?? new HypersenSensor();
            else
                ModelParam.HypersenSensor = new HypersenSensor();

            InitChartStyle();

            InitRefreshChartTimer();

            Init();
        }

        private void Init()
        {
            y_interval = ModelParam.HypersenSensor.Config.YInterval;
            ModelParam.HypersenSensor.ContourConfig.SignalControlMaximum = 2048;
            ModelParam.HypersenSensor.ContourConfig.NUDSignalControlMaximum = 2048;
            ModelParam.HypersenSensor.ContourConfig.SignalControl = -1;
            ModelParam.HypersenSensor.ContourConfig.NUDSignalControl = -1;
            _signalControlChange = true;

            lcf_result.x_count = 0;
            lcf_result.y_count = 0;
            lcf_result.height = IntPtr.Zero;
            lcf_result.intensity = IntPtr.Zero;
            lcf_result.layer_number = 0;

            //hdr裁切上电默认关闭
            ModelParam.HypersenSensor.CmosConfig.IsHdrSatRemoveLen = false;

            //默认显示所有层
            ModelParam.HypersenSensor.ContourConfig.LayerIndex = (Layer)4;

            ModelParam.HypersenSensor.TriggerConfig.AdjustRoiForFpsIndex = 0;
            connect_flag = !ModelParam.HypersenSensor.IsConnected;

            if (!connect_flag)
            {
                //获取传感器所有参数
                ModelParam.HypersenSensor.GetSensorParam();

                //通过回调函数收集数据时，最多缓存4000条轮廓
                g_Height_Buf = new int[ModelParam.HypersenSensor.deviceSetting.max_resolution_X * 4000];
                g_Gray_Buf = new short[ModelParam.HypersenSensor.deviceSetting.max_resolution_X * 4000];

                ModelParam.HypersenSensor.Signal_Buf = new short[ModelParam.HypersenSensor.deviceSetting.max_resolution_Y];
            }
            GetCurMaxFrameRate();
        }

        private void InitRefreshChartTimer()
        {
            _RefreshChartTimer = new DispatcherTimer();
            _RefreshChartTimer.Interval = TimeSpan.FromMilliseconds(50);
            _RefreshChartTimer.Tick += (s, e) => RefreshChart();
            _RefreshChartTimer.Start();
        }

        /// <summary>
        /// 刷新LightningChart
        /// </summary>
        private void RefreshChart()
        {
            try
            {
                //判断设备是否连接或是否开始采集，没连接则跳出
                if (!LCFDevice.LCF_IsConnect(ModelParam.HypersenSensor.ControlerHandle) || !LCFDevice.LCF_IsStart(ModelParam.HypersenSensor.ControlerHandle))
                {
                    return;
                }

                lock (this)
                {
                    _singlePointChart.Dispatcher.Invoke(() =>
                    {
                        ////清空上次图表
                        //this.chart1.Series[0].Points.Clear();
                        //for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                        //{
                        //    this.chart2.Series[i].Points.Clear();
                        //}

                        //判断是否有图像信号数据传入
                        if (lcf_result.x_count != 0 && lcf_result.y_count != 0 && lcf_result.layer_number != 0 && lcf_result.height != IntPtr.Zero && lcf_result.intensity != IntPtr.Zero)
                        {
                            LCF_StatusTypeDef ret;
                            //获取当前X轴分辨率（X轴点数）

                            object temp;
                            ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_X_COUNT, out temp);
                            int x_count = (int)temp;

                            //获取当前光斑信号长度（单个点信号长度）
                            ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_SIGNAL_LEN, out temp);
                            ModelParam.HypersenSensor.signal_len = (int)temp;

                            if (ModelParam.HypersenSensor.Fx_Buf[0].Length != (int)x_count)
                            {
                                ModelParam.HypersenSensor.Fx_Buf = new float?[LCFDevice.MAX_DETECT_LAYER_NUMBER][];
                                for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                                {
                                    ModelParam.HypersenSensor.Fx_Buf[i] = new float?[(int)x_count];
                                }
                            }

                            //判断当前层数
                            if (lcf_result.layer_number == 1)
                            {
                                ModelParam.HypersenSensor.ContourConfig.LayerIndex = 0;
                                ModelParam.HypersenSensor.ContourConfig.IsLayer = false;
                            }
                            else
                            {
                                ModelParam.HypersenSensor.ContourConfig.IsLayer = true;
                            }

                            //初始化单点信号索引值
                            int signalIndex = 0;
                            //判断滑动条是否为两个顶端，如果不是，则显示单点信号
                            if (ModelParam.HypersenSensor.ContourConfig.NUDSignalControl != -1 && ModelParam.HypersenSensor.ContourConfig.NUDSignalControl != x_count)
                            {
                                signalIndex = (int)ModelParam.HypersenSensor.ContourConfig.NUDSignalControl;
                            }

                            //使能单个点信号波形输出，仅用于调试
                            var rtn = LCFDevice.LCF_SetSignalOutput(ModelParam.HypersenSensor.ControlerHandle, setsignal_flag, signalIndex);
                            if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return ;

                            //uint len;
                            ////获取单个点的信号波形，仅用于调试(len为单个信号长度)
                            //rtn = LCFDevice.LCF_GetLastSignal(ModelParam.HypersenSensor.ControlerHandle, ModelParam.HypersenSensor.Signal_Buf, out len);
                            //if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return ;

                            //根据X轴分辨率(X轴点数)匹配滑动条最大值
                            ModelParam.HypersenSensor.ContourConfig.SignalControlMaximum = x_count;

                            #region 拷贝数据
                            //把轮廓数据和信号数据从非托管内存拷贝出来
                            int x_cpoy_lenth = (int)(LCFDevice.MAX_DETECT_LAYER_NUMBER * x_count * lcf_result.y_count);
                            short[] Signal_All = new short[x_count];
                            int[] Fx_Buf_temp = new int[x_cpoy_lenth];

                            Marshal.Copy(lcf_result.height, Fx_Buf_temp, 0, x_cpoy_lenth);
                            Marshal.Copy(lcf_result.intensity, Signal_All, 0, x_count);
                            //判断轮廓数据是否有信号
                            for (uint i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                            {
                                for (uint j = 0; j < x_count; j++)
                                {
                                    if (i * x_count * lcf_result.y_count + j > x_cpoy_lenth)
                                    {
                                        //showInfo("数组长度不匹配，请重试\r\n");
                                        //CommonHelper.ShowMessageBox("数组长度不匹配，请重试", "Tips");

                                        return;
                                    }
                                    if (Fx_Buf_temp[i * x_count * lcf_result.y_count + j] != LCFDevice.INVALID_VALUE * 10)
                                    {
                                        ModelParam.HypersenSensor.Fx_Buf[i][j] = (float)(Fx_Buf_temp[i * x_count * lcf_result.y_count + j] / 10.0);
                                    }
                                    else
                                    {
                                        ModelParam.HypersenSensor.Fx_Buf[i][j] = null;
                                    }
                                }

                            }
                            #endregion

                            #region 绑定图像
                            _singlePointChart.BeginUpdate();
                            _outlineChart.BeginUpdate();

                            _outlineChart.ViewXY.FreeformPointLineSeries.Clear();
                            //将相机传出的信号绑定到对应的图像上  
                            if (ModelParam.HypersenSensor.ContourConfig.LayerIndex != (Layer)4)
                            {
                                int index = (int)ModelParam.HypersenSensor.ContourConfig.LayerIndex;
                                //this.chart2.Series[index].Points.DataBindY(Fx_Buf[index]);
                                SeriesPoint[] points = new SeriesPoint[ModelParam.HypersenSensor.Fx_Buf[index].Length];
                                for (int pointIndex = 0; pointIndex < ModelParam.HypersenSensor.Fx_Buf[index].Length; pointIndex++)
                                {
                                    if (ModelParam.HypersenSensor.Fx_Buf[index][pointIndex] != null)
                                    {
                                        points[pointIndex].X = pointIndex;
                                        points[pointIndex].Y = (double)(ModelParam.HypersenSensor.Fx_Buf[index][pointIndex] ?? 0);
                                    }
                                }

                                _outlineChart.ViewXY.YAxes[0].SetRange(points.Min(p => p.Y) - 20, points.Max(p => p.Y) + 20);

                                // Create series and set generated points.
                                FreeformPointLineSeries fpls = new FreeformPointLineSeries(_outlineChart.ViewXY, _outlineChart.ViewXY.XAxes[0], _outlineChart.ViewXY.YAxes[0]);
                                fpls.PointStyle.Shape = Shape.Circle;
                                fpls.PointsVisible = true;
                                fpls.PointsType = PointsType.Points;
                                fpls.LineVisible = false;
                                fpls.PointStyle.Width = 5;
                                fpls.PointStyle.Height = 5;
                                fpls.Points = points;
                                fpls.PointStyle.Color1 = System.Windows.Media.Colors.Yellow;
                                fpls.PointStyle.GradientFill = GradientFillPoint.Solid;
                                _outlineChart.ViewXY.FreeformPointLineSeries.Add(fpls);
                            }
                            else
                            {
                                for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                                {
                                    //this.chart2.Series[i].Points.DataBindY(Fx_Buf[i]);
                                    SeriesPoint[] points = new SeriesPoint[ModelParam.HypersenSensor.Fx_Buf[i].Length];
                                    for (int pointIndex = 0; pointIndex < ModelParam.HypersenSensor.Fx_Buf[i].Length; pointIndex++)
                                    {
                                        if (ModelParam.HypersenSensor.Fx_Buf[i][pointIndex] != null)
                                        {
                                            points[pointIndex].X = pointIndex;
                                            points[pointIndex].Y = (double)(ModelParam.HypersenSensor.Fx_Buf[i][pointIndex] ?? 0);
                                        }
                                    }

                                    _outlineChart.ViewXY.YAxes[0].SetRange(points.Min(p => p.Y) - 20, points.Max(p => p.Y) + 20);

                                    // Create series and set generated points.
                                    FreeformPointLineSeries fpls = new FreeformPointLineSeries(_outlineChart.ViewXY, _outlineChart.ViewXY.XAxes[0], _outlineChart.ViewXY.YAxes[0]);
                                    fpls.PointStyle.Shape = Shape.Circle;
                                    fpls.PointsVisible = true;
                                    fpls.PointsType = PointsType.Points;
                                    fpls.LineVisible = false;
                                    fpls.PointStyle.Width = 5;
                                    fpls.PointStyle.Height = 5;
                                    fpls.Points = points;
                                    fpls.PointStyle.Color1 = System.Windows.Media.Colors.Yellow;
                                    fpls.PointStyle.GradientFill = GradientFillPoint.Solid;
                                    _outlineChart.ViewXY.FreeformPointLineSeries.Add(fpls);
                                }
                            }

                            _singlePointChart.ViewXY.FreeformPointLineSeries.Clear();

                            if (ModelParam.HypersenSensor.ContourConfig.NUDSignalControl != -1 && ModelParam.HypersenSensor.ContourConfig.NUDSignalControl != x_count)
                            {
                                short[] Signal_Buf_Change = new short[ModelParam.HypersenSensor.signal_len];
                                Array.Copy(ModelParam.HypersenSensor.Signal_Buf, Signal_Buf_Change, ModelParam.HypersenSensor.signal_len);
                                //this.chart1.Series[0].Points.DataBindY(Signal_Buf_Change);
                                SeriesPoint[] points = new SeriesPoint[Signal_Buf_Change.Length];
                                for (int pointIndex = 0; pointIndex < Signal_Buf_Change.Length; pointIndex++)
                                {
                                    points[pointIndex].X = pointIndex;
                                    points[pointIndex].Y = Signal_Buf_Change[pointIndex];
                                }

                                if (_signalControlChange)
                                {
                                    _signalControlChange = false;
                                    _singlePointChart.ViewXY.XAxes[0].SetRange(0, points.Max(p => p.X) + 20);
                                }
                                _singlePointChart.ViewXY.YAxes[0].SetRange(points.Min(p => p.Y) - 20, points.Max(p => p.Y) + 20);

                                FreeformPointLineSeries fpls = new FreeformPointLineSeries(_singlePointChart.ViewXY, _singlePointChart.ViewXY.XAxes[0], _singlePointChart.ViewXY.YAxes[0]);
                                fpls.PointStyle.Shape = Shape.Circle;
                                fpls.PointsVisible = false;
                                fpls.PointsType = PointsType.Points;
                                fpls.LineVisible = true;
                                fpls.LineStyle.Color = Colors.Yellow;
                                fpls.LineStyle.Width = 2;
                                fpls.PointStyle.Width = 5;
                                fpls.PointStyle.Height = 5;
                                fpls.Points = points;
                                fpls.PointStyle.Color1 = System.Windows.Media.Colors.Yellow;
                                fpls.PointStyle.GradientFill = GradientFillPoint.Solid;
                                _singlePointChart.ViewXY.FreeformPointLineSeries.Add(fpls);
                            }
                            else
                            {
                                //this.chart1.Series[0].Points.DataBindY(Signal_All);
                                SeriesPoint[] points = new SeriesPoint[Signal_All.Length];
                                for (int pointIndex = 0; pointIndex < Signal_All.Length; pointIndex++)
                                {
                                    points[pointIndex].X = pointIndex;
                                    points[pointIndex].Y = Signal_All[pointIndex];
                                }
                                if (_signalControlChange)
                                {
                                    _signalControlChange = false;
                                    _singlePointChart.ViewXY.XAxes[0].SetRange(0, points.Max(p => p.X) + 20);
                                }
                                _singlePointChart.ViewXY.YAxes[0].SetRange(points.Min(p => p.Y) - 20, points.Max(p => p.Y) + 20);

                                FreeformPointLineSeries fpls = new FreeformPointLineSeries(_singlePointChart.ViewXY, _singlePointChart.ViewXY.XAxes[0], _singlePointChart.ViewXY.YAxes[0]);
                                fpls.PointStyle.Shape = Shape.Circle;
                                fpls.PointsVisible = false;
                                fpls.PointsType = PointsType.Points;
                                fpls.LineVisible = true;
                                fpls.LineStyle.Color = Colors.Yellow;
                                fpls.LineStyle.Width = 2;
                                fpls.PointStyle.Width = 5;
                                fpls.PointStyle.Height = 5;
                                fpls.Points = points;
                                fpls.PointStyle.Color1 = System.Windows.Media.Colors.Yellow;
                                fpls.PointStyle.GradientFill = GradientFillPoint.Solid;
                                _singlePointChart.ViewXY.FreeformPointLineSeries.Add(fpls);
                            }
                            #endregion

                            //如果不在连续触发模式，则返回
                            //if (!radioButton_Continues.Checked)
                            //    return;

                            #region 坐标选点及显示
                            //判断当前选取X坐标范围
                            if (xValue > 0 && xValue < x_count)
                            {
                                int index = (int)ModelParam.HypersenSensor.ContourConfig.LayerIndex;
                                ModelParam.HypersenSensor.ContourConfig.OutlinesX = "X:" + xValue.ToString();
                                ModelParam.HypersenSensor.ContourConfig.OutlinesL1 = "L1:0";
                                ModelParam.HypersenSensor.ContourConfig.OutlinesL2 = "L2:0";
                                ModelParam.HypersenSensor.ContourConfig.OutlinesL3 = "L3:0";
                                ModelParam.HypersenSensor.ContourConfig.OutlinesL4 = "L4:0";
                                if (ModelParam.HypersenSensor.ContourConfig.LayerIndex != (Layer)4 /*&& !this.chart2.Series[index].Points[xValue].IsEmpty*/&& _outlineChart.ViewXY.PointLineSeries[index].Points.Length <= xValue)
                                {
                                    //this.chart2.Series[index].Points[xValue].MarkerSize = 7;
                                    //this.chart2.Series[index].Points[xValue].MarkerColor = System.Drawing.Color.Red;
                                    //_outlineChart.ViewXY.PointLineSeries[index].Points[xValue].PointColor = Red;
                                    if (ModelParam.HypersenSensor.ContourConfig.LayerIndex == Layer.One)
                                        ModelParam.HypersenSensor.ContourConfig.OutlinesL1 = "L1:" + ModelParam.HypersenSensor.Fx_Buf[0][xValue].ToString();
                                    else if (ModelParam.HypersenSensor.ContourConfig.LayerIndex == Layer.Two)
                                        ModelParam.HypersenSensor.ContourConfig.OutlinesL2 = "L1:" + ModelParam.HypersenSensor.Fx_Buf[1][xValue].ToString();
                                    else if (ModelParam.HypersenSensor.ContourConfig.LayerIndex == Layer.Three)
                                        ModelParam.HypersenSensor.ContourConfig.OutlinesL3 = "L1:" + ModelParam.HypersenSensor.Fx_Buf[2][xValue].ToString();
                                    else if (ModelParam.HypersenSensor.ContourConfig.LayerIndex == Layer.Four)
                                        ModelParam.HypersenSensor.ContourConfig.OutlinesL4 = "L1:" + ModelParam.HypersenSensor.Fx_Buf[3][xValue].ToString();
                                }

                                if (ModelParam.HypersenSensor.ContourConfig.LayerIndex == Layer.All)
                                {
                                    ModelParam.HypersenSensor.ContourConfig.OutlinesL1 = "L1:" + ModelParam.HypersenSensor.Fx_Buf[0][xValue].ToString();
                                    ModelParam.HypersenSensor.ContourConfig.OutlinesL2 = "L2:" + ModelParam.HypersenSensor.Fx_Buf[1][xValue].ToString();
                                    ModelParam.HypersenSensor.ContourConfig.OutlinesL3 = "L3:" + ModelParam.HypersenSensor.Fx_Buf[2][xValue].ToString();
                                    ModelParam.HypersenSensor.ContourConfig.OutlinesL4 = "L4:" + ModelParam.HypersenSensor.Fx_Buf[3][xValue].ToString();
                                    //数据为空则跳过标记点
                                    for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                                    {
                                        //if (this.chart2.Series[i].Points[xValue].IsEmpty)
                                        //    continue;
                                        //this.chart2.Series[i].Points[xValue].MarkerSize = 7;
                                        //this.chart2.Series[i].Points[xValue].MarkerColor = System.Drawing.Color.Red;
                                    }
                                }
                            }
                            #endregion

                            _singlePointChart.EndUpdate();
                            _outlineChart.EndUpdate();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 获取当前ROI下最大帧率
        /// </summary>
        private void GetCurMaxFrameRate()
        {
            object max_farmerate;
            ModelParam.HypersenSensor.GetSingleParam("float", LCF_ParameterDefine.PARAM_MAX_FRAME_RATE, out max_farmerate);
            if (max_farmerate != null)
            {
                ModelParam.HypersenSensor.TriggerConfig.MaxFrameRate = $"Max:{max_farmerate}";
            }
        }

        /// <summary>
        /// 获取光强相关信息
        /// </summary>
        private void GetLightParam()
        {
            if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
            {
                var rtn = LCFDevice.LCF_GetDeviceSetting(ModelParam.HypersenSensor.ControlerHandle, out ModelParam.HypersenSensor.deviceSetting);
                if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                return rtn;
            })) return;

            ModelParam.HypersenSensor.CmosConfig.Intensity = (int)ModelParam.HypersenSensor.deviceSetting.lightIntensity * 10;
            ModelParam.HypersenSensor.CmosConfig.Intensity = (int)ModelParam.HypersenSensor.deviceSetting.lightIntensity;
            ModelParam.HypersenSensor.CmosConfig.HdrModelIndex = (HdrModel)(ModelParam.HypersenSensor.deviceSetting.HDR_Num - 1);
            //获取HDR设置
            float[] intenSity = new float[(_hdrindex + 1) * 8];
            intenSity = ModelParam.HypersenSensor.deviceSetting.HDR_LightIntensity;

            for (int j = 0; j < _hdrindex + 1; j++)
            {
                ModelParam.HypersenSensor.intenSity_value[j] = intenSity[_hdrindex * 8 + j].ToString();
            }
            ModelParam.HypersenSensor.setIntenSity();
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
                case "执行":

                    break;

                case "取消":
                    _RefreshChartTimer.Stop();
                    CloseDialog(ButtonResult.No);

                    break;

                case "确认":
                    Task.Run(() =>
                    {
                        ModelParam.HypersenSensor.SaveToSensor();
                    });

                    _RefreshChartTimer.Stop();
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.HypersenSensor },
                    });

                    break;

                default:
                    break;
            }

        });

        /// <summary>
        /// 传感器控制指令
        /// </summary>
        public DelegateCommand<string> SensorCtrlCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "查找好的亮度":
                    {
                        if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            int HdrEnable = ModelParam.HypersenSensor.CmosConfig.IsInputeHdrModel ? 1 : 0;
                            return LCFDevice.LCF_FindGoodLight(ModelParam.HypersenSensor.ControlerHandle, HdrEnable);
                        })) return;

                        //刷新光强相关参数
                        GetLightParam();
                    }
                    break;

                case "清除触发计数":
                    {
                        ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            return LCFDevice.LCF_ClearEncoderTriggerCount(ModelParam.HypersenSensor.ControlerHandle);
                        });
                        
                    }break;

                case "检测":
                    {

                    }break;

                case "设定":
                    {
                        if (RoiSet_flag)
                        {
                            uint frame_rate = (uint)ModelParam.HypersenSensor.TriggerConfig.DetectFrameRate;

                            ////加载等待动画
                            //
                            //ingHelper.ShowLoadingScreen();

                            //调整传感器量程(ROI)，适应用户指定的帧率
                            float ZAxisRange;

                            ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                            {
                                var rtn = LCFDevice.LCF_AdjustRoiForFps(ModelParam.HypersenSensor.ControlerHandle, (LCF_AdjustRoiFpsPara_t)AdjustRoiForFps_Mode, (int)frame_rate, out ZAxisRange);
                                if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                                Console.WriteLine("调整ROI成功，当前Z轴量程:" + ZAxisRange.ToString());

                                //刷新测量相关数据
                                ModelParam.HypersenSensor.GetMeasureParam();
                                object x_count;
                                //获取轮廓长度(X轴点数)
                                ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_X_COUNT, out x_count);

                                if(x_count != null)
                                {
                                    //刷新单点信号的数据显示和滑动条最大值
                                    ModelParam.HypersenSensor.ContourConfig.SignalControl = (int)x_count;
                                    ModelParam.HypersenSensor.ContourConfig.NUDSignalControl = (int)x_count;
                                }
                                return true;
                            });

                            ////关闭等待动画
                            //LoadingHelper.CloseForm();
                        }
                        else
                        {
                            uint framerate_temp = (uint)ModelParam.HypersenSensor.TriggerConfig.DetectFrameRate;
                            //获取当前ROI下最大帧率
                            object max_farmerate;

                            ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_MAX_FRAME_RATE, out max_farmerate);

                            if(max_farmerate == null) return;

                            ModelParam.HypersenSensor.TriggerConfig.MaxFrameRate = $"Max:{(int)max_farmerate}";

                            int framerate = 0;
                            //如果输入帧率大于当前最大帧率，则只传入当前支持的最大值
                            if ((int)max_farmerate - (int)framerate_temp > 0)
                            {
                                framerate = (int)framerate_temp;
                            }
                            else
                            {
                                framerate = (int)max_farmerate;
                            }

                            if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_FRAME_RATE, framerate))
                                return;

                            ModelParam.HypersenSensor.TriggerConfig.DetectFrameRate = framerate;
                            Console.WriteLine("采集帧率设置为:" + framerate.ToString());
                        }
                    }
                    break;

                case "清除缓存数据":
                    {
                        if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            return LCFDevice.LCF_ClearCacheData(ModelParam.HypersenSensor.ControlerHandle);
                        }))return;

                        //清除回调缓存索引
                        g_CallbackIndex = 0;
                        //清空轮廓缓存
                        Array.Clear(g_Gray_Buf, 0, g_Gray_Buf.Length);
                        Array.Clear(g_Height_Buf, 0, g_Height_Buf.Length);

                        if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            return LCFDevice.LCF_ClearEncoderTriggerCount(ModelParam.HypersenSensor.ControlerHandle);
                        })) return;

                    }break;

                case "生成图像":
                    {
                        object trigger_pass;

                        ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_IS_TRIGGER_PASS, out trigger_pass);

                        if ((int)trigger_pass == 1)
                        {
                            //MessageBoxButton mess = MessageBoxButton.OKCancel;
                            //MessageBoxResult d = MessageBox.Show("编码器触发过载，是否导出数据?", "", mess);
                            //if (d == MessageBoxResult.OK)
                            //{

                            //获取扫描数据三种方式: 
                            //1、扫描结束后通过LCF_ExportCacheData接口将扫描数据一次性从控制器导出来


                            //2、扫描过程中通过回调函数收集扫描数据
                            //注意: 1、回调函数里面不应该做耗时的操作，应该只做数据拷贝。
                            //      2、在扫描帧率比较高的情况下通过设置PARAM_COUNTOUR_LINE_THRESHOLD参数，调高轮廓回调阈值，减少回调的频率，保证数据能及时缓存不会丢失数据。
                            //      3、该参数仅用于降低回调的频率，不用于一次性导出缓存数据


                            //3、通过PARAM_CACHE_COUNTOUR_THRESHOLD设置Cache缓存轮廓的阈值，达到阈值后通过回调函数通知用户，用户通过LCF_ExportCacheData导出缓存数据，通过LCF_ClearCacheData清空缓存数据后重新开始计数
                            //注意: 1、该参数一般用在用户已知总共扫描轮廓个数的场景，达到设定轮廓数后通知用户

                            if (g_GetDataMethod == 0)
                            {
                                //直接从控制器导出所有扫描数据并刷新到界面
                                //ExportData();
                            }
                            else if (g_GetDataMethod == 1)
                            {
                                //将通过回调函数收集的扫描数据刷新到界面
                                //ExportCallbackData();
                            }
                        }
                        else
                        {
                            //获取扫描数据三种方式: 
                            //1、扫描结束后通过LCF_ExportCacheData接口将扫描数据一次性从控制器导出来


                            //2、扫描过程中通过回调函数收集扫描数据
                            //注意: 1、回调函数里面不应该做耗时的操作，应该只做数据拷贝。
                            //      2、在扫描帧率比较高的情况下通过设置PARAM_COUNTOUR_LINE_THRESHOLD参数，调高轮廓回调阈值，减少回调的频率，保证数据能及时缓存不会丢失数据。
                            //      3、该参数仅用于降低回调的频率，不用于一次性导出缓存数据


                            //3、通过PARAM_CACHE_COUNTOUR_THRESHOLD设置Cache缓存轮廓的阈值，达到阈值后通过回调函数通知用户，用户通过LCF_ExportCacheData导出缓存数据，通过LCF_ClearCacheData清空缓存数据后重新开始计数
                            //注意: 1、该参数一般用在用户已知总共扫描轮廓个数的场景，达到设定轮廓数后通知用户

                            if (g_GetDataMethod == 0)
                            {
                                //直接从控制器导出所有扫描数据并刷新到界面
                                //ExportData();
                            }
                            else if (g_GetDataMethod == 1)
                            {
                                //将通过回调函数收集的扫描数据刷新到界面
                                //ExportCallbackData();
                            }
                        }
                    }
                    break;

                case "保存图片":
                    {
                        ////灰度图和深度图无数据时跳出
                        //if (Gray_pic.Image == null || Gray_pic.Image.Size.Height == 0 || Gray_pic.Image.Width == 0)
                        //    return;
                        //if (Height_pic.Image == null || Height_pic.Image.Size.Height == 0 || Height_pic.Image.Width == 0)
                        //    return;

                        ////string fileName_Height = "\\深度图_" + DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".jpg";
                        ////string fileName_Gray = "\\灰度图_" + DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".jpg";

                        //string fileName_Height = "\\深度图_" + DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".bmp";
                        //string fileName_Gray = "\\灰度图_" + DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".bmp";

                        ////保存图片到指定文件夹
                        //FolderBrowserDialog dialog = new FolderBrowserDialog();
                        //if (dialog.ShowDialog() == DialogResult.OK)
                        //{
                        //    string path = dialog.SelectedPath;
                        //    Height_pic.Image.Save(path + fileName_Height);
                        //    Gray_pic.Image.Save(path + fileName_Gray);

                        //    //保存质量比较高的图片的方法
                        //    //System.Drawing.Imaging.Encoder myEncoder;
                        //    //EncoderParameter myEncoderParameter;
                        //    //EncoderParameters myEncoderParameters;
                        //    //ImageCodecInfo myImageCodecInfo;

                        //    //myEncoder = System.Drawing.Imaging.Encoder.Quality;
                        //    //myEncoderParameters = new EncoderParameters(1);
                        //    //myEncoderParameter = new EncoderParameter(myEncoder, 100L); //设置质量 数字越大质量越好，但是到了一定程度质量就不会增加了

                        //    //myEncoderParameters.Param[0] = myEncoderParameter;
                        //    //myImageCodecInfo = GetEncoderInfo("image/bmp");

                        //    //string path = dialog.SelectedPath;
                        //    //Height_pic_bit.Save(path + fileName_Height, myImageCodecInfo, myEncoderParameters);
                        //    //Gray_pic_bit.Save(path + fileName_Gray, myImageCodecInfo, myEncoderParameters); 
                        //}
                    }
                    break;

                default:
                    break;
            }

        });

        /// <summary>
        /// 设置参数变更指令
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "曝光时间":
                    {
                        Console.WriteLine("设置曝光时间为:" + ModelParam.HypersenSensor.CmosConfig.ExposureTime.ToString());
                        ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_EXPOSURE_TIME, ModelParam.HypersenSensor.CmosConfig.ExposureTime);
                    }break;
                case "轮廓显示":
                    {
                        _signalControlChange = true;
                    }
                    break;
                case "光强设定":
                    {
                        //显示当前的光照强度百分比
                        ModelParam.HypersenSensor.CmosConfig.IntensityPercentage = ((float)ModelParam.HypersenSensor.CmosConfig.Intensity).ToString("f1") + "%";
                        //将面板上光强的数据传入设备，其中滑动条分为1000份，对应0.0-100.0的光强值要除以10
                        float percent = (float)ModelParam.HypersenSensor.CmosConfig.Intensity;

                        ModelParam.HypersenSensor.SetSingleParam("float", LCF_ParameterDefine.PARAM_LIGHT_INTENSITY, percent);

                    }break;

                case "增益":
                    {
                        switch (ModelParam.HypersenSensor.CmosConfig.GainIndex)
                        {
                            case Gain.GAIN_1:
                                {
                                    ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_GAIN, (int)LCF_CameraGain_t.LCF_2K_Gain_1);
                                }break;
                            case Gain.GAIN_2:
                                {
                                    ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_GAIN, (int)LCF_CameraGain_t.LCF_2K_Gain_2);
                                }break;
                            case Gain.GAIN_4:
                                {
                                    ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_GAIN, (int)LCF_CameraGain_t.LCF_2K_Gain_4);
                                }break;
                            default:
                                break;
                        }
                    }
                    break;

                case "HDR模式":
                    {
                        //将下拉框的索引值保存
                        _hdrindex = (int)ModelParam.HypersenSensor.CmosConfig.HdrModelIndex;
                        ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_HDR, _hdrindex + 1);

                        LCF_StatusTypeDef ret;

                        //HDR模式为1时，轮廓数量回调阈值设置为32，其他HDR模式下设置回调阈值为1
                        if ((_hdrindex + 1) == 1)
                        {
                            //HDR模式为1时,关闭HDR裁切模式
                            ModelParam.HypersenSensor.CmosConfig.IsHdrSatRemoveLen = false;

                            ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_COUNTOUR_LINE_THRESHOLD, 32);
                        }
                        else
                        {
                            ModelParam.HypersenSensor.CmosConfig.IsHdrSatRemoveLen = true;

                            ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_COUNTOUR_LINE_THRESHOLD, 1);
                        }

                        //调整选择的帧数范围
                        for (uint j = 0; j < _hdrindex + 1; j++)
                        {
                            ModelParam.HypersenSensor.intenSity_able[j] = true;
                        }
                        if (_hdrindex == 0)
                            ModelParam.HypersenSensor.intenSity_able[0] = false;
                        for (uint j = 7; j > _hdrindex; j--)
                        {
                            ModelParam.HypersenSensor.intenSity_able[j] = false;
                            ModelParam.HypersenSensor.intenSity_value[j] = " ";
                        }

                        if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            var rtn = LCFDevice.LCF_GetDeviceSetting(ModelParam.HypersenSensor.ControlerHandle, out ModelParam.HypersenSensor.deviceSetting);
                            if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                            return rtn;
                        })) return;

                        //每次改变HDR模式，获取对应的每一帧的数据
                        float[] intenSity = new float[(_hdrindex + 1) * 8];
                        intenSity = ModelParam.HypersenSensor.deviceSetting.HDR_LightIntensity;

                        for (int j = 0; j < _hdrindex + 1; j++)
                        {
                            ModelParam.HypersenSensor.intenSity_value[j] = intenSity[_hdrindex * 8 + j].ToString();
                        }

                        //八个文本框的使能和显示
                        ModelParam.HypersenSensor.setIntenSity();
                    }
                    break;

                case "X点间隔":
                    {
                        //_sensorService.SendCommand("IntervalX", newValue);
                    }
                    break;
                case "HDR裁剪":
                    {
                        int hdr_sat_remove_len = Convert.ToInt32(ModelParam.HypersenSensor.CmosConfig.HdrSatRemoveLen);
                        ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_HDR_SAT_REMOVE_LEN, hdr_sat_remove_len);
                    }
                    break;
                case "HDR强度":
                    {
                        ModelParam.HypersenSensor.intenSity_value[0] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity1.ToString();
                        ModelParam.HypersenSensor.intenSity_value[1] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity2.ToString();
                        ModelParam.HypersenSensor.intenSity_value[2] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity3.ToString();
                        ModelParam.HypersenSensor.intenSity_value[3] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity4.ToString();
                        ModelParam.HypersenSensor.intenSity_value[4] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity5.ToString();
                        ModelParam.HypersenSensor.intenSity_value[5] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity6.ToString();
                        ModelParam.HypersenSensor.intenSity_value[6] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity7.ToString();
                        ModelParam.HypersenSensor.intenSity_value[7] = ModelParam.HypersenSensor.CmosConfig.HdrIntensity8.ToString();

                        float[] intenSity = new float[_hdrindex + 1];
                        //例如HDR模式为4，则_hdrindex + 1 的值为4，
                        for (int j = 0; j < _hdrindex + 1; j++)
                        {
                            //尝试将文本框中的值转换成float类型
                            Success_conver[j] = float.TryParse(ModelParam.HypersenSensor.intenSity_value[j], out intenSity[j]);
                            //判断转换后的值是否在0到100的范围     
                            if (!(Success_conver[j] && intenSity[j] >= 0 && intenSity[j] <= 100.0f))
                            {
                                Console.WriteLine("失败:HDR范围0~100");
                                return;
                            }
                        }

                        if (!ModelParam.HypersenSensor.ExecuteCustomCommand(() =>
                        {
                            var rtn = LCFDevice.LCF_SetHDRIntensityGroup(ModelParam.HypersenSensor.ControlerHandle, (LCF_HDRMode_t)_hdrindex + 1, intenSity);
                            if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                            return rtn;
                        })) return;
                    }
                    break;

                case "X量程":
                    {
                        if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_X_RANGE, (int)ModelParam.HypersenSensor.CmosConfig.XRangeIndex)) return;

                        //刷新测量参数
                        ModelParam.HypersenSensor.GetMeasureParam();
                    }
                    break;

                case "固定预分频值":
                    {
                        encoderDivision = ModelParam.HypersenSensor.TriggerConfig.NUDEncoderDivision;
                        if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_ENCODER_DIV, encoderDivision)) return;
                    }
                    break;
                case "1个脉冲间隔(um)":
                    {
                        y_interval = ModelParam.HypersenSensor.Config.YInterval;
                    }
                    break;

                case "Binning":
                    {
                        //if (value < 0)
                        //{
                        //    return;
                        //}

                        //LCF_StatusTypeDef ret;
                        //if (!LCFDevice.LCF_IsConnect(_controlerHandler))
                        //{
                        //    //showInfo("请先连接设备\r\n");
                        //    //CommonHelper.ShowMessageBox("请先连接设备", "Tips");

                        //    return;
                        //}
                        ////设置Binning模式
                        //ret = LCFDevice.LCF_SetIntParameter(_controlerHandler, LCF_ParameterDefine.PARAM_BINNING_MODE, (int)(LCF_BinningMode_t)value);
                        //if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        //{
                        //    //MessageBox.Show("失败! 错误码:" + ret.ToString());
                        //    //CommonHelper.ShowMessageBox("失败! 错误码:" + ret.ToString(), "Tips");

                        //    return;
                        //}
                        //else
                        //{
                        //    //刷新测量参数
                        //    GetMeasureParam();
                        //    //showInfo("Binning设置成功\r\n");
                        //    //CommonHelper.ShowMessageBox("Binning设置成功", "Tips");

                        //}
                    }
                    break;

                case "X降采样":
                    {
                        if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_X_SUBSAMPLE, ModelParam.HypersenSensor.CmosConfig.XSubsampleIndex))
                            return;

                        //刷新测量参数
                        ModelParam.HypersenSensor.GetMeasureParam();

                        object temp;
                        ModelParam.HypersenSensor.GetSingleParam("float", LCF_ParameterDefine.PARAM_X_INTERVAL, out temp);

                        if(temp != null)
                            Console.WriteLine("当前X点间隔为:" + temp.ToString());
                    }
                    break;

                case "Z降采样":
                    {
                        if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_Z_SUBSAMPLE, ModelParam.HypersenSensor.CmosConfig.ZSubsampleIndex))
                            return;

                        //刷新测量参数
                        ModelParam.HypersenSensor.GetMeasureParam();
                        object temp;
                        ModelParam.HypersenSensor.GetSingleParam("int", LCF_ParameterDefine.PARAM_Z_SUBSAMPLE, out temp);
                        if(temp != null)
                            Console.WriteLine("当前Z降采样为:" + temp.ToString());
                    }
                    break;

                case "输入模式":
                    {
                        switch (ModelParam.HypersenSensor.TriggerConfig.InputModelIndex)
                        {
                            case InputModel.两相一倍频:
                                {
                                    ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_ENCODER_INPUT_MODE, LCF_EncoderInputMode_t.LCF_ENCODER_MUT_2_INC_1);
                                } break;

                            case InputModel.两相两倍频:
                            {
                                ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_ENCODER_INPUT_MODE, LCF_EncoderInputMode_t.LCF_ENCODER_MUT_2_INC_2);
                            } break;

                            case InputModel.两相四倍频:
                            {
                                ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_ENCODER_INPUT_MODE, LCF_EncoderInputMode_t.LCF_ENCODER_MUT_2_INC_4);
                            } break;

                        }
                    }
                    break;

                case "触发模式":
                    {
                        switch (ModelParam.HypersenSensor.TriggerConfig.TriggerModelIndex)
                        {
                            case TriggerModel.连续触发:
                                {
                                    ModelParam.HypersenSensor.EnTrig_flag = false;

                                    if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_TRIGGER_MODE, LCF_TriggerMode_t.LCF_InternalTrigger))
                                        return;

                                    ModelParam.HypersenSensor.Encoder_enable();

                                    Console.WriteLine("触发模式设置成功");

                                    ModelParam.HypersenSensor.StartCollect();
                                }break;
                            case TriggerModel.编码器触发:
                                {
                                    ModelParam.HypersenSensor.EnTrig_flag = true;

                                    if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_TRIGGER_MODE, LCF_TriggerMode_t.LCF_EncoderTrigger))
                                        return;

                                    ModelParam.HypersenSensor.Encoder_enable();
                                    Console.WriteLine("触发模式设置成功");

                                    ModelParam.HypersenSensor.StopCollect();
                                }break;
                            case TriggerModel.外部触发:
                                {
                                    ModelParam.HypersenSensor.EnTrig_flag = false;
                                    ExternalTrigger_flag = true;
                                    if (!ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_TRIGGER_MODE, LCF_TriggerMode_t.LCF_ExternalTrigger))
                                        return;

                                    ModelParam.HypersenSensor.Encoder_enable();
                                    Console.WriteLine("触发模式设置成功");
                                }break;
                        }
                    }
                    break;

                case "帧率控制":
                    {
                        int frameRate_flag = Convert.ToInt32(ModelParam.HypersenSensor.TriggerConfig.IsFrameRateControl);
                        ////如果勾选，则开启帧率控制，不勾选默认采集帧率为最大值
                        ModelParam.HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_FRAME_RATE_CONTROL, frameRate_flag);
                        ModelParam.HypersenSensor.TriggerConfig.IsDetectFrameRate = ModelParam.HypersenSensor.TriggerConfig.IsFrameRateControl;

                    }
                    break;

                case "自适应ROI模式":
                    {
                        if (ModelParam.HypersenSensor.TriggerConfig.AdjustRoiForFpsIndex != 0)
                        {
                            AdjustRoiForFps_Mode = (int)(ModelParam.HypersenSensor.TriggerConfig.AdjustRoiForFpsIndex - 1);
                            RoiSet_flag = true;
                        }
                        else
                        {
                            RoiSet_flag = false;
                        }
                    }
                    break;

                case "层索引":
                    {
                        //if (!LCFDevice.LCF_IsConnect(_controlerHandler))
                        //    return;
                        //////判断是否开始采集，没开始采集则跳出
                        ////if (LCFDevice.LCF_IsStart(ControlerHandler))              
                        //_outlineChart.BeginUpdate();
                        ////清空上次图表
                        //foreach (PointLineSeries item in _outlineChart.ViewXY.PointLineSeries)
                        //{
                        //    item.Points = null;
                        //}

                        ////将相机传出的信号绑定到对应的图像上  
                        //if (LayerIndex != 4)
                        //{
                        //    int index = LayerIndex;
                        //    //this.chart2.Series[index].Points.DataBindY(Fx_Buf[index]);
                        //    SeriesPoint[] points = new SeriesPoint[Fx_Buf[index].Length];
                        //    for (int pointIndex = 0; pointIndex < Fx_Buf[index].Length; pointIndex++)
                        //    {
                        //        points[pointIndex].X = pointIndex;
                        //        points[pointIndex].Y = (double)Fx_Buf[index][pointIndex];
                        //    }
                        //    _outlineChart.ViewXY.PointLineSeries[index].Points = points;
                        //}
                        //else
                        //{
                        //    for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                        //    {
                        //        //this.chart2.Series[i].Points.DataBindY(Fx_Buf[i]);
                        //        SeriesPoint[] points = new SeriesPoint[Fx_Buf[i].Length];
                        //        for (int pointIndex = 0; pointIndex < Fx_Buf[i].Length; pointIndex++)
                        //        {
                        //            points[pointIndex].X = pointIndex;
                        //            points[pointIndex].Y = (double)Fx_Buf[i][pointIndex];
                        //        }
                        //        _outlineChart.ViewXY.PointLineSeries[i].Points = points;
                        //    }
                        //}
                        //_outlineChart.EndUpdate();
                    }
                    break;


                default:
                    break;
            }
        });

        #endregion

        #region Discard


        #endregion

    }
}
