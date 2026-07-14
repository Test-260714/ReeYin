using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Linq;
using System.Reflection;

namespace ReeYin_V.Core.Services.Recipe
{
    [Serializable]
    public class RecipeParamInfo : BindableBase
    {
        private Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private bool _isEnable;
        public bool IsEnable
        {
            get => _isEnable;
            set => SetProperty(ref _isEnable, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        private bool _requiresPageEditor;
        public bool RequiresPageEditor
        {
            get => _requiresPageEditor;
            set
            {
                if (SetProperty(ref _requiresPageEditor, value))
                {
                    RaisePropertyChanged(nameof(PageEditorDisplayText));
                }
            }
        }

        private string _editorPageName = string.Empty;
        public string EditorPageName
        {
            get => _editorPageName;
            set => SetProperty(ref _editorPageName, value ?? string.Empty);
        }

        private int _serial = -1;
        public int Serial
        {
            get => _serial;
            set => SetProperty(ref _serial, value);
        }

        private string _subjection = string.Empty;
        public string Subjection
        {
            get => _subjection;
            set => SetProperty(ref _subjection, value ?? string.Empty);
        }

        private string _recipeKey = string.Empty;
        public string RecipeKey
        {
            get => _recipeKey;
            set => SetProperty(ref _recipeKey, value ?? string.Empty);
        }

        private string _path = string.Empty;
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value ?? string.Empty);
        }

        private Type _memberType;
        private string _memberTypeName = string.Empty;

        public string MemberTypeName
        {
            get => _memberTypeName;
            set => SetProperty(ref _memberTypeName, value ?? string.Empty);
        }

        [JsonIgnore]
        public Type MemberType
        {
            get
            {
                if (_memberType == null && !string.IsNullOrWhiteSpace(_memberTypeName))
                {
                    _memberType = ResolveStoredMemberType(_memberTypeName);
                }

                return _memberType;
            }
            set
            {
                if (SetProperty(ref _memberType, value))
                {
                    if (value != null)
                    {
                        MemberTypeName = value.AssemblyQualifiedName ?? string.Empty;
                    }

                    RaisePropertyChanged(nameof(IsEnumParameter));
                    RaisePropertyChanged(nameof(EnumOptions));
                    RaisePropertyChanged(nameof(ValueText));
                }
            }
        }

        [JsonIgnore]
        public Type DeclaringType { get; set; }

        [JsonIgnore]
        public MemberInfo MemberInfo { get; set; }

        [JsonIgnore]
        public bool IsField { get; set; }

        private object _value = string.Empty;
        public object Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(ValueText));
                    RaisePropertyChanged(nameof(PageEditorDisplayText));
                }
            }
        }

        private string _unit = string.Empty;
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value ?? string.Empty);
        }

        private bool _isRequired;
        public bool IsRequired
        {
            get => _isRequired;
            set => SetProperty(ref _isRequired, value);
        }

        private string _optionsText = string.Empty;
        public string OptionsText
        {
            get => _optionsText;
            set
            {
                if (SetProperty(ref _optionsText, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(IsEnumParameter));
                    RaisePropertyChanged(nameof(EnumOptions));
                }
            }
        }

        [JsonIgnore]
        public bool IsEnumParameter
            => ResolveEnumType() != null || GetOptionsFromText().Length > 0;

        [JsonIgnore]
        public string[] EnumOptions
        {
            get
            {
                Type enumType = ResolveEnumType();
                return enumType != null
                    ? Enum.GetNames(enumType)
                    : GetOptionsFromText();
            }
        }

        [JsonIgnore]
        public string ValueText
        {
            get => GetDisplayValueText();
            set
            {
                string text = value ?? string.Empty;
                Type memberType = MemberType;
                Type actualType = memberType == null
                    ? null
                    : Nullable.GetUnderlyingType(memberType) ?? memberType;
                object normalizedValue;
                if (string.IsNullOrWhiteSpace(text))
                {
                    normalizedValue = string.Empty;
                }
                else if (actualType?.IsEnum == true)
                {
                    // 枚举配方值统一落盘为枚举名称，避免 Newtonsoft 将枚举对象保存成 0/1/2。
                    normalizedValue = RecipeValueConverter.SerializeValue(text, memberType);
                }
                else
                {
                    normalizedValue = actualType != null && actualType != typeof(string)
                        ? RecipeValueConverter.DeserializeValue(text, memberType)
                        : text;
                }

                Value = normalizedValue;
            }
        }

        [JsonIgnore]
        public string PageEditorDisplayText
            => RequiresPageEditor
                ? (RecipeValueConverter.IsValueEmpty(Value) ? "请点击编辑按钮配置" : "已配置，点击编辑按钮修改")
                : string.Empty;

        //[JsonIgnore]
        //public string DisplayParameterType
        //{
        //    get
        //    {
        //        Type actualType = Nullable.GetUnderlyingType(MemberType) ?? MemberType;
        //        if (actualType == null && Value != null)
        //        {
        //            actualType = Value.GetType();
        //        }

        //        if (actualType == null)
        //        {
        //            return "Unknown";
        //        }

        //        if (actualType == typeof(bool))
        //        {
        //            return "Boolean";
        //        }

        //        if (actualType == typeof(string))
        //        {
        //            return "String";
        //        }

        //        if (actualType == typeof(DateTime))
        //        {
        //            return "DateTime";
        //        }

        //        if (actualType.IsEnum)
        //        {
        //            return "Enum";
        //        }

        //        if (actualType == typeof(int) ||
        //            actualType == typeof(long) ||
        //            actualType == typeof(short) ||
        //            actualType == typeof(byte))
        //        {
        //            return "Integer";
        //        }

        //        if (actualType == typeof(float) ||
        //            actualType == typeof(double) ||
        //            actualType == typeof(decimal))
        //        {
        //            return "Number";
        //        }

        //        return actualType.Name;
        //    }
        //}

        public void UpdateDefinition(RecipeParamInfo source)
        {
            if (source == null)
            {
                return;
            }

            Name = source.Name;
            Description = source.Description;
            RequiresPageEditor = source.RequiresPageEditor;
            EditorPageName = source.EditorPageName;
            Serial = source.Serial;
            Subjection = source.Subjection;
            RecipeKey = source.RecipeKey;
            Path = source.Path;
            MemberTypeName = source.MemberTypeName;
            MemberType = source.MemberType;
            DeclaringType = source.DeclaringType;
            MemberInfo = source.MemberInfo;
            IsField = source.IsField;
        }

        public string SerializeValue()
        {
            return RecipeValueConverter.SerializeValue(Value, MemberType);
        }

        public object DeserializeValue(string valueText)
        {
            return RecipeValueConverter.DeserializeValue(valueText, MemberType);
        }

        public RecipeParamInfo CreateCopy()
        {
            return new RecipeParamInfo
            {
                Id = Id,
                IsEnable = IsEnable,
                Name = Name,
                Description = Description,
                RequiresPageEditor = RequiresPageEditor,
                EditorPageName = EditorPageName,
                Serial = Serial,
                Subjection = Subjection,
                RecipeKey = RecipeKey,
                Path = Path,
                MemberTypeName = MemberTypeName,
                MemberType = MemberType,
                DeclaringType = DeclaringType,
                MemberInfo = MemberInfo,
                IsField = IsField,
                Value = Value,
                Unit = Unit,
                IsRequired = IsRequired,
                OptionsText = OptionsText,
            };
        }

        private Type ResolveEnumType()
        {
            Type memberType = MemberType;
            if (memberType == null)
            {
                return null;
            }

            Type actualType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            return actualType?.IsEnum == true ? actualType : null;
        }

        private static Type ResolveStoredMemberType(string memberTypeName)
        {
            if (string.IsNullOrWhiteSpace(memberTypeName))
            {
                return null;
            }

            Type resolvedType = Type.GetType(memberTypeName, throwOnError: false);
            if (resolvedType != null)
            {
                return resolvedType;
            }

            string fullName = memberTypeName.Split(',')[0].Trim();
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, throwOnError: false))
                .FirstOrDefault(type => type != null);
        }

        private string[] GetOptionsFromText()
        {
            if (string.IsNullOrWhiteSpace(OptionsText))
            {
                return Array.Empty<string>();
            }

            return OptionsText
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private string GetDisplayValueText()
        {
            Type memberType = MemberType;
            string displayText = RecipeValueConverter.SerializeValue(Value, memberType);
            if (memberType != null)
            {
                return displayText;
            }

            string[] options = GetOptionsFromText();
            if (options.Length == 0)
            {
                return displayText;
            }

            if (Value is string textValue)
            {
                if (options.Contains(textValue, StringComparer.OrdinalIgnoreCase))
                {
                    return options.First(option => string.Equals(option, textValue, StringComparison.OrdinalIgnoreCase));
                }

                if (int.TryParse(textValue, out int optionIndex) &&
                    optionIndex >= 0 &&
                    optionIndex < options.Length)
                {
                    return options[optionIndex];
                }

                return textValue;
            }

            try
            {
                int optionIndex = Convert.ToInt32(Value);
                return optionIndex >= 0 && optionIndex < options.Length
                    ? options[optionIndex]
                    : displayText;
            }
            catch
            {
                return displayText;
            }
        }
    }
}
