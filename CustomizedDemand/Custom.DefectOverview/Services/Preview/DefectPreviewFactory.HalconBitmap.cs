using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;

namespace Custom.DefectOverview.Services
{
    public static partial class DefectPreviewFactory
    {
	private static BitmapSource CreateBitmapFromGray(HImage hImage, int width, int height)
	{
		HImage hImage2 = hImage;
		bool flag = false;
		string text = hImage.GetImageType();
		if (text != "byte")
		{
			try
			{
				hImage2 = hImage.ConvertImageType("byte");
				flag = hImage2 != hImage;
			}
			catch
			{
				hImage2 = hImage;
			}
		}
		HImage hImage3 = hImage2;
		bool flag2 = false;
		try
		{
			int num = width;
			int num2 = height;
			if (width > MaxDisplayWidth || height > MaxDisplayHeight)
			{
				double num3 = Math.Min((double)MaxDisplayWidth / (double)width, (double)MaxDisplayHeight / (double)height);
				num = Math.Max(1, (int)Math.Round((double)width * num3));
				num2 = Math.Max(1, (int)Math.Round((double)height * num3));
				HOperatorSet.ZoomImageSize(hImage2, out var imageZoom, num, num2, "constant");
				try
				{
					hImage3 = new HImage(imageZoom);
					flag2 = true;
				}
				finally
				{
					imageZoom.Dispose();
				}
			}
			string type;
			int width2;
			int height2;
			nint imagePointer = hImage3.GetImagePointer1(out type, out width2, out height2);
			num = width2;
			num2 = height2;
			byte[] array = new byte[num * num2];
			Marshal.Copy(imagePointer, array, 0, array.Length);
			BitmapSource bitmapSource = BitmapSource.Create(num, num2, 96.0, 96.0, PixelFormats.Gray8, null, array, num);
			((Freezable)bitmapSource).Freeze();
			return bitmapSource;
		}
		finally
		{
			if (flag2)
			{
				hImage3.Dispose();
			}
			if (flag)
			{
				hImage2.Dispose();
			}
		}
	}

	private static BitmapSource CreateBitmapFromRgb(HImage hImage, int width, int height)
	{
		HImage hImage2 = hImage;
		bool flag = false;
		try
		{
			int num = width;
			int num2 = height;
			if (width > MaxDisplayWidth || height > MaxDisplayHeight)
			{
				double num3 = Math.Min((double)MaxDisplayWidth / (double)width, (double)MaxDisplayHeight / (double)height);
				num = Math.Max(1, (int)Math.Round((double)width * num3));
				num2 = Math.Max(1, (int)Math.Round((double)height * num3));
				HOperatorSet.ZoomImageSize(hImage, out var imageZoom, num, num2, "constant");
				try
				{
					hImage2 = new HImage(imageZoom);
					flag = true;
				}
				finally
				{
					imageZoom.Dispose();
				}
			}
			HOperatorSet.GetImagePointer3(hImage2, out var pointerRed, out var pointerGreen, out var pointerBlue, out var _, out var width2, out var height2);
			nint source = (nint)pointerRed;
			nint source2 = (nint)pointerGreen;
			nint source3 = (nint)pointerBlue;
			num = width2;
			num2 = height2;
			int num4 = num * num2;
			byte[] array = new byte[num4];
			byte[] array2 = new byte[num4];
			byte[] array3 = new byte[num4];
			Marshal.Copy(source, array, 0, num4);
			Marshal.Copy(source2, array2, 0, num4);
			Marshal.Copy(source3, array3, 0, num4);
			byte[] array4 = new byte[num4 * 3];
			for (int i = 0; i < num4; i++)
			{
				array4[i * 3] = array3[i];
				array4[i * 3 + 1] = array2[i];
				array4[i * 3 + 2] = array[i];
			}
			BitmapSource bitmapSource = BitmapSource.Create(num, num2, 96.0, 96.0, PixelFormats.Bgr24, null, array4, num * 3);
			((Freezable)bitmapSource).Freeze();
			return bitmapSource;
		}
		finally
		{
			if (flag)
			{
				hImage2.Dispose();
			}
		}
	}

	private static HImage NormalizeDisplayImage(HImage hImage, out bool disposeNormalizedImage)
	{
		disposeNormalizedImage = false;
		HImage hImage2 = hImage;
		try
		{
			if (hImage2.CountObj() > 1)
			{
				using HObject obj = hImage2.SelectObj(1);
				hImage2 = new HImage(obj);
				disposeNormalizedImage = true;
			}
			string a = hImage2.GetImageType();
			if (!string.Equals(a, "byte", StringComparison.OrdinalIgnoreCase))
			{
				HImage hImage3 = hImage2.ConvertImageType("byte");
				if (hImage3 != hImage2)
				{
					if (disposeNormalizedImage)
					{
						hImage2.Dispose();
					}
					hImage2 = hImage3;
					disposeNormalizedImage = true;
				}
			}
			return hImage2;
		}
		catch
		{
			if (disposeNormalizedImage)
			{
				hImage2.Dispose();
			}
			throw;
		}
	}
    }
}
