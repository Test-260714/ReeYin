using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// 单条轨迹的执行状态，用于区分绘制颜色和线型。
    /// </summary>
    public enum AxisTrajectoryState
    {
        待执行 = 0,
        执行中 = 1,
        已完成 = 2
    }

    /// <summary>
    /// 单条轴运动轨迹的数据模型，包含轨迹点、显示名称和几何统计信息。
    /// </summary>
    public sealed class AxisTrajectoryItem : BindableBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _displayName = "轨迹";
        private IReadOnlyList<Point> _points = Array.Empty<Point>();
        private AxisTrajectoryState _state = AxisTrajectoryState.待执行;
        private bool _isVisible = true;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value ?? string.Empty);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, string.IsNullOrWhiteSpace(value) ? "轨迹" : value.Trim());
        }

        public IReadOnlyList<Point> Points
        {
            get => _points;
            set
            {
                if (SetProperty(ref _points, value ?? Array.Empty<Point>()))
                {
                    RaiseGeometryChanged();
                }
            }
        }

        public AxisTrajectoryState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public int PointCount => _points.Count;

        public bool HasPoints => _points.Count > 0;

        /// <summary>
        /// 起点和终点在无有效点时返回 NaN，便于调用方统一判定点是否可显示。
        /// </summary>
        public Point StartPoint => _points.Count > 0 ? _points[0] : new Point(double.NaN, double.NaN);

        public Point EndPoint => _points.Count > 0 ? _points[^1] : new Point(double.NaN, double.NaN);

        /// <summary>
        /// 按轨迹点顺序累计相邻点距离，作为计划路径长度。
        /// </summary>
        public double PathLength
        {
            get
            {
                if (_points.Count < 2)
                {
                    return 0;
                }

                double length = 0;
                for (int index = 1; index < _points.Count; index++)
                {
                    length += Distance(_points[index - 1], _points[index]);
                }

                return length;
            }
        }

        public static AxisTrajectoryItem FromPoints(string displayName, IEnumerable<Point>? points)
        {
            return new AxisTrajectoryItem
            {
                DisplayName = displayName,
                Points = points?.ToArray() ?? Array.Empty<Point>()
            };
        }

        private void RaiseGeometryChanged()
        {
            // Points 改变后同步刷新所有依赖几何信息的只读属性绑定。
            RaisePropertyChanged(nameof(PointCount));
            RaisePropertyChanged(nameof(HasPoints));
            RaisePropertyChanged(nameof(StartPoint));
            RaisePropertyChanged(nameof(EndPoint));
            RaisePropertyChanged(nameof(PathLength));
        }

        private static double Distance(Point startPoint, Point endPoint)
        {
            double deltaX = endPoint.X - startPoint.X;
            double deltaY = endPoint.Y - startPoint.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}
