using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Custom.KBTBox.KBTDispensing_Algorithm;
using Point = OpenCvSharp.Point;

namespace Custom.KBTBox.Models
{
    public static class ColorConverter
    {
        /// <summary>
        /// 将 OpenCvSharp.Scalar (BGR 顺序) 转换为 System.Drawing.Color (RGB 顺序)。
        /// </summary>
        /// <param name="scalar">OpenCvSharp.Scalar 颜色值 (B, G, R, Alpha/unused)</param>
        /// <returns>System.Drawing.Color 颜色值 (R, G, B)</returns>
        public static System.Drawing.Color ToDrawingColor(this Scalar scalar)
        {
            // 提取 B, G, R 通道值 (Scalar 的值通常是 double/byte，这里转换为 byte)
            // scalar.Val0 是 Blue (B)
            // scalar.Val1 是 Green (G)
            // scalar.Val2 是 Red (R)

            // System.Drawing.Color.FromArgb 要求 R, G, B 顺序
            byte R = (byte)scalar.Val2;
            byte G = (byte)scalar.Val1;
            byte B = (byte)scalar.Val0;

            // 忽略 Scalar 的第四个值 (Val3)，因为它通常用于 Alpha 或未用
            return System.Drawing.Color.FromArgb(R, G, B);
        }
    }

    public class Drawing
    {
        #region Fields
        public Dictionary<int, List<int>> PosInfo = new Dictionary<int, List<int>>();
        #endregion


        // 统一颜色常量，使代码更清晰
        public static class Colors
        {
            public static readonly Scalar MaxWidth = new Scalar(0, 0, 255);       // BGR: 红色
            public static readonly Scalar MaxThickness = new Scalar(255, 0, 255); // BGR: 品红色
            public static readonly Scalar SamplePoint = new Scalar(153, 204, 49); // BGR: 浅蓝色/紫红色
            public static readonly Scalar AllMeasurePoint = new Scalar(128, 128, 255); // BGR: 浅橙色/粉色
            public static readonly Scalar DefectBox = new Scalar(255, 0, 0);       // BGR: 蓝色
            public static readonly Scalar DefectContour = new Scalar(0, 255, 255);  // BGR: 黄色
            public static readonly Scalar GlobalResult = new Scalar(50, 250, 100);  // BGR: 浅绿色
        }

        public void DrawRotatedRect(Mat img, Point2f center, Size2f size, float angleDeg, Scalar color, int thickness = 2)
        {
            var rRect = new RotatedRect(center, size, angleDeg);

            Point2f[] verticesF = Cv2.BoxPoints(rRect);

            Point[] vertices = Array.ConvertAll(verticesF, p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));

            Cv2.Polylines(
                img,
                new[] { vertices },
                isClosed: true,
                color: color,
                thickness: thickness,
                lineType: LineTypes.AntiAlias
            );
        }

        /// <summary>
        /// 绘制单个宽度或厚度测量点及其标注文本。
        /// </summary>
        public void DrawMeasurementPoint(Mat grayImage, int sideId, bool isWidth, bool isMax, double value,
            double[] pointX, double[] pointY, double[] angle, double pixelSize,
            int index, Scalar color)
        {
            int x = (int)pointX[index];
            int y = (int)pointY[index];
            string type = isWidth ? "宽" : "厚";
            string minMax = isMax ? "最大" : "最小";
            string text = $"{type}-{minMax}:{value:F1}um";

            // 1. 绘制点
            Cv2.Circle(grayImage, x, y, 10, color, -1);

            // 2. 绘制旋转矩形 (仅宽度测量点有)
            if (isWidth)
            {
                float angleDeg = 90 - (float)angle[index];
                float w, h;
                if (sideId == 0 || sideId == 2) // 水平边
                {
                    w = (float)pixelSize;
                    h = 20;
                }
                else // 垂直边
                {
                    w = 20;
                    h = (float)pixelSize;
                }
                DrawRotatedRect(grayImage, new Point2f((float)(x), (float)(y)), new Size2f(w, h), angleDeg, color, 5);
            }

            // 3. 放置文本
            // 文本的Y坐标根据宽度/厚度和sideId进行调整，以避免重叠
            OpenCvSharp.Point textPos = new OpenCvSharp.Point(x, y);
            int yOffset = isWidth ? -250 : 250; // 宽度文本在上，厚度文本在下

            if (sideId == 2) // 侧边2 (通常是底边)，文本可能需要左移
            {
                textPos = new OpenCvSharp.Point(x - 1500, y);
            }

            // 微调yOffset，使厚度文本在上方的点显示在上方
            if (!isWidth && (minMax == "最小")) // 最小厚度点，假设其数量较少，位置可以灵活
            {
                // 根据 sideId=2 的特殊处理方式，这里保持与原代码一致
            }
            int fontsize = 150;
            #region 绘制中文
            if (sideId == 0)
            {
                textPos.X += 700;
                if (PosInfo.TryGetValue(sideId, out var posList) &&
                    posList.Any(p => Math.Abs(textPos.Y - p) < 300))
                {
                    textPos.Y += 500;
                }

                grayImage = DrawChineseTextOnImageOptimized(
                    grayImage,
                    text,
                    // 假设 4096 和 (4096 + 1*800) 是您希望的起始绘制坐标
                    new System.Drawing.Point(
                        Convert.ToInt32(textPos.X),
                        Convert.ToInt32(textPos.Y)
                    ),
                    fontsize,
                    ColorConverter.ToDrawingColor(color),
                    0);
                if (!PosInfo.Keys.Contains(sideId))
                    PosInfo.Add(sideId, new List<int> { textPos.Y });
                else
                    PosInfo[sideId].Add(textPos.Y);
            }
            else if (sideId == 1)
            {
                textPos.Y -= 700;
                if (PosInfo.TryGetValue(sideId, out var posList) &&
    posList.Any(p => Math.Abs(textPos.X - p) < 300))
                {
                    textPos.X += 500;
                }

                grayImage = DrawChineseTextOnImageOptimized(
                    grayImage,
                    text,
                    // 假设 4096 和 (4096 + 1*800) 是您希望的起始绘制坐标
                    new System.Drawing.Point(
                        Convert.ToInt32(textPos.X),
                        Convert.ToInt32(textPos.Y - 1500)
                    ),
                    fontsize,
                    ColorConverter.ToDrawingColor(color),
                    -90);
                if (!PosInfo.Keys.Contains(sideId))
                    PosInfo.Add(sideId, new List<int> { textPos.X });
                else
                    PosInfo[sideId].Add(textPos.X);
            }
            else if (sideId == 2)
            {
                textPos.X -= 700;
                if (PosInfo.TryGetValue(sideId, out var posList) &&
                    posList.Any(p => Math.Abs(textPos.Y - p) < 300))
                {
                    textPos.Y += 500;
                }

                grayImage = DrawChineseTextOnImageOptimized(
                    grayImage,
                    text,
                    // 假设 4096 和 (4096 + 1*800) 是您希望的起始绘制坐标
                    new System.Drawing.Point(
                        Convert.ToInt32(textPos.X),
                        Convert.ToInt32(textPos.Y)
                    ),
                    fontsize,
                    ColorConverter.ToDrawingColor(color),
                    360);
                if (!PosInfo.Keys.Contains(sideId))
                    PosInfo.Add(sideId, new List<int> { textPos.Y });
                else
                    PosInfo[sideId].Add(textPos.Y);
            }
            else if (sideId == 3)
            {
                textPos.Y += 700;
                if (PosInfo.TryGetValue(sideId, out var posList) &&
                    posList.Any(p => Math.Abs(textPos.X - p) < 300))
                {
                    textPos.X += 500;
                }

                grayImage = DrawChineseTextOnImageOptimized(
                    grayImage,
                    text,
                    // 假设 4096 和 (4096 + 1*800) 是您希望的起始绘制坐标
                    new System.Drawing.Point(
                        Convert.ToInt32(textPos.X),
                        Convert.ToInt32(textPos.Y)
                    ),
                    fontsize,
                    ColorConverter.ToDrawingColor(color),
                    90);
                if (!PosInfo.Keys.Contains(sideId))
                    PosInfo.Add(sideId, new List<int> { textPos.X });
                else
                    PosInfo[sideId].Add(textPos.X);
            }
            #endregion

            // 4. 绘制直线
            Cv2.Line( grayImage, new Point(x , y), textPos, color,5, LineTypes.Link8 );


        }

        /// <summary>
        /// 绘制全局结果统计文本。
        /// </summary>
        public void DrawGlobalResultText(Mat grayImage, string text, int yStep, double drawScale, Scalar color)
        {
            // 起始 X 坐标 (4096 * drawScale) 和基础 Y 坐标 (4096 * drawScale) 是固定的
            double baseX = 4096 * drawScale;
            double baseY = 4096 * drawScale;

            // Y 坐标根据步骤累加
            double y = baseY + yStep * 800 * drawScale;

            Cv2.PutText(grayImage, text, new OpenCvSharp.Point((int)baseX, (int)y),
                        HersheyFonts.HersheyDuplex, 24 * drawScale, color, 40);
        }


        public int CvDrawResult(KBTDispensing_MeasureResult measureResult, out Mat grayImage, out Mat HeightImage, bool showGuides = false)
        {
            //Drawing drawing = new Drawing();

            // ⭐ 优化 1：只克隆一次灰度图用于绘制，高度图直接引用（假设不绘制/修改）
            grayImage = measureResult.GrayImage.Clone();
            HeightImage = measureResult.HeightImage;

            // Mat temp2 = new Mat(); // 移除未使用的变量

            try
            {
                // 转换灰度图为 BGR 三通道，以便绘制彩色图形 (必要操作，保持)
                Cv2.CvtColor(grayImage, grayImage, ColorConversionCodes.GRAY2BGR);

                double defectSizeMax = 0;
                double defectNumTotal = 0;
                double defectdepthMax = 0;

                // 存储所有需要绘制的文本信息，以便集中处理（应对 GDI+ 性能瓶颈）
                List<(string Text, System.Drawing.Point Pos, int Angle)> textsToDraw = new List<(string, System.Drawing.Point, int)>();

                for (int sideId = 0; sideId < measureResult.SideResults.Count; sideId++)
                {
                    SideResult sideResult = measureResult.SideResults[sideId];

                    // 1. 绘制最大胶宽点 (使用 DrawMeasurementPoint 假设性能可接受)
                    for (int i = 0; i < sideResult.GlueWidthMaxPointX.Length; i++)
                    {
                        DrawMeasurementPoint(grayImage, sideId, true, true, sideResult.GlueWidthMax,
                            sideResult.GlueWidthMaxPointX, sideResult.GlueWidthMaxPointY, sideResult.GlueWidthMaxAngle,
                            sideResult.GlueWidthPixelMax, i, Drawing.Colors.MaxWidth);
                    }

                    // 2. 绘制最小胶宽点
                    for (int i = 0; i < sideResult.GlueWidthMinPointX.Length; i++)
                    {
                        DrawMeasurementPoint(grayImage, sideId, true, false, sideResult.GlueWidthMin,
                            sideResult.GlueWidthMinPointX, sideResult.GlueWidthMinPointY, sideResult.GlueWidthMinAngle,
                            sideResult.GlueWidthPixelMin, i, Drawing.Colors.MaxWidth);
                    }

                    // 3. 绘制最大胶厚点
                    for (int i = 0; i < sideResult.GlueThicknessMaxPointX.Length; i++)
                    {
                        DrawMeasurementPoint(grayImage, sideId, false, true, sideResult.GlueThicknessMax,
                            sideResult.GlueThicknessMaxPointX, sideResult.GlueThicknessMaxPointY, null,
                            0, i, Drawing.Colors.MaxThickness);
                    }

                    // 4. 绘制最小胶厚点
                    for (int i = 0; i < sideResult.GlueThicknessMinPointX.Length; i++)
                    {
                        DrawMeasurementPoint(grayImage, sideId, false, false, sideResult.GlueThicknessMin,
                            sideResult.GlueThicknessMinPointX, sideResult.GlueThicknessMinPointY, null,
                            0, i, Drawing.Colors.MaxThickness);
                    }

                    // 5. 绘制所有测量点及可视化采样点
                    // int tmpV = 0; // 移除未使用的变量
                    int tmpCount = 0;
                    for (int i = 0; i < sideResult.MeasurePointXList.Length; i++)
                    {
                        int x = (int)sideResult.MeasurePointXList[i];
                        int y = (int)sideResult.MeasurePointYList[i];

                        // 绘制所有测量点的小圆圈 (性能影响较小，保持)
                        Cv2.Circle(grayImage, x, y, 2, Drawing.Colors.AllMeasurePoint, -1);

                        // 绘制可视化采样点及其详细信息
                        if (sideResult.SampleViewIdx[i] == 1)
                        {
                            Cv2.Circle(grayImage, x, y, 5, Drawing.Colors.SamplePoint, -1);
                            tmpCount++;
                            string text;
                            if (sideResult.GlueThicknessList[i] < 0)
                            {
                                text = $"{tmpCount}-宽:{sideResult.GlueWidthList[i]:F1}um\r\n厚:?um";
                            }
                            else
                            {
                                text = $"{tmpCount}-宽:{sideResult.GlueWidthList[i]:F1}um\r\n厚:{sideResult.GlueThicknessList[i]:F1}um";
                            }

                            // ⭐ 优化 2：将文本绘制操作移到循环外集中处理
                            OpenCvSharp.Point textPos;
                            int angle = 0;
                            System.Drawing.Point finalPos;

                            if (sideId == 0)
                            {
                                textPos = new OpenCvSharp.Point(x, y - 80);
                                angle = 0;
                            }
                            else if (sideId == 1)
                            {
                                textPos = new OpenCvSharp.Point(x, y - 600);
                                angle = -90;
                            }
                            else if (sideId == 2)
                            {
                                textPos = new OpenCvSharp.Point(x - 500, y);
                                angle = 360; // 360 度等同于 0 度
                            }
                            else // sideId == 3
                            {
                                textPos = new OpenCvSharp.Point(x, y); // 假设这里是右侧边
                                angle = 90;
                            }

                            finalPos = new System.Drawing.Point(
                                Convert.ToInt32(textPos.X),
                                Convert.ToInt32(textPos.Y)
                            );

                            textsToDraw.Add((text, finalPos, angle));
                        }
                    }

                    // 6. 绘制缺陷
                    int defectNum = sideResult.Defects.Count;
                    for (int i = 0; i < defectNum; i++)
                    {
                        DefectResult defect = sideResult.Defects[i];

                        if (defect.IsOk)
                            continue;

                        // 更新最大缺陷尺寸和深度
                        if (defect.DiameterFeature > defectSizeMax)
                            defectSizeMax = defect.DiameterFeature;

                        if (defect.DepthFeature > defectdepthMax)
                            defectdepthMax = defect.DepthFeature;

                        // 绘制缺陷矩形框和轮廓
                        if (defect.InstanceId != -1)
                        {
                            // 绘制矩形框 (OpenCV 原生函数，性能较高)
                            Cv2.Rectangle(grayImage, new OpenCvSharp.Point((int)defect.Left, (int)defect.Top),
                                                        new OpenCvSharp.Point((int)defect.Right, (int)defect.Bottom),
                                                        Drawing.Colors.DefectBox, 3);

                            // 绘制缺陷轮廓 (OpenCV 原生函数，性能较高)
                            for (int j = 0; j < defect.DefectPolygons.Count; j++)
                            {
                                if (defect.DefectPolygons[j].Contours.Length > 0)
                                {
                                    Cv2.DrawContours(grayImage, defect.DefectPolygons[j].Contours, -1, Drawing.Colors.DefectContour, 1);
                                }
                            }
                        }

                        defectNumTotal++;
                    }
                }

                // ⭐ 优化 3：集中绘制所有采样点文本 (只调用一次昂贵的 GDI+ 方法)
                // 假设 DrawChineseTextOnImageOptimized 可以处理批量绘制或者
                // 在内部对 Mat -> Bitmap -> Mat 的转换进行了优化（比如只转换一次）。

                foreach (var item in textsToDraw)
                {
                    grayImage = DrawChineseTextOnImageOptimized(
                        grayImage,
                        item.Text,
                        item.Pos,
                        80, // 字体大小
                        System.Drawing.Color.YellowGreen,
                        item.Angle
                    );
                }

                // 7. 绘制全局结果统计文本 (保持不变)
                double drawScale = 1.0;
                // 假设 _measureParam 可用，重新计算 drawScale
                // if (_measureParam != null)
                // {
                //     drawScale = (5 / _measureParam.IntervalY); 
                // }

                string globalText = $""; // 填充您的全局统计信息

                grayImage = DrawChineseTextOnImageOptimized(
                    grayImage,
                    globalText,
                    // 假设这是左上角统计信息的绘制坐标
                    new System.Drawing.Point(
                        Convert.ToInt32(100 * drawScale),
                        Convert.ToInt32(100 * drawScale)
                    ),
                    100, // 字体大小
                    System.Drawing.Color.Green,
                    0
                );

                // 返回成功
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绘图异常: {ex.Message}\n{ex.StackTrace}");
                // 异常情况下返回空白图像
                grayImage = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_8UC3);
                HeightImage = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_32FC1);
                return -1;
            }
            finally
            {
                // 如果在函数开头没有克隆 HeightImage，这里不需要释放
                // 如果克隆了，需要确保在不再使用时释放
                // temp2.Dispose(); // 移除未使用的变量的 Dispose
            }
        }


        public Mat DrawChineseTextOnImageOptimized(
    Mat image,
    string text,
    System.Drawing.Point position,
    int fontSize,
    System.Drawing.Color textColor,
    float angleDeg,
    string fontName = "宋体")
        {
            if (image == null || image.Empty() || string.IsNullOrEmpty(text))
                return image;

            // ===================== 1. 创建字体和画刷 =====================
            using System.Drawing.Font font = new System.Drawing.Font(fontName, fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
            using System.Drawing.Brush brush = new SolidBrush(textColor);

            // ===================== 2. 计算文本原始尺寸 =====================
            int textWidth = 0;
            int textHeight = 0;
            float lineHeight;

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            using (Bitmap tmpBmp = new Bitmap(1, 1))
            using (Graphics tmpG = Graphics.FromImage(tmpBmp))
            {
                tmpG.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                lineHeight = font.GetHeight(tmpG);

                foreach (string line in lines)
                {
                    SizeF size = tmpG.MeasureString(line, font);
                    textWidth = Math.Max(textWidth, (int)Math.Ceiling(size.Width));
                }

                textHeight = (int)Math.Ceiling(lines.Length * lineHeight);
            }

            // ===================== 3. 旋转后包围盒尺寸 =====================
            double rad = angleDeg * Math.PI / 180.0;

            int rotatedWidth = (int)Math.Ceiling(
                Math.Abs(textWidth * Math.Cos(rad)) +
                Math.Abs(textHeight * Math.Sin(rad)));

            int rotatedHeight = (int)Math.Ceiling(
                Math.Abs(textWidth * Math.Sin(rad)) +
                Math.Abs(textHeight * Math.Cos(rad)));

            // padding 防止裁剪
            int padding = 6;
            rotatedWidth += padding * 2;
            rotatedHeight += padding * 2;

            // ===================== 4. ROI 边界安全检查 =====================
            if (position.X >= image.Width || position.Y >= image.Height)
                return image;

            int roiWidth = Math.Min(rotatedWidth, image.Width - position.X);
            int roiHeight = Math.Min(rotatedHeight, image.Height - position.Y);

            if (roiWidth <= 0 || roiHeight <= 0)
                return image;

            // ===================== 5. 创建文本 Bitmap =====================
            using Bitmap textBitmap = new Bitmap(roiWidth, roiHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(textBitmap))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // ---------- 坐标系旋转 ----------
                float cx = roiWidth / 2f;
                float cy = roiHeight / 2f;

                g.TranslateTransform(cx, cy);
                g.RotateTransform(angleDeg);
                g.TranslateTransform(-textWidth / 2f, -textHeight / 2f);

                float y = 0;
                foreach (string line in lines)
                {
                    g.DrawString(line, font, brush, 0, y);
                    y += lineHeight;
                }

                g.ResetTransform();
            }

            // ===================== 6. Bitmap → Mat =====================
            using Mat textMat = textBitmap.ToMat();

            // 拆分 BGRA
            Mat[] channels = Cv2.Split(textMat);
            using Mat textBGR = new Mat();
            Cv2.Merge(new[] { channels[0], channels[1], channels[2] }, textBGR);
            using Mat alpha = channels[3];

            // ===================== 7. Alpha Blend =====================
            using Mat alphaF = new Mat();
            alpha.ConvertTo(alphaF, MatType.CV_32F, 1.0 / 255.0);

            using Mat textBF = new Mat();
            textBGR.ConvertTo(textBF, MatType.CV_32F);

            OpenCvSharp.Rect roiRect = new OpenCvSharp.Rect(position.X, position.Y, roiWidth, roiHeight);
            using Mat roiMat = new Mat(image, roiRect);
            using Mat roiF = new Mat();
            roiMat.ConvertTo(roiF, MatType.CV_32F);

            using Mat alpha3 = new Mat();
            Cv2.Merge(new[] { alphaF, alphaF, alphaF }, alpha3);

            using Mat invAlpha = new Mat();
            Cv2.Subtract(Scalar.All(1.0), alpha3, invAlpha);

            using Mat fg = new Mat();
            using Mat bg = new Mat();
            using Mat blended = new Mat();

            Cv2.Multiply(textBF, alpha3, fg);
            Cv2.Multiply(roiF, invAlpha, bg);
            Cv2.Add(fg, bg, blended);

            blended.ConvertTo(roiMat, MatType.CV_8UC3);

            // ===================== 8. 清理 =====================
            foreach (var c in channels)
                c.Dispose();

            return image;
        }
    }



}
