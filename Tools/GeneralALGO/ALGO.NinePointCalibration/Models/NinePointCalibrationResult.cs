using System;

namespace ALGO.NinePointCalibration.Models
{
    [Serializable]
    public class NinePointCalibrationResult
    {
        public double[] HomMat2D { get; set; } = Array.Empty<double>();

        public double[] InverseHomMat2D { get; set; } = Array.Empty<double>();

        public double AverageError { get; set; }

        public double MaxError { get; set; }

        public DateTime CalibratedTime { get; set; } = DateTime.Now;
    }
}
