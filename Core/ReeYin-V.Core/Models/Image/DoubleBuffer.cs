using ReeYin_V.Core.Helper.Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Image
{
    enum BufferType
    {
        Buffer1, Buffer2
    }

    /// <summary>
    /// 双缓冲区的非托管二维数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class DoubleBuffer<T> : INotifyPropertyChanged, IDisposable where T : struct
    {
        private UnmanagedArray2D<T> Buffer1 { get; set; }
        private UnmanagedArray2D<T> Buffer2 { get; set; }
        private UnmanagedArray2D<T> _current;

        /// <summary>
        /// 当前图像数据
        /// </summary>
        public UnmanagedArray2D<T> Current
        {
            get { return _current; }
            set { _current = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前使用的缓冲区
        /// </summary>
        private BufferType BufferType { get; set; } = BufferType.Buffer1;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Write(UnmanagedArray2D<T> buffer)
        {
            if (buffer == null)
            {
                Current = null;
                return;
            }

            Write(buffer.Header, buffer.Width, buffer.Height);
        }

        public void Write(IntPtr header, int width, int height)
        {
            UnmanagedArray2D<T> result = null;
            BufferType next = BufferType.Buffer1;
            switch (BufferType)
            {
                case BufferType.Buffer1:
                    if (Buffer2 == null)
                    {
                        Buffer2 = new UnmanagedArray2D<T>(width, height);
                    }
                    result = Buffer2;
                    next = BufferType.Buffer2;
                    break;
                case BufferType.Buffer2:
                    if (Buffer1 == null)
                    {
                        Buffer1 = new UnmanagedArray2D<T>(width, height);
                    }
                    result = Buffer1;
                    next = BufferType.Buffer1;
                    break;
                default: break;
            }
            MemoryHelper.CopyMemory(result.Header, header, result.Length);
            BufferType = next;
            Current = result;
        }

        public void Dispose()
        {
            Buffer1?.Dispose();
            Buffer2?.Dispose();
        }
    }
}
