using ReeYin_V.Core.IOC;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Custom.DefectOverview.Services
{
    public sealed partial class BandMapStateService
    {
        private const string BatchStateFileName = "DefectOverviewBatchState.json";

        private sealed class BatchStateDto
        {
            public int BatchNumber { get; set; } = 1;

            public DateTime BatchStartedLocalTime { get; set; } = DateTime.Now;
        }

        private void LoadBatchState()
        {
            BatchStateDto state = ReadBatchState();
            _batchNumber = Math.Max(1, state.BatchNumber);
            _batchStartedLocalTime = state.BatchStartedLocalTime == default
                ? DateTime.Now
                : state.BatchStartedLocalTime;
        }

        private void SaveBatchStateLocked()
        {
            WriteBatchState(new BatchStateDto
            {
                BatchNumber = Math.Max(1, _batchNumber),
                BatchStartedLocalTime = _batchStartedLocalTime == default ? DateTime.Now : _batchStartedLocalTime,
            });
        }

        private static BatchStateDto ReadBatchState()
        {
            try
            {
                string path = GetBatchStatePath();
                if (!File.Exists(path))
                {
                    return new BatchStateDto();
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<BatchStateDto>(json) ?? new BatchStateDto();
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[BandMapState] Load batch state failed: {ex.Message}");
                return new BatchStateDto();
            }
        }

        private static void WriteBatchState(BatchStateDto state)
        {
            try
            {
                string path = GetBatchStatePath();
                string directory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;
                Directory.CreateDirectory(directory);

                string tempPath = Path.Combine(directory, $"{BatchStateFileName}.{Guid.NewGuid():N}.tmp");
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

                File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                File.Move(tempPath, path, overwrite: true);
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[BandMapState] Save batch state failed: {ex.Message}");
            }
        }

        private static string GetBatchStatePath()
        {
            string basePath = string.IsNullOrWhiteSpace(PrismProvider.AppBasePath)
                ? AppContext.BaseDirectory
                : PrismProvider.AppBasePath;

            return Path.Combine(basePath, "Config", BatchStateFileName);
        }
    }
}
