namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models
{
    public class ThicknessCorrectionItem
    {
        public int LayerIndex { get; set; }

        public string DisplayName
        {
            get { return $"厚度{LayerIndex}"; }
        }

        public string MaterialName { get; set; } = string.Empty;

        public double RefractiveIndex1285 { get; set; }

        public double RefractiveIndex1310 { get; set; }

        public double RefractiveIndex1335 { get; set; }

        public double CorrectionFactor { get; set; } = 1d;

        public int RefractionTag { get; set; } = 1;

        public ThicknessCorrectionItem()
        {
        }

        public ThicknessCorrectionItem(int layerIndex)
        {
            LayerIndex = layerIndex;
        }
    }
}
