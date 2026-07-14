using ReeYin_V.Core.Helper.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Image
{
    /// <summary>
    /// 非托管数组基类
    /// </summary>
    public abstract class UnmanagedArrayBase : IDisposable
    {
        /// <summary>
        /// 图像数据的指针
        /// </summary>
        public IntPtr Header { get; private set; }

        /// <summary>
        /// 元素的数量
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// 单个元素的字节数长度
        /// </summary>
        private readonly int Size;

        public long Length => Count * Size;

        protected UnmanagedArrayBase(long count, int size, bool isResetMemory = true)
        {
            if (count <= 0) throw new ArgumentException("count不能小于零");
            if (size <= 0) throw new ArgumentException("size不能小于零");
            Count = count;
            Size = size;
            Header = Marshal.AllocHGlobal(new IntPtr(Length));
            if (isResetMemory)
                MemoryHelper.ZeroMemory(Header, Length);
        }

        ~UnmanagedArrayBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool disposed;

        protected void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                //todo  清理托管内存
            }

            if (Header != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Header);
                Header = IntPtr.Zero;
            }

            disposed = true;
        }
    }
}
