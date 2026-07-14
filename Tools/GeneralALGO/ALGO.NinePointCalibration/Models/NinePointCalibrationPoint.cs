using Prism.Mvvm;
using System;

namespace ALGO.NinePointCalibration.Models
{
    [Serializable]
    public class NinePointCalibrationPoint : BindableBase
    {
        private bool _isUsed = true;
        private int _index;
        private double _pixelX;
        private double _pixelY;
        private double _machineX;
        private double _machineY;
        private double _fitMachineX;
        private double _fitMachineY;
        private double _error;

        public bool IsUsed
        {
            get { return _isUsed; }
            set { SetProperty(ref _isUsed, value); }
        }

        public int Index
        {
            get { return _index; }
            set
            {
                if (SetProperty(ref _index, value))
                {
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string DisplayName => $"P{Index}";

        public double PixelX
        {
            get { return _pixelX; }
            set { SetProperty(ref _pixelX, value); }
        }

        public double PixelY
        {
            get { return _pixelY; }
            set { SetProperty(ref _pixelY, value); }
        }

        public double MachineX
        {
            get { return _machineX; }
            set { SetProperty(ref _machineX, value); }
        }

        public double MachineY
        {
            get { return _machineY; }
            set { SetProperty(ref _machineY, value); }
        }

        public double FitMachineX
        {
            get { return _fitMachineX; }
            set { SetProperty(ref _fitMachineX, value); }
        }

        public double FitMachineY
        {
            get { return _fitMachineY; }
            set { SetProperty(ref _fitMachineY, value); }
        }

        public double Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }
    }
}
