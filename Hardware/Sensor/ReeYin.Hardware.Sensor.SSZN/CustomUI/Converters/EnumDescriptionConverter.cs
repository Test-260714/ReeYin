using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using SR7Link;

namespace ReeYin.Hardware.Sensor.SSZN.CustomUI.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        private static readonly Dictionary<Type, IReadOnlyDictionary<string, string>> EnumDisplayTextMap =
            new()
            {
                [typeof(SAVEPOWEROFF)] = new Dictionary<string, string>
                {
                    [nameof(SAVEPOWEROFF.ESAPO_NOSAVE)] = "掉电不保存",
                    [nameof(SAVEPOWEROFF.ESAPO_SAVE)] = "掉电保存",
                },
                [typeof(SR7IF_BATCH_ON_OFF)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_BATCH_ON_OFF.OFF)] = "OFF",
                    [nameof(SR7IF_BATCH_ON_OFF.ON)] = "ON",
                },
                //[typeof(SR7IF_ENCODER_TYPE)] = new Dictionary<string, string>
                //{
                //    [nameof(SR7IF_ENCODER_TYPE.E_1_1)] = "1相1递增",
                //    [nameof(SR7IF_ENCODER_TYPE.E_2_1)] = "2相1递增",
                //    [nameof(SR7IF_ENCODER_TYPE.E_2_2)] = "2相2递增",
                //    [nameof(SR7IF_ENCODER_TYPE.E_2_4)] = "2相4递增",
                //},
                [typeof(SR7IF_CYCLICAL_PATTERN)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_CYCLICAL_PATTERN.CLOSE)] = "关闭",
                    [nameof(SR7IF_CYCLICAL_PATTERN.OPEN)] = "开启",
                },
                [typeof(SR7IF_Z_MEASURING_RANGE)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z840)] = "840mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z768)] = "768mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z512)] = "512mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z384)] = "384mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z256)] = "256mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z192)] = "192mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z128)] = "128mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z96)] = "96mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z64)] = "64mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z48)] = "48mm",
                    [nameof(SR7IF_Z_MEASURING_RANGE.Z32)] = "32mm",
                },
                [typeof(SR7IF_SENSITIVITY)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_SENSITIVITY.HIGH)] = "高精度",
                    [nameof(SR7IF_SENSITIVITY.HIGH_RANGE_1)] = "高动态范围1",
                    [nameof(SR7IF_SENSITIVITY.HIGH_RANGE_2)] = "高动态范围2",
                    [nameof(SR7IF_SENSITIVITY.HIGH_RANGE_3)] = "高动态范围3",
                    [nameof(SR7IF_SENSITIVITY.HIGH_RANGE_4)] = "高动态范围4",
                    [nameof(SR7IF_SENSITIVITY.CUSTOMIZATION)] = "自定义高动态",
                },
                [typeof(SR7IF_EXP_TIME)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_EXP_TIME.T10US)] = "10us",
                    [nameof(SR7IF_EXP_TIME.T15US)] = "15us",
                    [nameof(SR7IF_EXP_TIME.T30US)] = "30us",
                    [nameof(SR7IF_EXP_TIME.T60US)] = "60us",
                    [nameof(SR7IF_EXP_TIME.T120US)] = "120us",
                    [nameof(SR7IF_EXP_TIME.T240US)] = "240us",
                    [nameof(SR7IF_EXP_TIME.T480US)] = "480us",
                    [nameof(SR7IF_EXP_TIME.T960US)] = "960us",
                    [nameof(SR7IF_EXP_TIME.T1920US)] = "1920us",
                    [nameof(SR7IF_EXP_TIME.T2400US)] = "2400us",
                    [nameof(SR7IF_EXP_TIME.T4900US)] = "4900us",
                    [nameof(SR7IF_EXP_TIME.T9800US)] = "9800us",
                },
                [typeof(SR7IF_LIGHT_CONTROL)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_LIGHT_CONTROL.AUTO)] = "自动",
                    [nameof(SR7IF_LIGHT_CONTROL.MAN)] = "手动",
                },
                [typeof(SR7IF_PEAK_SENSITIVITY)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_PEAK_SENSITIVITY.N_1)] = "等级1",
                    [nameof(SR7IF_PEAK_SENSITIVITY.N_2)] = "等级2",
                    [nameof(SR7IF_PEAK_SENSITIVITY.N_3)] = "等级3",
                    [nameof(SR7IF_PEAK_SENSITIVITY.N_4)] = "等级4",
                    [nameof(SR7IF_PEAK_SENSITIVITY.N_5)] = "等级5",
                },
                [typeof(SR7IF_PEAK_SELECT)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_PEAK_SELECT.PS_STANDARD)] = "标准",
                    [nameof(SR7IF_PEAK_SELECT.PS_SRNEAR)] = "near",
                    [nameof(SR7IF_PEAK_SELECT.PS_SRFAR)] = "far",
                    [nameof(SR7IF_PEAK_SELECT.PS_BE_NULL)] = "使之转为无效数据",
                    [nameof(SR7IF_PEAK_SELECT.PS_CONTINUE)] = "连续",
                    [nameof(SR7IF_PEAK_SELECT.PS_GLUE)] = "粘连",
                },
                [typeof(SR7IF_X_SAMPLING)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_X_SAMPLING.XS_OFF)] = "OFF",
                    [nameof(SR7IF_X_SAMPLING.XS_X2)] = "2",
                    [nameof(SR7IF_X_SAMPLING.XS_X4)] = "4",
                    [nameof(SR7IF_X_SAMPLING.XS_X8)] = "8",
                    [nameof(SR7IF_X_SAMPLING.XS_X16)] = "16",
                },
                [typeof(SR7IF_FILTER_X_MEDIAN)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_FILTER_X_MEDIAN.XM_OFF)] = "关闭",
                    [nameof(SR7IF_FILTER_X_MEDIAN.XM_N3)] = "3点",
                    [nameof(SR7IF_FILTER_X_MEDIAN.XM_N5)] = "5点",
                    [nameof(SR7IF_FILTER_X_MEDIAN.XM_N7)] = "7点",
                    [nameof(SR7IF_FILTER_X_MEDIAN.XM_N9)] = "9点",
                },
                [typeof(SR7IF_FILTER_Y_MEDIAN)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_FILTER_Y_MEDIAN.YM_OFF)] = "关闭",
                    [nameof(SR7IF_FILTER_Y_MEDIAN.YM_N3)] = "3点",
                    [nameof(SR7IF_FILTER_Y_MEDIAN.YM_N5)] = "5点",
                    [nameof(SR7IF_FILTER_Y_MEDIAN.YM_N7)] = "7点",
                    [nameof(SR7IF_FILTER_Y_MEDIAN.YM_N9)] = "9点",
                },
                [typeof(SR7IF_FILTER_X_SMOOTH)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_FILTER_X_SMOOTH.N1)] = "1点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N2)] = "2点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N4)] = "4点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N8)] = "8点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N16)] = "16点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N32)] = "32点",
                    [nameof(SR7IF_FILTER_X_SMOOTH.N64)] = "64点",
                },
                [typeof(SR7IF_FILTER_Y_SMOOTH)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N1)] = "1点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N2)] = "2点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N4)] = "4点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N8)] = "8点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N16)] = "16点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N32)] = "32点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N64)] = "64点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N128)] = "128点",
                    [nameof(SR7IF_FILTER_Y_SMOOTH.YS_N256)] = "256点",
                },
                [typeof(SR7IF_CHANGE_3D_25D)] = new Dictionary<string, string>
                {
                    [nameof(SR7IF_CHANGE_3D_25D.T3D)] = "3D",
                    [nameof(SR7IF_CHANGE_3D_25D.T25D)] = "2.5D",
                },
            };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                if (TryGetDisplayText(enumValue, out string displayText))
                {
                    return displayText;
                }

                var field = enumValue.GetType().GetField(enumValue.ToString());
                var description = field?.GetCustomAttribute<DescriptionAttribute>();
                return description?.Description ?? enumValue.ToString();
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static bool TryGetDisplayText(Enum enumValue, out string displayText)
        {
            if (EnumDisplayTextMap.TryGetValue(enumValue.GetType(), out IReadOnlyDictionary<string, string>? nameMap) &&
                nameMap.TryGetValue(enumValue.ToString(), out string? mappedText))
            {
                displayText = mappedText;
                return true;
            }

            displayText = string.Empty;
            return false;
        }
    }
}
