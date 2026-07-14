#nullable enable

using Prism.Mvvm;
using System;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed class DefectMapItem : BindableBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Defect";
        private string _defectType = "Unknown";
        private DefectMapSeverity _severity = DefectMapSeverity.Minor;
        private double _widthPosition;
        private double _lengthPosition;
        private double? _displaySize;
        private string _description = string.Empty;
        private object? _tag;

        public string Id
        {
            get => _id;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                SetProperty(ref _id, value.Trim());
            }
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Defect" : value.Trim());
        }

        public string DefectType
        {
            get => _defectType;
            set => SetProperty(ref _defectType, string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim());
        }

        public DefectMapSeverity Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        public double WidthPosition
        {
            get => _widthPosition;
            set => SetProperty(ref _widthPosition, value);
        }

        public double LengthPosition
        {
            get => _lengthPosition;
            set => SetProperty(ref _lengthPosition, value);
        }

        public double? DisplaySize
        {
            get => _displaySize;
            set => SetProperty(ref _displaySize, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        public object? Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }
    }
}
