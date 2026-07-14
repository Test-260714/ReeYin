using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace Custom.XYHD.Models
{
    /// <summary>
    /// 缺陷记录
    /// </summary>
    public class DefectRecord
    {
        public string FrameId { get; set; }
        public DateTime Time { get; set; }
        public string DefectType { get; set; }
        public string ImagePath { get; set; }
        public double Confidence { get; set; }
        public string PathName { get; set; }  // "左" / "右"
    }

    /// <summary>
    /// 批次统计信息
    /// </summary>
    public class BatchSummary
    {
        public int BatchNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalFrames { get; set; }
        public int OKFrames { get; set; }
        public int NGFrames { get; set; }
        public double NGRate => TotalFrames > 0 ? (double)NGFrames / TotalFrames * 100 : 0;
        public List<DefectRecord> DefectRecords { get; set; } = new();
    }

    /// <summary>
    /// 批次管理器 - 独立的批次和缺陷记录管理
    /// 
    /// 设计原则：
    /// 1. 完全独立，不依赖其他 XYHD 类的内部实现
    /// 2. 所有操作都有异常保护，不影响主流程
    /// 3. 文件操作异步执行，不阻塞 UI
    /// </summary>
    public class BatchManager : BindableBase, IDisposable
    {
        #region 字段
        private readonly object _lock = new object();
        private bool _disposed = false;
        #endregion

        #region 属性
        private int _batchNumber = 1;
        /// <summary>
        /// 当前批号
        /// </summary>
        public int BatchNumber
        {
            get => _batchNumber;
            private set => SetProperty(ref _batchNumber, value);
        }

        private int _frameIndex = 0;
        /// <summary>
        /// 当前批次内帧序号
        /// </summary>
        public int FrameIndex
        {
            get => _frameIndex;
            private set => SetProperty(ref _frameIndex, value);
        }

        private DateTime _batchStartTime = DateTime.Now;
        /// <summary>
        /// 批次开始时间
        /// </summary>
        public DateTime BatchStartTime
        {
            get => _batchStartTime;
            private set => SetProperty(ref _batchStartTime, value);
        }

        private int _totalFrames = 0;
        /// <summary>
        /// 批次总帧数
        /// </summary>
        public int TotalFrames
        {
            get => _totalFrames;
            private set => SetProperty(ref _totalFrames, value);
        }

        private int _okFrames = 0;
        /// <summary>
        /// OK帧数
        /// </summary>
        public int OKFrames
        {
            get => _okFrames;
            private set => SetProperty(ref _okFrames, value);
        }

        private int _ngFrames = 0;
        /// <summary>
        /// NG帧数
        /// </summary>
        public int NGFrames
        {
            get => _ngFrames;
            private set => SetProperty(ref _ngFrames, value);
        }

        /// <summary>
        /// NG率 (%)
        /// </summary>
        public double NGRate => TotalFrames > 0 ? (double)NGFrames / TotalFrames * 100 : 0;

        private string _savePath = "";
        /// <summary>
        /// 保存路径
        /// </summary>
        public string SavePath
        {
            get => _savePath;
            set => SetProperty(ref _savePath, value);
        }

        /// <summary>
        /// 当前批次的缺陷记录
        /// </summary>
        public ObservableCollection<DefectRecord> DefectRecords { get; } = new();

        /// <summary>
        /// 批号显示文本
        /// </summary>
        public string BatchNumberText => $"B{BatchNumber:D3}";

        /// <summary>
        /// 最后生成的 FrameId
        /// </summary>
        public string LastFrameId { get; private set; } = "";
        #endregion

        #region 事件
        /// <summary>
        /// 日志事件
        /// </summary>
        public event Action<string, string> OnLog;
        #endregion

        #region 构造函数
        public BatchManager()
        {
            BatchStartTime = DateTime.Now;
        }

        public BatchManager(string savePath) : this()
        {
            SavePath = savePath;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 生成新的 FrameId
        /// 格式: B001-20260306-143052-0001
        /// </summary>
        public string GenerateFrameId()
        {
            return RunOnUiThread(() =>
            {
                lock (_lock)
                {
                    FrameIndex++;
                    var now = DateTime.Now;
                    LastFrameId = $"B{BatchNumber:D3}-{now:yyyyMMdd}-{now:HHmmss}-{FrameIndex:D4}";
                    return LastFrameId;
                }
            });
        }

        /// <summary>
        /// 记录一帧结果
        /// </summary>
        /// <param name="frameId">帧ID</param>
        /// <param name="isNG">是否NG</param>
        /// <param name="defects">缺陷列表（可为null）</param>
        /// <param name="pathName">路径名称</param>
        public void RecordFrame(string frameId, bool isNG, IEnumerable<(string defectType, double confidence)> defects = null, string pathName = null)
        {
            RunOnUiThread(() =>
            {
                lock (_lock)
                {
                    TotalFrames++;
                    if (isNG)
                    {
                        NGFrames++;
                        if (defects != null)
                        {
                            foreach (var (defectType, confidence) in defects)
                            {
                                DefectRecords.Add(new DefectRecord
                                {
                                    FrameId = frameId,
                                    Time = DateTime.Now,
                                    DefectType = defectType,
                                    Confidence = confidence,
                                    PathName = pathName ?? ""
                                });
                            }
                        }
                    }
                    else
                    {
                        OKFrames++;
                    }

                    RaisePropertyChanged(nameof(NGRate));
                }
            });
        }

        /// <summary>
        /// 换卷：批号+1，序号归零，保存当前批次
        /// </summary>
        /// <returns>是否成功</returns>
        public bool ChangeBatch()
        {
            try
            {
                return RunOnUiThread(() =>
                {
                    lock (_lock)
                    {
                        if (TotalFrames > 0)
                        {
                            SaveBatchSummaryAsync();
                        }

                        BatchNumber++;
                        FrameIndex = 0;
                        TotalFrames = 0;
                        OKFrames = 0;
                        NGFrames = 0;
                        DefectRecords.Clear();
                        BatchStartTime = DateTime.Now;
                        LastFrameId = "";

                        RaisePropertyChanged(nameof(BatchNumberText));
                        RaisePropertyChanged(nameof(NGRate));

                        Log($"换卷成功，当前批号: {BatchNumberText}", "INFO");
                        return true;
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"换卷失败: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// 保存缺陷记录到文件
        /// </summary>
        /// <returns>保存路径，失败返回null</returns>
        public string SaveDefects()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SavePath))
                {
                    Log("保存路径未设置", "WARN");
                    return null;
                }

                var batchFolder = GetBatchFolder();
                Directory.CreateDirectory(batchFolder);

                // 保存 CSV
                var csvPath = Path.Combine(batchFolder, $"defects_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                SaveDefectsCsv(csvPath);

                // 保存 JSON
                var jsonPath = Path.Combine(batchFolder, $"batch_summary_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                SaveBatchSummaryJson(jsonPath);

                Log($"缺陷记录已保存: {batchFolder}", "INFO");
                return batchFolder;
            }
            catch (Exception ex)
            {
                Log($"保存缺陷记录失败: {ex.Message}", "ERROR");
                return null;
            }
        }

        /// <summary>
        /// 设置缺陷图片路径
        /// </summary>
        public void SetDefectImagePath(string frameId, string imagePath)
        {
            lock (_lock)
            {
                var records = DefectRecords.Where(r => r.FrameId == frameId).ToList();
                foreach (var record in records)
                {
                    record.ImagePath = imagePath;
                }
            }
        }

        /// <summary>
        /// 获取当前批次文件夹路径
        /// </summary>
        public string GetBatchFolder()
        {
            return Path.Combine(SavePath, BatchNumberText);
        }

        /// <summary>
        /// 获取当前批次统计摘要
        /// </summary>
        public BatchSummary GetCurrentSummary()
        {
            lock (_lock)
            {
                return new BatchSummary
                {
                    BatchNumber = BatchNumber,
                    StartTime = BatchStartTime,
                    EndTime = null,
                    TotalFrames = TotalFrames,
                    OKFrames = OKFrames,
                    NGFrames = NGFrames,
                    DefectRecords = DefectRecords.ToList()
                };
            }
        }
        #endregion

        #region 私有方法
        private void SaveDefectsCsv(string path)
        {
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("FrameId,Time,PathName,DefectType,Confidence,ImagePath");
            
            lock (_lock)
            {
                foreach (var record in DefectRecords)
                {
                    writer.WriteLine($"{record.FrameId},{record.Time:yyyy-MM-dd HH:mm:ss.fff},{record.PathName},{record.DefectType},{record.Confidence:F2},{record.ImagePath}");
                }
            }
        }

        private void SaveBatchSummaryJson(string path)
        {
            var summary = GetCurrentSummary();
            summary.EndTime = DateTime.Now;
            
            var json = JsonConvert.SerializeObject(summary, Formatting.Indented);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private void SaveBatchSummaryAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(SavePath)) return;
                    
                    var batchFolder = GetBatchFolder();
                    Directory.CreateDirectory(batchFolder);
                    
                    var jsonPath = Path.Combine(batchFolder, "batch_summary.json");
                    SaveBatchSummaryJson(jsonPath);
                    
                    var csvPath = Path.Combine(batchFolder, "defects.csv");
                    SaveDefectsCsv(csvPath);
                }
                catch (Exception ex)
                {
                    Log($"异步保存批次信息失败: {ex.Message}", "WARN");
                }
            });
        }

        private void Log(string message, string level)
        {
            OnLog?.Invoke(message, level);
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

            dispatcher.Invoke(action);
        }

        private static T RunOnUiThread<T>(Func<T> func)
        {
            if (func == null)
                return default;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                return func();

            return dispatcher.Invoke(func);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // 保存最后的批次信息
                if (TotalFrames > 0)
                {
                    SaveBatchSummaryAsync();
                }
            }
            catch { }
        }
        #endregion
    }
}
