using HalconDotNet;
using Prism.Mvvm;
using System;

namespace Custom.XYHD.Models
{
    /// <summary>
    /// 日志项
    /// </summary>
    public class LogItem : BindableBase
    {
        public string Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }

        public LogItem(string level, string message)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            Level = level;
            Message = message;
        }
    }

    [Serializable]
    public class XYHDInputPacket : IDisposable
    {
        public HImage OriginalImage { get; set; }

        /// <summary>
        /// 该路径对应的子图（左半或右半），用于在界面上显示并绘制缺陷框
        /// </summary>
        public HImage PathImage { get; set; }
        public object IsOks { get; set; }
        public object DefectResults { get; set; }
        /// <summary>
        /// 帧ID（时间戳，保留兼容）
        /// </summary>
        public long FrameId { get; set; }
        /// <summary>
        /// 帧ID文本（批号+日期时间格式，如 B001-20260306-143052-0001）
        /// </summary>
        public string FrameIdText { get; set; }
        public DateTime ReceiveTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 来源 DL 节点的 Serial（连续采集模式下用于区分不同推理路径）
        /// </summary>
        public int SourceSerial { get; set; } = -1;
        public int OwnerSerial { get; set; } = -1;
        public bool HasFieldOrientationSettings { get; set; }
        public bool SwapLeftRightPaths { get; set; }
        public bool LeftPathXMirror { get; set; }
        public bool RightPathXMirror { get; set; }
        /// <summary>
        /// 路径名称（"左"/"右"），UI 根据此决定显示到哪个窗口
        /// </summary>
        public string PathName { get; set; }

        public void Dispose()
        {
            DisposeImage(OriginalImage);
            DisposeImage(PathImage);
            OriginalImage = null;
            PathImage = null;
        }

        private static void DisposeImage(HImage image)
        {
            try
            {
                image?.Dispose();
            }
            catch
            {
            }
        }
    }
}
