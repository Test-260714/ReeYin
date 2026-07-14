using Custom.DefectOverview.Models.Common;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Custom.DefectOverview.Models.GroupedDualCamera
{
    /// <summary>
    /// 多组双相机中的一行结果绑定：后处理结果源 -> 检测组 -> 左/右侧。
    /// </summary>
    [Serializable]
    public sealed class GroupedDualCameraBinding : INotifyPropertyChanged
    {
        private int _sortIndex;
        private int _sourceSerial = -1;
        private string _sourceCameraName = string.Empty;
        private string _sourceOutputName = string.Empty;
        private string _groupKey = string.Empty;
        private string _groupName = string.Empty;
        private WidthSide _side = WidthSide.Unknown;
        private string _displayName = string.Empty;
        private TransmitParam _resultInput = new();
        private TransmitParam _imageInput = new();
        private bool _isRequired = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public int SortIndex
        {
            get => _sortIndex;
            set => SetField(ref _sortIndex, value);
        }

        public int SourceSerial
        {
            get => _sourceSerial;
            set => SetField(ref _sourceSerial, value);
        }

        public string SourceCameraName
        {
            get => _sourceCameraName;
            set => SetField(ref _sourceCameraName, value ?? string.Empty);
        }

        public string SourceOutputName
        {
            get => _sourceOutputName;
            set => SetField(ref _sourceOutputName, value ?? string.Empty);
        }

        public string GroupKey
        {
            get => _groupKey;
            set
            {
                string previousDefaultName = BuildDefaultDisplayName();
                if (SetField(ref _groupKey, value ?? string.Empty))
                {
                    RefreshDisplayNameIfUsingDefault(previousDefaultName);
                }
            }
        }

        public string GroupName
        {
            get => _groupName;
            set => SetField(ref _groupName, value ?? string.Empty);
        }

        public WidthSide Side
        {
            get => _side;
            set
            {
                string previousDefaultName = BuildDefaultDisplayName();
                if (SetField(ref _side, value))
                {
                    RefreshDisplayNameIfUsingDefault(previousDefaultName);
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetField(ref _displayName, value ?? string.Empty);
        }

        public TransmitParam ResultInput
        {
            get => _resultInput;
            set => SetField(ref _resultInput, value ?? new TransmitParam());
        }

        public TransmitParam ImageInput
        {
            get => _imageInput;
            set => SetField(ref _imageInput, value ?? new TransmitParam());
        }

        public bool IsRequired
        {
            get => _isRequired;
            set => SetField(ref _isRequired, value);
        }

        private void RefreshDisplayNameIfUsingDefault(string previousDefaultName)
        {
            if (string.IsNullOrWhiteSpace(_displayName)
                || string.Equals(_displayName, previousDefaultName, StringComparison.OrdinalIgnoreCase))
            {
                DisplayName = BuildDefaultDisplayName();
            }
        }

        private string BuildDefaultDisplayName()
        {
            string groupKey = FormatGroupKeyForDisplay(_groupKey);
            string sideText = ResolveSideText(_side);
            return string.IsNullOrWhiteSpace(sideText) ? groupKey : $"{groupKey}-{sideText}";
        }

        private static string FormatGroupKeyForDisplay(string groupKey)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
            {
                return "??";
            }

            string text = groupKey.Trim();
            if (text.Length >= 2
                && (text[0] == 'G' || text[0] == 'g')
                && int.TryParse(text[1..], out int gIndex))
            {
                return gIndex.ToString("D2");
            }

            return int.TryParse(text, out int index)
                ? index.ToString("D2")
                : text;
        }

        private static string ResolveSideText(WidthSide side)
        {
            return side switch
            {
                WidthSide.Left => "L",
                WidthSide.Right => "R",
                _ => string.Empty
            };
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
