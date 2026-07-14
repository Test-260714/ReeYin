using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Core.Calibration
{
    public static class MultiCameraStitchingInputSynchronizer
    {
        public static IReadOnlyList<TItem> Synchronize<TItem>(
            IEnumerable<string> cameraIds,
            IEnumerable<TItem> existingItems,
            Func<TItem, string> getCameraId,
            Action<TItem, string> setCameraId,
            Func<TItem> createItem)
            where TItem : class
        {
            if (cameraIds == null)
            {
                throw new ArgumentNullException(nameof(cameraIds));
            }

            if (existingItems == null)
            {
                throw new ArgumentNullException(nameof(existingItems));
            }

            if (getCameraId == null)
            {
                throw new ArgumentNullException(nameof(getCameraId));
            }

            if (setCameraId == null)
            {
                throw new ArgumentNullException(nameof(setCameraId));
            }

            if (createItem == null)
            {
                throw new ArgumentNullException(nameof(createItem));
            }

            List<string> resolvedCameraIds = cameraIds
                .Where(cameraId => !string.IsNullOrWhiteSpace(cameraId))
                .Select(cameraId => cameraId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var existingByCameraId = new Dictionary<string, TItem>(StringComparer.Ordinal);
            var blankRows = new Queue<TItem>();

            foreach (TItem item in existingItems.Where(item => item != null))
            {
                string cameraId = getCameraId(item);
                if (string.IsNullOrWhiteSpace(cameraId))
                {
                    blankRows.Enqueue(item);
                    continue;
                }

                string normalizedCameraId = cameraId.Trim();
                if (!existingByCameraId.ContainsKey(normalizedCameraId))
                {
                    existingByCameraId.Add(normalizedCameraId, item);
                }
            }

            var synchronizedItems = new List<TItem>(resolvedCameraIds.Count);
            foreach (string cameraId in resolvedCameraIds)
            {
                if (!existingByCameraId.TryGetValue(cameraId, out TItem item))
                {
                    item = blankRows.Count > 0 ? blankRows.Dequeue() : createItem();
                    if (item == null)
                    {
                        throw new InvalidOperationException("Created stitch input item must not be null.");
                    }
                }

                setCameraId(item, cameraId);
                synchronizedItems.Add(item);
            }

            return synchronizedItems;
        }
    }
}
