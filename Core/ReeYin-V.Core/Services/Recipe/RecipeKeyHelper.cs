using System;

namespace ReeYin_V.Core.Services.Recipe
{
    public static class RecipeKeyHelper
    {
        public static string Build(int serial, string path)
        {
            string normalizedPath = (path ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return serial >= 0 ? $"{serial:D3}:" : string.Empty;
            }

            return serial >= 0 ? $"{serial:D3}:{normalizedPath}" : normalizedPath;
        }

        public static string Normalize(int serial, string path, string recipeKey)
        {
            string normalizedPath = (path ?? string.Empty).Trim();
            string normalizedKey = (recipeKey ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return normalizedKey;
            }

            if (string.IsNullOrWhiteSpace(normalizedKey) ||
                IsPathOnlyKey(normalizedKey, normalizedPath) ||
                IsGeneratedForPath(normalizedKey, normalizedPath))
            {
                return Build(serial, normalizedPath);
            }

            return normalizedKey;
        }

        public static bool IsGeneratedForPath(string recipeKey, string path)
        {
            string normalizedKey = (recipeKey ?? string.Empty).Trim();
            string normalizedPath = (path ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            int separatorIndex = normalizedKey.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            return int.TryParse(normalizedKey[..separatorIndex], out _) &&
                   string.Equals(normalizedKey[(separatorIndex + 1)..], normalizedPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathOnlyKey(string recipeKey, string path)
        {
            return string.Equals(
                (recipeKey ?? string.Empty).Trim(),
                (path ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
