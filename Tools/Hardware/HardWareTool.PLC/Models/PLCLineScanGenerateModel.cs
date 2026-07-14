using System;

namespace HardWareTool.PLC.Models
{
    [Serializable]
    public class PLCLineScanGenerateModel : BindableBase
    {
        private string _targetPlcId = string.Empty;
        public string TargetPlcId
        {
            get { return _targetPlcId; }
            set { _targetPlcId = value; RaisePropertyChanged(); }
        }

        private string _axisGroupName = string.Empty;
        public string AxisGroupName
        {
            get { return _axisGroupName; }
            set { _axisGroupName = value; RaisePropertyChanged(); }
        }

        private double _leftTopX;
        public double LeftTopX
        {
            get { return _leftTopX; }
            set { _leftTopX = value; RaisePropertyChanged(); }
        }

        private double _leftTopY;
        public double LeftTopY
        {
            get { return _leftTopY; }
            set { _leftTopY = value; RaisePropertyChanged(); }
        }

        private double _rightBottomX;
        public double RightBottomX
        {
            get { return _rightBottomX; }
            set { _rightBottomX = value; RaisePropertyChanged(); }
        }

        private double _rightBottomY;
        public double RightBottomY
        {
            get { return _rightBottomY; }
            set { _rightBottomY = value; RaisePropertyChanged(); }
        }

        private double _stepY = 10;
        public double StepY
        {
            get { return _stepY; }
            set { _stepY = value; RaisePropertyChanged(); }
        }

        private double _scanSpeed = 10;
        public double ScanSpeed
        {
            get { return _scanSpeed; }
            set { _scanSpeed = value; RaisePropertyChanged(); }
        }

        private double _offsetSpeed = 10;
        public double OffsetSpeed
        {
            get { return _offsetSpeed; }
            set { _offsetSpeed = value; RaisePropertyChanged(); }
        }

        private double _acc = 1;
        public double Acc
        {
            get { return _acc; }
            set { _acc = value; RaisePropertyChanged(); }
        }

        private double _dec = 1;
        public double Dec
        {
            get { return _dec; }
            set { _dec = value; RaisePropertyChanged(); }
        }

        private bool _waitMoveDone = true;
        public bool WaitMoveDone
        {
            get { return _waitMoveDone; }
            set { _waitMoveDone = value; RaisePropertyChanged(); }
        }

        private int _moveTimeoutMs = 60000;
        public int MoveTimeoutMs
        {
            get { return _moveTimeoutMs; }
            set { _moveTimeoutMs = Math.Clamp(value, 100, 600000); RaisePropertyChanged(); }
        }

        private string _startCollectEventName = "TrrigerStartCollect";
        public string StartCollectEventName
        {
            get { return _startCollectEventName; }
            set { _startCollectEventName = value; RaisePropertyChanged(); }
        }

        private string _stopCollectEventName = "TrrigerStopCollect";
        public string StopCollectEventName
        {
            get { return _stopCollectEventName; }
            set { _stopCollectEventName = value; RaisePropertyChanged(); }
        }

        private int _offsetStableDelayMs = 200;
        public int OffsetStableDelayMs
        {
            get { return _offsetStableDelayMs; }
            set { _offsetStableDelayMs = Math.Clamp(value, 0, 600000); RaisePropertyChanged(); }
        }

        private int _stopCollectDelayMs = 0;
        public int StopCollectDelayMs
        {
            get { return _stopCollectDelayMs; }
            set { _stopCollectDelayMs = Math.Clamp(value, 0, 600000); RaisePropertyChanged(); }
        }

        private int _previewLineCount;
        public int PreviewLineCount
        {
            get { return _previewLineCount; }
            set { _previewLineCount = value; RaisePropertyChanged(); }
        }

        private int _previewOrderCount;
        public int PreviewOrderCount
        {
            get { return _previewOrderCount; }
            set { _previewOrderCount = value; RaisePropertyChanged(); }
        }
    }
}
