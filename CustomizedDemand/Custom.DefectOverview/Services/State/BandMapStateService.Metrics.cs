using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Custom.DefectOverview.Models;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;

namespace Custom.DefectOverview.Services
{
    public sealed partial class BandMapStateService : IBandMapStateService
    {
	private static double? TryGetResultPhysicalCoordinate(Result result, string key)
	{
		if (result?.Others == null || string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		if (!result.Others.TryGetValue(key, out var value) || value == null)
		{
			return null;
		}
		double coordinate;
		return TryConvertToDouble(value, out coordinate) ? new double?(coordinate) : ((double?)null);
	}

	private static string ResolveCoordinateSourceText(Result result)
	{
		if (result?.Others == null)
		{
			return string.Empty;
		}
		if (!result.Others.TryGetValue("DefectPostProcess.CoordinateSource", out var value))
		{
			return string.Empty;
		}
		return ResolveCoordinateSourceDisplayText(value?.ToString());
	}

	private static string ResolveCoordinateSourceDisplayText(string coordinateSource)
	{
		if (string.IsNullOrWhiteSpace(coordinateSource))
		{
			return string.Empty;
		}
		string text = coordinateSource.Trim();
		if (1 == 0)
		{
		}
		string result = ((text == "CalibrationFile") ? "标定文件" : ((!(text == "PixelEquivalent")) ? coordinateSource.Trim() : "像素当量"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool IsFiniteNullable(double? value)
	{
		return value.HasValue && double.IsFinite(value.Value);
	}

	private static bool TryConvertToDouble(object value, out double coordinate)
	{
		if (!(value is double num))
		{
			if (!(value is float num2))
			{
				if (value is decimal num3)
				{
					decimal num4 = num3;
					coordinate = (double)num4;
					return double.IsFinite(coordinate);
				}
				if (value is int num5)
				{
					int num6 = num5;
					coordinate = num6;
					return true;
				}
				if (value is long num7)
				{
					long num8 = num7;
					coordinate = num8;
					return true;
				}
				if (value is short num9)
				{
					short num10 = num9;
					coordinate = num10;
					return true;
				}
				if (value is byte b)
				{
					byte b2 = b;
					coordinate = (int)b2;
					return true;
				}
				if (value is string text)
				{
					string s = text;
					if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out coordinate))
					{
						return double.IsFinite(coordinate);
					}
					string s2 = text;
					if (double.TryParse(s2, NumberStyles.Float, CultureInfo.CurrentCulture, out coordinate))
					{
						return double.IsFinite(coordinate);
					}
				}
			}
			else
			{
				float num11 = num2;
				if (float.IsFinite(num11))
				{
					coordinate = num11;
					return true;
				}
			}
		}
		else
		{
			double num12 = num;
			if (double.IsFinite(num12))
			{
				coordinate = num12;
				return true;
			}
		}
		coordinate = 0.0;
		return false;
	}

	private static double? GetMaxFiniteOrNull(IEnumerable<double?> values)
	{
		if (values == null)
		{
			return null;
		}
		double? result = null;
		foreach (double? value in values)
		{
			if (IsFiniteNullable(value))
			{
				result = ((!result.HasValue) ? value.Value : Math.Max(result.Value, value.Value));
			}
		}
		return result;
	}

	private static bool IsNgFrame(BandMapFrameInput frame)
	{
		if (frame == null)
		{
			return false;
		}
		return IsNgResult(frame.Left?.ResultText) || IsNgResult(frame.Right?.ResultText);
	}

	private static bool IsNgResult(string resultText)
	{
		if (string.IsNullOrWhiteSpace(resultText))
		{
			return false;
		}
		return string.Equals(resultText, "NG", StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveCurrentStatusCode(DateTime lastFrameUtc, string path1Result, string path2Result)
	{
		if (lastFrameUtc == DateTime.MinValue)
		{
			return "Idle";
		}
		if (IsNgResult(path1Result) || IsNgResult(path2Result))
		{
			return "NG";
		}
		if (string.Equals(path1Result, "OK", StringComparison.OrdinalIgnoreCase) || string.Equals(path2Result, "OK", StringComparison.OrdinalIgnoreCase))
		{
			return "OK";
		}
		return "Idle";
	}

	private static string ResolveCurrentStatusText(string statusCode)
	{
		if (1 == 0)
		{
		}
		string result = ((statusCode == "NG") ? "NG" : ((!(statusCode == "OK")) ? "未运行" : "OK"));
		if (1 == 0)
		{
		}
		return result;
	}

    }
}
