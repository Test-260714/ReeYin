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
	private LegendStyleAssignment ResolveLegendStyleLocked(HistoryItem item)
	{
		if (item == null)
		{
			return ResolveOrCreateLegendStyleLocked(null, 0);
		}
		if (!string.IsNullOrWhiteSpace(item.LegendKey) && _legendStyles.TryGetValue(item.LegendKey, out var value))
		{
			if (string.IsNullOrWhiteSpace(value.ClassName) && !string.IsNullOrWhiteSpace(item.ClassName))
			{
				value.ClassName = item.ClassName;
			}
			return value;
		}
		return ResolveOrCreateLegendStyleLocked(item.ClassName, item.ClassId);
	}

	private LegendStyleAssignment ResolveOrCreateLegendStyleLocked(string className, int classId)
	{
		string text = BuildLegendKey(className, classId);
		if (_legendStyles.TryGetValue(text, out var value))
		{
			if (string.IsNullOrWhiteSpace(value.ClassName))
			{
				value.ClassName = ResolveLegendDisplayName(className, classId);
			}
			return value;
		}
		(string, string) tuple = LegendStyleTemplates[_legendStyles.Count % LegendStyleTemplates.Length];
		value = new LegendStyleAssignment
		{
			LegendKey = text,
			ClassName = ResolveLegendDisplayName(className, classId),
			MarkerKind = tuple.Item1,
			Fill = CreateFrozenBrush(tuple.Item2),
			Stroke = CreateFrozenBrush("#334155"),
			DisplayOrder = _legendStyles.Count
		};
		_legendStyles[text] = value;
		return value;
	}

	private static string BuildLegendKey(string className, int classId)
	{
		if (classId != 0)
		{
			return $"ID:{classId}";
		}
		return "NAME:" + (className ?? "Unknown").Trim();
	}

	private static string ResolveLegendDisplayName(string className, int classId)
	{
		if (!string.IsNullOrWhiteSpace(className))
		{
			return className;
		}
		return (classId != 0) ? $"Class_{classId}" : "Unknown";
	}

	private static Brush ResolvePathBrush(string pathName)
	{
		Color color = (IsLeftPathName(pathName) ? ((Color)ColorConverter.ConvertFromString("#0F766E")) : ((Color)ColorConverter.ConvertFromString("#B45309")));
		SolidColorBrush solidColorBrush = new SolidColorBrush(color);
		((Freezable)solidColorBrush).Freeze();
		return solidColorBrush;
	}

	private static bool IsLeftPathName(string pathName)
	{
		if (string.IsNullOrWhiteSpace(pathName))
		{
			return false;
		}
		if (IsSchemeCLeftPathName(pathName))
		{
			return true;
		}
		return pathName.Contains("左", StringComparison.OrdinalIgnoreCase) || pathName.Contains("left", StringComparison.OrdinalIgnoreCase) || pathName.Contains("path1", StringComparison.OrdinalIgnoreCase) || pathName.Contains("lane1", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsRightPathName(string pathName)
	{
		if (string.IsNullOrWhiteSpace(pathName))
		{
			return false;
		}
		if (IsSchemeCRightPathName(pathName))
		{
			return true;
		}
		return pathName.Contains("右", StringComparison.OrdinalIgnoreCase) || pathName.Contains("right", StringComparison.OrdinalIgnoreCase) || pathName.Contains("path2", StringComparison.OrdinalIgnoreCase) || pathName.Contains("lane2", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSchemeCLeftPathName(string pathName)
	{
		return IsSchemeCSidePathName(pathName, "-L");
	}

	private static bool IsSchemeCRightPathName(string pathName)
	{
		return IsSchemeCSidePathName(pathName, "-R");
	}

	private static bool IsSchemeCSidePathName(string pathName, string suffix)
	{
		if (string.IsNullOrWhiteSpace(pathName) || string.IsNullOrWhiteSpace(suffix))
		{
			return false;
		}

		string text = pathName.Trim();
		return (text.StartsWith("方案C", StringComparison.OrdinalIgnoreCase)
				|| text.StartsWith("多相机", StringComparison.OrdinalIgnoreCase)
				|| TryResolveGroupedDualCameraGroupKey(text, out _))
			&& text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryResolveGroupedDualCameraGroupKey(string pathName, out string groupKey)
	{
		groupKey = string.Empty;
		if (string.IsNullOrWhiteSpace(pathName))
		{
			return false;
		}

		string text = pathName.Trim();
		int startIndex = 0;
		if (text.Length >= 2 && (text[0] == 'G' || text[0] == 'g'))
		{
			startIndex = 1;
		}

		if (text.Length <= startIndex || !char.IsDigit(text[startIndex]))
		{
			return false;
		}

		int digitCount = 0;
		for (int i = startIndex; i < text.Length && char.IsDigit(text[i]); i++)
		{
			digitCount++;
		}

		if (digitCount == 0)
		{
			return false;
		}

		int endIndex = startIndex + digitCount;
		if (text.Length > endIndex)
		{
			string suffix = text[endIndex..].Trim();
			if (!string.Equals(suffix, "-L", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(suffix, "-R", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		groupKey = NormalizeGroupedDualCameraGroupKey(text[..endIndex]);
		return !string.IsNullOrWhiteSpace(groupKey);
	}

	private static string NormalizeGroupedDualCameraGroupKey(string groupKey)
	{
		if (string.IsNullOrWhiteSpace(groupKey))
		{
			return string.Empty;
		}

		string text = groupKey.Trim();
		if (text.Length >= 2
			&& (text[0] == 'G' || text[0] == 'g')
			&& int.TryParse(text[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
			&& index >= 0
			&& index <= 99)
		{
			return $"{index:D2}";
		}

		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericIndex)
			&& numericIndex >= 0
			&& numericIndex <= 99)
		{
			return $"{numericIndex:D2}";
		}

		return text.ToUpperInvariant();
	}

	private static Brush CreateFrozenBrush(string color)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		((Freezable)solidColorBrush).Freeze();
		return solidColorBrush;
	}
    }
}
