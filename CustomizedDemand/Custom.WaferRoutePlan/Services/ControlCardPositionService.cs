using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Custom.WaferRoutePlan.Services
{
    /// <summary>
    /// 控制卡位置获取服务，用于从控制卡模块读取当前 X/Y 轴坐标。
    /// </summary>
    public class ControlCardPositionService
    {
        /// <summary>
        /// 尝试从控制卡获取当前 X/Y 坐标。
        /// </summary>
        /// <param name="x">获取到的 X 坐标。</param>
        /// <param name="y">获取到的 Y 坐标。</param>
        /// <param name="message">操作结果消息。</param>
        /// <returns>是否成功获取坐标。</returns>
        public bool TryGetCurrentXY(out double x, out double y, out string message)
        {
            x = 0;
            y = 0;

            try
            {
                if (!IsControlCardModuleAvailable())
                {
                    message = "未找到控制卡模块，无法获取当前坐标。";
                    return false;
                }

                object controlCard = GetControlCardInstance();
                if (controlCard == null)
                {
                    message = "未找到可用控制卡，无法获取当前坐标。";
                    return false;
                }

                if (!TryReadAxisPositions(controlCard, out x, out y, out message))
                {
                    return false;
                }

                message = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                message = $"获取当前坐标失败：{ex.Message}";
                return false;
            }
        }

        private static bool IsControlCardModuleAvailable()
        {
            return PrismProvider.HardwareModuleManager.Modules
                .ContainsKey(ConfigKey.ControlCard) &&
                PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] != null;
        }

        private static object? GetControlCardInstance()
        {
            object controlCardModule = PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard];
            object? controlCard = GetPropertyValue(controlCardModule, "CurSltCard");

            if (controlCard == null)
            {
                controlCard = (GetPropertyValue(controlCardModule, "CardModels") as IEnumerable)
                    ?.Cast<object>()
                    .FirstOrDefault();
            }

            return controlCard;
        }

        private static bool TryReadAxisPositions(object controlCard, out double x, out double y, out string message)
        {
            x = 0;
            y = 0;

            var getAllPosInfosMethod = controlCard.GetType()
                .GetMethods()
                .FirstOrDefault(item => item.Name == "GetAllPosInfos" && item.GetParameters().Length <= 1);

            if (getAllPosInfosMethod == null)
            {
                message = "当前控制卡不支持读取当前位置。";
                return false;
            }

            object? invokeResult = getAllPosInfosMethod.GetParameters().Length == 0
                ? getAllPosInfosMethod.Invoke(controlCard, null)
                : getAllPosInfosMethod.Invoke(controlCard, new object[] { (short)2 });

            if (invokeResult is bool readSuccess && !readSuccess)
            {
                message = "读取控制卡当前位置失败。";
                return false;
            }

            object? config = GetPropertyValue(controlCard, "Config");
            if (GetPropertyValue(config, "AllAxis") is not IEnumerable allAxis)
            {
                message = "控制卡轴配置为空，无法获取 X/Y 当前坐标。";
                return false;
            }

            return TryExtractXYPositions(allAxis, out x, out y, out message);
        }

        private static bool TryExtractXYPositions(IEnumerable allAxis, out double x, out double y, out string message)
        {
            x = 0;
            y = 0;

            double? xPos = null;
            double? yPos = null;
            var axisPositions = allAxis.Cast<object>().ToList();

            foreach (object axis in axisPositions)
            {
                string? axisName = GetPropertyValue(axis, "AxisNum")?.ToString();
                if (!TryConvertToDouble(GetPropertyValue(axis, "CurPos"), out double curPos))
                {
                    continue;
                }

                if (string.Equals(axisName, "X", StringComparison.OrdinalIgnoreCase))
                {
                    xPos = curPos;
                }
                else if (string.Equals(axisName, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    yPos = curPos;
                }
            }

            // 回退：如果未找到命名轴，按索引取前两个轴
            if (!xPos.HasValue && axisPositions.Count > 0 &&
                TryConvertToDouble(GetPropertyValue(axisPositions[0], "CurPos"), out double fallbackX))
            {
                xPos = fallbackX;
            }

            if (!yPos.HasValue && axisPositions.Count > 1 &&
                TryConvertToDouble(GetPropertyValue(axisPositions[1], "CurPos"), out double fallbackY))
            {
                yPos = fallbackY;
            }

            if (!xPos.HasValue || !yPos.HasValue)
            {
                message = "控制卡未配置 X/Y 轴，无法获取当前坐标。";
                return false;
            }

            x = xPos.Value;
            y = yPos.Value;
            message = string.Empty;
            return true;
        }

        private static object? GetPropertyValue(object? source, string propertyName)
        {
            return source?.GetType().GetProperty(propertyName)?.GetValue(source);
        }

        private static bool TryConvertToDouble(object? value, out double result)
        {
            if (value == null)
            {
                result = 0;
                return false;
            }

            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
    }
}
