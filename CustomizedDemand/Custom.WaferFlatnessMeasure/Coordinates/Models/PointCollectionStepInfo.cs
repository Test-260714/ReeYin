using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public class PointCollectionStepInfo
    {
        public const string EventName = "PointCollectionSteps";

        public double X { get; set; }

        public double Y { get; set; }

        public bool IsCalibrationReference { get; set; }

        public int NormalPointIndex { get; set; } = -1;

        public PointCollectionStepInfo Clone()
        {
            return new PointCollectionStepInfo
            {
                X = X,
                Y = Y,
                IsCalibrationReference = IsCalibrationReference,
                NormalPointIndex = NormalPointIndex
            };
        }
    }
}
