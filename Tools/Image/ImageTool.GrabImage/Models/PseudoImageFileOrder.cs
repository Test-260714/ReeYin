using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageTool.GrabImage.Models
{
    internal static class PseudoImageFileOrder
    {
        private static readonly string[] SupportedImageExtensions =
        [
            ".bmp", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif"
        ];

        public static IReadOnlyList<string> LoadSupportedImageFiles(string folder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    return Array.Empty<string>();

                return Directory
                    .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedImageFile)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static bool IsSupportedImageFile(string path)
        {
            return SupportedImageExtensions.Contains(
                Path.GetExtension(path),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
