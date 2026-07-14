using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;

namespace ALGO.BlobAnalysis.Models
{
    [Serializable]
    public enum BlobThresholdMode
    {
        固定阈值,
        局部阈值,
        自动暗,
        自动亮,
    }

    [Serializable]
    public enum BlobLocalThresholdType
    {
        暗,
        亮,
    }

    [Serializable]
    public enum BlobFilterMode
    {
        与,
        或,
    }

    [Serializable]
    public class BlobFeatureDefinition : BindableBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _hName = string.Empty;
        public string HName
        {
            get { return _hName; }
            set { SetProperty(ref _hName, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        private bool _isOut;
        public bool IsOut
        {
            get { return _isOut; }
            set { SetProperty(ref _isOut, value); }
        }

        private double _minValue;
        public double MinValue
        {
            get { return _minValue; }
            set { SetProperty(ref _minValue, value); }
        }

        private double _maxValue = 100;
        public double MaxValue
        {
            get { return _maxValue; }
            set { SetProperty(ref _maxValue, value); }
        }

        private double _smallChange = 1;
        public double SmallChange
        {
            get { return _smallChange; }
            set { SetProperty(ref _smallChange, value); }
        }

        public BlobFeatureDefinition()
        {
        }

        public BlobFeatureDefinition(
            string name,
            string hName,
            double minValue,
            double maxValue,
            double smallChange,
            bool isSelected = false,
            bool isOut = false)
        {
            Name = name;
            HName = hName;
            MinValue = minValue;
            MaxValue = maxValue;
            SmallChange = smallChange;
            IsSelected = isSelected;
            IsOut = isOut;
        }

        public static ObservableCollection<BlobFeatureDefinition> CreateDefaultDefinitions()
        {
            return new ObservableCollection<BlobFeatureDefinition>
            {
                new BlobFeatureDefinition("面积", "area", 0, 1000000, 1),
                new BlobFeatureDefinition("中心行坐标", "row", 0, 1000000, 1),
                new BlobFeatureDefinition("中心列坐标", "column", 0, 1000000, 1),
                new BlobFeatureDefinition("区域宽度", "width", 0, 1000000, 1),
                new BlobFeatureDefinition("区域高度", "height", 0, 1000000, 1),
                new BlobFeatureDefinition("宽高比", "ratio", 0, 1000, 0.1),
                new BlobFeatureDefinition("左上角行坐标", "row1", 0, 1000000, 1),
                new BlobFeatureDefinition("左上角列坐标", "column1", 0, 1000000, 1),
                new BlobFeatureDefinition("右下角行坐标", "row2", 0, 1000000, 1),
                new BlobFeatureDefinition("右下角列坐标", "column2", 0, 1000000, 1),
                new BlobFeatureDefinition("圆度", "circularity", 0, 1, 0.01),
                new BlobFeatureDefinition("紧凑度", "compactness", 0, 1, 0.01),
                new BlobFeatureDefinition("周长", "contlength", 0, 1000000, 1),
                new BlobFeatureDefinition("凸性", "convexity", 0, 1000, 0.1),
                new BlobFeatureDefinition("矩形度", "rectangularity", 0, 1, 0.01),
                new BlobFeatureDefinition("等效椭圆长半径", "ra", 0, 1000000, 1),
                new BlobFeatureDefinition("等效椭圆短半径", "rb", 0, 1000000, 1),
                new BlobFeatureDefinition("等效椭圆方向", "phi", -180, 180, 1),
                new BlobFeatureDefinition("偏心率", "anisometry", 0, 1000, 0.1),
                new BlobFeatureDefinition("膨松度", "bulkiness", 0, 1000, 0.1),
                new BlobFeatureDefinition("结构因子", "struct_factor", 0, 1000, 0.1),
                new BlobFeatureDefinition("外切圆半径", "outer_radius", 0, 1000000, 1),
                new BlobFeatureDefinition("内接圆半径", "inner_radius", 0, 1000000, 1),
                new BlobFeatureDefinition("内接矩形高度", "inner_height", 0, 1000000, 1),
                new BlobFeatureDefinition("内接矩形宽度", "inner_width", 0, 1000000, 1),
                new BlobFeatureDefinition("边界到中心平均距离", "dist_mean", 0, 1000000, 0.1),
                new BlobFeatureDefinition("边界到中心距离偏差", "dist_deviation", 0, 1000000, 0.1),
                new BlobFeatureDefinition("多边形边数", "num_sides", 0, 1000, 1),
                new BlobFeatureDefinition("连通数", "connect_num", 0, 1000, 1),
                new BlobFeatureDefinition("孔洞数量", "holes_num", 0, 1000, 1),
                new BlobFeatureDefinition("孔洞总面积", "area_holes", 0, 1000000, 1),
                new BlobFeatureDefinition("最大直径", "max_diameter", 0, 1000000, 1),
                new BlobFeatureDefinition("区域方向", "orientation", -180, 180, 1),
                new BlobFeatureDefinition("欧拉数", "euler_number", -1000, 1000, 1),
                new BlobFeatureDefinition("外接矩形方向", "rect2_phi", -180, 180, 1),
            };
        }
    }
}
