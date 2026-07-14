using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTScraper.Models
{
    public class Common_Algorithm
    {
        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <returns>状态标志</returns>
        public int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject)
        {
            int height = data.Count;
            if (height == 0)
            {
                HOperatorSet.GenEmptyObj(out hoObject);
                return -1;
            }

            int width = data[0].Length;
            GCHandle handle = default;

            try
            {
                if (imageType == ImageType.Gray)
                {
                    byte[] imageData = data.SelectMany(row => row.Select(value => (byte)Math.Clamp(value, 0, 255))).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "byte", width, height, handle.AddrOfPinnedObject());
                }
                else if (imageType == ImageType.Depth)
                {
                    float[] imageData = data.SelectMany(row => row).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "real", width, height, handle.AddrOfPinnedObject());
                }
                else
                {
                    HOperatorSet.GenEmptyObj(out hoObject);
                    return -1;
                }

                return 0;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }


        /// <summary>
        /// 将List<float[]>数组转换为OpenCvSharp Mat图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="cvImage">输出的opencv图片对象</param>
        /// <returns>状态标志</returns>
        public static int ConvertListToMat(List<float[]> data, ImageType imageType, out Mat cvImage)
        {
            int height = data.Count;
            int width;
            if (height > 0)
            {
                width = data[0].Length;

                if (imageType == ImageType.Gray)
                {
                    byte[] imageData = data.SelectMany(row => row.Select(value => (byte)Math.Max(0, Math.Min(255, value)))).ToArray();
                    cvImage = new Mat(height, width, MatType.CV_8UC1);
                    cvImage.SetArray(imageData);
                }
                else if (imageType == ImageType.Depth)
                {
                    float[] imageData = data.SelectMany(row => row).ToArray();
                    cvImage = new Mat(height, width, MatType.CV_32FC1);
                    cvImage.SetArray(imageData);
                }
                else
                {
                    cvImage = new Mat();
                    return -1;
                }
                return 0;
            }
            else
            {
                cvImage = new Mat();
                return -1;
            }
        }


        /// <summary>
        /// halcon HObject类型图片转OpenCVSharp Mat类型
        /// </summary>
        public static Mat HobjectToMat(HObject hoImage, ImageType imageType)
        {
            Mat dst = new Mat();
            try
            {
                HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);

                if (hvChannels.Length == 0)
                {
                    return dst;
                }
                if (hvChannels[0].I == 1)
                {
                    IntPtr intPtr = IntPtr.Zero;
                    HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    intPtr = hvPointer;
                    if (imageType == ImageType.Gray)
                    {
                        dst = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, intPtr);
                    }
                    else
                    {
                        dst = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, intPtr);
                    }
                }
                else if (hvChannels[0].I == 3)
                {
                    IntPtr ptrRed = IntPtr.Zero;
                    IntPtr ptrGreen = IntPtr.Zero;
                    IntPtr ptrBlue = IntPtr.Zero;

                    HOperatorSet.GetImagePointer3(hoImage, out HTuple hvPtrRed, out HTuple hvPtrGreen, out HTuple hvPtrBlue,
                                                  out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    ptrRed = hvPtrRed;
                    ptrGreen = hvPtrGreen;
                    ptrBlue = hvPtrBlue;

                    //分别生成3张图片
                    Mat matRed = new Mat();
                    Mat matGreen = new Mat();
                    Mat matBlue = new Mat();

                    if (imageType == ImageType.Gray)
                    {
                        matRed = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrRed);
                        matGreen = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrGreen);
                        matBlue = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrBlue);
                    }
                    else
                    {
                        matRed = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrRed);
                        matGreen = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrGreen);
                        matBlue = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrBlue);
                    }

                    //合成
                    Mat[] multi = new Mat[] { matBlue, matGreen, matRed };
                    Cv2.Merge(multi, dst);

                    //释放
                    matBlue.Dispose();
                    matGreen.Dispose();
                    matRed.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return dst.Clone();
        }


        /// <summary>
        /// 将 OpenCvSharp 的 Mat 图像转换为 Halcon 的 HObject
        /// </summary>
        public static HObject MatToHObject(Mat mat)
        {
            HObject hoImage = new HObject();
            HOperatorSet.GenEmptyObj(out hoImage);

            if (mat.Empty())
                return hoImage;

            int width = mat.Width;
            int height = mat.Height;

            try
            {
                if (mat.Channels() == 1)
                {
                    if (mat.Type() == MatType.CV_8UC1)
                    {
                        byte[] data = new byte[width * height];
                        Marshal.Copy(mat.Data, data, 0, data.Length);
                        HOperatorSet.GenImage1(out hoImage, "byte", width, height, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
                    }
                    else if (mat.Type() == MatType.CV_32FC1)
                    {
                        float[] data = new float[width * height];
                        Marshal.Copy(mat.Data, data, 0, data.Length);
                        HOperatorSet.GenImage1(out hoImage, "real", width, height, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
                    }
                }
                else if (mat.Channels() == 3 && mat.Type() == MatType.CV_8UC3)
                {
                    // 按 BGR 拆分
                    Mat[] channels = Cv2.Split(mat);
                    byte[] red = new byte[width * height];
                    byte[] green = new byte[width * height];
                    byte[] blue = new byte[width * height];

                    Marshal.Copy(channels[2].Data, red, 0, red.Length);   // R
                    Marshal.Copy(channels[1].Data, green, 0, green.Length); // G
                    Marshal.Copy(channels[0].Data, blue, 0, blue.Length);  // B

                    HOperatorSet.GenImage3(out hoImage, "byte", width, height,
                                           Marshal.UnsafeAddrOfPinnedArrayElement(red, 0),
                                           Marshal.UnsafeAddrOfPinnedArrayElement(green, 0),
                                           Marshal.UnsafeAddrOfPinnedArrayElement(blue, 0));

                    foreach (var c in channels)
                        c.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return hoImage;

        }


        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public static HObject GetLocalDepthValidMask(HObject hoHeightImage, double MinDepth, double MaxDepth)
        {
            HObject hoValidMask = new HObject();

            HObject hoRectangle, hoIrregularRegion;
            HObject hoIrregularRegion0, hoIrregularRegion1, hoIrregularRegion2, hoIrregularMask;

            HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvWidth, out HTuple hvHeight);
            HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight, hvWidth);

            HOperatorSet.Threshold(hoHeightImage, out hoValidMask, MinDepth, MaxDepth);

            HOperatorSet.GenEmptyObj(out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion0, 8888880, 8888880);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion2, 0, 0);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
            HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
            HOperatorSet.Difference(hoRectangle, hoIrregularMask, out hoRectangle);
            HOperatorSet.Intersection(hoValidMask, hoRectangle, out hoValidMask);

            hoRectangle.Dispose();
            hoIrregularRegion.Dispose();
            hoIrregularRegion0.Dispose();
            hoIrregularRegion1.Dispose();
            hoIrregularRegion2.Dispose();
            hoIrregularMask.Dispose();

            return hoValidMask;
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

        private static void ProcessShortMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            short[] buffer = new short[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转 float 存储
                }
                data.Add(row);
            }
        }


        /// <summary>
        /// OpenCVSharp Mat转List<float[]>
        /// </summary>
        public static List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat.Empty())
                return data;

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
                else if (type == MatType.CV_16SC1)
                {
                    ProcessShortMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                if (!mat.IsContinuous())
                    if (mat.Data != IntPtr.Zero)
                        mat.Dispose();
            }

            return data;
        }
    }

    public enum ImageType
    {
        Gray,    // 灰度图
        Depth,   // 深度图
        RGB,     // 三通道RGB图
        BGR      // 三通道BGR图
    }
}
