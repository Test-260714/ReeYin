using ALGO.LineScanSheetCounter.Models;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.LineScanSheetCounter.Views;

/// <summary>
/// 线扫计数模块的 HALCON 图像预览控件。
/// </summary>
public partial class LineScanSheetCounterPreviewControl : UserControl
{
    private bool _previewRenderQueued;

    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(LineScanSheetCounterModel),
            typeof(LineScanSheetCounterPreviewControl),
            new PropertyMetadata(null, OnModelChanged));

    public LineScanSheetCounterModel? Model
    {
        get => (LineScanSheetCounterModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public LineScanSheetCounterPreviewControl()
    {
        InitializeComponent();
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LineScanSheetCounterPreviewControl)d;
        control.UnsubscribeModel(e.OldValue as LineScanSheetCounterModel);
        control.SubscribeModel(e.NewValue as LineScanSheetCounterModel);
    }

    private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
    {
        QueuePreviewRender();
    }

    private void PreviewControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _previewRenderQueued = false;
        HalconPreview?.ClearWindow();
    }

    private void SubscribeModel(LineScanSheetCounterModel? model)
    {
        if (model == null)
        {
            return;
        }

        model.PropertyChanged += Model_PropertyChanged;
        model.PreviewDrawObjects.CollectionChanged += PreviewDrawObjects_CollectionChanged;
        QueuePreviewRender();
    }

    private void UnsubscribeModel(LineScanSheetCounterModel? model)
    {
        if (model == null)
        {
            return;
        }

        model.PropertyChanged -= Model_PropertyChanged;
        model.PreviewDrawObjects.CollectionChanged -= PreviewDrawObjects_CollectionChanged;
        HalconPreview?.ClearWindow();
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LineScanSheetCounterModel.PreviewImageObject))
        {
            QueuePreviewRender();
        }
    }

    private void PreviewDrawObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        QueuePreviewRender();
    }

    /// <summary>
    /// 合并本轮图像和叠加对象变化，在同一次 HALCON 重绘中显示。
    /// </summary>
    private void QueuePreviewRender()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(QueuePreviewRender));
            return;
        }

        if (_previewRenderQueued)
        {
            return;
        }

        _previewRenderQueued = true;
        Dispatcher.BeginInvoke(new Action(RenderPreview), DispatcherPriority.Render);
    }

    private void RenderPreview()
    {
        _previewRenderQueued = false;
        if (Model == null || HalconPreview == null)
        {
            return;
        }

        // 隐藏控件到本轮 UI 绘制结束，避免用户看到底图和叠加线的中间刷新状态。
        HalconPreview.Visibility = Visibility.Hidden;
        try
        {
            if (Model.PreviewImageObject == null)
            {
                HalconPreview.ClearWindow();
                return;
            }

            HalconPreview.Image = Model.PreviewImageObject;
            SyncPreviewDrawObjects();
        }
        finally
        {
            HalconPreview.Visibility = Visibility.Visible;
        }
    }

    private void SyncPreviewDrawObjects()
    {
        if (Model == null || HalconPreview?.DrawObjectList == null)
        {
            return;
        }

        ClearMirroredPreviewObjects();
        foreach (var drawObject in Model.PreviewDrawObjects.ToList())
        {
            if (drawObject?.Hobject == null || !drawObject.Hobject.IsInitialized())
            {
                continue;
            }

            try
            {
                HalconPreview.DrawObjectList.Add(new HalconDrawingObject
                {
                    ShapeType = drawObject.ShapeType,
                    Hobject = drawObject.Hobject.Clone(),
                    HTuples = drawObject.HTuples,
                    Color = drawObject.Color,
                    IsFillDisplay = drawObject.IsFillDisplay
                });
            }
            catch
            {
            }
        }
    }

    private void ClearMirroredPreviewObjects()
    {
        if (HalconPreview?.DrawObjectList == null)
        {
            return;
        }

        foreach (var item in HalconPreview.DrawObjectList.ToList())
        {
            try
            {
                item.Hobject?.Dispose();
            }
            catch
            {
            }
        }

        HalconPreview.DrawObjectList.Clear();
    }
}
