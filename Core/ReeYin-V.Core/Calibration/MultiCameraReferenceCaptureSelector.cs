using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Core.Calibration
{
    public static class MultiCameraReferenceCaptureSelector
    {
        public static string SelectBestReferenceCapture<T>(
            IEnumerable<T> observations,
            Func<T, string> cameraIdSelector,
            Func<T, string> captureIdSelector)
        {
            if (observations == null)
            {
                throw new ArgumentNullException(nameof(observations));
            }
            if (cameraIdSelector == null)
            {
                throw new ArgumentNullException(nameof(cameraIdSelector));
            }
            if (captureIdSelector == null)
            {
                throw new ArgumentNullException(nameof(captureIdSelector));
            }

            var camerasByCapture = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (T observation in observations)
            {
                string cameraId = cameraIdSelector(observation);
                string captureId = captureIdSelector(observation);
                if (string.IsNullOrWhiteSpace(cameraId) || string.IsNullOrWhiteSpace(captureId))
                {
                    continue;
                }

                if (!camerasByCapture.TryGetValue(captureId, out HashSet<string> cameraIds))
                {
                    cameraIds = new HashSet<string>(StringComparer.Ordinal);
                    camerasByCapture.Add(captureId, cameraIds);
                }

                cameraIds.Add(cameraId);
            }

            return camerasByCapture
                .OrderByDescending(item => item.Value.Count)
                .ThenBy(item => item.Key, NaturalStringComparer.Instance)
                .Select(item => item.Key)
                .FirstOrDefault() ?? string.Empty;
        }

        private sealed class NaturalStringComparer : IComparer<string>
        {
            public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

            public int Compare(string x, string y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }

                int xIndex = 0;
                int yIndex = 0;
                while (xIndex < x.Length && yIndex < y.Length)
                {
                    bool xDigit = char.IsDigit(x[xIndex]);
                    bool yDigit = char.IsDigit(y[yIndex]);
                    if (xDigit && yDigit)
                    {
                        int numberCompare = CompareNumberRun(x, ref xIndex, y, ref yIndex);
                        if (numberCompare != 0)
                        {
                            return numberCompare;
                        }
                        continue;
                    }

                    int charCompare = x[xIndex].CompareTo(y[yIndex]);
                    if (charCompare != 0)
                    {
                        return charCompare;
                    }

                    xIndex++;
                    yIndex++;
                }

                return x.Length.CompareTo(y.Length);
            }

            private static int CompareNumberRun(string x, ref int xIndex, string y, ref int yIndex)
            {
                int xRunStart = xIndex;
                int yRunStart = yIndex;
                while (xIndex < x.Length && char.IsDigit(x[xIndex]))
                {
                    xIndex++;
                }
                while (yIndex < y.Length && char.IsDigit(y[yIndex]))
                {
                    yIndex++;
                }

                int xSignificantStart = xRunStart;
                int ySignificantStart = yRunStart;
                while (xSignificantStart < xIndex && x[xSignificantStart] == '0')
                {
                    xSignificantStart++;
                }
                while (ySignificantStart < yIndex && y[ySignificantStart] == '0')
                {
                    ySignificantStart++;
                }

                int xSignificantLength = xIndex - xSignificantStart;
                int ySignificantLength = yIndex - ySignificantStart;
                if (xSignificantLength == 0)
                {
                    xSignificantStart = xIndex - 1;
                    xSignificantLength = 1;
                }
                if (ySignificantLength == 0)
                {
                    ySignificantStart = yIndex - 1;
                    ySignificantLength = 1;
                }

                int lengthCompare = xSignificantLength.CompareTo(ySignificantLength);
                if (lengthCompare != 0)
                {
                    return lengthCompare;
                }

                for (int offset = 0; offset < xSignificantLength; offset++)
                {
                    int digitCompare = x[xSignificantStart + offset].CompareTo(y[ySignificantStart + offset]);
                    if (digitCompare != 0)
                    {
                        return digitCompare;
                    }
                }

                int runLengthCompare = (xIndex - xRunStart).CompareTo(yIndex - yRunStart);
                return runLengthCompare;
            }
        }
    }
}
