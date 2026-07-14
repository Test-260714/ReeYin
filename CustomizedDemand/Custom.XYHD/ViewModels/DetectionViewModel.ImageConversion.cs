using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Custom.XYHD.Models;
using Custom.XYHD.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {
        /// <summary>
        /// 将 HImage 转为 WPF BitmapSource，支持单通道灰度和三通道 RGB
        /// </summary>
        private BitmapSource CreateBitmapFromHImage(HImage hImage)
        {
            try
            {
                if (hImage == null || !hImage.IsInitialized())
                    return null;

                hImage.GetImageSize(out int width, out int height);
                int channels = hImage.CountChannels();

                if (channels == 3)
                    return CreateBitmapFromRGB(hImage, width, height);
                else
                    return CreateBitmapFromGray(hImage, width, height);
            }
            catch (Exception ex)
            {
                Model.AddLog($"CreateBitmap异常: {ex.Message}", "WARN");
                return null;
            }
        }

        private BitmapSource CreateBitmapFromGray(HImage hImage, int width, int height)
        {
            HImage grayImage = hImage;
            bool disposeGrayImage = false;
            var type = hImage.GetImageType();
            if (type != "byte")
            {
                try
                {
                    grayImage = hImage.ConvertImageType("byte");
                    disposeGrayImage = !ReferenceEquals(grayImage, hImage);
                }
                catch
                {
                    grayImage = hImage;
                }
            }

            HImage displayImage = grayImage;
            bool disposeDisplayImage = false;
            try
            {
                int displayWidth = width;
                int displayHeight = height;
                if (width > MaxDisplayWidth || height > MaxDisplayHeight)
                {
                    var scale = Math.Min((double)MaxDisplayWidth / width, (double)MaxDisplayHeight / height);
                    displayWidth = Math.Max(1, (int)Math.Round(width * scale));
                    displayHeight = Math.Max(1, (int)Math.Round(height * scale));

                    HOperatorSet.ZoomImageSize(grayImage, out HObject zoomedObj, displayWidth, displayHeight, "constant");
                    try
                    {
                        displayImage = new HImage(zoomedObj);
                        disposeDisplayImage = true;
                    }
                    finally
                    {
                        zoomedObj.Dispose();
                    }
                }

                var ptr = displayImage.GetImagePointer1(out string _, out int ptrW, out int ptrH);
                displayWidth = ptrW;
                displayHeight = ptrH;

                var imageData = new byte[displayWidth * displayHeight];
                Marshal.Copy(ptr, imageData, 0, imageData.Length);

                var bitmap = BitmapSource.Create(
                    displayWidth, displayHeight, 96, 96,
                    PixelFormats.Gray8, null, imageData, displayWidth);
                bitmap.Freeze();
                return bitmap;
            }
            finally
            {
                if (disposeDisplayImage)
                    displayImage.Dispose();
                if (disposeGrayImage)
                    grayImage.Dispose();
            }
        }

        private BitmapSource CreateBitmapFromRGB(HImage hImage, int width, int height)
        {
            HImage displayImage = hImage;
            bool disposeDisplayImage = false;
            try
            {
                int displayWidth = width;
                int displayHeight = height;
                if (width > MaxDisplayWidth || height > MaxDisplayHeight)
                {
                    var scale = Math.Min((double)MaxDisplayWidth / width, (double)MaxDisplayHeight / height);
                    displayWidth = Math.Max(1, (int)Math.Round(width * scale));
                    displayHeight = Math.Max(1, (int)Math.Round(height * scale));

                    HOperatorSet.ZoomImageSize(hImage, out HObject zoomedObj, displayWidth, displayHeight, "constant");
                    try
                    {
                        displayImage = new HImage(zoomedObj);
                        disposeDisplayImage = true;
                    }
                    finally
                    {
                        zoomedObj.Dispose();
                    }
                }

                // 分解三通道（使用项目中统一的 HOperatorSet 调用方式）
                HOperatorSet.GetImagePointer3(displayImage,
                    out HTuple hvPtrR, out HTuple hvPtrG, out HTuple hvPtrB,
                    out HTuple hvType, out HTuple hvW, out HTuple hvH);

                IntPtr ptrR = hvPtrR;
                IntPtr ptrG = hvPtrG;
                IntPtr ptrB = hvPtrB;
                displayWidth = hvW;
                displayHeight = hvH;

                int pixelCount = displayWidth * displayHeight;
                var r = new byte[pixelCount];
                var g = new byte[pixelCount];
                var b = new byte[pixelCount];
                Marshal.Copy(ptrR, r, 0, pixelCount);
                Marshal.Copy(ptrG, g, 0, pixelCount);
                Marshal.Copy(ptrB, b, 0, pixelCount);

                var bgr = new byte[pixelCount * 3];
                for (int i = 0; i < pixelCount; i++)
                {
                    bgr[i * 3 + 0] = b[i];
                    bgr[i * 3 + 1] = g[i];
                    bgr[i * 3 + 2] = r[i];
                }

                var stride = displayWidth * 3;
                var bitmap = BitmapSource.Create(
                    displayWidth, displayHeight, 96, 96,
                    PixelFormats.Bgr24, null, bgr, stride);
                bitmap.Freeze();
                return bitmap;
            }
            finally
            {
                if (disposeDisplayImage)
                    displayImage.Dispose();
            }
        }
    }
}
