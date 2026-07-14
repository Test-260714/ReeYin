using OpenCvSharp;
using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core
{
    public class ReeYinImage
    {
        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 每个灰度值的字节数
        /// </summary>
        public int ElementSize { get; set; }

        /// <summary>
        /// 通道数
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// 图像位深
        /// </summary>
        public EnumImageDepth Depth { get; set; }

        /// <summary>
        /// 图像数据
        /// </summary>
        public byte[] Data { get; set; }


        public ReeYinImage(int width, int height, int elementSize, int channels, EnumImageDepth depth)
        {
            this.Width = width;
            this.Height = height;
            this.ElementSize = elementSize;
            this.Channels = channels;
            this.Depth = depth;
            this.Data = new byte[Width * Height * ElementSize * Channels];
        }

        public ReeYinImage(int width, int height, int elementSize, int channels, EnumImageDepth depth, byte[] data)
        {
            this.Width = width;
            this.Height = height;
            this.ElementSize = elementSize;
            this.Channels = channels;
            this.Depth = depth;
            this.Data = data;
        }

        public Mat ConvertToCvMat()
        {
            Mat image = new Mat();

            int arrayLength = Width * Height * Channels;

            if (Depth == EnumImageDepth.UInt8)
            {
                byte[] array = new byte[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_8UC(Channels), array);
            }
            else if (Depth == EnumImageDepth.Int8)
            {
                sbyte[] array = new sbyte[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_8SC(Channels), array);
            }
            else if (Depth == EnumImageDepth.UInt16)
            {
                ushort[] array = new ushort[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_16UC(Channels), array);
            }
            else if (Depth == EnumImageDepth.Int16)
            {
                short[] array = new short[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_16SC(Channels), array);
            }
            else if (Depth == EnumImageDepth.Int32)
            {
                int[] array = new int[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_32SC(Channels), array);
            }
            else if (Depth == EnumImageDepth.Float32)
            {
                float[] array = new float[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_32FC(Channels), array);
            }
            else if (Depth == EnumImageDepth.Double64)
            {
                double[] array = new double[arrayLength];
                Buffer.BlockCopy(Data, 0, array, 0, Data.Length);
                image = Mat.FromPixelData(Height, Width, MatType.CV_64FC(Channels), array);
            }


            return image;
        }


    }
}
