using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {
        private const int MaxRetiredPacketBatches = 6;

        [JsonIgnore]
        private List<XYHDInputPacket> _publishedPackets = new List<XYHDInputPacket>();

        [JsonIgnore]
        private readonly Queue<List<XYHDInputPacket>> _retiredPacketBatches = new();

        [JsonIgnore]
        [OutputParam(DetectionOutputParamName, DetectionOutputParamDescription)]
        public List<XYHDInputPacket> PublishedPackets
        {
            get => _publishedPackets ??= new List<XYHDInputPacket>();
            private set => ReplacePublishedPackets(value);
        }

        private void ReplacePublishedPackets(List<XYHDInputPacket> value)
        {
            var next = value ?? new List<XYHDInputPacket>();
            var previous = _publishedPackets;
            if (ReferenceEquals(previous, next))
            {
                SetProperty(ref _publishedPackets, next);
                return;
            }

            SetProperty(ref _publishedPackets, next);
            DisposePacketBatch(previous);
        }

        private void RetirePacketBatch(List<XYHDInputPacket> packets)
        {
            if (packets == null || packets.Count == 0)
                return;

            _retiredPacketBatches.Enqueue(packets);
            while (_retiredPacketBatches.Count > MaxRetiredPacketBatches)
                DisposePacketBatch(_retiredPacketBatches.Dequeue());
        }

        private static void DisposePacketBatch(IEnumerable<XYHDInputPacket> packets)
        {
            if (packets == null)
                return;

            foreach (var packet in packets)
                packet?.Dispose();
        }

        private static XYHDInputPacket CreateOutputPacketWithoutImages(XYHDInputPacket packet)
        {
            if (packet == null)
                return null;

            return new XYHDInputPacket
            {
                OriginalImage = null,
                PathImage = null,
                IsOks = packet.IsOks,
                DefectResults = packet.DefectResults,
                FrameId = packet.FrameId,
                FrameIdText = packet.FrameIdText,
                ReceiveTime = packet.ReceiveTime,
                SourceSerial = packet.SourceSerial,
                OwnerSerial = packet.OwnerSerial,
                HasFieldOrientationSettings = packet.HasFieldOrientationSettings,
                SwapLeftRightPaths = packet.SwapLeftRightPaths,
                LeftPathXMirror = packet.LeftPathXMirror,
                RightPathXMirror = packet.RightPathXMirror,
                PathName = packet.PathName
            };
        }

        [JsonIgnore]
        private bool _leftNgOutput;

        [JsonIgnore]
        [OutputParam(LeftNgOutputParamName, LeftNgOutputParamDescription)]
        public bool LeftNgOutput
        {
            get => _leftNgOutput;
            private set => SetProperty(ref _leftNgOutput, value);
        }

        [JsonIgnore]
        private bool _rightNgOutput;

        [JsonIgnore]
        [OutputParam(RightNgOutputParamName, RightNgOutputParamDescription)]
        public bool RightNgOutput
        {
            get => _rightNgOutput;
            private set => SetProperty(ref _rightNgOutput, value);
        }

        [JsonIgnore]
        private bool _isNgOutput;

        [JsonIgnore]
        [OutputParam(IsNgOutputParamName, IsNgOutputParamDescription)]
        public bool IsNgOutput
        {
            get => _isNgOutput;
            private set => SetProperty(ref _isNgOutput, value);
        }

        public void EnsureDefaultOutputParam(Guid linkGuid, string parentNode)
        {
            OutputParams ??= new ObservableCollection<TransmitParam>();

            EnsureOutputParam(linkGuid, parentNode, DetectionOutputParamName, DetectionOutputParamDescription, DataType._object, nameof(PublishedPackets), PublishedPackets);
            EnsureOutputParam(linkGuid, parentNode, LeftNgOutputParamName, LeftNgOutputParamDescription, DataType.Bool, nameof(LeftNgOutput), LeftNgOutput);
            EnsureOutputParam(linkGuid, parentNode, RightNgOutputParamName, RightNgOutputParamDescription, DataType.Bool, nameof(RightNgOutput), RightNgOutput);
            EnsureOutputParam(linkGuid, parentNode, IsNgOutputParamName, IsNgOutputParamDescription, DataType.Bool, nameof(IsNgOutput), IsNgOutput);
        }

        public void SetNgOutputs(bool leftNg, bool rightNg, bool updateParam = true)
        {
            LeftNgOutput = leftNg;
            RightNgOutput = rightNg;
            IsNgOutput = leftNg || rightNg;

            EnsureDefaultOutputParam(Guid, Name);
            RefreshPublishedPacketsOutputValue();

            if (updateParam)
                UpdateParam();
        }

        private TransmitParam EnsureOutputParam(
            Guid linkGuid,
            string parentNode,
            string paramName,
            string description,
            DataType type,
            string propertyName,
            object value)
        {
            var output = OutputParams.FirstOrDefault(param => IsNamedOutputParam(param, paramName));
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
            output.Describe = description;
            output.Type = type;
            output.Resourece = ResoureceType.Output;
            output.ResourcePath = typeof(DetectionModel).FullName + "." + propertyName;
            output.Value = value;

            return output;
        }

        private static bool IsDetectionOutputParam(TransmitParam param)
        {
            return IsNamedOutputParam(param, DetectionOutputParamName);
        }

        private static bool IsNamedOutputParam(TransmitParam param, string paramName)
        {
            return param != null
                && (NameMatch(param.ParamName, paramName)
                    || NameMatch(param.Name, paramName));
        }

        private void RefreshPublishedPacketsOutputValue()
        {
            foreach (var output in OutputParams?.Where(IsDetectionOutputParam) ?? Enumerable.Empty<TransmitParam>())
            {
                output.Value = PublishedPackets;
                output.Serial = Serial;
                output.Resourece = ResoureceType.Output;
            }

            RefreshOutputValue(LeftNgOutputParamName, LeftNgOutput);
            RefreshOutputValue(RightNgOutputParamName, RightNgOutput);
            RefreshOutputValue(IsNgOutputParamName, IsNgOutput);
        }

        private void RefreshOutputValue(string paramName, object value)
        {
            foreach (var output in OutputParams?.Where(param => IsNamedOutputParam(param, paramName)) ?? Enumerable.Empty<TransmitParam>())
            {
                output.Value = value;
                output.Serial = Serial;
                output.Resourece = ResoureceType.Output;
            }
        }
    }
}
