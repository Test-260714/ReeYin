using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {
        /// <summary>
        /// XYHD 现只保留同步流程。保留该属性用于兼容旧方案文件中的 ContinuousMode 字段。
        /// </summary>
        [JsonIgnore]
        public bool ContinuousMode
        {
            get => false;
            set { }
        }

        private bool _swapLeftRightPaths;
        public bool SwapLeftRightPaths
        {
            get => _swapLeftRightPaths;
            set => SetProperty(ref _swapLeftRightPaths, value);
        }

        private bool _leftPathXMirror;
        public bool LeftPathXMirror
        {
            get => _leftPathXMirror;
            set => SetProperty(ref _leftPathXMirror, value);
        }

        private bool _rightPathXMirror;
        public bool RightPathXMirror
        {
            get => _rightPathXMirror;
            set => SetProperty(ref _rightPathXMirror, value);
        }

        private int _imageWidth;
        public int ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        private int _imageHeight;
        public int ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private int _okCount;
        public int OKCount
        {
            get => _okCount;
            set => SetProperty(ref _okCount, value);
        }

        private int _ngCount;
        public int NGCount
        {
            get => _ngCount;
            set => SetProperty(ref _ngCount, value);
        }

        public double OKRate => TotalCount > 0 ? (double)OKCount / TotalCount * 100 : 0;
        public double NGRate => TotalCount > 0 ? (double)NGCount / TotalCount * 100 : 0;

        public void UpdateRates()
        {
            RaisePropertyChanged(nameof(OKRate));
            RaisePropertyChanged(nameof(NGRate));
        }

        public void ResetStatistics()
        {
            TotalCount = 0;
            OKCount = 0;
            NGCount = 0;
            UpdateRates();
        }

        private string _savePath = "";
        public string SavePath
        {
            get => _savePath;
            set => SetProperty(ref _savePath, value);
        }

        private bool _saveOKImages;
        public bool SaveOKImages
        {
            get => _saveOKImages;
            set => SetProperty(ref _saveOKImages, value);
        }

        private bool _saveNGImages = true;
        public bool SaveNGImages
        {
            get => _saveNGImages;
            set => SetProperty(ref _saveNGImages, value);
        }
    }
}
