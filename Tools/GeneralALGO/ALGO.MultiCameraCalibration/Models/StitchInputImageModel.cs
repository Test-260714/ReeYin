using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using System;

namespace ALGO.MultiCameraCalibration.Models
{
    [Serializable]
    public class StitchInputImageModel : BindableBase
    {
        private bool _isSelected = true;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        private string _cameraId = string.Empty;
        public string CameraId
        {
            get { return _cameraId; }
            set { SetProperty(ref _cameraId, value); }
        }

        private TransmitParam _inputImage = new TransmitParam();
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set { SetProperty(ref _inputImage, value); }
        }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get { return _imagePath; }
            set { SetProperty(ref _imagePath, value); }
        }
    }
}
