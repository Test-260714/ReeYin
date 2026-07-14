using HalconDotNet;
using ReeYin_V.Core.Helper.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Image
{
    public sealed class UnmanagedArray2D<T> : UnmanagedArrayBase where T : struct
    {
        /// <summary>
        /// 相机宽度
        /// </summary>
        public int Width { get; }
        /// <summary>
        /// 相机高度
        /// </summary>
        public int Height { get; }

        public int Stride => Width * Marshal.SizeOf(typeof(T));
        public UnmanagedArray2D(int width, int height, bool isResetMemory = true) : base(width * height, Marshal.SizeOf(typeof(T)), isResetMemory)
        {
            Width = width;
            Height = height;
        }

        public UnmanagedArray2D<T> DeepClone()
        {
            var temp = new UnmanagedArray2D<T>(Width, Height);
            MemoryHelper.CopyMemory(temp.Header, this.Header, Length);
            return temp;
        }

        /// <summary>
        /// 转为Halcon_Mono8
        /// </summary>
        /// <returns></returns>
        public HImage GetHalconImage_Mono8()
        {
            return new HImage("byte", Width, Height, Header);
        }

        /// <summary>
        /// 转为Halcon_RGB
        /// </summary>
        /// <returns></returns>
        public HImage GetHalconImage_RGB()
        {
            HImage img = new HImage();
            img.GenImageInterleaved(Header, "rgb", Width, Height, -1, "byte",  0, 0, 0, 0, -1, 0 );
            return img;
        }
    }
}
