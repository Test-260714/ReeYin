namespace ImageTool.GrabImage.Models
{
    internal sealed class PseudoImageReadPlanner
    {
        private int _nextFileIndex;
        private int _roundIndex;

        public void Reset()
        {
            _nextFileIndex = 0;
            _roundIndex = 0;
        }

        public bool TryGetNext(
            int imageCount,
            bool loopEnabled,
            int loopCount,
            out int fileIndex,
            out int roundIndex)
        {
            fileIndex = -1;
            roundIndex = 0;

            if (imageCount <= 0)
            {
                return false;
            }

            bool hasRoundLimit = loopCount > 0;
            bool shouldCycleFiles = loopEnabled || hasRoundLimit;

            if (!shouldCycleFiles && _nextFileIndex >= imageCount)
            {
                return false;
            }

            if (_nextFileIndex >= imageCount)
            {
                _nextFileIndex = 0;
                _roundIndex++;
            }

            if (_roundIndex <= 0)
            {
                _roundIndex = 1;
            }

            if (hasRoundLimit && _roundIndex > loopCount)
            {
                return false;
            }

            fileIndex = _nextFileIndex;
            roundIndex = _roundIndex;
            _nextFileIndex++;
            return true;
        }

        public bool TryGetNextBatch(
            int imageCount,
            int batchSize,
            bool loopEnabled,
            int loopCount,
            out int[] fileIndexes,
            out int roundIndex)
        {
            fileIndexes = System.Array.Empty<int>();
            roundIndex = 0;

            if (imageCount <= 0 || batchSize <= 0 || batchSize > imageCount)
            {
                return false;
            }

            bool hasRoundLimit = loopCount > 0;
            bool shouldCycleFiles = loopEnabled || hasRoundLimit;

            if (!shouldCycleFiles && _nextFileIndex + batchSize > imageCount)
            {
                return false;
            }

            if (_nextFileIndex + batchSize > imageCount)
            {
                _nextFileIndex = 0;
                _roundIndex++;
            }

            if (_roundIndex <= 0)
            {
                _roundIndex = 1;
            }

            if (hasRoundLimit && _roundIndex > loopCount)
            {
                return false;
            }

            var indexes = new int[batchSize];
            for (var i = 0; i < batchSize; i++)
            {
                indexes[i] = _nextFileIndex + i;
            }

            _nextFileIndex += batchSize;
            roundIndex = _roundIndex;
            fileIndexes = indexes;
            return true;
        }
    }
}
