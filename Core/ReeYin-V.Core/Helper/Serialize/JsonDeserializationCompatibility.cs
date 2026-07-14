using Newtonsoft.Json;
using System;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// 判断项目反序列化错误是否可以兼容跳过。
    /// </summary>
    public static class JsonDeserializationCompatibility
    {
        private const string ModuleParamMemberName = "ModuleParam";
        private const string JsonTypeMemberName = "$type";

        public static bool ShouldRethrow(string path, object member, Exception error)
        {
            if (error == null)
            {
                return false;
            }

            string memberName = member?.ToString();
            if (IsDirectModuleParamMember(memberName))
            {
                return true;
            }

            if (!IsDirectModuleParamPath(path))
            {
                return false;
            }

            return IsTypeResolutionError(error) || IsModuleParamObjectError(error);
        }

        private static bool IsDirectModuleParamMember(string memberName)
        {
            return string.Equals(memberName, ModuleParamMemberName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectModuleParamPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalizedPath = path.Trim();
            if (normalizedPath.EndsWith("." + ModuleParamMemberName, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Equals(ModuleParamMemberName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string directTypePath = "." + ModuleParamMemberName + "." + JsonTypeMemberName;
            return normalizedPath.EndsWith(directTypePath, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Equals(ModuleParamMemberName + "." + JsonTypeMemberName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTypeResolutionError(Exception error)
        {
            return error is JsonSerializationException or JsonReaderException &&
                ContainsAny(error.Message,
                    "Could not resolve type",
                    "Error resolving type specified in JSON",
                    "Could not load assembly",
                    "Could not find type",
                    "Type specified in JSON");
        }

        private static bool IsModuleParamObjectError(Exception error)
        {
            return error is JsonSerializationException &&
                ContainsAny(error.Message,
                    "Could not create an instance",
                    "Cannot deserialize the current JSON object",
                    "Cannot deserialize the current JSON array");
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string candidate in candidates)
            {
                if (value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
