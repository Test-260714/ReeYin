using Custom.KCJC.Models.ALGO;
using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Custom.KCJC.Models
{
    public partial class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties
        private double _pitchSlope;
        /// <summary>
        /// 坡度
        /// </summary>
        public double PitchSlope
        {
            get { return _pitchSlope; }
            set { _pitchSlope = value; RaisePropertyChanged(); }
        }

        private float[] _depthBase;
        /// <summary>
        /// 深度图高度参考值
        /// </summary>
        public float[] DepthBase
        {
            get { return _depthBase; }
            set { _depthBase = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LinearSpectralSplicing_MeasureParam _concatenationParam = new LinearSpectralSplicing_MeasureParam();

        public LinearSpectralSplicing_MeasureParam ConcatenationParam
        {
            get { return _concatenationParam; }
            set { _concatenationParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _calibPath;
        /// <summary>
        /// 标定图片文件
        /// </summary>
        public string CalibPath
        {
            get { return _calibPath; }
            set { _calibPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _concatenationPath;
        /// <summary>
        /// 拼接图片路径
        /// </summary>
        public string ConcatenationPath
        {
            get { return _concatenationPath; }
            set { _concatenationPath = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Methods
        /// <summary>
        /// 标定方法
        /// </summary>
        /// <returns></returns>
        public bool CalibMethod()
        {
            try
            {
                LinearSpectralSplicing_Algorithm measureProcess = new LinearSpectralSplicing_Algorithm(ConcatenationParam);

                // 俯仰标定
                string calibGrayDir = CalibPath + "/image";
                string calibHeightDir = CalibPath + "/depth";
                string calibGrayImagePath = GetImagePaths(calibGrayDir)[0];
                string calibHeightImagePath = GetImagePaths(calibHeightDir)[0];
                Mat calibGrayImage = Cv2.ImRead(calibGrayImagePath, ImreadModes.Grayscale);
                Mat calibHeightImage = Cv2.ImRead(calibHeightImagePath, ImreadModes.Unchanged);
                List<float[]> calibGrayDate = ConvertMatToList(calibGrayImage);
                List<float[]> calibHeightDate = ConvertMatToList(calibHeightImage);

                measureProcess.PitchCalibration(calibGrayDate, calibHeightDate, ConcatenationParam, out _pitchSlope, out _depthBase);

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        /// <summary>
        /// 拼接
        /// </summary>
        /// <returns></returns>
        public bool Concatenation()
        {
            try
            {
                LinearSpectralSplicing_Algorithm measureProcess = new LinearSpectralSplicing_Algorithm(ConcatenationParam);
                string[] folders = Directory.GetDirectories(ConcatenationPath);

                for (int i = 0; i < folders.Length; i++)
                {
                    string folder = folders[i];

                    string grayImageDir = folder + "\\image";
                    string heightImageDir = folder + "\\depth";

                    string grayImagePath = GetImagePaths(grayImageDir)[0];
                    string heightImagePath = GetImagePaths(heightImageDir)[0];

                    Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                    Mat heightImage = Cv2.ImRead(heightImagePath, ImreadModes.Unchanged);
                    List<float[]> grayDate = ConvertMatToList(grayImage);
                    List<float[]> heightDate = ConvertMatToList(heightImage);

                    //string imageName = Path.GetFileNameWithoutExtension(grayImagePath);
                    //string[] parts = imageName.Split('_');
                    //double[] values = parts.Select(part => double.Parse(part)).ToArray();
                    //ConcatenationParam.IntervalX = values[0];
                    //ConcatenationParam.IntervalY = values[1];
                    //ConcatenationParam.IntervalZ = values[2];
                    //ConcatenationParam.MinDepth = values[3];
                    //ConcatenationParam.MaxDepth = values[4];
                    ConcatenationParam.IsFlip = false;
                    ConcatenationParam.IsScanEnd = false;
                    ConcatenationParam.IsPitchCalib = true;
                    ConcatenationParam.PitchSlope = PitchSlope;
                    ConcatenationParam.DepthBase = DepthBase;
                    ConcatenationParam.OffsetX += 6000;
                    ConcatenationParam.OffsetY = 0;
                    //ConcatenationParam.CompensationX = values[7];
                    //ConcatenationParam.CompensationY = values[8];

                    measureProcess.cache_images(grayDate, heightDate, ConcatenationParam);

                    if (grayImage.Data != IntPtr.Zero)
                        grayImage.Dispose();
                    if (heightImage.Data != IntPtr.Zero)
                        heightImage.Dispose();

                }
                LinearSpectralSplicing_MeasureResult result = measureProcess.merge_images(ConcatenationParam);
                HOperatorSet.WriteImage(result.GrayImage, "tiff", 0, ConcatenationPath + "\\TileGrayImage.tiff");
                HOperatorSet.WriteImage(result.HeightImage, "tiff", 8888880, ConcatenationPath + "\\TileHeightImage.tiff");

                string plyPath = ConcatenationPath + "\\TileImage.ply";
                var depthMapToCloud = new DepthMapToCloudUm(ConcatenationParam.IntervalX, ConcatenationParam.IntervalY, ConcatenationParam.IntervalZ);
                depthMapToCloud.SavePlyAsciiUm(plyPath, result.HeightImage, false, 1);
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public static List<string> GetImagePaths(string folderPath)
        {
            List<string> imagePaths = new List<string>();
            string[] supportedExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".ico" };

            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

            foreach (FileInfo file in files)
            {
                if (supportedExtensions.Contains(file.Extension.ToLower()))
                {
                    imagePaths.Add(file.FullName);
                }
            }

            return imagePaths;
        }

        public static List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat.Empty())
                return data;

            // 确保Mat是连续的内存块
            if (!mat.IsContinuous())
                mat = mat.Clone();

            int channels = mat.Channels();
            if (channels != 1)
                throw new InvalidOperationException("Only single-channel matrices are supported");

            int rows = mat.Rows;
            int cols = mat.Cols;
            MatType type = mat.Type();

            try
            {
                // 根据不同类型处理数据
                if (type == MatType.CV_8UC1)
                {
                    ProcessByteMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32FC1)
                {
                    ProcessFloatMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32SC1)
                {
                    ProcessIntMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                // 如果克隆了Mat需要释放
                if (!mat.IsContinuous())
                    mat.Dispose();
            }

            return data;
        }

        private static void ProcessByteMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            byte[] buffer = new byte[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                int offset = i * cols;
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[offset + j];
                }
                data.Add(row);
            }
        }

        private static void ProcessFloatMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                Array.Copy(buffer, i * cols, row, 0, cols);
                data.Add(row);
            }
        }

        private static void ProcessIntMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            int[] buffer = new int[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转为 float 存入结果
                }
                data.Add(row);
            }
        }
        #endregion

    }
}
