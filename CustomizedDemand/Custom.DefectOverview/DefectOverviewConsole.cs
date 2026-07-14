using ReeYin_V.Logger;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Custom.DefectOverview
{
    internal static class DefectOverviewConsole
    {
        private const int MaxConsoleQueueSize = 256;
        private static readonly BlockingCollection<string> ConsoleQueue = new(MaxConsoleQueueSize);
        private static readonly bool VerboseOutputEnabled = ResolveVerboseOutputEnabled();
        private static readonly bool FrameTraceEnabled = IsEnvironmentFlagEnabled("REEYIN_FRAME_TRACE_LOG");
        private static readonly int TraceSampleInterval = ResolveTraceSampleInterval();
        private static long _traceSequence;
        private static readonly Task ConsoleWriterTask = Task.Factory.StartNew(
            ProcessConsoleQueue,
            TaskCreationOptions.LongRunning);

        public static bool IsVerboseEnabled => VerboseOutputEnabled;

        public static bool IsFrameTraceEnabled => FrameTraceEnabled;

        public static void WriteLine(string message)
        {
            string safeMessage = message ?? string.Empty;

            if (ShouldWriteTrace(safeMessage))
                TryLogTrace(safeMessage);

            if (VerboseOutputEnabled)
                TryQueueConsoleWrite(safeMessage);
        }

        public static void WriteFrameTrace(string message)
        {
            if (!FrameTraceEnabled)
                return;

            string safeMessage = message ?? string.Empty;

            try
            {
                Logs.LogInfo(safeMessage);
            }
            catch
            {
                // Diagnostic logging must not affect production flow.
            }

            if (VerboseOutputEnabled)
                TryQueueConsoleWrite(safeMessage);
        }

        private static bool ShouldWriteTrace(string message)
        {
            if (IsImportantMessage(message))
                return true;

            if (!VerboseOutputEnabled)
                return false;

            if (TraceSampleInterval <= 1)
                return true;

            return Interlocked.Increment(ref _traceSequence) % TraceSampleInterval == 0;
        }

        private static bool IsImportantMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.Contains("failed", StringComparison.OrdinalIgnoreCase)
                || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
                || message.Contains("error", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryLogTrace(string message)
        {
            try
            {
                Logs.LogTrace(message);
            }
            catch
            {
                // Logging must not affect production flow.
            }
        }

        private static bool ResolveVerboseOutputEnabled()
        {
            return IsEnvironmentFlagEnabled("REEYIN_VERBOSE_DEFECT_OVERVIEW_LOG")
                || IsEnvironmentFlagEnabled("REEYIN_VERBOSE_FLOW_LOG");
        }

        private static bool IsEnvironmentFlagEnabled(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveTraceSampleInterval()
        {
            string value = Environment.GetEnvironmentVariable("REEYIN_DEFECT_OVERVIEW_LOG_SAMPLE");
            return int.TryParse(value, out int interval) && interval > 0
                ? interval
                : 1;
        }

        private static void TryQueueConsoleWrite(string message)
        {
            try
            {
                ConsoleQueue.TryAdd(message);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                try
                {
                    Logs.LogTrace($"[DefectOverview] Console queue ignored: {ex.Message}");
                }
                catch
                {
                    // Console output must not affect production flow.
                }
            }
        }

        private static void ProcessConsoleQueue()
        {
            foreach (string message in ConsoleQueue.GetConsumingEnumerable())
            {
                try
                {
                    Console.WriteLine(message);
                }
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    try
                    {
                        Logs.LogTrace($"[DefectOverview] Console write ignored: {ex.Message}");
                    }
                    catch
                    {
                        // Console output must not affect production flow.
                    }
                }
            }
        }
    }
}
