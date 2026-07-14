using System;
using System.Collections.Generic;
using Prism.Mvvm;
using OpenCvSharp;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Logger;

namespace ReeYin.Hardware.Sensor.SSZN.CustomUI.ViewModels
{
    public class SSZN3DDisplayViewModel : BindableBase
    {
        private const int InvalidHeight32 = -1000000000;
        private const float HeightScale32 = 1.0f / 100000.0f;
        private ImageResultsDisplay? _heightDisplayResult;

        /// <summary>
        /// 3D图表显示数据
        /// </summary>
        public ImageResultsDisplay? HeightDisplayResult
        {
            get { return _heightDisplayResult; }
            set
            {
                if (ReferenceEquals(_heightDisplayResult, value))
                {
                    return;
                }

                _heightDisplayResult?.HeightImage?.Dispose();
                _heightDisplayResult = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 加载高度图显示数据
        /// </summary>
        public bool TryLoadHeightDisplayResult(SSZNSensor sensor, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (sensor == null || sensor.ImgBuff32 == null || sensor.ImgBuff32.Length == 0 || sensor.ImgBuff32[0] == null)
                {
                    errorMessage = "没有可显示的高度数据，请先进行采集。";
                    Logs.LogWarning("SSZN_3D显示: 高度数据为空");
                    return false;
                }

                int width = sensor.iSR7APi.GetProfileDataWidth();
                if (sensor.iSR7APi.CameraBOnline)
                    width /= 2;

                int height = sensor.iSR7APi.BatchPoints;
                if (width <= 0 || height <= 0)
                {
                    errorMessage = $"图像尺寸无效: {width}x{height}";
                    Logs.LogWarning($"SSZN_3D显示: {errorMessage}");
                    return false;
                }

                int[] heightData = sensor.ImgBuff32[0];
                List<float[]> heightRows = new List<float[]>(height);

                for (int y = 0; y < height; y++)
                {
                    float[] row = new float[width];
                    int rowOffset = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        int index = rowOffset + x;
                        if (index >= heightData.Length || heightData[index] == InvalidHeight32)
                        {
                            row[x] = float.NaN;
                            continue;
                        }

                        row[x] = heightData[index] * HeightScale32;
                    }

                    heightRows.Add(row);
                }

                if (Common_Algorithm.ConvertListToMat(heightRows, ImageType.Depth, out Mat heightImage) != 0 || heightImage.Empty())
                {
                    heightImage.Dispose();
                    errorMessage = "高度图转换失败。";
                    Logs.LogWarning("SSZN_3D显示: 高度图转换失败");
                    return false;
                }

                HeightDisplayResult = new ImageResultsDisplay
                {
                    HeightImage = heightImage
                };

                Logs.LogInfo($"SSZN_3D显示: 高度图加载成功, 尺寸 {width}x{height}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"加载3D显示数据失败: {ex.Message}";
                Logs.LogError($"SSZN_3D显示: {errorMessage}");
                return false;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            HeightDisplayResult = null;
        }
    }
}
