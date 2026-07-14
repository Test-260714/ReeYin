using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
	private bool ShouldUseDualPathInputs()
	{
		return UseDualPathInputs;
	}

	private NodeStatus PublishDualPathInputs()
	{
		bool flag = HasConfiguredInputSelection(LeftInputImage);
		bool flag2 = HasConfiguredInputSelection(LeftInputResults);
		bool flag3 = HasConfiguredInputSelection(RightInputImage);
		bool flag4 = HasConfiguredInputSelection(RightInputResults);
		if (!flag || !flag2 || !flag3 || !flag4)
		{
			PublishStatusText = "双路直连需要同时配置：左路图像、左路结果、右路图像、右路结果。";
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] Error: {PublishStatusText} leftImage={flag}, leftResults={flag2}, rightImage={flag3}, rightResults={flag4}");
			return NodeStatus.Error;
		}
		string text = ResolveString(ResolveSelectedInputValue(InputFrameKey), string.Empty);
		string text2 = ResolveString(ResolveSelectedInputValue(InputFrameIdText), text);
		double laneWidth = ResolveDouble(ResolveSelectedInputValue(InputLaneWidth), LaneWidth);
		double pixelEquivalentX = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentX), PixelEquivalentX);
		double pixelEquivalentY = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentY), PixelEquivalentY);
		double edgeCalibrationX = ResolveDouble(ResolveSelectedInputValue(InputEdgeCalibrationX), EdgeCalibrationX);
		string schemeFilePath = ResolveString(ResolveSelectedInputValue(InputSchemeFilePath), SchemeFilePath);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = BuildDefaultFrameKey();
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = text;
		}
		string sourceName = (string.IsNullOrWhiteSpace(SourceName) ? "DefectOverview" : SourceName);
		List<Result> list = new List<Result>();
		int value = PublishDualPathSide(LeftInputImage, LeftInputResults, DefectOverviewPathRole.Left, ResolveDualPathName(DefectOverviewPathRole.Left), text, text2, laneWidth, pixelEquivalentX, pixelEquivalentY, edgeCalibrationX, schemeFilePath, sourceName, list);
		int value2 = PublishDualPathSide(RightInputImage, RightInputResults, DefectOverviewPathRole.Right, ResolveDualPathName(DefectOverviewPathRole.Right), text, text2, laneWidth, pixelEquivalentX, pixelEquivalentY, edgeCalibrationX, schemeFilePath, sourceName, list);
		PublishedResults = list;
		PublishedCount = list.Count;
		PublishedFrameKey = text;
		PublishStatusText = $"双路直连已发布：左路 {value} 条，右路 {value2} 条，FrameKey={text}";
		LastPublishTime = DateTime.Now;
		if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] DualPath published frameKey={text}, left={value}, right={value2}");
		}
		return NodeStatus.Success;
	}

	private int PublishDualPathSide(TransmitParam imageParam, TransmitParam resultsParam, DefectOverviewPathRole pathRole, string pathName, string frameKey, string frameIdText, double laneWidth, double pixelEquivalentX, double pixelEquivalentY, double edgeCalibrationX, string schemeFilePath, string sourceName, List<Result> allPublishedResults)
	{
		string source;
		object obj = ResolveSelectedInputValue(resultsParam, isDeepClone: false, out source);
		List<Result> list = ExtractResults(obj);
		(HImage, string) tuple = ResolveSelectedImageInput(imageParam, resultsParam);
		using HImage hImage = tuple.Item1;
		if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] DualPathSide path={pathName}, imageSelect={DescribeTransmitParam(imageParam)}, imageSource={tuple.Item2 ?? "none"}, resultsSelect={DescribeTransmitParam(resultsParam)}, resultsSource={source ?? "none"}, rawType={obj?.GetType().FullName ?? "null"}, count={list.Count}");
		}
		List<Result> list2 = BuildPublishedResults(hImage, list, frameKey, frameIdText, laneWidth, pixelEquivalentX, pixelEquivalentY, edgeCalibrationX, schemeFilePath, DefectOverviewFrameLayout.DualPath, pathRole, pathName);
		AttachPreviewMetadata(hImage, list2);
		allPublishedResults?.AddRange(list2);
		ResolveIngestService().PublishPath(new DefectOverviewPathPacket
		{
			SourceName = sourceName,
			FrameKey = frameKey,
			FrameIdText = frameIdText,
			CreatedUtc = DateTime.UtcNow,
			FrameLayout = DefectOverviewFrameLayout.DualPath,
			PathRole = pathRole,
			PathName = pathName,
			PathImage = hImage,
			OriginalImage = hImage,
			ApplyPostProcess = false,
			SaveLocalDefectImages = SaveLocalDefectImages,
			IsNg = (list2.Count > 0),
			Results = list2,
			LaneWidth = ((laneWidth > 0.0) ? new double?(laneWidth) : ((double?)null)),
			PixelEquivalentX = ((pixelEquivalentX > 0.0) ? new double?(pixelEquivalentX) : ((double?)null)),
			PixelEquivalentY = ((pixelEquivalentY > 0.0) ? new double?(pixelEquivalentY) : ((double?)null)),
			EdgeCalibrationX = edgeCalibrationX,
			SchemeFilePath = (schemeFilePath ?? string.Empty)
		});
		if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] DualPathSide role={pathRole}, path={pathName}, image={DescribeImageState(hImage)}, source={tuple.Item2}, input={list.Count}, published={list2.Count}");
		}
		return list2.Count;
	}

	private void ClearLegacyOutputs()
	{
		ObservableCollection<TransmitParam> outputParams = base.OutputParams;
		if (outputParams != null && outputParams.Count > 0)
		{
			base.OutputParams.Clear();
		}
		ModuleParam moduleParam;
		if (moduleOutputParam == null)
		{
			moduleParam = (moduleOutputParam = new ModuleParam());
		}
		moduleParam = moduleOutputParam;
		if (moduleParam.TransmitParams == null)
		{
			Dictionary<string, object> dictionary = (moduleParam.TransmitParams = new Dictionary<string, object>());
		}
		moduleOutputParam.TransmitParams.Clear();
	}

    }
}
