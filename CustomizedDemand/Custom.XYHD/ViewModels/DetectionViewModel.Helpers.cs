using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Custom.XYHD.Models;
using Custom.XYHD.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {
        private void EnsureFrameWatchdog()
        {
            RunOnUiThread(() =>
            {
                _frameWatchdogTimer ??= new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(FrameWatchdogIntervalMs)
                };
                _frameWatchdogTimer.Tick -= OnFrameWatchdogTick;
                _frameWatchdogTimer.Tick += OnFrameWatchdogTick;
                if (!_frameWatchdogTimer.IsEnabled)
                    _frameWatchdogTimer.Start();
            });
        }

        private void StopFrameWatchdog()
        {
            RunOnUiThread(() =>
            {
                if (_frameWatchdogTimer == null)
                    return;
                _frameWatchdogTimer.Stop();
                _frameWatchdogTimer.Tick -= OnFrameWatchdogTick;
            });
        }

        private void OnFrameWatchdogTick(object sender, EventArgs e)
        {
            var nowUtc = DateTime.UtcNow;
            CleanupStalePendingFrames(nowUtc);

            if (ShowNewFrameBadge && nowUtc >= _newFrameBadgeUntilUtc)
                ShowNewFrameBadge = false;

            if (_lastFrameUtc == DateTime.MinValue)
            {
                StreamState = "Idle";
                StreamStatusText = "等待首帧";
                _lastDisplayedStreamAgeBucket = -1;
                return;
            }

            var ageMs = (nowUtc - _lastFrameUtc).TotalMilliseconds;
            if (ageMs <= LiveTimeoutMs)
            {
                StreamState = "Live";
                UpdateStreamStatusTextByBucket(ageMs, "流正常");
            }
            else if (ageMs <= StaleTimeoutMs)
            {
                StreamState = "Warn";
                UpdateStreamStatusTextByBucket(ageMs, "等待新帧");
            }
            else
            {
                if (FrameCount <= 1)
                {
                    StreamState = "Idle";
                    StreamStatusText = $"单帧已完成 (帧数: {FrameCount})";
                    _lastDisplayedStreamAgeBucket = -1;
                }
                else
                {
                    StreamState = "Stale";
                    UpdateStreamStatusTextByBucket(ageMs, "疑似停流");
                }
            }
        }

        private void UpdateStreamStatusTextByBucket(double ageMs, string prefix)
        {
            var bucket = (int)(ageMs / 1000.0);
            if (bucket == _lastDisplayedStreamAgeBucket && StreamStatusText.StartsWith(prefix, StringComparison.Ordinal))
                return;

            _lastDisplayedStreamAgeBucket = bucket;
            StreamStatusText = bucket <= 0 ? prefix : $"{prefix} ({bucket}s)";
        }

        private void CleanupStalePendingFrames(DateTime nowUtc)
        {
            List<string> expiredKeys = null;
            List<PendingFrameUpdate> expiredFrames = null;

            lock (_pendingFrameLock)
            {
                foreach (var pair in _pendingFrames)
                {
                    if ((nowUtc - pair.Value.CreatedUtc).TotalMilliseconds <= StaleTimeoutMs * 2)
                        continue;

                    expiredKeys ??= [];
                    expiredKeys.Add(pair.Key);
                    expiredFrames ??= [];
                    expiredFrames.Add(pair.Value);
                }

                if (expiredKeys == null)
                    return;

                foreach (var key in expiredKeys)
                    _pendingFrames.Remove(key);
            }

            foreach (var frame in expiredFrames ?? [])
                DisposePendingFrameImages(frame);

            foreach (var key in expiredKeys)
                Model.AddLog($"整帧聚合超时，已清理未完成帧: {key}", "WARN");
        }

        private List<string> TrimPendingFramesLocked()
        {
            int overflow = _pendingFrames.Count - MaxPendingFrameUpdates;
            if (overflow <= 0)
                return null;

            var overflowKeys = _pendingFrames
                .OrderBy(item => item.Value.CreatedUtc)
                .ThenBy(item => item.Value.FrameId)
                .Take(overflow)
                .Select(item => item.Key)
                .ToList();

            foreach (string key in overflowKeys)
            {
                if (_pendingFrames.TryGetValue(key, out var frame))
                    DisposePendingFrameImages(frame);

                _pendingFrames.Remove(key);
            }

            return overflowKeys;
        }

        private void ClearPendingFrames()
        {
            lock (_pendingFrameLock)
            {
                foreach (var frame in _pendingFrames.Values)
                    DisposePendingFrameImages(frame);

                _pendingFrames.Clear();
            }
        }

        private string GetOrCreateFrameIdText(long frameId)
        {
            lock (_frameIdTextLock)
            {
                if (_frameIdTextCache.TryGetValue(frameId, out var existing))
                    return existing;

                var created = GenerateFrameIdText();
                _frameIdTextCache[frameId] = created;
                return created;
            }
        }

        private void RemoveFrameIdText(long frameId)
        {
            if (frameId <= 0)
                return;

            lock (_frameIdTextLock)
            {
                _frameIdTextCache.Remove(frameId);
            }
        }

        private void ClearFrameIdTextCache()
        {
            lock (_frameIdTextLock)
            {
                _frameIdTextCache.Clear();
            }
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }

        private void SaveImageIfNeeded(HImage image, bool isNG)
        {
            try
            {
                if (string.IsNullOrEmpty(Model.SavePath))
                    return;

                if (!Directory.Exists(Model.SavePath))
                    Directory.CreateDirectory(Model.SavePath);

                var shouldSave = (isNG && Model.SaveNGImages) || (!isNG && Model.SaveOKImages);
                if (!shouldSave)
                    return;

                var subFolder = isNG ? "NG" : "OK";
                var folder = Path.Combine(Model.SavePath, subFolder);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                var filePath = Path.Combine(folder, fileName);
                image.WriteImage("png", 0, filePath);
            }
            catch (Exception ex)
            {
                Model.AddLog($"保存图像失败: {ex.Message}", "ERROR");
            }
        }

        private static byte[] DownscaleImage(byte[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            var dest = new byte[dstWidth * dstHeight];
            var xRatio = (float)srcWidth / dstWidth;
            var yRatio = (float)srcHeight / dstHeight;
            for (int y = 0; y < dstHeight; y++)
            {
                var srcY = (int)(y * yRatio);
                for (int x = 0; x < dstWidth; x++)
                {
                    var srcX = (int)(x * xRatio);
                    dest[y * dstWidth + x] = source[srcY * srcWidth + srcX];
                }
            }
            return dest;
        }

        private static byte[] DownscaleImageRGB(byte[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            var dest = new byte[dstWidth * dstHeight * 3];
            var xRatio = (float)srcWidth / dstWidth;
            var yRatio = (float)srcHeight / dstHeight;
            for (int y = 0; y < dstHeight; y++)
            {
                var srcY = (int)(y * yRatio);
                for (int x = 0; x < dstWidth; x++)
                {
                    var srcX = (int)(x * xRatio);
                    int srcIdx = (srcY * srcWidth + srcX) * 3;
                    int dstIdx = (y * dstWidth + x) * 3;
                    dest[dstIdx] = source[srcIdx];
                    dest[dstIdx + 1] = source[srcIdx + 1];
                    dest[dstIdx + 2] = source[srcIdx + 2];
                }
            }
            return dest;
        }
    }
}
