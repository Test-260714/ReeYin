using Prism.Mvvm;
using System;

namespace ReeYin_V.Config.Models
{
    public enum StyleResourceKind
    {
        Color,
        FontSize
    }

    public enum StyleResourceValueSource
    {
        ColorElement,
        SolidColorBrushText,
        SolidColorBrushColorAttribute,
        DoubleElement
    }

    public sealed class StyleResourceItem : BindableBase
    {
        private string _value = string.Empty;
        private bool _isChanged;
        private string _error = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public StyleResourceKind Kind { get; set; }

        public StyleResourceValueSource ValueSource { get; set; }

        public string OriginalValue { get; private set; } = string.Empty;

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value ?? string.Empty))
                {
                    IsChanged = !string.Equals(_value, OriginalValue, StringComparison.Ordinal);
                }
            }
        }

        public bool IsChanged
        {
            get => _isChanged;
            private set => SetProperty(ref _isChanged, value);
        }

        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value ?? string.Empty);
        }

        public void SetInitialValue(string value)
        {
            OriginalValue = value ?? string.Empty;
            _value = OriginalValue;
            _isChanged = false;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(IsChanged));
        }

        public void AcceptChanges()
        {
            OriginalValue = Value;
            IsChanged = false;
        }
    }
}
