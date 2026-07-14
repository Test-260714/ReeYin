using Arction.Wpf.ChartingMVVM;
using Prism.Mvvm;
using System;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.PolarLineSeries
{
    /// <summary>
    /// 极坐标折线图数据模型
    /// </summary>
    public class LineSeriesModel : BindableBase, IDisposable
    {
        #region 图表配置参数

        /// <summary>
        /// 数据点数量（默认360度）
        /// </summary>
        
        public int PointCount { get; set; } = 360;

        /// <summary>
        /// 最小振幅
        /// </summary>
        public double MinAmplitude { get; set; } = 0;

        /// <summary>
        /// 最大振幅
        /// </summary>
        public double MaxAmplitude { get; set; } = 20;

        /// <summary>
        /// 内圆半径百分比
        /// </summary>
        public int InnerCircleRadiusPercentage { get; set; } = 10;

        /// <summary>
        /// 主刻度数量
        /// </summary>
        public int MajorDivCount { get; set; } = 4;

        /// <summary>
        /// 数据点大小
        /// </summary>
        public double PointSize { get; set; } = 4;

        /// <summary>
        /// 是否显示数据点
        /// </summary>
        public bool PointsVisible { get; set; } = true;

        /// <summary>
        /// 扇区起始角度
        /// </summary>
        public double SectorBeginAngle { get; set; } = 0;

        /// <summary>
        /// 扇区结束角度
        /// </summary>
        public double SectorEndAngle { get; set; } = 45;

        /// <summary>
        /// 扇区最小振幅
        /// </summary>
        public double SectorMinAmplitude { get; set; } = 10;

        /// <summary>
        /// 扇区最大振幅
        /// </summary>
        public double SectorMaxAmplitude { get; set; } = 20;

        /// <summary>
        /// 图表名称
        /// </summary>
        public string ChartName { get; set; } = "Polar Line Series Chart";

        #endregion

        #region 数据生成方法

        /// <summary>
        /// 生成随机测试数据
        /// </summary>
        public PolarSeriesPoint[] GenerateData()
        {
            PolarSeriesPoint[] points = new PolarSeriesPoint[PointCount];
            Random random = new Random();
            double baseAmplitude = (MaxAmplitude - MinAmplitude) / 2 + MinAmplitude;
            double variation = (MaxAmplitude - MinAmplitude) / 4;

            for (int i = 0; i < PointCount; i++)
            {
                points[i].Amplitude = baseAmplitude + variation * random.NextDouble() + variation * Math.Cos(Math.PI * i / 180.0);
                points[i].Angle = (double)i * 360.0 / PointCount;
            }
            return points;
        }

        /// <summary>
        /// 根据角度和振幅数组生成数据点
        /// </summary>
        /// <param name="angles">角度数组</param>
        /// <param name="amplitudes">振幅数组</param>
        public PolarSeriesPoint[] GenerateData(double[] angles, double[] amplitudes)
        {
            if (angles == null || amplitudes == null)
                return Array.Empty<PolarSeriesPoint>();

            int count = Math.Min(angles.Length, amplitudes.Length);
            PolarSeriesPoint[] points = new PolarSeriesPoint[count];

            for (int i = 0; i < count; i++)
            {
                points[i].Angle = angles[i];
                points[i].Amplitude = amplitudes[i];
            }
            return points;
        }

        /// <summary>
        /// 根据振幅数组生成数据点（角度自动均匀分布）
        /// </summary>
        /// <param name="amplitudes">振幅数组</param>
        public PolarSeriesPoint[] GenerateData(double[] amplitudes)
        {
            if (amplitudes == null || amplitudes.Length == 0)
                return Array.Empty<PolarSeriesPoint>();

            PolarSeriesPoint[] points = new PolarSeriesPoint[amplitudes.Length];
            double angleStep = 360.0 / amplitudes.Length;

            for (int i = 0; i < amplitudes.Length; i++)
            {
                points[i].Angle = i * angleStep;
                points[i].Amplitude = amplitudes[i];
            }
            return points;
        }

        /// <summary>
        /// 添加单个数据点到现有数据
        /// </summary>
        /// <param name="existingPoints">现有数据点</param>
        /// <param name="angle">新数据点角度</param>
        /// <param name="amplitude">新数据点振幅</param>
        public PolarSeriesPoint[] AddPoint(PolarSeriesPoint[] existingPoints, double angle, double amplitude)
        {
            int newLength = (existingPoints?.Length ?? 0) + 1;
            PolarSeriesPoint[] newPoints = new PolarSeriesPoint[newLength];

            if (existingPoints != null)
                Array.Copy(existingPoints, newPoints, existingPoints.Length);

            newPoints[newLength - 1].Angle = angle;
            newPoints[newLength - 1].Amplitude = amplitude;

            return newPoints;
        }

        /// <summary>
        /// 更新指定索引的数据点
        /// </summary>
        /// <param name="points">数据点数组</param>
        /// <param name="index">索引</param>
        /// <param name="angle">新角度</param>
        /// <param name="amplitude">新振幅</param>
        public void UpdatePoint(PolarSeriesPoint[] points, int index, double angle, double amplitude)
        {
            if (points == null || index < 0 || index >= points.Length)
                return;

            points[index].Angle = angle;
            points[index].Amplitude = amplitude;
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public PolarSeriesPoint[] ClearData()
        {
            return Array.Empty<PolarSeriesPoint>();
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
