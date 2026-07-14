using HalconDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// Halcon 图片与二进制数据互转工具类
    /// </summary>
    public static class HalconImageConverter
    {
        /// <summary>
        /// 将 Halcon 图片（HObject）转换为二进制数据（包含元信息和像素数据）
        /// </summary>
        /// <param name="image">Halcon 图片对象（需已初始化）</param>
        /// <returns>包含元信息和像素数据的二进制数组</returns>
        /// <exception cref="ArgumentNullException">图片未初始化时抛出</exception>
        /// <exception cref="NotSupportedException">不支持的通道数或像素类型时抛出</exception>
        public static byte[] ToBinary(HObject image)
        {
            // 验证输入图片
            if (image == null || !image.IsInitialized())
                throw new ArgumentNullException(nameof(image), "Halcon 图片未初始化");

            try
            {
                // 1. 获取图片元信息
                HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
                HOperatorSet.CountChannels(image, out HTuple channels);
                HOperatorSet.GetImageType(image, out HTuple pixelType);
                int pixelSize = GetPixelSize(pixelType); // 单个像素的字节数

                // 2. 提取像素数据
                byte[] pixelData = ExtractPixelData(image, channels, pixelSize, width, height);

                // 3. 拼接元信息和像素数据（格式：元信息长度(4字节) + 元信息 + 像素数据）
                // 3.1 序列化元信息（宽|高|通道数|像素类型，用竖线分隔）
                string metaInfo = $"{width}|{height}|{channels}|{pixelType}";
                byte[] metaBytes = Encoding.UTF8.GetBytes(metaInfo);

                // 3.2 计算总长度并分配缓冲区
                int totalLength = 4 + metaBytes.Length + pixelData.Length;
                byte[] result = new byte[totalLength];

                // 3.3 写入元信息长度（4字节int）
                BitConverter.GetBytes(metaBytes.Length).CopyTo(result, 0);
                // 3.4 写入元信息
                metaBytes.CopyTo(result, 4);
                // 3.5 写入像素数据
                pixelData.CopyTo(result, 4 + metaBytes.Length);

                return result;
            }
            catch (HOperatorException ex)
            {
                throw new InvalidOperationException("Halcon 算子执行失败", ex);
            }
        }

        /// <summary>
        /// 将二进制数据转回 Halcon 图片（HObject）
        /// </summary>
        /// <param name="binaryData">通过 ToBinary 方法生成的二进制数据</param>
        /// <returns>重建的 Halcon 图片对象</returns>
        /// <exception cref="ArgumentNullException">二进制数据为空时抛出</exception>
        /// <exception cref="FormatException">数据格式错误时抛出</exception>
        /// <exception cref="NotSupportedException">不支持的通道数或像素类型时抛出</exception>
        public static HObject FromBinary(byte[] binaryData)
        {
            // 验证输入数据
            if (binaryData == null || binaryData.Length < 4)
                throw new ArgumentNullException(nameof(binaryData), "二进制数据为空或不完整");

            try
            {
                // 1. 解析元信息长度（前4字节）
                int metaLength = BitConverter.ToInt32(binaryData, 0);
                if (metaLength <= 0 || metaLength + 4 > binaryData.Length)
                    throw new FormatException("元信息长度无效");

                // 2. 解析元信息
                string metaInfo = System.Text.Encoding.UTF8.GetString(binaryData, 4, metaLength);
                string[] metaParts = metaInfo.Split('|');
                if (metaParts.Length != 4)
                    throw new FormatException("元信息格式错误");

                // 2.1 解析宽、高、通道数、像素类型
                if (!int.TryParse(metaParts[0], out int width) || width <= 0)
                    throw new FormatException("宽度解析失败");
                if (!int.TryParse(metaParts[1], out int height) || height <= 0)
                    throw new FormatException("高度解析失败");
                if (!int.TryParse(metaParts[2], out int channels) || channels <= 0)
                    throw new FormatException("通道数解析失败");
                string pixelType = metaParts[3];
                int pixelSize = GetPixelSize(pixelType);

                // 3. 提取像素数据
                int pixelDataOffset = 4 + metaLength;
                int pixelDataLength = binaryData.Length - pixelDataOffset;
                int expectedLength = width * height * pixelSize * channels;
                if (pixelDataLength != expectedLength)
                    throw new FormatException($"像素数据长度不匹配（实际：{pixelDataLength}，预期：{expectedLength}）");

                byte[] pixelData = new byte[pixelDataLength];
                Array.Copy(binaryData, pixelDataOffset, pixelData, 0, pixelDataLength);

                // 4. 重建 Halcon 图片
                return ReconstructImage(pixelData, channels, pixelType, width, height);
            }
            catch (HOperatorException ex)
            {
                throw new InvalidOperationException("Halcon 算子执行失败", ex);
            }
        }

        /// <summary>
        /// 从 Halcon 图片中提取像素数据到字节数组
        /// </summary>
        private static byte[] ExtractPixelData(HObject image, int channels, int pixelSize, int width, int height)
        {
            int dataLength = width * height * pixelSize * channels;
            byte[] pixelData = new byte[dataLength];

            if (channels == 1)
            {
                // 单通道图片（如灰度图）
                HOperatorSet.GetImagePointer1(image, out HTuple ptr, out _, out _, out _);
                Marshal.Copy(ptr, pixelData, 0, dataLength);
            }
            else if (channels == 3)
            {
                // 三通道图片（如RGB图）
                HOperatorSet.GetImagePointer3(image, out HTuple ptrR, out HTuple ptrG, out HTuple ptrB, out _, out _, out _);
                int channelLength = width * height * pixelSize;

                // 按 R→G→B 顺序拼接像素数据
                Marshal.Copy(ptrR, pixelData, 0, channelLength);
                Marshal.Copy(ptrG, pixelData, channelLength, channelLength);
                Marshal.Copy(ptrB, pixelData, channelLength * 2, channelLength);
            }
            else
            {
                throw new NotSupportedException($"不支持 {channels} 通道图片（仅支持1或3通道）");
            }

            return pixelData;
        }

        /// <summary>
        /// 从字节数组重建 Halcon 图片
        /// </summary>
        private static HObject ReconstructImage(byte[] pixelData, int channels, string pixelType, int width, int height)
        {
            HObject image = null;
            int pixelSize = GetPixelSize(pixelType);
            int channelLength = width * height * pixelSize;

            try
            {
                if (channels == 1)
                {
                    // 重建单通道图片
                    var handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned); // 固定数组
                    try
                    {
                        HOperatorSet.GenImage1(out image, pixelType, width, height, handle.AddrOfPinnedObject());
                    }
                    finally
                    {
                        handle.Free(); // 手动释放，解除固定（必须执行）
                    }
                }
                else if (channels == 3)
                {
                    // 分离三通道数据
                    byte[] rData = new byte[channelLength];
                    byte[] gData = new byte[channelLength];
                    byte[] bData = new byte[channelLength];

                    Buffer.BlockCopy(pixelData, 0, rData, 0, channelLength);
                    Buffer.BlockCopy(pixelData, channelLength, gData, 0, channelLength);
                    Buffer.BlockCopy(pixelData, channelLength * 2, bData, 0, channelLength);

                    // 固定三个通道的数组
                    var handleR = GCHandle.Alloc(rData, GCHandleType.Pinned);
                    var handleG = GCHandle.Alloc(gData, GCHandleType.Pinned);
                    var handleB = GCHandle.Alloc(bData, GCHandleType.Pinned);

                    try
                    {
                        HOperatorSet.GenImage3(out image, pixelType, width, height,
                            handleR.AddrOfPinnedObject(),
                            handleG.AddrOfPinnedObject(),
                            handleB.AddrOfPinnedObject());
                    }
                    finally
                    {
                        // 必须手动释放所有句柄
                        handleR.Free();
                        handleG.Free();
                        handleB.Free();
                    }
                }
                else
                {
                    throw new NotSupportedException($"不支持 {channels} 通道图片（仅支持1或3通道）");
                }

                return image;
            }
            catch
            {
                // 重建失败时释放资源
                image?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 根据 Halcon 像素类型获取单个像素的字节数
        /// </summary>
        private static int GetPixelSize(string pixelType)
        {
            return pixelType switch
            {
                "byte" => 1,       // 8位无符号整数
                "uint2" => 2,      // 16位无符号整数
                "int2" => 2,       // 16位有符号整数
                "int4" => 4,       // 32位有符号整数
                "real" => 4,       // 32位浮点数
                "real4" => 4,      // 32位浮点数
                "real8" => 8,      // 64位浮点数
                _ => throw new NotSupportedException($"不支持的像素类型: {pixelType}")
            };
        }

        public static BitmapSource ConvertToBitmapSource(HImage halconImage)
        {
            if (halconImage == null || !halconImage.IsInitialized())
                throw new ArgumentException("Invalid Halcon image");

            // 获取图像基本属性
            int width, height;
            string type;
            halconImage.GetImageSize(out width, out height);
            type = halconImage.GetImageType();

            // 根据Halcon类型选择WPF像素格式
            PixelFormat wpfFormat = GetWpfPixelFormat(type);

            // 获取图像数据指针
            IntPtr imagePtr = halconImage.GetImagePointer1(out type, out _, out _);

            // 创建WPF位图
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, wpfFormat, null);
            bitmap.Lock();

            try
            {
                // 计算所需缓冲区大小
                int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
                int stride = width * bytesPerPixel;
                int bufferSize = height * stride;

                // 从Halcon复制数据到WPF位图
                bitmap.WritePixels(
                    new Int32Rect(0, 0, width, height),
                    imagePtr,
                    bufferSize,
                    stride
                );
            }
            finally
            {
                bitmap.Unlock();
            }

            // 处理坐标系差异（如果需要垂直翻转）
            if (IsCoordinateSystemFlipped)
            {
                // 创建翻转后的位图
                TransformedBitmap transformed = new TransformedBitmap();
                transformed.BeginInit();
                transformed.Source = bitmap;  // 使用原始位图作为源
                transformed.Transform = new ScaleTransform(1, -1); // 垂直翻转
                transformed.EndInit();
                return transformed;  // 返回BitmapSource类型
            }

            return bitmap;  // 直接返回WriteableBitmap
        }

        private static PixelFormat GetWpfPixelFormat(string halconType)
        {
            switch (halconType)
            {
                case "byte": return PixelFormats.Gray8;
                case "uint2": return PixelFormats.Gray16;
                case "real": return PixelFormats.Gray32Float;
                case "rgb": return PixelFormats.Rgb24;
                case "rgba": return PixelFormats.Rgba64;
                default:
                    throw new NotSupportedException($"Unsupported Halcon image type: {halconType}");
            }
        }

        // 根据坐标系差异设置（通常需要翻转）
        public static bool IsCoordinateSystemFlipped { get; set; } = true;

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool SaveImage(HObject Image, string path)
        {
            try
            {
                DirectoryInfo directoryInfo = Directory.GetParent(path);
                if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);
                if (Image != null && Image.IsInitialized())
                {
                    HOperatorSet.WriteImage(Image, "png", 0, path);
                    return true;
                }
                else
                {
                    Console.WriteLine("Image is null or not initialized");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }

        }

        /// <summary>
        /// 读取图片
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static HObject ReadImage(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    HOperatorSet.ReadImage(out HObject Image, path);
                    return Image;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
    }

    #region 示例
//    // 1. 读取一张 Halcon 图片（示例）
//HObject originalImage;
//HOperatorSet.ReadImage(out originalImage, "test.png"); // 替换为你的图片路径

//try
//{
//    // 2. 转换为二进制数据
//    byte[] binaryData = HalconImageConverter.ToBinary(originalImage);
//    Console.WriteLine($"转换成功，二进制数据长度：{binaryData.Length} 字节");

//    // 3. 从二进制数据重建图片
//    HObject restoredImage = HalconImageConverter.FromBinary(binaryData);
//    Console.WriteLine("重建图片成功");

//    // 4. 验证重建结果（可选）
//    HOperatorSet.GetImageSize(restoredImage, out int width, out int height);
//    Console.WriteLine($"重建图片尺寸：{width}x{height}");

//    // 5. 显示图片（可选）
//    HOperatorSet.DispObj(restoredImage, HDevWindowStack.GetActive());
//}
//finally
//{
//    // 释放资源（必须手动释放，避免内存泄漏）
//    originalImage.Dispose();
//    restoredImage?.Dispose();
//}
    #endregion

}
