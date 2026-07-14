using HalconDotNet;
using System;

namespace ReeYin_V.UI.Controls
{
    internal sealed class HalconDisplayObject : IDisposable
    {
        public HalconDisplayObject(HObject hObject, string color, bool isFillDisplay)
        {
            HObject = hObject ?? throw new ArgumentNullException(nameof(hObject));
            Color = string.IsNullOrWhiteSpace(color) ? "red" : color;
            IsFillDisplay = isFillDisplay;
        }

        public HObject HObject { get; }

        public string Color { get; }

        public bool IsFillDisplay { get; }

        public void Dispose()
        {
            HObject.Dispose();
        }
    }

    internal sealed class HalconDisplayText
    {
        public string Text { get; init; } = string.Empty;

        public double Row { get; init; }

        public double Column { get; init; }

        public string Color { get; init; } = "red";

        public string Font { get; init; } = "mono";

        public int Size { get; init; } = 16;
    }
}
