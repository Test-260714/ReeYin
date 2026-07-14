using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dra = System.Drawing;
using Ima = System.Drawing.Imaging;

namespace ReeYin.Hardware.Sensor.Hyperson.API
{
    public class LCF_Util
    {
        static LCF_Util instance = new LCF_Util();
        [DllImport("kernel32.dll")]
        private static extern int LoadLibrary(string lpAppName);
        [DllImport("kernel32.dll", EntryPoint = "GetModuleFileNameA")]
        private static extern int GetModuleFileName(int rlib, byte[] lptName, int lenName);
        [DllImport("kernel32.dll")]
        private static extern int FreeLibrary(int libHandle);
        public int width = ShareObjects.width;
        public int height = ShareObjects.height;
        ShareObjects g_params = null;
        private static Dra.Color[] RedToBluecolor = new Dra.Color[ShareObjects.ColorNumber];
        private static Dra.Color[] BlueToRedcolor = new Dra.Color[ShareObjects.ColorNumber];


        public struct CoordinateTypeDef
        {
            public int x;
            public int y;
        };
        public struct ConnectedDomainInfoTypeDef
        {
            public CoordinateTypeDef[] pos;

        };
        public enum ConnectedDomainModeTypeDef
        {
            FOUR_CONNECTED_DOMAIN,
            EIGHT_CONNECTED_DOMAIN
        };


        LCF_Util()
        {
            g_params = ShareObjects.getInstance();
        }
        public static LCF_Util getInstance()
        {

            CreatColorTable(RedToBluecolor, RedToBluecolor.Length, 0);
            CreatColorTable(BlueToRedcolor, BlueToRedcolor.Length, 1);
            return instance;
        }
        public bool RecordOK = false;
        public string FileNamePath = "";




        /// <summary>
        /// interpolate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        static double interpolate(double x, double x0, double y0, double x1, double y1)
        {
            if (x1 == x0)
            {
                return y0;
            }
            else
            {
                return ((x - x0) * (y1 - y0) / (x1 - x0) + y0);
            }
        }
        /// <summary>
        /// 创建颜色索引
        /// </summary>
        /// <param name="numSteps"></param>
        /// <param name="indx"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        static int createColorMapPixel(int numSteps, int indx)
        {
            byte red;
            byte green;
            byte blue;
            double k = 1;
            double B0 = -0.125 * k - 0.25;
            double B1 = B0 + 0.25 * k;
            double B2 = B1 + 0.25 * k;
            double B3 = B2 + 0.25 * k;

            double G0 = B1;
            double G1 = G0 + 0.25 * k;
            double G2 = G1 + 0.25 * k;
            double G3 = G2 + 0.25 * k + 0.125;

            double R0 = B2;
            double R1 = R0 + 0.25 * k;
            double R2 = R1 + 0.25 * k;
            double R3 = R2 + 0.25 * k + 0.25;

            double i = (double)indx / (double)numSteps - 0.25 * k;

            if (i >= R0 && i < R1)
            {
                red = (byte)(interpolate(i, R0, 0, R1, 255));
            }
            else if ((i >= R1) && (i < R2))
            {
                red = 255;
            }
            else if ((i >= R2) && (i < R3))
            {
                red = (byte)interpolate(i, R2, 255, R3, 0);
            }
            else
            {
                red = 0;
            }

            if (i >= G0 && i < G1)
            {
                green = (byte)interpolate(i, G0, 0, G1, 255);
            }
            else if ((i >= G1) && (i < G2))
            {
                green = 255;
            }
            else if ((i >= G2) && (i < G3))
            {
                green = (byte)interpolate(i, G2, 255, G3, 0);
            }
            else
            {
                green = 0;
            }


            if (i >= B0 && i < B1)
            {
                blue = (byte)interpolate(i, B0, 0, B1, 255);
            }
            else if ((i >= B1) && (i < B2))
            {
                blue = 255;
            }
            else if ((i >= B2) && (i < B3))
            {
                blue = (byte)interpolate(i, B2, 255, B3, 0);
            }
            else
            {
                blue = 0;
            }

            return ((red << 16) + (green << 8) + blue);
        }

        /// <summary>
        /// 创建颜色表
        /// </summary>
        /// <param name="ColorTable"></param>
        /// <param name="tableSize"></param>
        public static void CreatColorTable(Dra.Color[] ColorTable, int tableSize, byte sel)
        {
            byte red = 0, green = 0, blue = 0;
            int step = 0;
            for (int i = 0; i < tableSize; i++)
            {
                int rgb = createColorMapPixel(tableSize, i + step);
                red = (byte)((rgb >> 16) & 0xFF);
                green = (byte)((rgb >> 8) & 0xFF);
                blue = (byte)((rgb >> 0) & 0xFF);
                if (sel == 0)
                {
                    ColorTable[tableSize - i - 1] = Dra.Color.FromArgb(red, green, blue);
                }
                else
                {
                    ColorTable[i] = Dra.Color.FromArgb(red, green, blue);
                }
            }
        }

        /// <summary>
        /// 将距离数据转换为颜色数据
        /// </summary>
        /// <param name="rawValues"></param>
        /// <param name="ColorValue"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void DistanceToColor(int[] rawValues, byte[] ColorValue, byte sel)
        {
            if (rawValues == null)
            {
                return;
            }
            UInt32 colorMaxValue = 50;
            UInt32 scal = (UInt32)(BlueToRedcolor.Length / colorMaxValue);
            int posScan = 0;
            Dra.Color tempColor;
            int tempData = 0;
            int colorIndx = 0;
            // Parallel.For(0, rawValues.Length, posReal =>
            for (int posReal = 0; posReal < rawValues.Length; posReal++)
            {
                if (rawValues[posReal] > 0 && rawValues[posReal] < colorMaxValue)
                {
                    scal = (UInt32)(BlueToRedcolor.Length / colorMaxValue);
                    colorIndx = (int)(rawValues[posReal] * scal);
                    colorIndx = colorIndx > RedToBluecolor.Length - 1 ? RedToBluecolor.Length - 1 : colorIndx;
                    tempColor = sel == 0 ? RedToBluecolor[colorIndx] : BlueToRedcolor[colorIndx]; //找到该距离对应的颜色    
                }
                else if (rawValues[posReal] >= colorMaxValue && rawValues[posReal] < 13000)
                {
                    scal = (UInt32)(BlueToRedcolor.Length / 13000);
                    colorIndx = (int)(rawValues[posReal] * scal + RedToBluecolor.Length - 26000);
                    colorIndx = colorIndx > RedToBluecolor.Length - 1 ? RedToBluecolor.Length - 1 : colorIndx;
                    tempColor = sel == 0 ? RedToBluecolor[colorIndx] : BlueToRedcolor[colorIndx]; //找到该距离对应的颜色                   
                }
                else
                {
                    if (rawValues[posReal] == ShareObjects.LOW_AMPLITUDE || rawValues[posReal] == 0 || (rawValues[posReal] >= 13000 && rawValues[posReal] < 15000))  //LOW_AMPLITUDE
                    {
                        tempColor = Dra.Color.FromArgb(0, 0, 0);//能量太低显示黑色
                    }
                    else if (rawValues[posReal] == ShareObjects.SATURATION || rawValues[posReal] == ShareObjects.ADC_OVERFLOW) //SATURATION
                    {
                        tempColor = Dra.Color.FromArgb(255, 255, 255); //饱和位饱和显示白色
                    }
                    else if (rawValues[posReal] == ShareObjects.INVALID_DATA)
                    {
                        tempColor = Dra.Color.FromArgb(0, 0, 0);//能量太低显示黑色
                    }
                    else if (rawValues[posReal] == 65531)
                    {
                        tempColor = Dra.Color.FromArgb(255, 0, 255);//最小值颜色
                    }
                    else if (rawValues[posReal] == 65532)
                    {
                        tempColor = Dra.Color.FromArgb(0, 0, 0);//受环境光干扰为黑色
                    }
                    else
                    {
                        tempColor = Dra.Color.FromArgb(255, 0, 0);
                    }
                }
                tempData = tempColor.ToArgb(); //获取相应的ARGB分量
                ColorValue[posScan] = (byte)(tempData & 0xFF);            //保存红色分量
                ColorValue[posScan + 1] = (byte)((tempData >> 8) & 0xFF); //保存绿色分量
                ColorValue[posScan + 2] = (byte)((tempData >> 16) & 0xFF);//保存蓝色分量
                posScan += 3;
            }
            // );
        }


        /// <summary>
        /// 将距离数据转换为颜色数据
        /// </summary>
        /// <param name="rawValues"></param>
        /// <param name="ColorValue"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void DistanceToColor(int[] rawValues, byte[] ColorValue, int maxVal, int minVal)
        {
            if (rawValues == null)
            {
                return;
            }

            int posScan = 0;
            Dra.Color tempColor;
            int tempData = 0;
            int indx = 0;
            for (int posReal = 0; posReal < rawValues.Length; posReal++)
            {
                if (rawValues[posReal] == ShareObjects.LOW_AMPLITUDE || rawValues[posReal] == 0 || rawValues[posReal] == ShareObjects.INVALID_DATA || rawValues[posReal] == 65532)  //LOW_AMPLITUDE
                {
                    tempColor = Dra.Color.FromArgb(0, 0, 0);//能量太低显示黑色
                }
                else if (rawValues[posReal] == ShareObjects.SATURATION || rawValues[posReal] == ShareObjects.ADC_OVERFLOW) //SATURATION
                {
                    tempColor = Dra.Color.FromArgb(255, 255, 255); //饱和位饱和显示白色
                }
                else if (rawValues[posReal] == 65531)
                {
                    tempColor = Dra.Color.FromArgb(255, 0, 255);//最小值颜色
                }
                else
                {
                    indx = (rawValues[posReal] - maxVal) * (RedToBluecolor.Length - 1) / (minVal - maxVal);
                    indx = indx < 0 ? 0 : indx;
                    indx = indx >= RedToBluecolor.Length ? RedToBluecolor.Length - 1 : indx;
                    tempColor = RedToBluecolor[indx]; //找到该距离对应的颜色    
                }

                tempData = tempColor.ToArgb(); //获取相应的ARGB分量
                ColorValue[posScan] = (byte)(tempData & 0xFF);            //保存红色分量
                ColorValue[posScan + 1] = (byte)((tempData >> 8) & 0xFF); //保存绿色分量
                ColorValue[posScan + 2] = (byte)((tempData >> 16) & 0xFF);//保存蓝色分量
                posScan += 3;

            }
        }


        //将数组转换成结构体
        public static object BytesToStuct(byte[] bytes, int pos, Type type)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(type);
            //byte数组长度小于结构体的大小
            if (size > bytes.Length + pos)
            {
                //返回空
                return null;
            }
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组拷到分配好的内存空间
            Marshal.Copy(bytes, pos, structPtr, size);
            //将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回结构体
            return obj;
        }

        /// <summary>
        /// 将一个字节数组转换为8bit灰度位图
        /// </summary>
        /// <param name="rawValues">显示字节数组</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns>位图</returns>
        public Dra.Bitmap ToGrayBitmap(byte[] rawValues, int width, int height)
        {
            //// 申请目标位图的变量，并将其内存区域锁定
            Bitmap bmp = new Bitmap(width, height, Ima.PixelFormat.Format8bppIndexed);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, Ima.PixelFormat.Format8bppIndexed);

            //// 获取图像参数
            int stride = bmpData.Stride;  // 扫描线的宽度
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置
            int scanBytes = stride * height;   // 用stride宽度，表示这是内存区域的大小
            //// 下面把原始的显示大小字节数组转换为内存中实际存放的字节数组
            int posScan = 0, posReal = 0;   // 分别设置两个位置指针，指向源数组和目标数组
            byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存
            for (int x = 0; x < height; x++)
            {
                //// 下面的循环节是模拟行扫描
                for (int y = 0; y < width; y++)
                {
                    pixelValues[posScan++] = (byte)(rawValues[posReal++] & 0xFF);//(byte)(rawValues[posReal++] & 0xFFFF);
                }
                posScan += offset;  //行扫描结束，要将目标位置指针移过那段“间隙”
            }

            //// 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中
            System.Runtime.InteropServices.Marshal.Copy(pixelValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域

            //// 下面的代码是为了修改生成位图的索引表，从伪彩修改为灰度
            ColorPalette tempPalette;
            using (Bitmap tempBmp = new Bitmap(1, 1, Ima.PixelFormat.Format8bppIndexed))
            {
                tempPalette = tempBmp.Palette;
            }
            for (int i = 0; i < 256; i++)
            {
                tempPalette.Entries[i] = Dra.Color.FromArgb(i, i, i);
            }

            bmp.Palette = tempPalette;

            //// 算法到此结束，返回结果
            return bmp;
        }

        // <summary>
        // 将一个4字节数组转换为32位彩色图
        // </summary>0
        // <param name="rawValues">显示数组</param>
        // <param name="width">图像宽度</param>
        // <param name="height">图像高度</param>
        // <returns>位图</returns>
        public Bitmap ToBMPmap(int[] rawValues, int width, int height)
        {
            //// 申请目标位图的变量，并将其内存区域锁定
            Bitmap bmp = new Bitmap(width, height, Ima.PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                 ImageLockMode.WriteOnly, Ima.PixelFormat.Format24bppRgb);

            //// 获取图像参数
            int stride = bmpData.Stride;  // 扫描线的宽度
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置
            int scanBytes = stride * height;   // 用stride宽度，表示这是内存区域的大小
            byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存
            DistanceToColor(rawValues, pixelValues, 0);//距离数据转换为颜色
            //// 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中
            System.Runtime.InteropServices.Marshal.Copy(pixelValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域       
            //// 算法到此结束，返回结果
            return bmp;
        }


        // <summary>
        // 将一个4字节数组转换为32位彩色图
        // </summary>0
        // <param name="rawValues">显示数组</param>
        // <param name="width">图像宽度</param>
        // <param name="height">图像高度</param>
        // <returns>位图</returns>
        public Bitmap ToBMPmap(int[] rawValues, int width, int height, int MaxVal, int MinVal)
        {
            //// 申请目标位图的变量，并将其内存区域锁定
            Bitmap bmp = new Bitmap(width, height, Ima.PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                 ImageLockMode.WriteOnly, Ima.PixelFormat.Format24bppRgb);

            //// 获取图像参数
            int stride = bmpData.Stride;  // 扫描线的宽度
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置
            int scanBytes = stride * height;   // 用stride宽度，表示这是内存区域的大小
            byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存
            DistanceToColor(rawValues, pixelValues, MaxVal, MinVal);//距离数据转换为颜色
            // 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中
            System.Runtime.InteropServices.Marshal.Copy(pixelValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域       
            // 算法到此结束，返回结果
            return bmp;
        }
        public enum ZoomType { NearestNeighborInterpolation, BilinearInterpolation }
        /// <summary>
        /// 图像缩放
        /// </summary>
        /// <param name="srcBmp">原始图像</param>
        /// <param name="width">目标图像宽度缩放比例</param>
        /// <param name="height">目标图像高度缩放比例</param>
        /// <param name="dstBmp">目标图像</param>
        /// <param name="GetNearOrBil">缩放选用的算法</param>
        /// <returns>处理成功 true 失败 false</returns>
        public static bool Zoom(Bitmap srcBmp, double ratioW, double ratioH, out Bitmap dstBmp, ZoomType zoomType)
        {//ZoomType为自定义的枚举类型
            if (srcBmp == null)
            {
                dstBmp = null;
                return false;
            }
            //若缩放大小与原图一样，则返回原图不做处理
            if ((ratioW == 1.0) && ratioH == 1.0)
            {
                dstBmp = new Bitmap(srcBmp);
                return true;
            }
            //计算缩放高宽
            double height = ratioH * (double)srcBmp.Height;
            double width = ratioW * (double)srcBmp.Width;
            dstBmp = new Bitmap((int)width, (int)height);

            BitmapData srcBmpData = srcBmp.LockBits(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), ImageLockMode.ReadWrite, Ima.PixelFormat.Format24bppRgb);
            BitmapData dstBmpData = dstBmp.LockBits(new Rectangle(0, 0, dstBmp.Width, dstBmp.Height), ImageLockMode.ReadWrite, Ima.PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* srcPtr = null;
                byte* dstPtr = null;
                int srcI = 0;
                int srcJ = 0;
                double srcdI = 0;
                double srcdJ = 0;
                double a = 0;
                double b = 0;
                double F1 = 0;//横向插值所得数值
                double F2 = 0;//纵向插值所得数值
                if (zoomType == ZoomType.NearestNeighborInterpolation)
                {//邻近插值法

                    for (int i = 0; i < dstBmp.Height; i++)
                    {
                        srcI = (int)(i / ratioH);//srcI是此时的i对应的原图像的高
                        srcPtr = (byte*)srcBmpData.Scan0 + srcI * srcBmpData.Stride;
                        dstPtr = (byte*)dstBmpData.Scan0 + i * dstBmpData.Stride;
                        for (int j = 0; j < dstBmp.Width; j++)
                        {
                            dstPtr[j * 3] = srcPtr[(int)(j / ratioW) * 3];//j / ratioW求出此时j对应的原图像的宽
                            dstPtr[j * 3 + 1] = srcPtr[(int)(j / ratioW) * 3 + 1];
                            dstPtr[j * 3 + 2] = srcPtr[(int)(j / ratioW) * 3 + 2];
                        }
                    }
                }
                else if (zoomType == ZoomType.BilinearInterpolation)
                {//双线性插值法
                    byte* srcPtrNext = null;
                    for (int i = 0; i < dstBmp.Height; i++)
                    {
                        srcdI = i / ratioH;
                        srcI = (int)srcdI;//当前行对应原始图像的行数
                        srcPtr = (byte*)srcBmpData.Scan0 + srcI * srcBmpData.Stride;//指原始图像的当前行
                        srcPtrNext = (byte*)srcBmpData.Scan0 + (srcI + 1) * srcBmpData.Stride;//指向原始图像的下一行
                        dstPtr = (byte*)dstBmpData.Scan0 + i * dstBmpData.Stride;//指向当前图像的当前行
                        for (int j = 0; j < dstBmp.Width; j++)
                        {
                            srcdJ = j / ratioW;
                            srcJ = (int)srcdJ;//指向原始图像的列
                            if (srcdJ < 1 || srcdJ > srcBmp.Width - 1 || srcdI < 1 || srcdI > srcBmp.Height - 1)
                            {//避免溢出（也可使用循环延拓）
                                dstPtr[j * 3] = 255;
                                dstPtr[j * 3 + 1] = 255;
                                dstPtr[j * 3 + 2] = 255;
                                continue;
                            }
                            a = srcdI - srcI;//计算插入的像素与原始像素距离（决定相邻像素的灰度所占的比例）
                            b = srcdJ - srcJ;
                            for (int k = 0; k < 3; k++)
                            {//插值    公式：f(i+p,j+q)=(1-p)(1-q)f(i,j)+(1-p)qf(i,j+1)+p(1-q)f(i+1,j)+pqf(i+1, j + 1)
                                F1 = (1 - b) * srcPtr[srcJ * 3 + k] + b * srcPtr[(srcJ + 1) * 3 + k];
                                F2 = (1 - b) * srcPtrNext[srcJ * 3 + k] + b * srcPtrNext[(srcJ + 1) * 3 + k];
                                dstPtr[j * 3 + k] = (byte)((1 - a) * F1 + a * F2);

                            }
                        }
                    }
                }
            }
            srcBmp.UnlockBits(srcBmpData);
            dstBmp.UnlockBits(dstBmpData);
            return true;
        }

    }
}
