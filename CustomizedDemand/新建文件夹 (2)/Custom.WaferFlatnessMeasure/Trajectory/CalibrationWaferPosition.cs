using Prism.Mvvm;
using System;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// Editable position of a calibration wafer used by the calibration run.
    /// </summary>
    [Serializable]
    public class CalibrationWaferPosition : BindableBase
    {
        private string _name = string.Empty;
        private double _x;
        private double _y;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
    }
}
