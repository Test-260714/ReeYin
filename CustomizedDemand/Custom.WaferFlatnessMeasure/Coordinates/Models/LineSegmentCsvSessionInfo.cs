using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public sealed class LineSegmentCsvSessionInfo
    {
        public const string EventName = "LineSegmentCsvSessionStart";

        public int ExpectedSegmentCount { get; set; }
    }
}
