using HalconDotNet;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Services
{
    internal static partial class DefectPreviewFactory
    {
        private static BitmapSource CreateBitmapFromGray(HImage hImage, int width, int height)
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

                var bitmap = BitmapSource.Create(displayWidth, displayHeight, 96, 96, PixelFormats.Gray8, null, imageData, displayWidth);
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

        private static BitmapSource CreateBitmapFromRgb(HImage hImage, int width, int height)
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

                HOperatorSet.GetImagePointer3(displayImage,
                    out HTuple hvPtrR, out HTuple hvPtrG, out HTuple hvPtrB,
                    out HTuple _, out HTuple hvW, out HTuple hvH);

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
                    bgr[i * 3] = b[i];
                    bgr[i * 3 + 1] = g[i];
                    bgr[i * 3 + 2] = r[i];
                }

                var bitmap = BitmapSource.Create(displayWidth, displayHeight, 96, 96, PixelFormats.Bgr24, null, bgr, displayWidth * 3);
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
