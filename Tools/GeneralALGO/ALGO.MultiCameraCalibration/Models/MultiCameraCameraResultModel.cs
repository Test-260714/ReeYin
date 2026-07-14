using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace ALGO.MultiCameraCalibration.Models
{
    [Serializable]
    public class MultiCameraCameraResultModel : BindableBase
    {
        private string _cameraId = string.Empty;
        public string CameraId
        {
            get { return _cameraId; }
            set { SetProperty(ref _cameraId, value); }
        }

        private int _imageWidth;
        public int ImageWidth
        {
            get { return _imageWidth; }
            set { SetProperty(ref _imageWidth, value); }
        }

        private int _imageHeight;
        public int ImageHeight
        {
            get { return _imageHeight; }
            set { SetProperty(ref _imageHeight, value); }
        }

        private double _rmsError;
        public double RmsError
        {
            get { return _rmsError; }
            set { SetProperty(ref _rmsError, value); }
        }

        private ObservableCollection<double> _intrinsicMatrix = new ObservableCollection<double>();
        public ObservableCollection<double> IntrinsicMatrix
        {
            get { return _intrinsicMatrix; }
            set { SetProperty(ref _intrinsicMatrix, value); }
        }

        private ObservableCollection<double> _distortionCoefficients = new ObservableCollection<double>();
        public ObservableCollection<double> DistortionCoefficients
        {
            get { return _distortionCoefficients; }
            set { SetProperty(ref _distortionCoefficients, value); }
        }

        private ObservableCollection<double> _rotationVector = new ObservableCollection<double>();
        public ObservableCollection<double> RotationVector
        {
            get { return _rotationVector; }
            set { SetProperty(ref _rotationVector, value); }
        }

        private ObservableCollection<double> _translationVector = new ObservableCollection<double>();
        public ObservableCollection<double> TranslationVector
        {
            get { return _translationVector; }
            set { SetProperty(ref _translationVector, value); }
        }

        private ObservableCollection<double> _extrinsicMatrix = new ObservableCollection<double>();
        public ObservableCollection<double> ExtrinsicMatrix
        {
            get { return _extrinsicMatrix; }
            set { SetProperty(ref _extrinsicMatrix, value); }
        }

        public static MultiCameraCameraResultModel FromParams(MultiCameraCalibrationSdk.MultiCameraCameraParams param)
        {
            return new MultiCameraCameraResultModel
            {
                CameraId = param.cameraId ?? string.Empty,
                ImageWidth = param.imageWidth,
                ImageHeight = param.imageHeight,
                RmsError = param.rmsError,
                IntrinsicMatrix = new ObservableCollection<double>(param.intrinsic ?? Array.Empty<double>()),
                DistortionCoefficients = new ObservableCollection<double>(param.distortion ?? Array.Empty<double>()),
                RotationVector = new ObservableCollection<double>(param.rvecCommonFromCamera ?? Array.Empty<double>()),
                TranslationVector = new ObservableCollection<double>(param.tvecCommonFromCamera ?? Array.Empty<double>()),
                ExtrinsicMatrix = new ObservableCollection<double>(param.extrinsicCommonFromCamera ?? Array.Empty<double>())
            };
        }
    }
}
