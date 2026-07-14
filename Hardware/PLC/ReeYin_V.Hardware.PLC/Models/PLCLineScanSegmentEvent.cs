using Prism.Events;
using System;

namespace ReeYin_V.Hardware.PLC.Models
{
    public class PLCLineScanSegmentEvent : PubSubEvent<PLCLineScanSegmentInfo>
    {
    }

    [Serializable]
    public class PLCLineScanSegmentInfo
    {
        public int RowIndex { get; set; }

        public string Stage { get; set; } = string.Empty;

        public string SourceEventName { get; set; } = string.Empty;

        public double StartX { get; set; }

        public double StartY { get; set; }

        public double StartZ { get; set; }

        public double EndX { get; set; }

        public double EndY { get; set; }

        public double EndZ { get; set; }

        public string Direction { get; set; } = string.Empty;

        public int SourceSerial { get; set; }

        public string OrderDescribe { get; set; } = string.Empty;

        public DateTime EventTime { get; set; }

        public bool PositionReadSuccess { get; set; }
    }
}