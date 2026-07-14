using ClosedXML.Excel;
using FileTool.BRJReportOutput.Models;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FileTool.BRJReportOutput.Services
{
    public static class BrjReportExportService
    {
        public static string ReportDirectory => Path.Combine(PrismProvider.AppBasePath, "Report", "BRJ");

        public static string ResolveReportDirectory(string? customDirectory)
        {
            return string.IsNullOrWhiteSpace(customDirectory) ? ReportDirectory : customDirectory.Trim();
        }

        public static async Task<string> ExportBatchReportAsync(BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects, string? reportDirectory = null)
        {
            string targetDirectory = ResolveReportDirectory(reportDirectory);
            Directory.CreateDirectory(targetDirectory);
            string filePath = Path.Combine(targetDirectory, $"{record.SN}.xlsx");

            await Task.Run(() =>
            {
                using XLWorkbook workbook = new();
                List<BrjReportSetting> diameterGroups = BrjReportStorage.QueryDiameterGroupSettingsAsync().GetAwaiter().GetResult();
                WriteDistributionSheet(workbook, record, defects);
                WriteDiameterSheet(workbook, record, defects, diameterGroups);
                WriteDetailSheet(workbook, record, defects);
                SaveWorkbook(workbook, filePath);
            }).ConfigureAwait(false);

            return filePath;
        }

        private static void WriteDistributionSheet(XLWorkbook workbook, BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects)
        {
            IXLWorksheet worksheet = workbook.Worksheets.Add("缺陷分布");
            PrepareWorksheet(worksheet, 10);
            WriteTitleAndHeader(worksheet, "产品质量检测报告-缺陷分布图", record, 10);
            byte[] imageBytes = BrjDefectMapImageService.RenderMap(record, defects);
            using MemoryStream stream = new(imageBytes);
            worksheet.AddPicture(stream, "缺陷分布图")
                .MoveTo(worksheet.Cell(6, 1))
                .Scale(0.55);
        }

        private static void WriteDiameterSheet(XLWorkbook workbook, BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects, IReadOnlyList<BrjReportSetting> diameterGroups)
        {
            IXLWorksheet worksheet = workbook.Worksheets.Add("直径统计");
            PrepareWorksheet(worksheet, 8);
            WriteTitleAndHeader(worksheet, "产品质量检测报告-缺陷直径统计", record, 8);

            int total = Math.Max(1, defects.Count);
            List<(string Name, int Count, XLColor Color)> bins = diameterGroups
                .OrderBy(item => item.SortIndex)
                .Select(item => (
                    BuildDiameterGroupName(item),
                    defects.Count(defect => IsInDiameterGroup(defect.DiameterMm, item)),
                    ResolveColor(item.ColorHex)))
                .ToList();

            worksheet.Range(5, 1, 5, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            WriteRow(worksheet, 5, new[] { "直径区间/mm", "个数-总", "占比", string.Empty });
            int lastRow = Math.Max(5, bins.Count + 5);
            worksheet.Range(5, 1, lastRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(5, 1, lastRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            for (int i = 0; i < bins.Count; i++)
            {
                int row = i + 6;
                worksheet.Cell(row, 1).Value = bins[i].Name;
                worksheet.Cell(row, 2).Value = bins[i].Count;
                worksheet.Cell(row, 3).Value = bins[i].Count * 1.0 / total;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
                worksheet.Cell(row, 1).Style.Font.FontColor = bins[i].Color;
            }

        }

        private static bool IsInDiameterGroup(double diameter, BrjReportSetting group)
        {
            bool minMatched = group.MinDiameterMm == null || diameter >= group.MinDiameterMm.Value;
            bool maxMatched = group.MaxDiameterMm == null || diameter < group.MaxDiameterMm.Value;
            return minMatched && maxMatched;
        }

        private static string BuildDiameterGroupName(BrjReportSetting group)
        {
            if (!string.IsNullOrWhiteSpace(group.GroupName))
            {
                return group.GroupName;
            }

            if (group.MinDiameterMm != null && group.MaxDiameterMm != null)
            {
                return $"{FormatNumber(group.MinDiameterMm.Value)}<=直径<{FormatNumber(group.MaxDiameterMm.Value)}";
            }

            if (group.MinDiameterMm != null)
            {
                return $"{FormatNumber(group.MinDiameterMm.Value)}<=直径";
            }

            return $"直径<{FormatNumber(group.MaxDiameterMm ?? 0d)}";
        }

        private static XLColor ResolveColor(string colorHex)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
            {
                return XLColor.FromHtml("#3858D6");
            }

            try
            {
                return XLColor.FromHtml(colorHex);
            }
            catch
            {
                return XLColor.FromHtml("#3858D6");
            }
        }

        private static void WriteDetailSheet(XLWorkbook workbook, BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects)
        {
            IXLWorksheet worksheet = workbook.Worksheets.Add("缺陷详情");
            PrepareWorksheet(worksheet, 9);
            WriteTitleAndHeader(worksheet, "产品质量检测报告-缺陷详情", record, 9);

            int row = 6;
            int index = 1;
            foreach (BrjDefectRecord defect in defects)
            {
                worksheet.Cell(row, 1).Value = index;
                worksheet.Cell(row, 2).Value = "类型:";
                worksheet.Cell(row, 3).Value = defect.DefectType;
                worksheet.Cell(row, 4).Value = $"位置: {FormatNumber(defect.PositionXMm)}mm";
                worksheet.Cell(row, 5).Value = $"{FormatNumber(defect.PositionYM)}m";
                worksheet.Cell(row, 6).Value = $"面积: {FormatNumber(defect.AreaMm2)}mm^2";
                worksheet.Cell(row, 7).Value = $"直径: {FormatNumber(defect.DiameterMm)}mm";
                worksheet.Cell(row, 8).Value = $"相机:{defect.CameraDisplayName}";
                worksheet.Cell(row, 9).Value = $"分切号:{defect.SlitIndex}";
                worksheet.Range(row, 1, row, 9).Style.Font.FontSize = 12;
                worksheet.Range(row, 1, row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;
                index++;
            }
        }

        private static void PrepareWorksheet(IXLWorksheet worksheet, int columnCount)
        {
            worksheet.Style.Font.FontName = "宋体";
            worksheet.Style.Font.FontSize = 11;
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.Margins.Top = 0.4;
            worksheet.PageSetup.Margins.Bottom = 0.4;
            worksheet.PageSetup.Margins.Left = 0.3;
            worksheet.PageSetup.Margins.Right = 0.3;

            for (int column = 1; column <= columnCount; column++)
            {
                worksheet.Column(column).Width = 13;
            }

            worksheet.Column(1).Width = 10;
        }

        private static void WriteTitleAndHeader(IXLWorksheet worksheet, string title, BrjReportRecord record, int lastColumn)
        {
            worksheet.Range(1, 1, 1, lastColumn).Merge().Value = title;
            worksheet.Range(1, 1, 1, lastColumn).Style.Font.FontSize = 20;
            worksheet.Range(1, 1, 1, lastColumn).Style.Font.Bold = false;
            worksheet.Range(1, 1, 1, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Row(1).Height = 30;

            worksheet.Cell(2, 1).Value = "生产批号:";
            worksheet.Range(2, 2, 2, 3).Merge().Value = record.SN;
            worksheet.Cell(2, 4).Value = "生产米数:";
            worksheet.Cell(2, 5).Value = FormatNumber(record.DetectMeters) + "米";
            worksheet.Cell(2, 6).Value = "操作员:";
            worksheet.Range(2, 7, 2, lastColumn).Merge().Value = record.OperatorName;

            worksheet.Cell(3, 1).Value = "生产时间:";
            worksheet.Range(3, 2, 3, 3).Merge().Value = FormatTime(record.CreateTime);
            worksheet.Cell(3, 4).Value = "缺陷个数:";
            worksheet.Cell(3, 5).Value = record.DefectCount;
            worksheet.Cell(3, 6).Value = "产品型号:";
            worksheet.Range(3, 7, 3, lastColumn).Merge().Value = record.ProductModel;

            worksheet.Cell(4, 1).Value = "结束时间:";
            worksheet.Range(4, 2, 4, 3).Merge().Value = FormatTime(record.EndTime);

            worksheet.Range(2, 1, 4, lastColumn).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 4, lastColumn).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(2, 1, 4, lastColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(2, 1, 4, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range(2, 1, 4, lastColumn).Style.Font.FontSize = 11;
            worksheet.Range(2, 1, 4, lastColumn).Style.Font.FontColor = XLColor.FromHtml("#1F4E79");
        }

        private static void WriteRow(IXLWorksheet worksheet, int row, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                worksheet.Cell(row, i + 1).Value = values[i];
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatTime(DateTime value)
        {
            return value.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        }

        private static string FormatTime(DateTime? value)
        {
            return value?.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static void SaveWorkbook(XLWorkbook workbook, string filePath)
        {
            CloseOpenedReportIfNeeded(filePath);
            try
            {
                workbook.SaveAs(filePath);
            }
            catch (IOException)
            {
                CloseOpenedReportIfNeeded(filePath);
                workbook.SaveAs(filePath);
            }
        }

        private static void CloseOpenedReportIfNeeded(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            foreach (string progId in new[] { "Excel.Application", "Ket.Application", "ET.Application" })
            {
                CloseOpenedWorkbookIfNeeded(filePath, progId);
            }
        }

        private static void CloseOpenedWorkbookIfNeeded(string filePath, string progId)
        {
            object? excelObject = null;
            object? workbooksObject = null;
            try
            {
                CLSIDFromProgID(progId, out Guid excelGuid);
                GetActiveObject(ref excelGuid, IntPtr.Zero, out excelObject);
                dynamic excel = excelObject;
                workbooksObject = excel.Workbooks;
                dynamic workbooks = workbooksObject;
                int workbookCount = workbooks.Count;
                string targetPath = Path.GetFullPath(filePath);

                for (int index = workbookCount; index >= 1; index--)
                {
                    object? workbookObject = null;
                    try
                    {
                        workbookObject = workbooks[index];
                        dynamic openedWorkbook = workbookObject;
                        string workbookPath = Convert.ToString(openedWorkbook.FullName) ?? string.Empty;
                        if (string.Equals(Path.GetFullPath(workbookPath), targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            openedWorkbook.Close(false);
                            Thread.Sleep(200);
                            break;
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        ReleaseComObject(workbookObject);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                ReleaseComObject(workbooksObject);
                ReleaseComObject(excelObject);
            }
        }

        private static void ReleaseComObject(object? comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void CLSIDFromProgID(string lpszProgID, out Guid pclsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }
}
