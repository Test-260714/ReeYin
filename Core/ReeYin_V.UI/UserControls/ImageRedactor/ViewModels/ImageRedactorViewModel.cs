using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Annotations;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using Prism.Mvvm;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReeYin_V.UI.UserControls.ImageRedactor
{
    public enum ImageRedactorEditMode
    {
        None = 0,
        AddTextAnnotation,
        AddBitmapAnnotation,
        AddRingPoint,
        AddPolygon,
        AddPolyline
    }

    public class ImageRedactorViewModel : BindableBase
    {
        private readonly ImageRedactorModel _model = new();

        private AnnotationXY? _backgroundAnnotation;
        private BitmapFrame? _annotationImage;
        private Color _drawColor = Colors.Orange;
        private string _annotationText = "Annotation";
        private string _hintText = "Hint: left mouse to zoom, right mouse to pan";
        private string _statusText = "No background image loaded";
        private string _imagePath = string.Empty;
        private double _imageWidth;
        private double _imageHeight;
        private double _lineWidth = 2;
        private ImageRedactorEditMode _editMode;
        private FreeformPointLineSeries? _currentSketch;

        public ImageRedactorViewModel()
        {
            XAxes = new AxisXCollection { _model.CreateAxisX() };
            YAxes = new AxisYCollection { _model.CreateAxisY() };
            Annotations = new AnnotationXYCollection();
            FreeformPointLineSeries = new FreeformPointLineSeriesCollection();
            PolygonSeriesCollection = new PolygonSeriesCollection();
        }

        public AxisXCollection XAxes { get; }

        public AxisYCollection YAxes { get; }

        public AnnotationXYCollection Annotations { get; }

        public FreeformPointLineSeriesCollection FreeformPointLineSeries { get; }

        public PolygonSeriesCollection PolygonSeriesCollection { get; }

        public BitmapFrame? AnnotationImage
        {
            get => _annotationImage;
            private set
            {
                if (SetProperty(ref _annotationImage, value))
                {
                    RaisePropertyChanged(nameof(AnnotationImagePreview));
                }
            }
        }

        public ImageSource? AnnotationImagePreview => AnnotationImage;

        public Color DrawColor
        {
            get => _drawColor;
            private set => SetProperty(ref _drawColor, value);
        }

        public string AnnotationText
        {
            get => _annotationText;
            set => SetProperty(ref _annotationText, value);
        }

        public string HintText
        {
            get => _hintText;
            private set => SetProperty(ref _hintText, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string ImagePath
        {
            get => _imagePath;
            private set => SetProperty(ref _imagePath, value);
        }

        public double ImageWidth
        {
            get => _imageWidth;
            private set => SetProperty(ref _imageWidth, value);
        }

        public double ImageHeight
        {
            get => _imageHeight;
            private set => SetProperty(ref _imageHeight, value);
        }

        public double LineWidth
        {
            get => _lineWidth;
            private set => SetProperty(ref _lineWidth, value);
        }

        public ImageRedactorEditMode EditMode
        {
            get => _editMode;
            private set => SetProperty(ref _editMode, value);
        }

        public bool HasBackgroundImage => _backgroundAnnotation is not null;

        public bool LoadBackgroundImage(string filePath)
        {
            try
            {
                BitmapFrame imageFrame = _model.LoadBitmapFrame(filePath, out int pixelWidth, out int pixelHeight);

                CancelInProgressSketch();

                FreeformPointLineSeries.Clear();
                PolygonSeriesCollection.Clear();
                Annotations.Clear();

                XAxes[0].Minimum = 0;
                XAxes[0].Maximum = pixelWidth;
                YAxes[0].Minimum = 0;
                YAxes[0].Maximum = pixelHeight;

                _backgroundAnnotation = _model.CreateBackgroundAnnotation(imageFrame, pixelWidth, pixelHeight);
                Annotations.Add(_backgroundAnnotation);

                ImagePath = filePath;
                ImageWidth = pixelWidth;
                ImageHeight = pixelHeight;
                StatusText = $"Image: {Path.GetFileName(filePath)} ({pixelWidth} x {pixelHeight})";

                SetEditMode(ImageRedactorEditMode.None);
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to load image: {ex.Message}";
                return false;
            }
        }

        public bool LoadAnnotationImage(string filePath)
        {
            try
            {
                AnnotationImage = _model.LoadBitmapFrame(filePath, out _, out _);
                StatusText = $"Annotation bitmap: {Path.GetFileName(filePath)}";
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to load annotation bitmap: {ex.Message}";
                return false;
            }
        }

        public void SetDrawColor(Color color)
        {
            DrawColor = color;

            if (_currentSketch is not null)
            {
                _currentSketch.LineStyle.Color = color;
            }
        }

        public void SetLineWidth(double lineWidth)
        {
            LineWidth = lineWidth;

            if (_currentSketch is not null)
            {
                _currentSketch.LineStyle.Width = lineWidth;
            }
        }

        public void SetEditMode(ImageRedactorEditMode mode)
        {
            EditMode = mode;
            HintText = mode switch
            {
                ImageRedactorEditMode.AddTextAnnotation => "Hint: left click image to add a text annotation",
                ImageRedactorEditMode.AddBitmapAnnotation => "Hint: left click image to add a bitmap annotation",
                ImageRedactorEditMode.AddRingPoint => "Hint: left click image to add a ring point",
                ImageRedactorEditMode.AddPolygon => "Hint: hold left mouse button and drag to draw a polygon",
                ImageRedactorEditMode.AddPolyline => "Hint: hold left mouse button and drag to draw a polyline",
                _ => "Hint: left mouse to zoom, right mouse to pan"
            };
        }

        public bool AddTextAnnotation(double xValue, double yValue)
        {
            if (!HasBackgroundImage)
            {
                StatusText = "Load a background image first.";
                return false;
            }

            string text = string.IsNullOrWhiteSpace(AnnotationText) ? "Annotation" : AnnotationText.Trim();
            Annotations.Add(_model.CreateTextAnnotation(text, xValue, yValue));
            StatusText = $"Text annotation added at ({xValue:F1}, {yValue:F1}).";
            return true;
        }

        public bool AddBitmapAnnotation(double xValue, double yValue)
        {
            if (!HasBackgroundImage)
            {
                StatusText = "Load a background image first.";
                return false;
            }

            if (AnnotationImage is null)
            {
                StatusText = "Select an annotation bitmap first.";
                return false;
            }

            Annotations.Add(_model.CreateBitmapAnnotation(AnnotationImage, xValue, yValue));
            StatusText = $"Bitmap annotation added at ({xValue:F1}, {yValue:F1}).";
            return true;
        }

        public bool AddRingPoint(double xValue, double yValue)
        {
            if (!HasBackgroundImage)
            {
                StatusText = "Load a background image first.";
                return false;
            }

            Annotations.Add(_model.CreateRingPointAnnotation(xValue, yValue, DrawColor, LineWidth));
            StatusText = $"Ring point added at ({xValue:F1}, {yValue:F1}).";
            return true;
        }

        public bool StartOrContinueSketch(double xValue, double yValue)
        {
            if (!HasBackgroundImage)
            {
                StatusText = "Load a background image first.";
                return false;
            }

            if (_currentSketch is null)
            {
                _currentSketch = _model.CreateSketchSeries(DrawColor, LineWidth);
                FreeformPointLineSeries.Add(_currentSketch);
            }

            _currentSketch.AddPoints(new[] { new SeriesPoint(xValue, yValue) }, true);
            return true;
        }

        public void FinishSketch()
        {
            if (_currentSketch is null)
            {
                return;
            }

            if (EditMode == ImageRedactorEditMode.AddPolygon)
            {
                if (_currentSketch.PointCount > 2)
                {
                    PolygonSeriesCollection.Add(_model.CreatePolygon(DrawColor, _currentSketch.Points));
                    StatusText = $"Polygon created with {_currentSketch.PointCount} points.";
                }
                else
                {
                    StatusText = "A polygon needs at least 3 points.";
                }

                FreeformPointLineSeries.Remove(_currentSketch);
            }
            else if (EditMode == ImageRedactorEditMode.AddPolyline)
            {
                StatusText = $"Polyline created with {_currentSketch.PointCount} points.";
            }

            _currentSketch = null;
        }

        public void ClearOverlays()
        {
            CancelInProgressSketch();

            FreeformPointLineSeries.Clear();
            PolygonSeriesCollection.Clear();

            Annotations.Clear();
            if (_backgroundAnnotation is not null)
            {
                Annotations.Add(_backgroundAnnotation);
            }

            StatusText = HasBackgroundImage ? "Overlays cleared." : "There are no overlays to clear.";
        }

        public void CancelInProgressSketch()
        {
            if (_currentSketch is null)
            {
                return;
            }

            FreeformPointLineSeries.Remove(_currentSketch);
            _currentSketch = null;
        }
    }
}
