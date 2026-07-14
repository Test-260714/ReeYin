using Custom.DefectOverview.Models;
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
using OverviewDefectPreviewFactory = Custom.DefectOverview.Services.DefectPreviewFactory;
using XYHDDefectPreviewFactory = Custom.XYHD.Services.DefectPreviewFactory;

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {
        private void UpdateMainImage(HImage image, bool isNG)
        {
            try
            {
                if (image == null || !image.IsInitialized())
                {
                    Model.AddLog("图像为空或未初始化，跳过显示", "WARN");
                    return;
                }

                using (var imageCopy = image.CopyImage())
                {
                    imageCopy.GetImageSize(out int w, out int h);
                    Model.ImageWidth = w;
                    Model.ImageHeight = h;

                    var bitmap = CreateBitmapFromHImage(imageCopy);
                    if (bitmap != null)
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            DisplayImage = bitmap;
                        }), DispatcherPriority.Render);
                    }
                    else
                    {
                        Model.AddLog($"图像转换失败 ({w}x{h})", "WARN");
                    }

                    SaveImageIfNeeded(imageCopy, isNG);
                }
            }
            catch (Exception ex)
            {
                Model.AddLog($"图像处理异常: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// 根据 pathName ("左"/"右") 更新对应路的缺陷详情
        /// </summary>
        private void UpdatePathDetails(
            string pathName,
            int serial,
            bool isNG,
            int pieceCount,
            List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            bool isLeft = pathName != null && pathName.Contains("左");
            bool isRight = pathName != null && pathName.Contains("右");

            // 更新 Header 显示 Serial 信息
            if (isLeft && _path1Serial != serial)
            {
                _path1Serial = serial;
                RunOnUiThread(() => Path1Header = $"路1 (DL Serial={serial})");
            }
            else if (isRight && _path2Serial != serial)
            {
                _path2Serial = serial;
                RunOnUiThread(() => Path2Header = $"路2 (DL Serial={serial})");
            }

            int defectCount = Math.Max(0, results?.Count ?? 0);
            string resultText = pieceCount <= 0 ? "无片" : (isNG ? "NG" : "OK");

            if (isLeft)
            {
                RunOnUiThread(() =>
                {
                    Path1Result = resultText;
                    Path1DefectCount = defectCount;
                    Path1DefectDetails.Clear();
                });
            }
            else if (isRight)
            {
                RunOnUiThread(() =>
                {
                    Path2Result = resultText;
                    Path2DefectCount = defectCount;
                    Path2DefectDetails.Clear();
                });
            }
        }

        /// <summary>
        /// 更新左/右路子图显示。主界面显示输入原图，不叠加分段结果框。
        /// </summary>
        private void UpdatePathImage(
            string pathName,
            HImage pathImage,
            bool isNG,
            int pieceCount,
            IReadOnlyList<ReeYin_V.Core.DeepLearning.Result> results)
        {
            try
            {
                var baseBitmap = ResolvePathDisplayBitmap(pathImage, results, out _, out _);
                if (baseBitmap == null) return;

                string statusText = pieceCount <= 0 ? "无片" : (isNG ? "NG" : "OK");
                var annotated = DrawStatusOverlay(baseBitmap, statusText, isNG);

                var role = XYHDFieldOrientationMapper.ResolvePathRole(pathName);

                RunOnUiThread(() =>
                {
                    if (role == DefectOverviewPathRole.Left)
                        LeftDisplayImage = annotated;
                    else if (role == DefectOverviewPathRole.Right)
                        RightDisplayImage = annotated;
                });
            }
            catch (Exception ex)
            {
                Model.AddLog($"子图绘制异常: {ex.Message}", "WARN");
            }
        }

        private BitmapSource ResolvePathDisplayBitmap(
            HImage pathImage,
            IReadOnlyList<ReeYin_V.Core.DeepLearning.Result> results,
            out int sourceWidth,
            out int sourceHeight)
        {
            sourceWidth = 0;
            sourceHeight = 0;

            if (pathImage != null && pathImage.IsInitialized())
            {
                try
                {
                    pathImage.GetImageSize(out sourceWidth, out sourceHeight);
                    using var imgCopy = pathImage.CopyImage();
                    return CreateBitmapFromHImage(imgCopy);
                }
                catch (Exception ex)
                {
                    Model.AddLog($"路图转换异常: {ex.Message}", "DEBUG");
                }
            }

            var target = ResolveDisplayTargetFromResultMetadata(results);
            sourceWidth = target.SourceWidth;
            sourceHeight = target.SourceHeight;
            return target.Bitmap;
        }

        private static (BitmapSource Bitmap, int SourceWidth, int SourceHeight) ResolveDisplayTargetFromResultMetadata(
            IReadOnlyList<ReeYin_V.Core.DeepLearning.Result> results)
        {
            if (results == null || results.Count == 0)
                return default;

            foreach (var result in results)
            {
                if (result?.Others == null)
                    continue;

                var bitmap = ResolveMetadataBitmap(result);
                if (bitmap == null)
                    continue;

                int sourceWidth = ResolveMetadataInt(
                    result,
                    bitmap.PixelWidth,
                    XYHDDefectPreviewFactory.DisplayTargetSourceWidthKey,
                    OverviewDefectPreviewFactory.DisplayTargetSourceWidthKey);
                int sourceHeight = ResolveMetadataInt(
                    result,
                    bitmap.PixelHeight,
                    XYHDDefectPreviewFactory.DisplayTargetSourceHeightKey,
                    OverviewDefectPreviewFactory.DisplayTargetSourceHeightKey);

                return (bitmap, sourceWidth, sourceHeight);
            }

            return default;
        }

        private static BitmapSource ResolveMetadataBitmap(ReeYin_V.Core.DeepLearning.Result result)
        {
            if (result?.Others == null)
                return null;

            foreach (string key in new[]
            {
                XYHDDefectPreviewFactory.DisplayTargetBitmapKey,
                OverviewDefectPreviewFactory.DisplayTargetBitmapKey
            })
            {
                if (result.Others.TryGetValue(key, out object value) && value is BitmapSource bitmap)
                    return bitmap;
            }

            return null;
        }

        private static int ResolveMetadataInt(
            ReeYin_V.Core.DeepLearning.Result result,
            int fallback,
            params string[] keys)
        {
            if (result?.Others == null || keys == null)
                return fallback;

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || !result.Others.TryGetValue(key, out object value)
                    || value == null)
                {
                    continue;
                }

                try
                {
                    int converted = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    if (converted > 0)
                        return converted;
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static BitmapSource CreateDefectPreviewBitmap(
            BitmapSource baseBitmap,
            int sourceWidth,
            int sourceHeight,
            ReeYin_V.Core.DeepLearning.Result defect,
            double paddingScale,
            double targetAspectRatio = 1.0)
        {
            return XYHDDefectPreviewFactory.CreateDefectPreviewBitmap(baseBitmap, sourceWidth, sourceHeight, defect, paddingScale, targetAspectRatio);
        }

        /// <summary>
        /// 在图像上画 OK/NG 状态标签，不绘制缺陷框。
        /// </summary>
        private static BitmapSource DrawStatusOverlay(BitmapSource baseBitmap, string text, bool isNG)
        {
            int w = baseBitmap.PixelWidth;
            int h = baseBitmap.PixelHeight;
            bool isEmpty = string.Equals(text, "无片", StringComparison.Ordinal);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(baseBitmap, new Rect(0, 0, w, h));

                var bgColor = isEmpty
                    ? Color.FromArgb(200, 71, 85, 105)
                    : isNG
                    ? Color.FromArgb(200, 220, 38, 38)
                    : Color.FromArgb(200, 22, 163, 74);
                var bgBrush = new SolidColorBrush(bgColor);
                bgBrush.Freeze();

                var ft = new FormattedText(text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    24, Brushes.White, 1.0);

                dc.DrawRectangle(bgBrush, null, new Rect(w - ft.Width - 16, 4, ft.Width + 12, ft.Height + 4));
                dc.DrawText(ft, new System.Windows.Point(w - ft.Width - 10, 6));
            }

            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }
    }
}
