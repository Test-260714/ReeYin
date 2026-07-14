using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
	private static long _defaultFrameKeySequence;

	private string BuildDefaultFrameKey()
	{
		long sequence = Interlocked.Increment(ref _defaultFrameKeySequence);
		return $"{(string.IsNullOrWhiteSpace(SourceName) ? "DefectOverview" : SourceName)}_{DateTime.Now:yyyyMMddHHmmssfffffff}_{base.Serial}_{sequence}";
	}

	private static List<Result> ExtractResults(object value)
	{
		if (value == null)
		{
			return new List<Result>();
		}
		if (value is TransmitParam transmitParam)
		{
			return ExtractResults(transmitParam.Value);
		}
		if (value is Result item)
		{
			return new List<Result> { item };
		}
		if (TryExtractObjectResults(value, out List<Result> objectResults))
		{
			return objectResults;
		}
		if (value is IEnumerable<Result> source)
		{
			return source.Where((Result result) => result != null).ToList();
		}
		if (value is IEnumerable enumerable)
		{
			List<Result> list = new List<Result>();
			foreach (object item3 in enumerable)
			{
				if (item3 is Result item2)
				{
					list.Add(item2);
				}
				else if (item3 is IEnumerable<Result> source2)
				{
					list.AddRange(source2.Where((Result entry) => entry != null));
				}
				else if (item3 is TransmitParam transmitParam2)
				{
					list.AddRange(ExtractResults(transmitParam2.Value));
				}
				else if (TryExtractObjectResults(item3, out List<Result> objectResults2))
				{
					list.AddRange(objectResults2);
				}
				else if (item3 is IEnumerable value2 && !(item3 is string))
				{
					list.AddRange(ExtractResults(value2));
				}
			}
			return list;
		}
		return new List<Result>();
	}

	private static bool TryExtractObjectResults(object value, out List<Result> results)
	{
		results = null;
		if (value == null || value is string)
		{
			return false;
		}

		Type type = value.GetType();
		foreach (string memberName in new[] { "DefectResults", "Results" })
		{
			PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.CanRead)
			{
				object nested = property.GetValue(value);
				if (!ReferenceEquals(nested, value))
				{
					results = ExtractResults(nested);
					return true;
				}
			}

			FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
			if (field != null)
			{
				object nested = field.GetValue(value);
				if (!ReferenceEquals(nested, value))
				{
					results = ExtractResults(nested);
					return true;
				}
			}
		}

		return false;
	}

	private static string ResolveString(object value, string fallback)
	{
		if (value == null)
		{
			return fallback;
		}
		string text = Convert.ToString(value, CultureInfo.InvariantCulture);
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

	private static double ResolveDouble(object value, double fallback)
	{
		if (value == null)
		{
			return fallback;
		}
		try
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return fallback;
		}
	}

	private IDefectOverviewIngestService ResolveIngestService()
	{
		if (_ingestService == null)
		{
			_ingestService = PrismProvider.Container.Resolve(typeof(IDefectOverviewIngestService)) as IDefectOverviewIngestService;
		}
		if (_ingestService == null)
		{
			throw new InvalidOperationException("IDefectOverviewIngestService is not registered.");
		}
		return _ingestService;
	}

	private IDefectOverviewPostProcessService ResolvePostProcessService()
	{
		if (_postProcessService == null)
		{
			_postProcessService = PrismProvider.Container.Resolve(typeof(IDefectOverviewPostProcessService)) as IDefectOverviewPostProcessService;
		}
		if (_postProcessService == null)
		{
			throw new InvalidOperationException("IDefectOverviewPostProcessService is not registered.");
		}
		return _postProcessService;
	}
    }
}
