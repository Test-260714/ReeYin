using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel
    {
        private const string ResultsOutputParamName = "Results";
        private const string CountOutputParamName = "Count";
        private const string FrameKeyOutputParamName = "FrameKey";
        private const string StatusTextOutputParamName = "StatusText";
        private const string PublishTimeOutputParamName = "PublishTime";
        private const string SnOutputParamName = "SN";
        private const string DetectMetersOutputParamName = "DetectMeters";
        private const string IsRollCompletedOutputParamName = "IsRollCompleted";
        private const string PacketOutputParamName = "DefectOverviewPacket";

        public void EnsureDefaultOutputParams(Guid linkGuid, string parentNode)
        {
            OutputParams ??= new ObservableCollection<TransmitParam>();

            EnsureOutputParam(linkGuid, parentNode, ResultsOutputParamName, DataType._object, nameof(PublishedResults), PublishedResults);
            EnsureOutputParam(linkGuid, parentNode, CountOutputParamName, DataType.Int, nameof(PublishedCount), PublishedCount);
            EnsureOutputParam(linkGuid, parentNode, FrameKeyOutputParamName, DataType.String, nameof(PublishedFrameKey), PublishedFrameKey);
            EnsureOutputParam(linkGuid, parentNode, StatusTextOutputParamName, DataType.String, nameof(PublishStatusText), PublishStatusText);
            EnsureOutputParam(linkGuid, parentNode, PublishTimeOutputParamName, DataType.Datetime, nameof(LastPublishTime), LastPublishTime);
            EnsureOutputParam(linkGuid, parentNode, SnOutputParamName, DataType.String, nameof(PublishedSN), PublishedSN);
            EnsureOutputParam(linkGuid, parentNode, DetectMetersOutputParamName, DataType.Double, nameof(PublishedDetectMeters), PublishedDetectMeters);
            EnsureOutputParam(linkGuid, parentNode, IsRollCompletedOutputParamName, DataType.Bool, nameof(PublishedIsRollCompleted), PublishedIsRollCompleted);
            EnsureOutputParam(linkGuid, parentNode, PacketOutputParamName, DataType.Dict, nameof(PublishedPacket), PublishedPacket);
        }

        private TransmitParam EnsureOutputParam(Guid linkGuid, string parentNode, string paramName, DataType type, string propertyName, object value)
        {
            TransmitParam output = OutputParams.FirstOrDefault(param => IsNamedOutputParam(param, paramName));
            if (output == null)
            {
                output = new TransmitParam
                {
                    Guid = Guid.NewGuid(),
                    IsGlobal = false
                };
                OutputParams.Add(output);
            }

            output.LinkGuid = linkGuid;
            output.Serial = Serial;
            output.ParentNode = parentNode;
            output.Name = paramName;
            output.ParamName = paramName;
            output.Type = type;
            output.Resourece = ResoureceType.Output;
            output.ResourcePath = typeof(DefectOverviewPublishModel).FullName + "." + propertyName;
            output.Value = value;

            return output;
        }

        private bool UpdatePublishedOutputParams()
        {
            try
            {
                EnsureDefaultOutputParams(Guid, Name);

                Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
                foreach (TransmitParam output in OutputParams ?? Enumerable.Empty<TransmitParam>())
                {
                    if (output == null)
                    {
                        continue;
                    }

                    string paramName = string.IsNullOrWhiteSpace(output.ParamName)
                        ? output.Name
                        : output.ParamName;
                    if (string.IsNullOrWhiteSpace(paramName) || !values.TryGetValue(paramName, out object value))
                    {
                        continue;
                    }

                    output.Value = value;
                    output.Serial = Serial;
                    output.ParentNode = Name;
                    output.Resourece = ResoureceType.Output;
                }

                return UpdateParam();
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] Update outputs failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsNamedOutputParam(TransmitParam param, string paramName)
        {
            return param != null
                && (NameMatch(param.ParamName, paramName)
                    || NameMatch(param.Name, paramName));
        }

        private static bool NameMatch(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
