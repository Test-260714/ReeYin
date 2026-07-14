using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public sealed class LineSegmentStartPositionInfo
    {
        public const string EventName = "LineSegmentStartPosition";

        public int SegmentIndex { get; set; }

        public double StartX { get; set; }

        public double StartY { get; set; }

        public LineSegmentStartPositionInfo Clone()
        {
            return new LineSegmentStartPositionInfo
            {
                SegmentIndex = SegmentIndex,
                StartX = StartX,
                StartY = StartY
            };
        }
    }
}
