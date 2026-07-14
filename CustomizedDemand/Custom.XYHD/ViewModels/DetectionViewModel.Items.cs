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
    public class DefectDetailItem
    {
        public int Index { get; set; }
        public string ClassName { get; set; }
        public int ClassId { get; set; }
        public string ConfidenceText { get; set; }
        public string CenterText { get; set; }
        public string SizeText { get; set; }
    }

    internal sealed class PendingPathUpdate
    {
        public string PathName { get; init; }
        public int Serial { get; init; }
        public double LaneWidth { get; init; }
        public HImage PathImage { get; init; }
        public HImage OriginalImage { get; init; }
        public bool IsNG { get; init; }
        public int PieceCount { get; init; }
        public int NgPieceCount { get; init; }
        public List<ReeYin_V.Core.DeepLearning.Result> Results { get; init; } = [];
    }

    internal sealed class PendingFrameUpdate
    {
        public long FrameId { get; init; }
        public string FrameIdText { get; init; }
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public bool MainImageUpdated { get; set; }
        public bool Committed { get; set; }
        public PendingPathUpdate Left { get; set; }
        public PendingPathUpdate Right { get; set; }

        public bool IsComplete => Left != null && Right != null;

        public bool FrameIsNg => (Left?.IsNG ?? false) || (Right?.IsNG ?? false);

        public int TotalPieceCount => Math.Max(0, Left?.PieceCount ?? 0) + Math.Max(0, Right?.PieceCount ?? 0);

        public int NgPieceCount => Math.Max(0, Left?.NgPieceCount ?? 0) + Math.Max(0, Right?.NgPieceCount ?? 0);

        public List<ReeYin_V.Core.DeepLearning.Result> GetMergedResults()
        {
            var merged = new List<ReeYin_V.Core.DeepLearning.Result>();
            if (Left?.Results != null)
                merged.AddRange(Left.Results);
            if (Right?.Results != null)
                merged.AddRange(Right.Results);
            return merged;
        }

        public string GetPathSummary()
        {
            var hasLeftNg = Left?.IsNG ?? false;
            var hasRightNg = Right?.IsNG ?? false;
            if (hasLeftNg && hasRightNg)
                return "左右";
            if (hasLeftNg)
                return "左";
            if (hasRightNg)
                return "右";
            return "整帧";
        }
    }
}
