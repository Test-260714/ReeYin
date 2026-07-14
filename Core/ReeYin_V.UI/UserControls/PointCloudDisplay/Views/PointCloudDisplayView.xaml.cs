using Arction.Wpf.ChartingMVVM;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin_V.UI.UserControls.PointCloudDisplay
{
    public partial class PointCloudDisplayView : IDisposable
    {
        public static readonly DependencyProperty SourceFilePathProperty =
            DependencyProperty.Register(
                nameof(SourceFilePath),
                typeof(string),
                typeof(PointCloudDisplayView),
                new PropertyMetadata(string.Empty, OnSourceFilePathChanged));

        private PointCloudDisplayViewModel _viewModel;

        public PointCloudDisplayView()
        {
            InitializeComponent();
            _viewModel = new PointCloudDisplayViewModel();
            DataContext = _viewModel;
        }

        public string SourceFilePath
        {
            get => (string)GetValue(SourceFilePathProperty);
            set => SetValue(SourceFilePathProperty, value);
        }

        public Task<bool> LoadPointCloudFileAsync(string filePath)
        {
            return LoadPointCloudFileCoreAsync(filePath);
        }

        public void LoadPointCloud(IEnumerable<PointCloudPointData> points, string sourceName = "Memory Data")
        {
            _viewModel.LoadPoints(points, sourceName);
            ApplySettingsFromUi();
        }

        public void LoadPointCloud(
            double[] xValues,
            double[] yValues,
            double[] zValues,
            double[]? intensityValues = null,
            string sourceName = "Memory Data")
        {
            _viewModel.LoadPoints(xValues, yValues, zValues, intensityValues, sourceName);
            ApplySettingsFromUi();
        }

        private static async void OnSourceFilePathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is not PointCloudDisplayView view)
            {
                return;
            }

            string? filePath = args.NewValue as string;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            await view.LoadPointCloudFileCoreAsync(filePath);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettingsFromUi();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Point Cloud File",
                Filter = "Point Cloud|*.ply;*.xyz;*.txt;*.csv;*.obj|PLY|*.ply|XYZ|*.xyz;*.txt;*.csv|OBJ|*.obj|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SourceFilePath = dialog.FileName;
            }
        }

        private void ResetViewButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetView();
        }

        private void TopViewButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetTopView();
        }

        private void DisplaySettingChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplySettingsFromUi();
        }

        private async Task<bool> LoadPointCloudFileCoreAsync(string filePath)
        {
            bool result = await _viewModel.LoadFromFileAsync(filePath);
            ApplySettingsFromUi();
            return result;
        }

        private void ApplySettingsFromUi()
        {
            PointCloudColorSource colorSource = ((cb_ColorSource.SelectedItem as ComboBoxItem)?.Tag?.ToString()) switch
            {
                "SingleColor" => PointCloudColorSource.SingleColor,
                "XAxis" => PointCloudColorSource.XAxis,
                "YAxis" => PointCloudColorSource.YAxis,
                "Intensity" => PointCloudColorSource.Intensity,
                _ => PointCloudColorSource.ZAxis
            };

            PointCloudPaletteType paletteType = ((cb_PaletteType.SelectedItem as ComboBoxItem)?.Tag?.ToString()) switch
            {
                "Heatmap" => PointCloudPaletteType.Heatmap,
                _ => PointCloudPaletteType.Classic
            };

            PointShape3D pointShape = ((cb_PointShape.SelectedItem as ComboBoxItem)?.Tag?.ToString()) switch
            {
                "Box" => PointShape3D.Box,
                "Cone" => PointShape3D.Cone,
                _ => PointShape3D.Sphere
            };

            ProjectionType projectionType = ((cb_Projection.SelectedItem as ComboBoxItem)?.Tag?.ToString()) switch
            {
                "Orthographic" => ProjectionType.Orthographic,
                _ => ProjectionType.Perspective
            };

            bool connectPoints = cb_ConnectPoints.IsChecked == true;
            bool planeSmooth = cb_PlaneSmooth.IsChecked == true;
            double lineWidth = slider_LineWidth.Value;

            _viewModel.ApplyDisplaySettings(
                colorSource,
                paletteType,
                pointShape,
                slider_PointSize.Value,
                projectionType,
                connectPoints,
                lineWidth,
                planeSmooth);
        }

        public void Dispose()
        {
            _viewModel.Dispose();
        }
    }
}
