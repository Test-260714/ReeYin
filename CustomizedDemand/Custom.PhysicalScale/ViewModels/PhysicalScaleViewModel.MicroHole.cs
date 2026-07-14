using Custom.PhysicalScale.Helpers;
using Custom.PhysicalScale.Models;
using HalconDotNet;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.PhysicalScale.ViewModels
{
    public partial class PhysicalScaleViewModel
    {
        private PhysicalScaleMicroHoleAnalysisResult _holeAnalysisResult;

        private string GetToolDisplayName()
        {
            if (Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
                return "框选单孔";

            return Model.SelectedTool switch
            {
                PhysicalScaleTool.Point => "点测量",
                PhysicalScaleTool.Line => "线测量",
                PhysicalScaleTool.Rectangle => "矩形测量",
                PhysicalScaleTool.Circle => "圆测量",
                _ => "选择"
            };
        }

        private string GetCenterTitle()
        {
            if (Model.SelectedMode == PhysicalScaleMode.BasicMeasurement)
                return "交互量测区";

            return Model.HolePreviewVisible ? "微孔局部预览" : "单孔框选区";
        }

        private string GetCenterSubTitle()
        {
            if (Model.SelectedMode == PhysicalScaleMode.BasicMeasurement)
                return ImageSummary;

            if (Model.HolePreviewVisible)
                return $"{DisplayImageSummary} | {Model.HolePreviewSummary}";

            return Model.HoleRoiMeasurement == null
                ? "操作顺序：1. 点击“框选单孔” 2. 在图上拖出圆框 3. 点击“执行判定”"
                : $"{HoleRoiSummary} | 点击“执行判定”开始检查";
        }

        private string GetHolePreviewModeText()
        {
            return Model.HolePreviewMode switch
            {
                PhysicalScaleHolePreviewMode.Binary => "二值化",
                PhysicalScaleHolePreviewMode.Edge => "边缘",
                PhysicalScaleHolePreviewMode.SubPixelBoundary => "亚像素边界",
                _ => "原图"
            };
        }

        private string GetHoleRoiSummary()
        {
            return Model.HoleRoiMeasurement == null
                ? "未框选单孔"
                : $"圆心({Model.HoleRoiMeasurement.StartX:F1}, {Model.HoleRoiMeasurement.StartY:F1})  半径 {Model.HoleRoiMeasurement.RadiusPx:F1}px";
        }

        private string GetHoleActionGuide()
        {
            return Model.HolePreviewVisible
                ? "当前显示的是判定预览。点击“返回原图”可重新看圆框位置，点击“框选单孔”可直接重新圈孔。"
                : "先框选单孔，再执行判定。显示结果只影响中间预览，不影响最终 OK/NG。";
        }

        private string GetHoleJudgeDisplayText()
        {
            return Model.HoleJudgementText switch
            {
                "正常" => "OK",
                "孔内缺陷" => "NG",
                "孔堵塞" => "NG",
                _ => "待判定"
            };
        }

        private string GetHoleJudgeReasonText()
        {
            return string.IsNullOrWhiteSpace(Model.HoleJudgementText)
                ? "未判定"
                : Model.HoleJudgementText;
        }

        public void SetHoleRoiMeasurement(PhysicalScaleMeasurement measurement)
        {
            measurement.DisplayName = "单孔圆框";
            measurement.ShapeTypeName = "圆";
            measurement.PixelSummary = $"R={measurement.RadiusPx:F1}px";
            measurement.PhysicalSummary = $"R={measurement.RadiusPx * ((Model.ScaleX + Model.ScaleY) / 2.0):F4}mm";
            measurement.DetailText =
                $"图形：单孔圆框{Environment.NewLine}" +
                $"中心 ({measurement.StartX:F2}, {measurement.StartY:F2}){Environment.NewLine}" +
                $"半径 {measurement.RadiusPx:F2} px";

            Model.HoleRoiMeasurement = measurement;
            Model.HolePreviewVisible = false;
            _holeAnalysisResult = null;
            RestoreLoadedImageDisplay();
            Model.HoleJudgementText = "待判定";
            Model.HoleReportDetail = "已更新单孔圆框，请执行判定";
            Model.StatusText = "已框选单孔，可执行判定";
            RaiseHoleStateChanged();
        }

        public void SelectHoleRoiAt(Point imagePoint)
        {
            if (Model.HoleRoiMeasurement == null)
            {
                Model.StatusText = "当前没有可用的单孔圆框";
                return;
            }

            double distance = (new Point(Model.HoleRoiMeasurement.StartX, Model.HoleRoiMeasurement.StartY) - imagePoint).Length;
            Model.StatusText = distance <= Model.HoleRoiMeasurement.RadiusPx + 4
                ? "已选中当前单孔圆框"
                : "当前位置不在当前圆框上";
        }

        private void SetMode(string modeName)
        {
            Model.SelectedMode = modeName == "MicroHole"
                ? PhysicalScaleMode.MicroHoleJudgement
                : PhysicalScaleMode.BasicMeasurement;
        }

        private void PrepareHoleRoi()
        {
            if (Model.SelectedMode != PhysicalScaleMode.MicroHoleJudgement)
                Model.SelectedMode = PhysicalScaleMode.MicroHoleJudgement;

            Model.HolePreviewVisible = false;
            RestoreLoadedImageDisplay();
            Model.SelectedTool = PhysicalScaleTool.Circle;
            Model.StatusText = "请在原图上框选单个圆孔";
            RaisePropertyChanged(nameof(CanvasVisibility));
            FitRequested?.Invoke();
        }

        private void ExecuteMicroHoleJudgement()
        {
            if (Model.LoadedImage == null)
            {
                Model.StatusText = "请先加载原始图像";
                return;
            }

            if (Model.HoleRoiMeasurement == null)
            {
                Model.StatusText = "请先框选单个圆孔";
                return;
            }

            if (!PhysicalScaleMicroHoleAnalyzer.TryAnalyze(
                    Model.LoadedImage,
                    Model.HoleRoiMeasurement,
                    Model.HolePolarity,
                    Model.HoleThreshold,
                    Model.HoleMinDefectAreaPx,
                    out var result,
                    out string errorMessage))
            {
                Model.StatusText = errorMessage;
                Model.HoleJudgementText = "待判定";
                Model.HoleReportDetail = errorMessage;
                RaiseHoleStateChanged();
                return;
            }

            _holeAnalysisResult = result;
            CommitHoleAnalysisResult(result);
            Model.HolePreviewVisible = true;
            UpdateHolePreviewDisplay();
            Model.StatusText = $"微孔判定完成：{HoleJudgeDisplayText}" + (HoleJudgeDisplayText == "NG" ? $" | {HoleJudgeReasonText}" : string.Empty);
            FitRequested?.Invoke();
        }

        private void ResetMicroHolePreview()
        {
            Model.HolePreviewVisible = false;
            RestoreLoadedImageDisplay();
            Model.SelectedTool = PhysicalScaleTool.Circle;
            Model.StatusText = Model.HoleRoiMeasurement == null
                ? "请框选单个圆孔"
                : "已返回原图，可重新框选或再次执行判定";
            RaisePropertyChanged(nameof(CanvasVisibility));
            RaisePropertyChanged(nameof(CenterTitle));
            RaisePropertyChanged(nameof(CenterSubTitle));
            FitRequested?.Invoke();
        }

        private void HandleModeChanged()
        {
            if (Model.SelectedMode == PhysicalScaleMode.BasicMeasurement)
            {
                RestoreLoadedImageDisplay();
                Model.HolePreviewVisible = false;
                Model.SelectedTool = PhysicalScaleTool.Line;
                Model.StatusText = Model.Measurements.Count > 0
                    ? "已切换到基础量测模式"
                    : "请先加载一张原始图像，再进行基础量测";
            }
            else
            {
                RestoreLoadedImageDisplay();
                Model.HolePreviewVisible = false;
                Model.SelectedTool = PhysicalScaleTool.Circle;
                Model.StatusText = "已切换到微孔判定模式，请先框选单孔";
            }

            RaisePropertyChanged(nameof(BasicModeVisibility));
            RaisePropertyChanged(nameof(MicroHoleModeVisibility));
            RaisePropertyChanged(nameof(ModeSummary));
            RaisePropertyChanged(nameof(ToolDisplayName));
            RaisePropertyChanged(nameof(CenterTitle));
            RaisePropertyChanged(nameof(CenterSubTitle));
            RaisePropertyChanged(nameof(CenterBadgeText));
            RaisePropertyChanged(nameof(CanvasVisibility));
            FitRequested?.Invoke();
        }

        private void ApplyDisplayImage(BitmapSource bitmap)
        {
            Model.DisplayImageSource = bitmap;
            Model.DisplayImagePixelWidth = bitmap?.PixelWidth ?? 0;
            Model.DisplayImagePixelHeight = bitmap?.PixelHeight ?? 0;
        }

        private void RestoreLoadedImageDisplay()
        {
            ApplyDisplayImage(GetDisplaySourceForPreview());
        }

        private void ResetHoleMetrics()
        {
            _holeAnalysisResult = null;
            Model.HolePreviewVisible = false;
            Model.HoleJudgementText = "待判定";
            Model.HoleOpenAreaPx = 0;
            Model.HoleOpenAreaMm2 = 0;
            Model.HoleOpenDiameterPx = 0;
            Model.HoleOpenDiameterMm = 0;
            Model.HoleOpeningRatio = 0;
            Model.HoleDefectCount = 0;
            Model.HoleMaxDefectAreaPx = 0;
            Model.HoleMaxDefectAreaMm2 = 0;
            Model.HoleTotalDefectAreaPx = 0;
            Model.HoleTotalDefectAreaMm2 = 0;
            Model.HolePreviewSummary = "当前显示：原图";
            Model.HoleReportDetail = "请先框选单孔，再执行判定";
        }

        private void UpdateHolePhysicalValues()
        {
            if (Model.HoleRoiMeasurement != null)
            {
                Model.HoleRoiMeasurement.PhysicalSummary =
                    $"R={Model.HoleRoiMeasurement.RadiusPx * ((Model.ScaleX + Model.ScaleY) / 2.0):F4}mm";
            }

            if (_holeAnalysisResult != null)
                CommitHoleAnalysisResult(_holeAnalysisResult);
        }

        private void RaiseHoleStateChanged()
        {
            RaisePropertyChanged(nameof(HoleRoiSummary));
            RaisePropertyChanged(nameof(HoleActionGuide));
            RaisePropertyChanged(nameof(CenterTitle));
            RaisePropertyChanged(nameof(CenterSubTitle));
            RaisePropertyChanged(nameof(CenterBadgeText));
            RaisePropertyChanged(nameof(HolePreviewModeText));
            RaisePropertyChanged(nameof(HoleJudgeDisplayText));
            RaisePropertyChanged(nameof(HoleJudgeReasonText));
            RaisePropertyChanged(nameof(HoleDiameterSummary));
            RaisePropertyChanged(nameof(HoleOpeningRatioSummary));
            RaisePropertyChanged(nameof(HoleDefectAreaSummary));
            RaisePropertyChanged(nameof(HoleCountSummary));
            RaisePropertyChanged(nameof(HoleJudgeResultTitle));
            RaisePropertyChanged(nameof(HoleJudgeResultMeta));
        }

        private void CommitHoleAnalysisResult(PhysicalScaleMicroHoleAnalysisResult result)
        {
            double averageScale = (Model.ScaleX + Model.ScaleY) / 2.0;
            double openDiameterPx = result.OpenArea > 0 ? 2.0 * Math.Sqrt(result.OpenArea / Math.PI) : 0;
            double openAreaMm2 = result.OpenArea * Model.ScaleX * Model.ScaleY;
            double openDiameterMm = openDiameterPx * averageScale;
            double maxDefectAreaPx = result.DefectRegions.Count == 0 ? 0 : result.DefectRegions.Max(item => item.Area);
            double totalDefectAreaPx = result.DefectRegions.Sum(item => item.Area);
            double maxDefectAreaMm2 = maxDefectAreaPx * Model.ScaleX * Model.ScaleY;
            double totalDefectAreaMm2 = totalDefectAreaPx * Model.ScaleX * Model.ScaleY;

            Model.HoleOpenAreaPx = result.OpenArea;
            Model.HoleOpenAreaMm2 = openAreaMm2;
            Model.HoleOpenDiameterPx = openDiameterPx;
            Model.HoleOpenDiameterMm = openDiameterMm;
            Model.HoleOpeningRatio = result.OpeningRatio;
            Model.HoleDefectCount = result.DefectRegions.Count;
            Model.HoleMaxDefectAreaPx = maxDefectAreaPx;
            Model.HoleMaxDefectAreaMm2 = maxDefectAreaMm2;
            Model.HoleTotalDefectAreaPx = totalDefectAreaPx;
            Model.HoleTotalDefectAreaMm2 = totalDefectAreaMm2;

            if (result.OpenArea <= 0 || result.OpeningRatio < 0.18)
                Model.HoleJudgementText = "孔堵塞";
            else if (result.DefectRegions.Count > 0)
                Model.HoleJudgementText = "孔内缺陷";
            else
                Model.HoleJudgementText = "正常";

            Model.HoleReportDetail =
                $"判定结果：{HoleJudgeDisplayText}{Environment.NewLine}" +
                $"异常类型：{HoleJudgeReasonText}{Environment.NewLine}" +
                $"单孔圆框：{HoleRoiSummary}{Environment.NewLine}" +
                $"等效开口直径：{openDiameterPx:F2} px / {openDiameterMm:F4} mm{Environment.NewLine}" +
                $"缺陷数量：{result.DefectRegions.Count}{Environment.NewLine}" +
                $"最大缺陷面积：{maxDefectAreaPx:F1} px² / {maxDefectAreaMm2:F4} mm²{Environment.NewLine}" +
                $"缺陷总面积：{totalDefectAreaPx:F1} px² / {totalDefectAreaMm2:F4} mm²{Environment.NewLine}" +
                $"诊断参数：{GetHolePolarityText(Model.HolePolarity)} | 阈值 {Model.HoleThreshold} | 有效开口率 {result.OpeningRatio * 100:F2}%";

            RaiseHoleStateChanged();
        }

        private static string GetHolePolarityText(PhysicalScaleHolePolarity polarity)
        {
            return polarity switch
            {
                PhysicalScaleHolePolarity.BrightHoleOnDarkBackground => "亮孔 / 暗背景",
                _ => "暗孔 / 亮背景"
            };
        }
    }
}
