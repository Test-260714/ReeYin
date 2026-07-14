using Custom.DefectOverview.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using System.Windows.Media;

namespace Custom.DefectOverview.ViewModels
{
    public class DefectDetailViewModel : DialogViewModelBase
    {
        public ImageSource PreviewImage { get; private set; }
        public string ClassName { get; private set; } = "-";
        public string PathText { get; private set; } = "-";
        public string FrameIdText { get; private set; } = "-";
        public string MeterText { get; private set; } = "-";
        public string PositionText { get; private set; } = "-";
        public string ConfidenceText { get; private set; } = "-";
        public string SizeText { get; private set; } = "-";
        public string SummaryText { get; private set; } = "-";
        public string DetailText { get; private set; } = "-";

        public DelegateCommand CloseCommand { get; }

        public DefectDetailViewModel()
        {
            CloseCommand = new DelegateCommand(() => CloseDialog(ButtonResult.OK));
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            switch (Param)
            {
                case BandMapWallItem bandMapItem:
                    ApplyItem(
                        bandMapItem.PreviewImage ?? bandMapItem.ThumbnailImage,
                        bandMapItem.ClassName,
                        bandMapItem.PathText,
                        bandMapItem.FrameIdText,
                        bandMapItem.MeterText,
                        bandMapItem.PositionText,
                        bandMapItem.ConfidenceText,
                        bandMapItem.SizeText,
                        bandMapItem.SummaryText,
                        bandMapItem.DetailText);
                    break;
                default:
                    return;
            }

            RaisePropertyChanged(nameof(PreviewImage));
            RaisePropertyChanged(nameof(ClassName));
            RaisePropertyChanged(nameof(PathText));
            RaisePropertyChanged(nameof(FrameIdText));
            RaisePropertyChanged(nameof(MeterText));
            RaisePropertyChanged(nameof(PositionText));
            RaisePropertyChanged(nameof(ConfidenceText));
            RaisePropertyChanged(nameof(SizeText));
            RaisePropertyChanged(nameof(SummaryText));
            RaisePropertyChanged(nameof(DetailText));
        }

        private void ApplyItem(
            ImageSource previewImage,
            string className,
            string pathText,
            string frameIdText,
            string meterText,
            string positionText,
            string confidenceText,
            string sizeText,
            string summaryText,
            string detailText)
        {
            PreviewImage = previewImage;
            ClassName = className ?? "-";
            PathText = string.IsNullOrWhiteSpace(pathText) ? "-" : pathText;
            FrameIdText = frameIdText ?? "-";
            MeterText = meterText ?? "-";
            PositionText = positionText ?? "-";
            ConfidenceText = confidenceText ?? "-";
            SizeText = sizeText ?? "-";
            SummaryText = summaryText ?? "-";
            DetailText = detailText ?? "-";
        }
    }
}
