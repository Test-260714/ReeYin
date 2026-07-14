using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
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

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {
        private void UpdateStatistics(bool isNG, int totalPieces, int ngPieces)
        {
            int safeTotalPieces = Math.Max(0, totalPieces);
            int safeNgPieces = Math.Min(Math.Max(0, ngPieces), safeTotalPieces);
            int okPieces = safeTotalPieces - safeNgPieces;

            RunOnUiThread(() =>
            {
                Model.TotalCount += safeTotalPieces;
                Model.NGCount += safeNgPieces;
                Model.OKCount += okPieces;

                if (isNG)
                {
                    LastResult = "NG";
                    ConsecutiveNGCount++;
                    if (ConsecutiveNGCount > MaxConsecutiveNGCount)
                        MaxConsecutiveNGCount = ConsecutiveNGCount;
                }
                else
                {
                    LastResult = "OK";
                    ConsecutiveNGCount = 0;
                }

                Model.UpdateRates();
                StatusText = "检测中";
            });
        }

        private void UpdateDefectDetails(List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            var safeResults = results ?? new List<ReeYin_V.Core.DeepLearning.Result>();
            int defectCount = safeResults.Count;
            var summary = defectCount == 0 ? "无缺陷" : $"缺陷 {defectCount} 个";

            RunOnUiThread(() =>
            {
                LastDefectCount = defectCount;
                LastDefectSummary = summary;
                DefectDetails.Clear();
            });
        }
    }
}
