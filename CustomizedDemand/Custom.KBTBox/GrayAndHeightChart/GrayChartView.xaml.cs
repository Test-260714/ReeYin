using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Series3D;
using HalconDotNet;
using HandyControl.Controls;
using Newtonsoft.Json;
using OpenCvSharp;
using Prism.Events;
using ReeYin_V.Core;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Border = System.Windows.Controls.Border;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
using DialogResult = System.Windows.Forms.DialogResult;
using GroupBox = System.Windows.Controls.GroupBox;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using ProjectionType = Arction.Wpf.Charting.ProjectionType;

namespace Custom.UI
{
    /// <summary>
    /// GrayChartView.xaml 的交互逻辑
    /// </summary>
    public partial class GrayChartView 
    {
        #region Fields
        private WriteableBitmap _writeableBitmap;

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void MoveMemory(IntPtr dest, IntPtr src, uint count);

        // 圆形绘制相关
        private bool _isDrawingCircle = false; // 是否正在绘制圆形
        private Point _circleCenter; // 圆心坐标
        private double _circleRadius; // 圆半径
        private Ellipse _currentCircle; // 当前绘制的圆形
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public GrayChartView()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        private void ConvertMatToBitmapFrame(Mat mat)
        {
            // 确保 Mat 是有效的
            if (mat.Empty())
                return;

            // 将 Mat 转换为 BGRA 格式 (每个通道占用 1 字节)
            Mat matBgra = new Mat();
            if (mat.Channels() == 1)
            {
                mat.ConvertTo(matBgra, MatType.CV_32F);
                Cv2.Normalize(matBgra, matBgra, 0, 255, NormTypes.MinMax, MatType.CV_8U);
                Cv2.CvtColor(matBgra, matBgra, ColorConversionCodes.GRAY2BGRA);
            }
            else if (mat.Channels() == 2)
            {
                // 如果是 GRAY 格式，转换为 BGRA 格式 (添加 Alpha 通道)
                Cv2.CvtColor(mat, matBgra, ColorConversionCodes.GRAY2BGRA);
            }
            else if (mat.Channels() == 3)
            {
                // 如果是 BGR 格式，转换为 BGRA 格式 (添加 Alpha 通道)
                Cv2.CvtColor(mat, matBgra, ColorConversionCodes.BGR2BGRA);
            }
            else if (mat.Channels() == 4)
            {
                // 如果本身就是 BGRA 格式，直接使用
                matBgra = mat;
            }
            else
            {
                throw new NotSupportedException("Only BGR and BGRA images are supported.");
            }

            // 获取 Mat 的数据
            int width = matBgra.Width;
            int height = matBgra.Height;
            int stride = width * 4; // 每个像素 4 字节 (BGRA)
            byte[] pixelData = new byte[stride * height];

            // 将 Mat 数据复制到 byte 数组
            Marshal.Copy(matBgra.Data, pixelData, 0, pixelData.Length);
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 创建 WriteableBitmap 并加载 Mat 数据
                if (_writeableBitmap == null || _writeableBitmap.PixelWidth != width || _writeableBitmap.PixelHeight != height)
                {
                    _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                    drawViewer.UpdateBackgroundImageAsync(_writeableBitmap);
                }
                CopyMemory(_writeableBitmap, pixelData, width, height);
            }, DispatcherPriority.Send);
        }

        private unsafe void CopyMemory(WriteableBitmap bitmap, byte[] pixelData, int width, int height)
        {
            bitmap.Lock();
            fixed (byte* ptr = pixelData)
            {
                var p = new IntPtr(ptr);
                MoveMemory(bitmap.BackBuffer, new IntPtr(ptr), (uint)pixelData.Length);
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();
        }

        /// <summary>
        /// 保存为原始TIFF格式
        /// </summary>
        private void SaveAsOriginalTiff(string fileName)
        {
            var para = new Dictionary<string, string>()
            {
                {"Path", fileName }
            };
            // 如果有原始数据保存服务，调用它
            //_dataProcessorService?.SendCommand("SaveRawImage", para);

            // 临时实现：如果没有原始数据服务，保存当前显示的灰度图
            if (drawViewer.BackgroundImage != null)
            {
                SavePureGrayImage(fileName, drawViewer.BackgroundImage);
            }
        }

        /// <summary>
        /// 保存纯灰度图（不包含绘制内容）
        /// </summary>
        private void SavePureGrayImage(string fileName, BitmapSource grayImage)
        {
            // 直接保存灰度图，不包含Canvas上的绘制内容
            var encoder = GetImageEncoder(fileName);
            if (encoder != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(grayImage));

                using (var fileStream = new FileStream(fileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }

        /// <summary>
        /// 保存复合图像（包含绘制内容）
        /// </summary>
        private void SaveCompositeImage(string fileName, BitmapSource backgroundImage)
        {
            // 生成包含绘制内容的复合图像
            var frame = drawCanvas.ToBitmapFrame(
                backgroundImage.PixelWidth,
                backgroundImage.PixelHeight,
                DpiHelper.GetDpiFromVisual(drawCanvas),
                backgroundImage);

            if (frame != null)
            {
                ImageHelper.Save(fileName, frame);
            }
        }

        /// <summary>
        /// 根据文件扩展名获取对应的图像编码器
        /// </summary>
        private BitmapEncoder GetImageEncoder(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                ".bmp" => new BmpBitmapEncoder(),
                ".tif" or ".tiff" => new TiffBitmapEncoder { Compression = TiffCompressOption.None },
                _ => new PngBitmapEncoder() // 默认使用PNG
            };
        }

        /// <summary>
        /// 将Canvas坐标转换为图像坐标
        /// </summary>
        private Point ConvertCanvasToImageCoordinates(Point canvasPoint)
        {
            // 获取背景图像的实际尺寸
            if (drawViewer.BackgroundImage != null)
            {
                double scaleX = drawViewer.BackgroundImage.PixelWidth / drawCanvas.ActualWidth;
                double scaleY = drawViewer.BackgroundImage.PixelHeight / drawCanvas.ActualHeight;

                return new Point(
                    canvasPoint.X * scaleX,
                    canvasPoint.Y * scaleY
                );
            }

            return canvasPoint;
        }

        /// <summary>
        /// 将Canvas半径转换为图像半径
        /// </summary>
        private double ConvertCanvasToImageRadius(double canvasRadius)
        {
            // 获取背景图像的实际尺寸
            if (drawViewer.BackgroundImage != null)
            {
                double scaleX = drawViewer.BackgroundImage.PixelWidth / drawCanvas.ActualWidth;
                double scaleY = drawViewer.BackgroundImage.PixelHeight / drawCanvas.ActualHeight;

                // 使用平均缩放比例
                double averageScale = (scaleX + scaleY) / 2.0;
                return canvasRadius * averageScale;
            }

            return canvasRadius;
        }

        /// <summary>
        /// 更新圆形信息显示
        /// </summary>
        private void UpdateCircleInfo()
        {
            if (_currentCircle == null) return;

            // 转换为图像坐标系（如果需要的话）
            var imageCoords = ConvertCanvasToImageCoordinates(_circleCenter);
            var imageRadius = ConvertCanvasToImageRadius(_circleRadius);

            // 在UI上显示圆形信息
            string info = $"圆心: ({imageCoords.X:F0}, {imageCoords.Y:F0}), 半径: {imageRadius:F1}";

            // 更新UI显示
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //tb_CircleInfo.Text = info;
                //tb_CircleInfo.Visibility = Visibility.Visible;
            });

            // 输出到调试窗口
            System.Diagnostics.Debug.WriteLine($"圆形信息: {info}");
        }
        #endregion

        #region Commands
        private void btn_SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var backgroundImage = drawViewer.BackgroundImage;
            if (backgroundImage == null)
            {
                MessageBox.Show("没有可保存的图像", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = @"Original tiff files (*.tif;*.tiff)|*.tif;*.tiff|Images files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                OverwritePrompt = true,
                RestoreDirectory = true,
                FileName = $"GrayImage_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var extension = Path.GetExtension(dlg.FileName).ToLower();

                    if (extension == ".tif" || extension == ".tiff")
                    {
                        // 保存为TIFF格式 - 保持原始数据精度
                        SaveAsOriginalTiff(dlg.FileName);
                    }
                    else
                    {
                        // 询问用户想要保存哪种图像
                        var result = MessageBox.Show(
                            "选择保存类型：\n" +
                            "是(Yes) - 保存纯灰度图\n" +
                            "否(No) - 保存带绘制内容的图像\n" +
                            "取消(Cancel) - 取消保存",
                            "选择保存类型",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        switch (result)
                        {
                            case DialogResult.Yes:
                                // 保存纯灰度图
                                SavePureGrayImage(dlg.FileName, backgroundImage);
                                break;
                            case DialogResult.No:
                                // 保存带绘制内容的图像
                                SaveCompositeImage(dlg.FileName, backgroundImage);
                                break;
                            default:
                                return; // 取消保存
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"图像已保存至：{dlg.FileName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存失败：{ex.Message}");
                }
            }
        }
        #endregion
    }
}
