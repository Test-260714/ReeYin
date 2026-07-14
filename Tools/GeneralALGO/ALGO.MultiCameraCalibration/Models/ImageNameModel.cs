using ReeYin_V.Core.Interfaces;
using System;

namespace ALGO.MultiCameraCalibration.Models
{
    [Serializable]
    public class ImageNameModel : BindableBase
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

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

        private string _captureId = string.Empty;
        public string CaptureId
        {
            get { return _captureId; }
            set { SetProperty(ref _captureId, value); }
        }

        private string _imageName = string.Empty;
        public string ImageName
        {
            get { return _imageName; }
            set { SetProperty(ref _imageName, value); }
        }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get { return _imagePath; }
            set { SetProperty(ref _imagePath, value); }
        }
    }
}
