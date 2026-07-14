using System;
namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 单条可执行轨迹：线段使用起点/终点，点位则保持起点与终点同步。
    /// </summary>
    [Serializable]
    public class LocusInfo : BindableBase
    {
        public const string LineType = "IsLine";
        public const string PointType = "IsPoint";

        private string _type = LineType;
        private double _originX;
        private double _originY;
        private double _targetX;
        private double _targetY;

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                RaisePropertyChanged();
            }
        }

        public double OriginX
        {
            get => _originX;
            set
            {
                _originX = value;
                RaisePropertyChanged();
            }
        }

        public double OriginY
        {
            get => _originY;
            set
            {
                _originY = value;
                RaisePropertyChanged();
            }
        }

        public double TargetX
        {
            get => _targetX;
            set
            {
                _targetX = value;
                RaisePropertyChanged();
            }
        }

        public double TargetY
        {
            get => _targetY;
            set
            {
                _targetY = value;
                RaisePropertyChanged();
            }
        }
    }
}
