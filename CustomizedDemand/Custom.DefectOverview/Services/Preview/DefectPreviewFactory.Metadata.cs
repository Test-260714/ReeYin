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
	private static string DescribeBitmap(BitmapSource bitmap)
	{
		if (bitmap == null)
		{
			return "null";
		}
		return $"{bitmap.PixelWidth}x{bitmap.PixelHeight}, format={bitmap.Format}";
	}

	private static string DescribeDefect(Result defect)
	{
		if (defect == null)
		{
			return "null";
		}
		string value = (string.IsNullOrWhiteSpace(defect.ClassName) ? "-" : defect.ClassName);
		return $"{value}#{defect.ClassId}";
	}

	private static HObject UnionSegRegion(HObject seg)
	{
		try
		{
			if (seg == null || !seg.IsInitialized())
			{
				return null;
			}
			HOperatorSet.CountObj(seg, out var number);
			if (number.TupleLength() == 0 || number[0].I <= 1)
			{
				return null;
			}
			HOperatorSet.Union1(seg, out var regionUnion);
			return regionUnion;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryGetMetadataBitmap(Result defect, out BitmapSource bitmap)
	{
		bitmap = null;
		if (TryResolveMetadataValue(defect, out var value, "DefectOverview_DisplayTargetBitmap", "XYHD_DisplayTargetBitmap"))
		{
			bitmap = value as BitmapSource;
		}
		return bitmap != null;
	}

	private static int ResolveMetadataInt(Result defect, int fallback, params string[] keys)
	{
		if (!TryResolveMetadataValue(defect, out var value, keys) || value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToInt32(value);
		}
		catch
		{
			return fallback;
		}
	}

	private static double ResolveMetadataDouble(Result defect, double fallback, params string[] keys)
	{
		if (!TryResolveMetadataValue(defect, out var value, keys) || value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToDouble(value);
		}
		catch
		{
			return fallback;
		}
	}

	private static bool TryResolveMetadataDouble(Result defect, out double value, params string[] keys)
	{
		value = 0.0;
		if (!TryResolveMetadataValue(defect, out var value2, keys) || value2 == null)
		{
			return false;
		}
		try
		{
			value = Convert.ToDouble(value2);
			return IsFinite(value);
		}
		catch
		{
			value = 0.0;
			return false;
		}
	}

	private static bool TryResolveMetadataValue(Result defect, out object value, params string[] keys)
	{
		value = null;
		if (defect?.Others == null || keys == null)
		{
			return false;
		}
		foreach (string text in keys)
		{
			if (!string.IsNullOrWhiteSpace(text) && defect.Others.TryGetValue(text, out value))
			{
				return true;
			}
		}
		value = null;
		return false;
	}

    }
}
