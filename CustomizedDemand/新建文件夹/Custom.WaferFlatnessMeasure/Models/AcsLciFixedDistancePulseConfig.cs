using Prism.Mvvm;
using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public class AcsLciFixedDistancePulseConfig : BindableBase
    {
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private int _bufferNo = 10;
        public int BufferNo
        {
            get => _bufferNo;
            set => SetProperty(ref _bufferNo, Math.Clamp(value, 0, 64));
        }

        private int _axisX;
        public int AxisX
        {
            get => _axisX;
            set => SetProperty(ref _axisX, value);
        }

        private int _axisY = 1;
        public int AxisY
        {
            get => _axisY;
            set => SetProperty(ref _axisY, value);
        }

        private double _pulseWidth = 0.01d;
        public double PulseWidth
        {
            get => _pulseWidth;
            set => SetProperty(ref _pulseWidth, Math.Max(double.Epsilon, value));
        }

        private bool _useSensorInterval = true;
        public bool UseSensorInterval
        {
            get => _useSensorInterval;
            set => SetProperty(ref _useSensorInterval, value);
        }

        private double _interval = 1d;
        public double Interval
        {
            get => _interval;
            set => SetProperty(ref _interval, Math.Max(double.Epsilon, value));
        }

        private double _startDistance;
        public double StartDistance
        {
            get => _startDistance;
            set => SetProperty(ref _startDistance, Math.Max(0d, value));
        }

        private double _endDistance;
        public double EndDistance
        {
            get => _endDistance;
            set => SetProperty(ref _endDistance, Math.Max(0d, value));
        }

        private bool _routeConfigOutput = true;
        public bool RouteConfigOutput
        {
            get => _routeConfigOutput;
            set => SetProperty(ref _routeConfigOutput, value);
        }

        private int _configOutputIndex;
        public int ConfigOutputIndex
        {
            get => _configOutputIndex;
            set => SetProperty(ref _configOutputIndex, value);
        }

        private int _configOutputCode = 7;
        public int ConfigOutputCode
        {
            get => _configOutputCode;
            set => SetProperty(ref _configOutputCode, value);
        }

        private int _timeout = 60000;
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, Math.Max(1000, value));
        }
    }
}
