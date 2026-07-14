using System;
using System.Runtime.InteropServices;

namespace SImagePro
{
    /// <summary>
    /// 点云头结构(zInterval初始化为 1e-5)
    /// Point cloud head structure (zInterval initialized to 1e-5)
    /// </summary>
    public struct SPointCloudHead
    {
        public uint height;         // 点云行数 / Number of point cloud rows
        public uint width;          // 点云列数 / Number of point cloud columns
        public double xInterval;    // 点云列间距 / Point cloud column spacing
        public double yInterval;    // 点云行间距 / Point cloud row spacing
        public double zInterval;    // 点云z方向分辨率系数 / Point cloud z-direction resolution coefficient

        public SPointCloudHead(uint height, uint width, double xInterval, double yInterval, double zInterval)
        {
            this.height = height;
            this.width = width;
            this.xInterval = xInterval;
            this.yInterval = yInterval;
            this.zInterval = zInterval;
        }
    }

    /// <summary>
    /// SImagePro库函数封装
    /// SImagePro library function wrapper
    /// </summary>
    public static class SCV
    {
        /// <summary>
        /// 保存32位Tiff格式
        /// Save 32-bit Tiff format
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">高度数据 / Height data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Save32Tif(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /// <summary>
        /// 保存16位Tiff格式（从16位数据）
        /// Save 16-bit Tiff format (from 16-bit data)
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">高度数据 / Height data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <param name="z_scale">缩放比例 / Scale ratio</param>
        /// <param name="isSigned">是否有符号 / Is signed</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Save16TifOfShort(char[] file, short[] heightImage, SPointCloudHead pcHead, double z_scale, int isSigned);

        /// <summary>
        /// 保存16位Tiff格式（从32位数据转换）
        /// Save 16-bit Tiff format (converted from 32-bit data)
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">高度数据 / Height data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <param name="z_scale">缩放比例 / Scale ratio</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Save16TifOfScale(char[] file, int[] heightImage, SPointCloudHead pcHead, double z_scale);

        /// <summary>
        /// 保存PCD格式
        /// Save PCD format
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">高度数据 / Height data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SavePcd(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /// <summary>
        /// 保存PLY格式
        /// Save PLY format
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">高度数据 / Height data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SavePly(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /// <summary>
        /// 保存Bmp图片
        /// Save Bmp image
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="imageData">图像数据 / Image data</param>
        /// <param name="width">图像宽度 / Image width</param>
        /// <param name="height">图像高度 / Image height</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SaveBmp(char[] file, byte[] imageData, int width, int height);

        /// <summary>
        /// 保存编码器数据
        /// Save encoder data
        /// </summary>
        /// <param name="file">文件名 / File name</param>
        /// <param name="heightImage">编码器数据 / Encoder data</param>
        /// <param name="pcHead">点云头文件 / Point cloud head</param>
        /// <returns>小于0: 失败, 等于0: 成功 / Less than 0: failure, equal to 0: success</returns>
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WriteEcd(char[] file, int[] heightImage, SPointCloudHead pcHead);
    }
}
