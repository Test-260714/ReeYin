using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Annotations;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Titles;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.ViewModels
{
    public class PartitionChartViewModel : DialogViewModelBase
    {
        #region Fields
        public ProcessedData? CurrentProcessedData;
        #endregion

        #region Properties
        private ConstantLineCollection _constantLines = new ConstantLineCollection();

        public ConstantLineCollection ConstantLines
        {
            get { return _constantLines; }
            set { _constantLines = value; RaisePropertyChanged(); }
        }

        private AxisXCollection _axisXCollection = new AxisXCollection();

        public AxisXCollection AxisXCollection
        {
            get { return _axisXCollection; }
            set { _axisXCollection = value; RaisePropertyChanged(); }
        }

        private AxisYCollection _axisYCollection = new AxisYCollection();

        public AxisYCollection AxisYCollection
        {
            get { return _axisYCollection; }
            set { _axisYCollection = value; RaisePropertyChanged(); }
        }

        private FreeformPointLineSeriesCollection _pointLineSeriesCollection = new FreeformPointLineSeriesCollection();

        public FreeformPointLineSeriesCollection PointLineSeriesCollection
        {
            get { return _pointLineSeriesCollection; }
            set { _pointLineSeriesCollection = value; RaisePropertyChanged(); }
        }

        private AnnotationXYCollection _annotations = new AnnotationXYCollection();

        public AnnotationXYCollection Annotations
        {
            get { return _annotations; }
            set { _annotations = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public PartitionChartViewModel()
        {
            PointLineSeriesCollection = new FreeformPointLineSeriesCollection();
            Annotations = new AnnotationXYCollection();
            SetUpAxis();
            //订阅
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((pd) =>
            {
                try
                {
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            CurrentProcessedData = pd;
                            var result = pd.GetMemoryPara("KCJC0_MeasureResult", new KCJC0_MeasureResult());
                            if (result?.PartitionResults == null || result.PartitionResults.Count == 0)
                            {
                                // 压花结果没有刻槽分区曲线，分区图不参与显示。
                                PointLineSeriesCollection.Clear();
                                return;
                            }

                            CreateYAxes(result);
                            InitPointLineSeries(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.StackTrace}");
                }

            }, ThreadOption.BackgroundThread);
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
                case "重置":
                    if (CurrentProcessedData == null) return;
                    var result = CurrentProcessedData.GetMemoryPara("KCJC0_MeasureResult", new KCJC0_MeasureResult());
                    if (result?.PartitionResults == null || result.PartitionResults.Count == 0)
                    {
                        PointLineSeriesCollection.Clear();
                        return;
                    }
                    CreateYAxes(result);
                    InitPointLineSeries(result);
                    break;

                default:
                    break;
            }

        });
        #endregion

        #region Methods
        private void SetUpAxis()
        {
            var axisX = new AxisX
            {
                AllowScrolling = false,
                AllowScaling = false,
                Title = new AxisXTitle
                {
                    Text = ""
                }
            };
            AxisXCollection!.Add(axisX);
            var axisY = new AxisY
            {
                AllowScrolling = false,
                AllowScaling = false,
                Title = new AxisYTitle
                {
                    Text = ""
                }
            };
            AxisYCollection!.Add(axisY);
        }

        private void InitPointLineSeries(KCJC0_MeasureResult result)
        {
            try
            {
                PointLineSeriesCollection.Clear();
                for (var i = 0; i < result.PartitionResults.Count; i++)
                {
                    if (result.PartitionResults[i].HeightCurveX == null ||
                        result.PartitionResults[i].HeightCurveY == null ||
                        result.PartitionResults[i].HeightCurveX.Length == 0 ||
                        result.PartitionResults[i].HeightCurveY.Length == 0)
                    {
                        continue;
                    }

                    var flsRectangles = new FreeformPointLineSeries();
                    //var color = DefaultColors.SeriesForBlackBackgroundWpf[i];
                    //flsRectangles.LineStyle.Color = color;
                    flsRectangles.AssignYAxisIndex = i;
                    flsRectangles.Title.Visible = false;
                    flsRectangles.Title.Text = "MeasureLine Series";
                    flsRectangles.Title.Color = flsRectangles.LineStyle.Color;
                    flsRectangles.LineVisible = true;
                    flsRectangles.PointsVisible = false;
                    flsRectangles.PointStyle.BorderWidth = 1;
                    flsRectangles.PointCountLimitEnabled = true;
                    flsRectangles.PointCountLimit = 10000;
                    flsRectangles.UsePalette = true;
                    flsRectangles.ValueRangePalette.MinValue = 0;
                    flsRectangles.ValueRangePalette.Type = PaletteType.Uniform;
                    PointLineSeriesCollection?.Add(flsRectangles);
                    Debug.WriteLine(i);
                    var seriesPoints = result.PartitionResults[i].HeightCurveX
                        .Select((t, i1) => new SeriesPoint(t, result.PartitionResults[i].HeightCurveY[i1])).ToArray();
                    flsRectangles.AddPoints(seriesPoints, true);
                    UpdateAxisXy(result.PartitionResults[i], i);
                    AddAnnotations(result.PartitionResults[i], i);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void CreateYAxes(KCJC0_MeasureResult result)
        {
            AxisYCollection?.Clear();
            Annotations.Clear();
            for (var i = 0; i < result.PartitionResults.Count; i++)
            {
                var axisY = new AxisY
                {
                    AllowScrolling = false,
                    AllowScaling = false,
                    Title = new AxisYTitle
                    {
                        Text = ""
                    },
                };
                axisY.Title.Text = $"分区 {i + 1}";
                axisY.Title.Font = new WpfFont("Segoe UI", 20, false, false);
                axisY.AutoFormatLabels = false;
                axisY.AutoDivSeparationPercent = 5;
                axisY.LabelsNumberFormat = "0";
                AxisYCollection?.Add(axisY);
            }
        }

        protected void UpdateAxisXy(KCJC0_PartitionResult partitionResult, int index)
        {
            var minY = partitionResult.HeightCurveY.Min();
            var maxY = partitionResult.HeightCurveY.Max();
            var minX = partitionResult.HeightCurveX.Min();
            var maxX = partitionResult.HeightCurveX.Max();
            Debug.WriteLine($"minY:{minY},maxY:{maxY},minX:{minX},maxX:{maxX}");
            AxisYCollection?[index].SetRange(minY, maxY + 300);
            AxisXCollection?[0].SetRange(minX, maxX);
        }

        private void AddAnnotations(KCJC0_PartitionResult partitionResult, int assignIndex)
        {

            var index = 1;
            foreach (var neg in partitionResult.EtchingLineNegProj)
            {
                var annoStart = GenAnnotationXy(assignIndex, 60);
                annoStart.Text = $"{index}S";
                annoStart.TargetAxisValues.X = partitionResult.HeightCurveX[Convert.ToInt32(neg)];
                annoStart.TargetAxisValues.Y = partitionResult.HeightCurveY[Convert.ToInt32(neg)];
                annoStart.LocationAxisValues.X = partitionResult.HeightCurveX[Convert.ToInt32(neg)] - 10;
                Annotations.Add(annoStart);
                index++;
            }

            index = 1;
            foreach (var neg in partitionResult.EtchingLinePosProj)
            {
                var annoStart = GenAnnotationXy(assignIndex, 60);
                annoStart.Text = $"{index}E";
                annoStart.TargetAxisValues.X = partitionResult.HeightCurveX[Convert.ToInt32(neg)];
                Annotations.Add(annoStart);
                annoStart.TargetAxisValues.Y = partitionResult.HeightCurveY[Convert.ToInt32(neg)];
                index++;
            }

            index = 1;
            foreach (var neg in partitionResult.EtchingPointProj)
            {
                var annoStart = GenAnnotationXy(assignIndex);
                annoStart.Text = $"{index}D";
                annoStart.TargetAxisValues.X = partitionResult.HeightCurveX[Convert.ToInt32(neg)];
                Annotations.Add(annoStart);
                annoStart.TargetAxisValues.Y = partitionResult.HeightCurveY[Convert.ToInt32(neg)];
                index++;
            }
        }

        private AnnotationXY GenAnnotationXy(int assignIndex, int rotateAngle = 0)
        {

            AnnotationXY anno = new AnnotationXY(null, AxisXCollection![0], null);
            anno.Style = AnnotationStyle.Arrow;
            anno.ArrowLineStyle.Pattern = LinePattern.Solid;
            anno.AssignYAxisIndex = assignIndex;
            anno.LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget;
            //anno.ArrowLineStyle.Color = DefaultColors.SeriesForBlackBackgroundWpf[assignIndex];
            anno.ArrowLineStyle.Width = 1;
            anno.TextStyle.Font = new WpfFont("Segoe UI", 12, true, false);
            anno.TextStyle.Color = Colors.White;
            anno.Visible = true;
            anno.AllowDragging = false;
            anno.AllowResize = false;
            anno.AllowRotate = false;
            anno.AllowTargetMove = false;
            return anno;
        }
        #endregion

    }
}
