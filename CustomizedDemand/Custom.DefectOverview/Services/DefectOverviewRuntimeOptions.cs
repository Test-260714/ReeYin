using System;

namespace Custom.DefectOverview.Services
{
    internal static class DefectOverviewRuntimeOptions
    {
        public static readonly int RefreshIntervalMs = ReadInt("REEYIN_DEFECT_OVERVIEW_REFRESH_MS", 100, 66, 500);
        public static readonly int UiRefreshIntervalMs = ReadInt("REEYIN_DEFECT_OVERVIEW_UI_REFRESH_MS", 200, 100, 1000);
        public static readonly int WallRefreshIntervalMs = ReadInt("REEYIN_DEFECT_OVERVIEW_WALL_REFRESH_MS", 400, 200, 2000);
        public static readonly int BaseBitmapMaxDimension = ReadInt("REEYIN_DEFECT_OVERVIEW_BASE_BITMAP_MAX", 768, 256, 1536);
        public static readonly int PreviewMaxDimension = ReadInt("REEYIN_DEFECT_OVERVIEW_PREVIEW_MAX", 320, 160, 512);
        public static readonly int ThumbnailMaxDimension = ReadInt("REEYIN_DEFECT_OVERVIEW_THUMBNAIL_MAX", 224, 96, 320);
        public static readonly int DefectImageSize = ReadInt("REEYIN_DEFECT_OVERVIEW_DEFECT_IMAGE_SIZE", 100, 64, 320);
        public static readonly int DefectImageBoxSize = ReadInt("REEYIN_DEFECT_OVERVIEW_DEFECT_IMAGE_BOX_SIZE", 56, 24, 160);
        public static readonly int DefectImageJpegQuality = ReadInt("REEYIN_DEFECT_OVERVIEW_DEFECT_IMAGE_JPEG_QUALITY", 80, 40, 95);
        public static readonly int MaxPreviewImagesPerPath = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_PREVIEW_IMAGES_PER_PATH", 25, 0, 64);
        public static readonly int MaxVisibleMapPoints = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_VISIBLE_MAP_POINTS", 1200, 200, 50000);
        public static readonly int MaxWallPages = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_WALL_PAGES", 20, 1, 100);
        public static readonly int MaxHistoryItems = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_HISTORY_ITEMS", 200000, 5000, 2000000);
        public static readonly int MaxHistoryImageItems = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_HISTORY_IMAGE_ITEMS", 500, 100, 5000);
        public static readonly int MaxArchivedRolls = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_ARCHIVED_ROLLS", 3, 0, 20);
        public static readonly int MaxLocalImageSaveQueue = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_LOCAL_IMAGE_SAVE_QUEUE", 512, 32, 5000);
        public static readonly int MaxAutoBrjSyncDefects = ReadInt("REEYIN_DEFECT_OVERVIEW_MAX_AUTO_BRJ_SYNC_DEFECTS", 20000, 1000, 200000);
        public static readonly bool UseSegmentationGeometry = ReadBool("REEYIN_DEFECT_OVERVIEW_USE_SEG_GEOMETRY", defaultValue: false);
        public static readonly bool AttachMetadataBitmap = ReadBool("REEYIN_DEFECT_OVERVIEW_ATTACH_METADATA_BITMAP", defaultValue: false);

        private static int ReadInt(string name, int defaultValue, int minValue, int maxValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (!int.TryParse(value, out int parsed))
                return defaultValue;

            return Math.Clamp(parsed, minValue, maxValue);
        }

        private static bool ReadBool(string name, bool defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }
    }
}
