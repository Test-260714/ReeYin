#nullable enable

using Arction.Wpf.ChartingMVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public partial class DefectMapView : UserControl, IDisposable
    {
        public static readonly DependencyProperty DefectsProperty =
            DependencyProperty.Register(
                nameof(Defects),
                typeof(IEnumerable<DefectMapItem>),
                typeof(DefectMapView),
                new PropertyMetadata(null, OnDefectsChanged));

        public static readonly DependencyProperty DefectTypeStylesProperty =
            DependencyProperty.Register(
                nameof(DefectTypeStyles),
                typeof(IEnumerable<DefectMapTypeStyle>),
                typeof(DefectMapView),
                new PropertyMetadata(null, OnDefectTypeStylesChanged));

        public static readonly DependencyProperty MaterialWidthProperty =
            DependencyProperty.Register(
                nameof(MaterialWidth),
                typeof(double),
                typeof(DefectMapView),
                new PropertyMetadata(1d, OnMaterialSizeChanged));

        public static readonly DependencyProperty MaterialLengthProperty =
            DependencyProperty.Register(
                nameof(MaterialLength),
                typeof(double),
                typeof(DefectMapView),
                new PropertyMetadata(1d, OnMaterialSizeChanged));

        public static readonly DependencyProperty LengthOriginProperty =
            DependencyProperty.Register(
                nameof(LengthOrigin),
                typeof(DefectMapLengthOrigin),
                typeof(DefectMapView),
                new PropertyMetadata(DefectMapLengthOrigin.Top, OnLengthOriginChanged));

        public static readonly DependencyProperty SelectedDefectProperty =
            DependencyProperty.Register(
                nameof(SelectedDefect),
                typeof(DefectMapItem),
                typeof(DefectMapView),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDefectChanged));

        public static readonly DependencyProperty DefectSelectedCommandProperty =
            DependencyProperty.Register(
                nameof(DefectSelectedCommand),
                typeof(ICommand),
                typeof(DefectMapView),
                new PropertyMetadata(null));

        public static readonly DependencyProperty MapTitleProperty =
            DependencyProperty.Register(
                nameof(MapTitle),
                typeof(string),
                typeof(DefectMapView),
                new PropertyMetadata("Defect Map", OnMapTitleChanged));

        private readonly DefectMapViewModel _viewModel = new();
        private bool _disposed;
        private bool _isViewActive;
        private bool _isSyncingSelectedDefect;

        public DefectMapView()
        {
            InitializeComponent();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public IEnumerable<DefectMapItem>? Defects
        {
            get => (IEnumerable<DefectMapItem>?)GetValue(DefectsProperty);
            set => SetValue(DefectsProperty, value);
        }

        public IEnumerable<DefectMapTypeStyle>? DefectTypeStyles
        {
            get => (IEnumerable<DefectMapTypeStyle>?)GetValue(DefectTypeStylesProperty);
            set => SetValue(DefectTypeStylesProperty, value);
        }

        public double MaterialWidth
        {
            get => (double)GetValue(MaterialWidthProperty);
            set => SetValue(MaterialWidthProperty, value);
        }

        public double MaterialLength
        {
            get => (double)GetValue(MaterialLengthProperty);
            set => SetValue(MaterialLengthProperty, value);
        }

        public DefectMapLengthOrigin LengthOrigin
        {
            get => (DefectMapLengthOrigin)GetValue(LengthOriginProperty);
            set => SetValue(LengthOriginProperty, value);
        }

        public DefectMapItem? SelectedDefect
        {
            get => (DefectMapItem?)GetValue(SelectedDefectProperty);
            set => SetValue(SelectedDefectProperty, value);
        }

        public ICommand? DefectSelectedCommand
        {
            get => (ICommand?)GetValue(DefectSelectedCommandProperty);
            set => SetValue(DefectSelectedCommandProperty, value);
        }

        public string MapTitle
        {
            get => (string)GetValue(MapTitleProperty);
            set => SetValue(MapTitleProperty, value);
        }

        public DefectMapViewModel ViewModel => _viewModel;

        private bool ShouldApplyDependencyChanges => _isViewActive && !_disposed;

        private static void OnDefectsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DefectMapView view && view.ShouldApplyDependencyChanges)
            {
                view._viewModel.SetDefects(e.NewValue as IEnumerable<DefectMapItem>);
            }
        }

        private static void OnDefectTypeStylesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DefectMapView view && view.ShouldApplyDependencyChanges)
            {
                view._viewModel.SetDefectTypeStyles(e.NewValue as IEnumerable<DefectMapTypeStyle>);
            }
        }

        private static void OnMaterialSizeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DefectMapView view && view.ShouldApplyDependencyChanges)
            {
                view._viewModel.SetMaterialSize(view.MaterialWidth, view.MaterialLength);
            }
        }

        private static void OnLengthOriginChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DefectMapView view &&
                view.ShouldApplyDependencyChanges &&
                e.NewValue is DefectMapLengthOrigin origin)
            {
                view._viewModel.SetLengthOrigin(origin);
            }
        }

        private static void OnSelectedDefectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not DefectMapView view ||
                !view.ShouldApplyDependencyChanges ||
                view._isSyncingSelectedDefect)
            {
                return;
            }

            view._viewModel.SetSelectedDefect(e.NewValue as DefectMapItem);
        }

        private static void OnMapTitleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is DefectMapView view && view.ShouldApplyDependencyChanges)
            {
                view._viewModel.SetMapTitle(e.NewValue as string);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                string.Equals(e.PropertyName, nameof(DefectMapViewModel.SelectedDefect), StringComparison.Ordinal))
            {
                SyncSelectedDefectFromViewModel();
            }
        }

        private void SyncSelectedDefectFromViewModel()
        {
            if (!ShouldApplyDependencyChanges || _isSyncingSelectedDefect)
            {
                return;
            }

            DefectMapItem? selectedDefect = _viewModel.SelectedDefect;
            if (ReferenceEquals(SelectedDefect, selectedDefect))
            {
                return;
            }

            _isSyncingSelectedDefect = true;
            try
            {
                SetCurrentValue(SelectedDefectProperty, selectedDefect);
            }
            finally
            {
                _isSyncingSelectedDefect = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _disposed = false;
            _isViewActive = true;
            ConfigureChart();
            _viewModel.SetMapTitle(MapTitle);
            _viewModel.SetMaterialSize(MaterialWidth, MaterialLength);
            _viewModel.SetLengthOrigin(LengthOrigin);
            _viewModel.SetDefects(Defects);
            _viewModel.SetDefectTypeStyles(DefectTypeStyles);
            _viewModel.SetSelectedDefect(SelectedDefect);
            SyncSelectedDefectFromViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _isViewActive = false;
            Dispose();
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            DefectMapSettingsWindow settingsWindow = new(_viewModel);
            Window? owner = Window.GetWindow(this);
            if (owner != null)
            {
                settingsWindow.Owner = owner;
            }

            settingsWindow.ShowDialog();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _viewModel.HideTooltip();
            _viewModel.SetDefects(null);
            _viewModel.SetDefectTypeStyles(null);
        }

        private void ConfigureChart()
        {
            if (PART_Chart?.ViewXY == null)
            {
                return;
            }

            var zoomPanOptions = PART_Chart.ViewXY.ZoomPanOptions;
            zoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.WheelZooming = WheelZooming.Off;
            zoomPanOptions.AxisWheelAction = AxisWheelAction.None;
            zoomPanOptions.RightToLeftZoomAction = RightToLeftZoomActionXY.Off;
            zoomPanOptions.MultiTouchZoomEnabled = false;
            zoomPanOptions.MultiTouchPanEnabled = false;
            zoomPanOptions.AspectRatioOptions.AspectRatio = ViewAspectRatio.Off;
            zoomPanOptions.AspectRatioOptions.XAxisIndex = 0;
            zoomPanOptions.AspectRatioOptions.YAxisIndex = 0;

            if (PART_Chart.ViewXY.LegendBoxes.Count > 0)
            {
                PART_Chart.ViewXY.LegendBoxes[0].Visible = false;
            }

            PART_Chart.ViewXY.DataCursor.Visible = false;
        }

        private void Chart_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!TryGetAxisValues(e, out double xValue, out double yValue))
            {
                _viewModel.HideTooltip();
                return;
            }

            DefectMapItem? item = _viewModel.FindNearestByAxisValues(xValue, yValue);
            if (item == null)
            {
                _viewModel.HideTooltip();
                return;
            }

            _viewModel.ShowTooltip(item, e.GetPosition(PART_Chart));
        }

        private void Chart_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TryGetAxisValues(e, out double xValue, out double yValue))
            {
                return;
            }

            if (!_viewModel.TrySelectNearest(xValue, yValue, out DefectMapItem? selectedDefect) || selectedDefect == null)
            {
                return;
            }

            _isSyncingSelectedDefect = true;
            try
            {
                SetCurrentValue(SelectedDefectProperty, selectedDefect);
            }
            finally
            {
                _isSyncingSelectedDefect = false;
            }

            if (DefectSelectedCommand?.CanExecute(selectedDefect) == true)
            {
                DefectSelectedCommand.Execute(selectedDefect);
            }

            e.Handled = true;
        }

        private void Chart_MouseLeave(object sender, MouseEventArgs e)
        {
            _viewModel.HideTooltip();
        }

        private bool TryGetAxisValues(MouseEventArgs e, out double xValue, out double yValue)
        {
            xValue = 0d;
            yValue = 0d;

            if (PART_Chart?.ViewXY == null || _viewModel.ChartXAxes.Count == 0 || _viewModel.ChartYAxes.Count == 0)
            {
                return false;
            }

            Point position = e.GetPosition(PART_Chart);
            _viewModel.ChartXAxes[0].CoordToValue((int)position.X, out xValue, false);
            _viewModel.ChartYAxes[0].CoordToValue((int)position.Y, out yValue);

            return double.IsFinite(xValue) && double.IsFinite(yValue);
        }
    }
}
