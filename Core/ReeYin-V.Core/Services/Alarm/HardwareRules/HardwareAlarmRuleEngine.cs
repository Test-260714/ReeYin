#nullable enable
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    [ExposedService(Lifetime.Singleton, 7)]
    public sealed class HardwareAlarmRuleEngine
    {
        private const string AlarmReportSourceKey = "AlarmReportSource";
        private readonly IHardwareAlarmRuleService _ruleService;

        public HardwareAlarmRuleEngine(IHardwareAlarmRuleService ruleService, IAlarmDefinitionService definitionService)
        {
            _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
            // Disabled definition checks are intentionally delegated to the reporter's BuildRaiseRequest path.
            _ = definitionService ?? throw new ArgumentNullException(nameof(definitionService));
        }

        public IReadOnlyList<HardwareAlarmRuleAction> Evaluate(HardwareAlarmRuleContext context)
        {
            if (context == null)
            {
                return Array.Empty<HardwareAlarmRuleAction>();
            }

            List<HardwareAlarmRuleAction> actions = new List<HardwareAlarmRuleAction>();
            foreach (HardwareAlarmRuleInfo rule in _ruleService.GetEnabledRulesSnapshot())
            {
                if (!RuleApplies(rule, context))
                {
                    continue;
                }

                if (MatchesTrigger(rule, context))
                {
                    actions.Add(CreateAction(rule, context, shouldRaise: true));
                    continue;
                }

                if (MatchesRecovery(rule, context))
                {
                    actions.Add(CreateAction(rule, context, shouldRaise: false));
                }
            }

            return actions;
        }

        private static bool RuleApplies(HardwareAlarmRuleInfo rule, HardwareAlarmRuleContext context)
        {
            if (rule == null || !rule.Enabled || string.IsNullOrWhiteSpace(rule.DefinitionCode))
            {
                return false;
            }

            return MatchesSourceType(rule.SourceType, context.SourceType) &&
                   PatternMatches(rule.SourcePattern, context.Source) &&
                   PatternMatches(rule.LocationPattern, context.Location);
        }

        private static bool MatchesTrigger(HardwareAlarmRuleInfo rule, HardwareAlarmRuleContext context)
        {
            object? actual = GetRuleFieldValue(rule, context);
            return Compare(actual, rule.Operator, rule.TriggerValue, allowInlineOperator: true);
        }

        private static bool MatchesRecovery(HardwareAlarmRuleInfo rule, HardwareAlarmRuleContext context)
        {
            switch (rule.ClearKind)
            {
                case HardwareAlarmClearKind.StateRecovery:
                    return Compare(context.Status, HardwareAlarmOperator.Equals, rule.ClearValue, allowInlineOperator: true);

                case HardwareAlarmClearKind.FieldRecovery:
                    object? actual = GetRuleFieldValue(rule, context);
                    if (rule.Operator == HardwareAlarmOperator.BitHasFlag)
                    {
                        string flags = string.IsNullOrWhiteSpace(rule.ClearValue) ? rule.TriggerValue : rule.ClearValue;
                        return !BitHasAnyFlag(actual, flags);
                    }

                    return Compare(actual, rule.Operator, rule.ClearValue, allowInlineOperator: true);

                default:
                    return false;
            }
        }

        private static object? GetRuleFieldValue(HardwareAlarmRuleInfo rule, HardwareAlarmRuleContext context)
        {
            switch (rule.TriggerKind)
            {
                case HardwareAlarmTriggerKind.State:
                    return context.Status;

                case HardwareAlarmTriggerKind.ErrorCode:
                    return context.ErrorCode;

                case HardwareAlarmTriggerKind.Heartbeat:
                case HardwareAlarmTriggerKind.ExtraData:
                    return TryGetExtraData(context, rule.TriggerField, out object? value) ? value : null;

                default:
                    return null;
            }
        }

        private static bool TryGetExtraData(HardwareAlarmRuleContext context, string field, out object? value)
        {
            value = null;
            if (context.ExtraData == null || string.IsNullOrWhiteSpace(field))
            {
                return false;
            }

            return context.ExtraData.TryGetValue(field.Trim(), out value);
        }

        private static HardwareAlarmRuleAction CreateAction(HardwareAlarmRuleInfo rule, HardwareAlarmRuleContext context, bool shouldRaise)
        {
            Dictionary<string, object?> extraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            Merge(extraData, rule.ExtraTemplate);
            Merge(extraData, context.ExtraData);
            extraData["HardwareName"] = context.Name;
            extraData["HardwareState"] = context.Status.ToString();
            extraData["IsConnect"] = context.IsConnect;
            extraData["ErrorCode"] = context.ErrorCode;
            extraData["Operation"] = context.Operation;
            extraData["RuleId"] = rule.Id;
            extraData["RuleName"] = rule.Name;
            extraData["TriggerKind"] = rule.TriggerKind.ToString();
            extraData["TriggerField"] = rule.TriggerField;
            extraData["TriggerValue"] = rule.TriggerValue;

            return new HardwareAlarmRuleAction
            {
                DefinitionCode = rule.DefinitionCode,
                Source = ResolveActionSource(context),
                SourceType = context.SourceType,
                Location = context.Location,
                Message = string.IsNullOrWhiteSpace(context.Describe)
                    ? (string.IsNullOrWhiteSpace(rule.Name) ? rule.DefinitionCode : rule.Name)
                    : context.Describe,
                ShouldRaise = shouldRaise,
                ShouldClear = !shouldRaise,
                DebounceMilliseconds = Math.Max(0, rule.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, rule.ThrottleSeconds),
                IsLatched = rule.LatchMode,
                ExtraData = extraData,
                OccurredAt = context.Timestamp == default ? DateTime.Now : context.Timestamp
            };
        }

        private static string ResolveActionSource(HardwareAlarmRuleContext context)
        {
            if (context.ExtraData != null &&
                context.ExtraData.TryGetValue(AlarmReportSourceKey, out object? source) &&
                !string.IsNullOrWhiteSpace(NormalizeValue(source)))
            {
                return NormalizeValue(source);
            }

            return context.Source;
        }

        private static void Merge(IDictionary<string, object?> target, IDictionary<string, object?>? source)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object?> item in source)
            {
                target[item.Key] = item.Value;
            }
        }

        private static bool Compare(object? actual, HardwareAlarmOperator configuredOperator, string expected, bool allowInlineOperator)
        {
            HardwareAlarmOperator operation = configuredOperator;
            string expectedValue = expected?.Trim() ?? string.Empty;
            if (allowInlineOperator)
            {
                ParseInlineOperator(expectedValue, configuredOperator, out operation, out expectedValue);
            }

            switch (operation)
            {
                case HardwareAlarmOperator.Equals:
                    return EqualsAny(actual, expectedValue);

                case HardwareAlarmOperator.NotEquals:
                    return !EqualsAny(actual, expectedValue);

                case HardwareAlarmOperator.GreaterThan:
                    return NumericCompare(actual, expectedValue, value => value > 0);

                case HardwareAlarmOperator.GreaterThanOrEqual:
                    return NumericCompare(actual, expectedValue, value => value >= 0);

                case HardwareAlarmOperator.LessThan:
                    return NumericCompare(actual, expectedValue, value => value < 0);

                case HardwareAlarmOperator.LessThanOrEqual:
                    return NumericCompare(actual, expectedValue, value => value <= 0);

                case HardwareAlarmOperator.Contains:
                    return ContainsExpected(actual, expectedValue);

                case HardwareAlarmOperator.BitHasFlag:
                    return BitHasAnyFlag(actual, expectedValue);

                default:
                    return false;
            }
        }

        private static void ParseInlineOperator(
            string expected,
            HardwareAlarmOperator fallbackOperator,
            out HardwareAlarmOperator operation,
            out string expectedValue)
        {
            operation = fallbackOperator;
            expectedValue = expected;
            if (string.IsNullOrWhiteSpace(expected))
            {
                return;
            }

            string value = expected.Trim();
            if (value.StartsWith(">=", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.GreaterThanOrEqual;
                expectedValue = value.Substring(2).Trim();
            }
            else if (value.StartsWith("<=", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.LessThanOrEqual;
                expectedValue = value.Substring(2).Trim();
            }
            else if (value.StartsWith("!=", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.NotEquals;
                expectedValue = value.Substring(2).Trim();
            }
            else if (value.StartsWith(">", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.GreaterThan;
                expectedValue = value.Substring(1).Trim();
            }
            else if (value.StartsWith("<", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.LessThan;
                expectedValue = value.Substring(1).Trim();
            }
            else if (value.StartsWith("=", StringComparison.Ordinal))
            {
                operation = HardwareAlarmOperator.Equals;
                expectedValue = value.Substring(1).Trim();
            }
        }

        private static bool EqualsAny(object? actual, string expected)
        {
            string actualText = NormalizeValue(actual);
            string[] expectedParts = SplitList(expected);
            if (expectedParts.Length == 0)
            {
                return string.IsNullOrWhiteSpace(actualText);
            }

            return expectedParts.Any(item => string.Equals(actualText, item, StringComparison.OrdinalIgnoreCase));
        }

        private static bool NumericCompare(object? actual, string expected, Func<int, bool> predicate)
        {
            if (!TryConvertDecimal(actual, out decimal actualNumber) ||
                !decimal.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal expectedNumber))
            {
                return false;
            }

            return predicate(actualNumber.CompareTo(expectedNumber));
        }

        private static bool ContainsExpected(object? actual, string expected)
        {
            string[] expectedParts = SplitList(expected);
            if (expectedParts.Length == 0)
            {
                return false;
            }

            if (actual is IEnumerable enumerable && actual is not string)
            {
                HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (object? item in enumerable)
                {
                    foreach (string token in SplitList(NormalizeValue(item)))
                    {
                        values.Add(token);
                    }
                }

                return expectedParts.Any(values.Contains);
            }

            string actualText = NormalizeValue(actual);
            return expectedParts.Any(item => actualText.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool BitHasAnyFlag(object? actual, string expected)
        {
            string[] expectedFlags = SplitList(expected);
            if (expectedFlags.Length == 0)
            {
                return false;
            }

            HashSet<string> actualFlags = GetFlagSet(actual);
            if (actualFlags.Count > 0 && expectedFlags.Any(actualFlags.Contains))
            {
                return true;
            }

            if (TryConvertInt64(actual, out long actualNumber))
            {
                foreach (string flag in expectedFlags)
                {
                    if (long.TryParse(flag, NumberStyles.Integer, CultureInfo.InvariantCulture, out long expectedNumber) &&
                        (actualNumber & expectedNumber) == expectedNumber)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static HashSet<string> GetFlagSet(object? actual)
        {
            HashSet<string> flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (actual is IEnumerable enumerable && actual is not string)
            {
                foreach (object? item in enumerable)
                {
                    foreach (string token in SplitList(NormalizeValue(item)))
                    {
                        flags.Add(token);
                    }
                }
            }
            else
            {
                foreach (string token in SplitList(NormalizeValue(actual)))
                {
                    flags.Add(token);
                }
            }

            return flags;
        }

        private static bool TryConvertDecimal(object? value, out decimal number)
        {
            number = default;
            if (value == null)
            {
                return false;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    number = convertible.ToDecimal(CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                }
                catch (InvalidCastException)
                {
                }
                catch (OverflowException)
                {
                }
            }

            return decimal.TryParse(NormalizeValue(value), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
        }

        private static bool TryConvertInt64(object? value, out long number)
        {
            number = default;
            if (value == null)
            {
                return false;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    number = convertible.ToInt64(CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                }
                catch (InvalidCastException)
                {
                }
                catch (OverflowException)
                {
                }
            }

            return long.TryParse(NormalizeValue(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        }

        private static string NormalizeValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                bool boolean => boolean ? "true" : "false",
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()?.Trim() ?? string.Empty
            };
        }

        private static string[] SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();
        }

        private static bool MatchesSourceType(string? expectedSourceType, string actualSourceType)
        {
            return string.IsNullOrWhiteSpace(expectedSourceType) ||
                   string.Equals(expectedSourceType.Trim(), actualSourceType?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PatternMatches(string? pattern, string value)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
            {
                return true;
            }

            value ??= string.Empty;
            string normalizedPattern = pattern.Trim();
            if (!normalizedPattern.Contains("*"))
            {
                return normalizedPattern.Equals(value, StringComparison.OrdinalIgnoreCase);
            }

            bool startsWithWildcard = normalizedPattern.StartsWith("*", StringComparison.Ordinal);
            bool endsWithWildcard = normalizedPattern.EndsWith("*", StringComparison.Ordinal);
            string[] parts = normalizedPattern.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return true;
            }

            int position = 0;
            int firstMiddlePart = 0;
            int lastMiddlePart = parts.Length - 1;
            int searchLimit = value.Length;

            if (!startsWithWildcard)
            {
                if (!value.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                position = parts[0].Length;
                firstMiddlePart = 1;
            }

            if (!endsWithWildcard && lastMiddlePart >= firstMiddlePart)
            {
                string lastPart = parts[lastMiddlePart];
                if (!value.EndsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                searchLimit = value.Length - lastPart.Length;
                lastMiddlePart--;
            }

            if (position > searchLimit)
            {
                return false;
            }

            for (int index = firstMiddlePart; index <= lastMiddlePart; index++)
            {
                string part = parts[index];
                int found = value.IndexOf(part, position, StringComparison.OrdinalIgnoreCase);
                if (found < 0 || found + part.Length > searchLimit)
                {
                    return false;
                }

                position = found + part.Length;
            }

            return true;
        }
    }
}
