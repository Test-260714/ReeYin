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
        /// <summary>
        /// 批次管理器（只读，供 UI 绑定）
        /// </summary>
        public BatchManager BatchManager => _batchManager;

        /// <summary>
        /// 最后生成的 FrameId 文本（批号+日期时间格式）
        /// </summary>
        private string _lastFrameIdText = "-";
        public string LastFrameIdText
        {
            get => _lastFrameIdText;
            set
            {
                if (_lastFrameIdText == value)
                    return;
                _lastFrameIdText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 初始化批次管理器（隔离调用，不影响主流程）
        /// </summary>
        private void InitBatchManager()
        {
            try
            {
                _batchManager = new BatchManager();
                _batchManager.OnLog += (msg, level) => Model?.AddLog($"[批次] {msg}", level);
                RaisePropertyChanged(nameof(BatchManager));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XYHD] 初始化批次管理器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 换卷命令
        /// </summary>
        private void OnChangeBatch()
        {
            try
            {
                if (_batchManager == null)
                {
                    Model?.AddLog("批次管理器未初始化", "WARN");
                    return;
                }

                // 确保保存路径已设置
                if (!string.IsNullOrWhiteSpace(Model?.SavePath))
                    _batchManager.SavePath = Model.SavePath;

                if (_batchManager.ChangeBatch())
                {
                    // 重置帧计数（但不重置统计）
                    FrameCount = 0;
                    LastFrameIdText = "-";
                    ResolveBandMapStateService()?.Reset();
                    RaisePropertyChanged(nameof(BatchManager));
                    
                    MessageBox.Show($"换卷成功！\n当前批号: {_batchManager.BatchNumberText}", 
                        "换卷", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Model?.AddLog($"换卷异常: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// 保存缺陷记录命令
        /// </summary>
        private void OnSaveDefects()
        {
            try
            {
                if (_batchManager == null)
                {
                    Model?.AddLog("批次管理器未初始化", "WARN");
                    return;
                }

                // 确保保存路径已设置
                if (string.IsNullOrWhiteSpace(Model?.SavePath))
                {
                    MessageBox.Show("请先设置保存路径！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SelectSavePath();
                    if (string.IsNullOrWhiteSpace(Model?.SavePath))
                        return;
                }

                _batchManager.SavePath = Model.SavePath;
                var savedPath = _batchManager.SaveDefects();

                if (!string.IsNullOrEmpty(savedPath))
                {
                    var result = MessageBox.Show($"缺陷记录已保存到:\n{savedPath}\n\n是否打开文件夹？", 
                        "保存成功", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", savedPath);
                    }
                }
                else
                {
                    MessageBox.Show("保存失败，请检查日志", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Model?.AddLog($"保存缺陷记录异常: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// 安全记录帧结果到批次管理器（隔离调用）
        /// </summary>
        private void SafeRecordFrame(string frameIdText, bool isNG, List<ReeYin_V.Core.DeepLearning.Result> results, string pathName)
        {
            try
            {
                if (_batchManager == null) return;

                var defects = results?
                    .Where(r => r != null)
                    .Select(r => (r.ClassName ?? "Unknown", (double)r.Confidence))
                    .ToList();

                _batchManager.RecordFrame(frameIdText, isNG, defects, pathName);
            }
            catch (Exception ex)
            {
                // 记录日志但不影响主流程
                System.Diagnostics.Debug.WriteLine($"[XYHD] 批次记录异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成新的 FrameId 文本
        /// </summary>
        private string GenerateFrameIdText()
        {
            try
            {
                if (_batchManager != null)
                {
                    var frameIdText = _batchManager.GenerateFrameId();
                    LastFrameIdText = frameIdText;
                    return frameIdText;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XYHD] 生成FrameId异常: {ex.Message}");
            }

            // 兜底：使用时间戳
            var fallback = $"F-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
            LastFrameIdText = fallback;
            return fallback;
        }
    }
}
