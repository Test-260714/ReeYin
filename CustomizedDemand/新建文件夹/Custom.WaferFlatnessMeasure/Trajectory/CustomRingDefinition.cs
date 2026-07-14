using System;
using Prism.Mvvm;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 自定义圆环半径配置，用于按指定半径生成圆环点位。
    /// </summary>
    [Serializable]
    public class CustomRingDefinition : BindableBase
    {
        private double _radius;

        public double Radius
        {
            get => _radius;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Max(value, 0.001) : 0.001;
                SetProperty(ref _radius, normalizedValue);
            }
        }
    }
}
