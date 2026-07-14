namespace Custom.CalibrationPlateMeasure.Views
{
    // 以图像像素坐标保存 ROI，避免视图缩放和平移影响算法输入。
    public readonly record struct CalibrationPlateImageRoi(
        double Row1,
        double Column1,
        double Row2,
        double Column2);

    public static class CalibrationPlateRoiViewportMapper
    {
        // 统一将用户框选区域约束到图像范围内，并保证最小 ROI 尺寸。
        public static CalibrationPlateImageRoi NormalizeRoi(
            double row1,
            double column1,
            double row2,
            double column2,
            double imageWidth,
            double imageHeight,
            double minSize)
        {
            double normalizedRow1 = Math.Clamp(Math.Min(row1, row2), 0, imageHeight - 1);
            double normalizedColumn1 = Math.Clamp(Math.Min(column1, column2), 0, imageWidth - 1);
            double normalizedRow2 = Math.Clamp(Math.Max(row1, row2), 0, imageHeight - 1);
            double normalizedColumn2 = Math.Clamp(Math.Max(column1, column2), 0, imageWidth - 1);

            if (normalizedRow2 - normalizedRow1 < minSize)
            {
                normalizedRow2 = Math.Min(imageHeight - 1, normalizedRow1 + minSize);
                normalizedRow1 = Math.Max(0, normalizedRow2 - minSize);
            }

            if (normalizedColumn2 - normalizedColumn1 < minSize)
            {
                normalizedColumn2 = Math.Min(imageWidth - 1, normalizedColumn1 + minSize);
                normalizedColumn1 = Math.Max(0, normalizedColumn2 - minSize);
            }

            return new CalibrationPlateImageRoi(
                normalizedRow1,
                normalizedColumn1,
                normalizedRow2,
                normalizedColumn2);
        }
    }
}
