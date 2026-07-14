using System;

namespace ReeYin_V.Hardware.Camera.Dalsa
{
    [Serializable]
    public sealed class DalsaCameraInfo
    {
        public string ServerName { get; set; } = string.Empty;

        public int ResourceIndex { get; set; }

        public string SerialNumber { get; set; } = string.Empty;

        public string UserDefinedName { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public string ConfigFileName { get; set; } = string.Empty;

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(UserDefinedName))
                return $"{UserDefinedName} ({SerialNumber})";

            return $"{VendorName} {ModelName} ({SerialNumber})".Trim();
        }
    }
}
