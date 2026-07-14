using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Series3D;
using Arction.Wpf.Charting.Views.View3D;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.Views
{
    /// <summary>
    /// SurfaceChart.xaml 的交互逻辑
    /// </summary>
    public partial class SurfaceChart : UserControl
    {
        #region Fields

        #endregion

        #region Constructor
        public SurfaceChart()
        {
            InitializeComponent();
            SetUpSurfaceChart();

            //订阅
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((pd) =>
            {
                try
                {
                    var result = pd.GetMemoryPara("KCJC0_MeasureResult", new KCJC0_MeasureResult());
                    var depthData = result.DepthMap;
                    if (depthData == null || depthData.Length < 2 || depthData[0] == null || depthData[0].Length < 2)
                    {
                        // 没有有效深度图时跳过3D显示，避免后台显示线程异常。
                        Console.WriteLine("SurfaceChart skipped: depth data is null or empty.");
                        return;
                    }
                    //view3d.Dimensions.Depth = result.ImageScaleW / result.ImageScaleH * 200;
                    LightningChart.View3D.Dimensions.Depth = (result.ImageScaleW * result.DepthMap[0].Length) / (result.ImageScaleH * result.DepthMap.Length) * 200;
                    //var depthData = ReadCsvTo2DArray(@"D:\\Work\\ReechiSystem\\ReeYin\\ReeYin\\bin\\Debug\\net8.0-windows\\TestImage\\1.csv");
                    ReplaceInvalidValues(depthData);
                    var _surfaceGround = CreateSurfaceGridSeries3D(LightningChart.View3D);
                    LightningChart.View3D.SurfaceGridSeries3D.Clear();
                    LightningChart.View3D.SurfaceGridSeries3D.Add(_surfaceGround);
                    _surfaceGround.SetSize(depthData.Length, depthData[0].Length);
                    var surfacePointArray = _surfaceGround.Data;
                    for (var i = 0; i < depthData.Length; i++)
                    {
                        for (var j = 0; j < depthData[i].Length; j++)
                        {
                            var x = LightningChart.View3D.XAxisPrimary3D.Minimum +
                                    (LightningChart.View3D.XAxisPrimary3D.Maximum - LightningChart.View3D.XAxisPrimary3D.Minimum) * i /
                                    (depthData.Length - 1);
                            var z = LightningChart.View3D.ZAxisPrimary3D.Minimum +
                                    (LightningChart.View3D.ZAxisPrimary3D.Maximum - LightningChart.View3D.ZAxisPrimary3D.Minimum) * j /
                                    (depthData[0].Length - 1);
                            var y = depthData[i][j];
                            surfacePointArray[i, j].X = x;
                            surfacePointArray[i, j].Y = y;
                            surfacePointArray[i, j].Z = z;
                            surfacePointArray[i, j].Value = depthData[i][j];
                        }
                    }


                    _surfaceGround.ContourPalette = CreateGradientPalette(_surfaceGround, depthData);
                    _surfaceGround.InvalidateData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.StackTrace}");
                }

            }, ThreadOption.BackgroundThread);

        }
        #endregion


        private void SetUpSurfaceChart()
        {
            LightningChart.BeginUpdate();

            LightningChart.Title.Text = "";
            LightningChart.ActiveView = ActiveView.View3D;
            LightningChart.View3D.Dimensions.Width = 200.0;
            //LightningChart.View3D.Dimensions.Height = ConfigCenter.FrameConfig.GetPara<double>("KCJC0_SurfaceChartSetting_DimensionsHeight", 10.0); ;
            LightningChart.View3D.Dimensions.Height = 10.0;
            LightningChart.View3D.Dimensions.Depth = 116;
            LightningChart.View3D.LegendBox.ShowCheckboxes = false;
            LightningChart.View3D.LegendBox.Visible = true;
            LightningChart.View3D.Camera.MinimumViewDistance = 10;
            LightningChart.View3D.Camera.ViewDistance = 180.0;
            LightningChart.View3D.Camera.Projection = ProjectionType.Orthographic;
            LightningChart.View3D.Camera.RotationX = 90;
            LightningChart.View3D.Camera.RotationY = 0;
            LightningChart.View3D.Camera.RotationZ = 90;
            LightningChart.View3D.ZoomPanOptions.DevicePrimaryButtonDoubleClickAction = DoubleClickAction3D.Off;
            LightningChart.View3D.ZAxisPrimary3D.Reversed = false;
            LightningChart.View3D.YAxisPrimary3D.Reversed = false;
            LightningChart.View3D.XAxisPrimary3D.Reversed = false;
            foreach (var wall in LightningChart.View3D.GetWalls())
            {
                wall.Visible = false;
            }

            foreach (var axis in LightningChart.View3D.GetAxes())
            {
                axis.Visible = false;
            }
            LightningChart.ChartRenderOptions.AntiAliasLevel = 0; // 禁用抗锯齿

            // 优化鼠标交互性能
            LightningChart.View3D.ZoomPanOptions.WheelZoomFactor = 1;

            LightningChart.EndUpdate();
        }

        private void RefreshLightningChart()
        {
            //LightningChart.View3D.Dimensions.Height = ConfigCenter.FrameConfig.GetPara<double>("KCJC0_SurfaceChartSetting_DimensionsHeight", 10.0); ;
        }

        public Task UpdateMeasureDataAsync(ProcessedData pd, CancellationToken token = default)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //LightningChart.BeginUpdate();
                //ViewModel.UpdateSurfaceData(pd, LightningChart.View3D);
                //LightningChart.EndUpdate();
            });

            return Task.CompletedTask;
        }

        public void ClearData()
        {
        }

        public void SetChartName(string chartName)
        {
            //ViewModel.ChartName = chartName;
        }

        public void ShowSettingView()
        {
            //var setting = new KCJC0_SurfaceChartSetting();
            //setting.ShowDialog();
            //RefreshLightningChart();
        }

        private ValueRangePalette CreateGradientPalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            var palette = new ValueRangePalette(ownerSeries);
            palette.Type = PaletteType.Gradient;
            palette.Steps.Clear();

            var maxVal = float.MinValue;
            var minVal = float.MaxValue;

            foreach (var t in heightDatas)
            {
                foreach (var t1 in t)
                {
                    if (float.IsNaN(t1)) continue;
                    if (float.IsNaN(t1) || t1 == 0)
                    {
                        continue;
                    }

                    maxVal = Math.Max(maxVal, t1);
                    minVal = Math.Min(minVal, t1);
                }
            }


            var range = maxVal - minVal;
            var step = range / 8;

            var firstQuarter = minVal + step;
            var secondQuarter = firstQuarter + step * 3;
            var thirdQuarter = secondQuarter + step * 3;
            var fourthQuarter = maxVal;

            palette.MinValue = minVal;
            palette.Steps.Add(new PaletteStep(palette, Colors.Blue, firstQuarter));
            palette.Steps.Add(new PaletteStep(palette, Colors.Lime, secondQuarter));
            palette.Steps.Add(new PaletteStep(palette, Colors.Yellow, thirdQuarter));
            palette.Steps.Add(new PaletteStep(palette, Colors.Red, fourthQuarter));

            return palette;
        }

        private SurfaceGridSeries3D CreateSurfaceGridSeries3D(View3D view3d)
        {
            var surfaceGround = new SurfaceGridSeries3D(view3d, Axis3DBinding.Primary,
                Axis3DBinding.Primary,
                Axis3DBinding.Primary);
            surfaceGround.Fill = SurfaceFillStyle.PalettedByY;
            surfaceGround.ContourPalette.Type = PaletteType.Gradient;
            surfaceGround.ContourLineType = ContourLineType3D.None;
            surfaceGround.WireframeType = SurfaceWireframeType3D.None;
            return surfaceGround;
        }

        private void ReplaceInvalidValues(float[][] depthMap)
        {
            foreach (var data in depthMap)
            {
                for (var j = 0; j < data.Length; j++)
                {
                    if (Math.Abs(data[j]) >= 888888)
                    {
                        data[j] = float.NaN;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LightningChart.View3D.Camera.ViewDistance = 180.0;
            LightningChart.View3D.Camera.Projection = ProjectionType.Orthographic;
            LightningChart.View3D.Camera.RotationX = 90;
            LightningChart.View3D.Camera.RotationY = 0;
            LightningChart.View3D.Camera.RotationZ = 90;

        }


    }
}
