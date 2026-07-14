using HalconDotNet;
using ReeYin.Customized.Algo.Algorithms;
using ReeYin.Customized.Algo.Models;
using ReeYin.Customized.Algo.ViewModels;
using Prism.Dialogs;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;
using UserControl = System.Windows.Controls.UserControl;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ReeYin.Customized.Algo.Views
{
    internal enum HeightDifferenceHeatmapInteractionState
    {
        Idle,
        WaitingLine,
        DrawingLine,
        WaitingFirstRectangle,
        DrawingFirstRectangle,
        WaitingSecondRectangle,
        DrawingSecondRectangle
    }

    public partial class HeightDifferenceMeasureView : UserControl
    {
        #region 字段与状态

        // 对话框固定尺寸，保证热力图、点云和结果列表布局稳定。
        private const double DialogHostWidth = 1600;
        /// <summary>
        /// 高度差测量弹窗的固定显示高度，避免内容区随外部布局压缩。
        /// </summary>
        private const double DialogHostHeight = 920;
        /// <summary>
        /// 高度曲线绘图区左侧预留宽度，用于显示纵轴刻度和留白。
        /// </summary>
        private const double ProfilePlotLeftMargin = 64.0;
        /// <summary>
        /// 高度曲线绘图区顶部留白，避免曲线贴近窗口边缘。
        /// </summary>
        private const double ProfilePlotTopMargin = 26.0;
        /// <summary>
        /// 高度曲线绘图区右侧留白，用于保证曲线末端显示完整。
        /// </summary>
        private const double ProfilePlotRightMargin = 18.0;
        /// <summary>
        /// 高度曲线绘图区底部预留高度，用于显示横轴和区间选框。
        /// </summary>
        private const double ProfilePlotBottomMargin = 42.0;
        /// <summary>
        /// 热力图叠加线在屏幕上的目标粗细，用于随窗口缩放换算图像线宽。
        /// </summary>
        private const double HeatmapOverlayTargetScreenPixels = 5.0;
        /// <summary>
        /// 热力图叠加线换算到图像坐标后的最小线宽。
        /// </summary>
        private const double HeatmapOverlayMinImageThickness = 1.2;
        /// <summary>
        /// 热力图叠加线换算到图像坐标后的最大线宽，防止放大后线条过粗。
        /// </summary>
        private const double HeatmapOverlayMaxImageThickness = 16.0;
        /// <summary>
        /// 热力图上画线测量主线的显示颜色。
        /// </summary>
        private const string ProfileLineOverlayColor = "#FF00FF";
        /// <summary>
        /// 第一段高度区间在热力图和曲线上的显示颜色。
        /// </summary>
        private const string Segment1OverlayColor = "#FF66CC";
        /// <summary>
        /// 第二段高度区间在热力图和曲线上的显示颜色。
        /// </summary>
        private const string Segment2OverlayColor = "#9D00FF";
        /// <summary>
        /// 鼠标拖拽过程中的临时预览图形颜色。
        /// </summary>
        private const string PreviewOverlayColor = "#FFFFFF";

        private readonly struct RotatedMeasureRectangle
        {
            /// <summary>
            /// 创建旋转矩形描述，使用图像坐标记录中心、角度和半轴长度。
            /// </summary>
            public RotatedMeasureRectangle(double centerX, double centerY, double phi, double length1, double length2)
            {
                CenterX = centerX;
                CenterY = centerY;
                Phi = phi;
                Length1 = length1;
                Length2 = length2;
            }

            /// <summary>
            /// 旋转矩形中心点的图像列坐标（像素）。
            /// </summary>
            public double CenterX { get; }

            /// <summary>
            /// 旋转矩形中心点的图像行坐标（像素）。
            /// </summary>
            public double CenterY { get; }

            /// <summary>
            /// 旋转矩形相对图像列方向的角度（弧度）。
            /// </summary>
            public double Phi { get; }

            /// <summary>
            /// 旋转矩形长轴半长，单位为图像像素。
            /// </summary>
            public double Length1 { get; }

            /// <summary>
            /// 旋转矩形短轴半长，单位为图像像素。
            /// </summary>
            public double Length2 { get; }
        }

        // 点云显示控件当前绑定的视图模型。
        private HeightDifferenceMeasureViewModel? _viewModel;
        // 当前视图绑定的高度差测量模型。
        private HeightDifferenceMeasureModel? _model;
        // 记录热力图中是否已经完成画线测量主线。
        private bool _hasSelectionLine;
        // 画线测量起点的图像像素坐标。
        private Point _selectionStartPixel;
        // 画线测量终点的图像像素坐标。
        private Point _selectionEndPixel;
        // 热力图当前鼠标交互状态。
        private HeightDifferenceHeatmapInteractionState _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.Idle;
        // 矩形1拖拽起点的图像像素坐标。
        private Point _rectangleStartPixel;
        // 矩形1拖拽终点的图像像素坐标。
        private Point _rectangleEndPixel;
        // 矩形1测量中已确认的第一块矩形区域。
        private Rect? _firstMeasureRectangle;
        // 矩形2测量中已确认的第一块旋转矩形区域。
        private RotatedMeasureRectangle? _firstMeasureRectangle2;

        // 标记高度曲线区间是否处于鼠标拖拽选择中。
        private bool _isSelectingProfileRange;
        // 高度曲线区间选择起始采样点索引。
        private int _profileSelectionStartIndex;
        // 高度曲线中当前正在选择的区间编号。
        private int _currentSelectionTarget = 1;
        private (int Start, int End)? _profileSegment1;
        private (int Start, int End)? _profileSegment2;
        private (int Start, int End)? _profilePreviewSegment;
        private IReadOnlyList<DepthProfilePoint> _profilePoints = Array.Empty<DepthProfilePoint>();
        // 嵌入到高度差窗口中的点云视图实例。
        private readonly HeightDifferencePointCloudViewerView _pointCloudView;
        // 热力图显示使用的 HALCON 智能窗口控件。
        private HSmartWindowControlWPF? _heatmapSmartWindow;
        // 高度曲线显示使用的 HALCON 智能窗口控件。
        private HSmartWindowControlWPF? _profileSmartWindow;
        // 高度曲线绘制后的 HALCON 图像对象。
        private HObject? _profileChartImage;
        // 高度曲线图像当前宽度。
        private int _profileChartWidth;
        // 高度曲线图像当前高度。
        private int _profileChartHeight;
        // 同步热力图预览对象时的递归更新保护标记。
        private bool _suppressHeatmapPreviewSync;
        // 标记矩形2绘制过程是否由 HALCON 交互控件接管。
        private bool _isDrawingRectangle2Interactively;
        // 标记热力图交互叠加层刷新是否已排队。
        private bool _isHeatmapInteractiveRefreshQueued;
        // 热力图叠加层异步刷新请求编号，用于丢弃过期请求。
        private int _heatmapOverlayRefreshRequestId;
        // 点云渲染异步请求编号，用于合并连续刷新。
        private int _pointCloudRenderRequestId;

        #endregion

        #region 初始化与绑定

        /// <summary>
        /// 初始化高度差测量视图、内嵌点云视图和 WPF 生命周期事件。
        /// </summary>
        public HeightDifferenceMeasureView()
        {
            InitializeComponent();
            _pointCloudView = new HeightDifferencePointCloudViewerView
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };

            Loaded += HeightDifferenceMeasureView_Loaded;
            Unloaded += HeightDifferenceMeasureView_Unloaded;
            DataContextChanged += HeightDifferenceMeasureView_DataContextChanged;
        }

        /// <summary>
        /// 高度差视图加载后绑定窗口、模型和点云子视图。
        /// </summary>
        private void HeightDifferenceMeasureView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as HeightDifferenceMeasureViewModel);
            ApplyDialogHostSize();
            ApplyCenterViewMode();
            UpdateSurfaceSize();
            RedrawProfileChart();
            Dispatcher.BeginInvoke(new Action(AttachHeatmapSmartWindow), DispatcherPriority.Background);
            Dispatcher.BeginInvoke(new Action(AttachProfileSmartWindow), DispatcherPriority.Background);

            if (_model?.HeatmapPreviewImage != null && _viewModel?.IsHeatmapViewActive == true)
            {
                Dispatcher.BeginInvoke(new Action(FitImageToViewport), DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 把固定弹窗尺寸应用到 DialogHost 外层容器。
        /// </summary>
        private void ApplyDialogHostSize()
        {
            Window? hostWindow = Window.GetWindow(this);
            if (hostWindow is not IDialogWindow)
            {
                return;
            }

            hostWindow.SizeToContent = SizeToContent.Manual;
            hostWindow.Width = DialogHostWidth;
            hostWindow.Height = DialogHostHeight;
            hostWindow.MinWidth = DialogHostWidth;
            hostWindow.MinHeight = DialogHostHeight;
        }

        /// <summary>
        /// 高度差视图卸载时解绑事件并释放图像窗口资源。
        /// </summary>
        private void HeightDifferenceMeasureView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachHeatmapSmartWindow();
            DetachProfileSmartWindow();
            DisposeProfileChartImage();
            DetachViewModel(_viewModel);
            DetachPointCloudViewFromHost();
        }

        /// <summary>
        /// 数据上下文变化时重新绑定视图模型和模型事件。
        /// </summary>
        private void HeightDifferenceMeasureView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModel(e.OldValue as HeightDifferenceMeasureViewModel);
            AttachViewModel(e.NewValue as HeightDifferenceMeasureViewModel);
        }

        /// <summary>
        /// 订阅高度差视图模型事件并同步点云子视图。
        /// </summary>
        private void AttachViewModel(HeightDifferenceMeasureViewModel? viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
            {
                return;
            }

            _viewModel = viewModel;
            if (_viewModel == null)
            {
                CenterTitleText.Text = string.Empty;
                _pointCloudView.DataContext = null;
                _pointCloudView.Tag = null;
                DetachModel(_model);
                return;
            }

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.FitRequested += ViewModel_FitRequested;
            _viewModel.ManualMeasurementRequested += ViewModel_ManualMeasurementRequested;
            CenterTitleText.Text = _viewModel.CenterViewTitle;
            _pointCloudView.Tag = _viewModel;
            _pointCloudView.DataContext = _viewModel.EmbeddedPointCloudViewModel;
            AttachModel(_viewModel.ModelParam);
            ApplyCenterViewMode();
        }

        /// <summary>
        /// 取消高度差视图模型事件订阅。
        /// </summary>
        private void DetachViewModel(HeightDifferenceMeasureViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.FitRequested -= ViewModel_FitRequested;
            viewModel.ManualMeasurementRequested -= ViewModel_ManualMeasurementRequested;

            if (ReferenceEquals(_viewModel, viewModel))
            {
                _viewModel = null;
            }

            CenterTitleText.Text = string.Empty;
            _pointCloudView.DataContext = null;
            _pointCloudView.Tag = null;
            DetachModel(_model);
        }

        /// <summary>
        /// 订阅模型属性和绘制集合变化，并刷新热力图显示。
        /// </summary>
        private void AttachModel(HeightDifferenceMeasureModel? model)
        {
            if (ReferenceEquals(_model, model))
            {
                return;
            }

            DetachModel(_model);
            _model = model;
            if (_model == null)
            {
                UpdateSurfaceSize();
                return;
            }

            _model.PropertyChanged += Model_PropertyChanged;
            _model.MeasureItems.CollectionChanged += MeasureItems_CollectionChanged;
            _model.HeatmapPreviewDrawObjects.CollectionChanged += HeatmapPreviewDrawObjects_CollectionChanged;
            UpdateSurfaceSize();
            RebuildManualMeasureLineOverlays();
            RedrawProfileChart();
        }

        /// <summary>
        /// 取消模型事件订阅，避免视图释放后继续回调。
        /// </summary>
        private void DetachModel(HeightDifferenceMeasureModel? model)
        {
            if (model != null)
            {
                model.PropertyChanged -= Model_PropertyChanged;
                model.MeasureItems.CollectionChanged -= MeasureItems_CollectionChanged;
                model.HeatmapPreviewDrawObjects.CollectionChanged -= HeatmapPreviewDrawObjects_CollectionChanged;
            }

            ClearMirroredHeatmapPreviewObjects();

            if (ReferenceEquals(_model, model))
            {
                _model = null;
            }
        }

        /// <summary>
        /// 视图模型属性变化时刷新视图切换、点云或热力图显示。
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HeightDifferenceMeasureViewModel.ModelParam))
            {
                AttachModel(_viewModel?.ModelParam);
                return;
            }

            if (e.PropertyName == nameof(HeightDifferenceMeasureViewModel.CenterViewMode))
            {
                CenterTitleText.Text = _viewModel?.CenterViewTitle ?? string.Empty;
                ApplyCenterViewMode();
                if (_viewModel?.IsHeatmapViewActive == true && _model?.HeatmapPreviewImage != null)
                {
                    Dispatcher.BeginInvoke(new Action(FitImageToViewport), DispatcherPriority.Background);
                }
            }
            else if (e.PropertyName == nameof(HeightDifferenceMeasureViewModel.SelectedMeasureItem))
            {
                RefreshHeatmapPreviewOverlays();
            }
        }

        /// <summary>
        /// 模型输出和选择结果变化时刷新热力图叠加和曲线显示。
        /// </summary>
        private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(HeightDifferenceMeasureModel.Zoom):
                        UpdateSurfaceSize();
                        UpdateHeatmapSelectionOverlay();
                        break;
                    case nameof(HeightDifferenceMeasureModel.HeatmapPreviewImage):
                        bool preserveLayout = _model?.IsReplacingHeatmapPreviewWithoutLayoutReset == true;
                        UpdateSurfaceSize();
                        ResetProfileState(clearSelectionLine: true);
                        if (_model?.HeatmapPreviewImage != null
                            && _viewModel?.IsHeatmapViewActive == true
                            && !preserveLayout)
                        {
                            Dispatcher.BeginInvoke(new Action(FitImageToViewport), DispatcherPriority.Background);
                        }
                        break;
                }
            });
        }

        /// <summary>
        /// 响应视图模型适配请求，复位热力图或点云显示范围。
        /// </summary>
        private void ViewModel_FitRequested()
        {
            if (_viewModel?.IsHeatmapViewActive == true)
            {
                FitImageToViewport();
            }
        }

        #endregion

        #region 手动测量交互入口

        /// <summary>
        /// 根据 ViewModel 发出的测量模式请求，切换热力图绘制状态。
        /// </summary>
        private void ViewModel_ManualMeasurementRequested(HeightDifferenceMeasureKind mode)
        {
            Dispatcher.Invoke(() =>
            {
                // 开始新测量前清空临时交互图形，结果图形由选中项重新生成。
                ResetHeatmapInteractionState(clearTemporaryShapes: true);
                if (mode == HeightDifferenceMeasureKind.LineProfile)
                {
                    ResetProfileState(clearSelectionLine: true);
                    _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.WaitingLine;
                }
                else if (mode == HeightDifferenceMeasureKind.Rectangle1)
                {
                    ResetProfileState(clearSelectionLine: true);
                    _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.WaitingFirstRectangle;
                }
                else if (mode == HeightDifferenceMeasureKind.Rectangle2)
                {
                    ResetProfileState(clearSelectionLine: true);
                    BeginRectangle2Measurement();
                    return;
                }

                HeatmapHalconPreview.DrawModel = _heatmapInteractionState != HeightDifferenceHeatmapInteractionState.Idle;
                RefreshHeatmapPreviewOverlays();
            });
        }

        /// <summary>
        /// 测量结果列表变化时同步热力图叠加图形。
        /// </summary>
        private void MeasureItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                HeightDifferenceMeasureModel? model = _model;
                if (_viewModel != null && model != null)
                {
                    if (e.NewItems?.Count > 0)
                    {
                        _viewModel.SelectedMeasureItem = e.NewItems[e.NewItems.Count - 1] as HeightDifferenceMeasureItem;
                    }
                    else if (model.MeasureItems.Count == 0)
                    {
                        _viewModel.SelectedMeasureItem = null;
                    }
                    else if (_viewModel.SelectedMeasureItem == null || !model.MeasureItems.Contains(_viewModel.SelectedMeasureItem))
                    {
                        _viewModel.SelectedMeasureItem = model.MeasureItems.LastOrDefault();
                    }
                }

                RebuildManualMeasureLineOverlays();
            });
        }

        /// <summary>
        /// 热力图预览绘制集合变化时刷新窗口叠加层。
        /// </summary>
        private void HeatmapPreviewDrawObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressHeatmapPreviewSync)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(SyncHeatmapPreviewDrawObjects), DispatcherPriority.Background);
        }

        #endregion

        #region 视图切换与点云刷新

        /// <summary>
        /// 根据当前中心视图模式挂载热力图或点云显示控件。
        /// </summary>
        private void ApplyCenterViewMode()
        {
            if (_viewModel?.IsPointCloudViewActive == true)
            {
                AttachPointCloudViewToHost();
                SchedulePointCloudRender(includeFirstShowWarmup: true);
            }
        }

        /// <summary>
        /// 把点云视图挂载到中间显示区容器。
        /// </summary>
        private void AttachPointCloudViewToHost()
        {
            if (!ReferenceEquals(PointCloudHost.Content, _pointCloudView))
            {
                PointCloudHost.Content = _pointCloudView;
            }
        }

        /// <summary>
        /// 从容器移除点云视图并断开数据上下文。
        /// </summary>
        private void DetachPointCloudViewFromHost()
        {
            if (PointCloudHost.Content != null)
            {
                PointCloudHost.Content = null;
            }
        }

        /// <summary>
        /// 合并连续点云刷新请求并延迟触发渲染。
        /// </summary>
        private void SchedulePointCloudRender(bool includeFirstShowWarmup)
        {
            int requestId = ++_pointCloudRenderRequestId;
            RunPointCloudRenderPass(requestId);

            Dispatcher.BeginInvoke(new Action(() => RunPointCloudRenderPass(requestId)), DispatcherPriority.DataBind);
            Dispatcher.BeginInvoke(new Action(() => RunPointCloudRenderPass(requestId)), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(() => RunPointCloudRenderPass(requestId)), DispatcherPriority.Render);
            Dispatcher.BeginInvoke(new Action(() => RunPointCloudRenderPass(requestId)), DispatcherPriority.ContextIdle);

            if (!includeFirstShowWarmup)
            {
                return;
            }

            _ = Dispatcher.InvokeAsync(async () =>
            {
                foreach (int delayMs in new[] { 60, 140, 260, 420 })
                {
                    await System.Threading.Tasks.Task.Delay(delayMs);
                    RunPointCloudRenderPass(requestId);
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 执行一次点云原生窗口重绘请求。
        /// </summary>
        private void RunPointCloudRenderPass(int requestId)
        {
            if (requestId != _pointCloudRenderRequestId ||
                _viewModel?.IsPointCloudViewActive != true)
            {
                return;
            }

            PointCloudHost.UpdateLayout();
            _pointCloudView.UpdateLayout();
            ForceNativeChildWindowRedraw(sendMouseMove: true);
            _pointCloudView.ForceRender();
        }

        /// <summary>
        /// 强制 WPF 内嵌原生子窗口刷新显示。
        /// </summary>
        private void ForceNativeChildWindowRedraw(bool sendMouseMove)
        {
            Window? window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            IntPtr rootHandle = new WindowInteropHelper(window).Handle;
            if (rootHandle == IntPtr.Zero)
            {
                return;
            }

            RedrawNativeWindow(rootHandle);
            EnumChildWindows(rootHandle, (childHandle, _) =>
            {
                RedrawNativeWindow(childHandle);
                if (sendMouseMove)
                {
                    PostMessage(childHandle, WindowMessages.MouseMove, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// 调用 Win32 接口重绘指定原生窗口句柄。
        /// </summary>
        private static void RedrawNativeWindow(IntPtr handle)
        {
            SetWindowPos(
                handle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NoMove
                | SetWindowPosFlags.NoSize
                | SetWindowPosFlags.NoZOrder
                | SetWindowPosFlags.NoActivate
                | SetWindowPosFlags.ShowWindow
                | SetWindowPosFlags.FrameChanged);

            RedrawWindow(
                handle,
                IntPtr.Zero,
                IntPtr.Zero,
                RedrawWindowFlags.Invalidate
                | RedrawWindowFlags.UpdateNow
                | RedrawWindowFlags.AllChildren);
            UpdateWindow(handle);
        }

        #endregion

        #region 热力图与曲线绘制

        /// <summary>
        /// 刷新热力图覆盖层和相关显示尺寸。
        /// </summary>
        private void UpdateSurfaceSize()
        {
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 将热力图适配到 HALCON 窗口当前可视区域。
        /// </summary>
        private void FitImageToViewport()
        {
            if (_viewModel == null
                || !_viewModel.IsHeatmapViewActive
                || _model?.HeatmapPreviewObject == null)
            {
                return;
            }

            HeatmapHalconPreview.DispImageFitImage();
            ScheduleHeatmapNavigationRefresh();
        }

        /// <summary>
        /// 绑定热力图 HALCON 窗口鼠标事件。
        /// </summary>
        private void AttachHeatmapSmartWindow()
        {
            DetachHeatmapSmartWindow();
            _heatmapSmartWindow = HeatmapHalconPreview.getHWindowControl();
            if (_heatmapSmartWindow == null)
            {
                return;
            }

            _heatmapSmartWindow.HMouseDown += HeatmapSmartWindow_HMouseDown;
            _heatmapSmartWindow.HMouseMove += HeatmapSmartWindow_HMouseMove;
            _heatmapSmartWindow.HMouseUp += HeatmapSmartWindow_HMouseUp;
            _heatmapSmartWindow.HMouseWheel += HeatmapSmartWindow_HMouseWheel;
            RefreshHeatmapPreviewOverlays();
            SyncHeatmapPreviewDrawObjects();
        }

        /// <summary>
        /// 解绑热力图 HALCON 窗口鼠标事件。
        /// </summary>
        private void DetachHeatmapSmartWindow()
        {
            if (_heatmapSmartWindow == null)
            {
                return;
            }

            _heatmapSmartWindow.HMouseDown -= HeatmapSmartWindow_HMouseDown;
            _heatmapSmartWindow.HMouseMove -= HeatmapSmartWindow_HMouseMove;
            _heatmapSmartWindow.HMouseUp -= HeatmapSmartWindow_HMouseUp;
            _heatmapSmartWindow.HMouseWheel -= HeatmapSmartWindow_HMouseWheel;
            _heatmapSmartWindow = null;
            HeatmapHalconPreview.DrawModel = false;
            ClearMirroredHeatmapPreviewObjects();
        }

        /// <summary>
        /// 根据当前测量模式处理热力图鼠标按下。
        /// </summary>
        private void HeatmapSmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            if (e.Button != MouseButton.Left)
            {
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.Idle)
            {
                ScheduleHeatmapOverlayRefresh();
                return;
            }

            CommitFocusedInputValue();

            if (_viewModel?.EnsureManualHeatmapFreshForMeasurement() == false)
            {
                return;
            }

            if (_model?.HeatmapPreviewObject == null || _model.DepthRawValues == null)
            {
                return;
            }

            if (!TryGetImagePixelFromHalcon(e.Column, e.Row, false, out Point pixelPoint))
            {
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.WaitingLine)
            {
                BeginLineMeasurement(pixelPoint);
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.WaitingFirstRectangle
                || _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.WaitingSecondRectangle)
            {
                BeginRectangleMeasurement(pixelPoint);
            }
        }

        /// <summary>
        /// 鼠标拖动时更新热力图临时线或矩形预览。
        /// </summary>
        private void HeatmapSmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingLine)
            {
                if (TryGetImagePixelFromHalcon(e.Column, e.Row, true, out Point pixelPoint))
                {
                    _selectionEndPixel = pixelPoint;
                    RequestHeatmapInteractiveOverlayRefresh();
                }

                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                || _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingSecondRectangle)
            {
                if (TryGetImagePixelFromHalcon(e.Column, e.Row, true, out Point pixelPoint))
                {
                    _rectangleEndPixel = pixelPoint;
                    RequestHeatmapInteractiveOverlayRefresh();
                }
            }
        }

        /// <summary>
        /// 鼠标释放时完成画线或矩形测量步骤。
        /// </summary>
        private void HeatmapSmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingLine)
            {
                FinishLineMeasurement(e.Column, e.Row);
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                || _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingSecondRectangle)
            {
                FinishRectangleMeasurement(e.Column, e.Row);
                return;
            }

            ScheduleHeatmapNavigationRefresh();
        }

        /// <summary>
        /// 在热力图窗口禁用鼠标滚轮缩放，避免影响绘制。
        /// </summary>
        private void HeatmapSmartWindow_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            ScheduleHeatmapOverlayRefresh();
        }

        /// <summary>
        /// 清空旧交互状态并进入画线测量模式。
        /// </summary>
        private void BeginLineMeasurement(Point pixelPoint)
        {
            _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.DrawingLine;
            _hasSelectionLine = true;
            _selectionStartPixel = pixelPoint;
            _selectionEndPixel = pixelPoint;
            _firstMeasureRectangle = null;
            _model?.ClearAutomaticMeasurementResult();
            HeatmapHalconPreview.DrawModel = true;
            _heatmapSmartWindow?.CaptureMouse();
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 完成画线测量主线并生成高度曲线。
        /// </summary>
        private void FinishLineMeasurement(double column, double row)
        {
            _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.Idle;
            HeatmapHalconPreview.DrawModel = false;
            ReleaseHeatmapMouseCapture();

            if (TryGetImagePixelFromHalcon(column, row, true, out Point pixelPoint))
            {
                _selectionEndPixel = pixelPoint;
            }

            RefreshHeatmapPreviewOverlays();
            BuildProfileFromSelectionLine();
        }

        /// <summary>
        /// 清空旧交互状态并进入矩形1测量模式。
        /// </summary>
        private void BeginRectangleMeasurement(Point pixelPoint)
        {
            _rectangleStartPixel = pixelPoint;
            _rectangleEndPixel = pixelPoint;
            _heatmapInteractionState = _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.WaitingFirstRectangle
                ? HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                : HeightDifferenceHeatmapInteractionState.DrawingSecondRectangle;
            _model?.ClearAutomaticMeasurementResult();
            HeatmapHalconPreview.DrawModel = true;
            _heatmapSmartWindow?.CaptureMouse();
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 完成矩形1单次拖拽，并在两块区域齐全后计算结果。
        /// </summary>
        private void FinishRectangleMeasurement(double column, double row)
        {
            if (TryGetImagePixelFromHalcon(column, row, true, out Point pixelPoint))
            {
                _rectangleEndPixel = pixelPoint;
            }

            Rect rectangle = NormalizePixelRect(_rectangleStartPixel, _rectangleEndPixel);
            if (rectangle.Width < 1 || rectangle.Height < 1)
            {
                _model!.ResultMessage = "矩形区域过小，请重新框选。";
                _heatmapInteractionState = _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                    ? HeightDifferenceHeatmapInteractionState.WaitingFirstRectangle
                    : HeightDifferenceHeatmapInteractionState.WaitingSecondRectangle;
                ReleaseHeatmapMouseCapture();
                RefreshHeatmapPreviewOverlays();
                return;
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle)
            {
                _firstMeasureRectangle = rectangle;
                _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.WaitingSecondRectangle;
                _model!.ResultMessage = "第一个矩形已选择，请继续框选第二个矩形区域。";
                ReleaseHeatmapMouseCapture();
                RefreshHeatmapPreviewOverlays();
                return;
            }

            Rect? firstRectangle = _firstMeasureRectangle;
            ResetHeatmapInteractionState(clearTemporaryShapes: false);
            if (firstRectangle.HasValue)
            {
                AddRectangle1MeasurementResult(firstRectangle.Value, rectangle);
            }

            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 释放热力图窗口鼠标捕获状态。
        /// </summary>
        private void ReleaseHeatmapMouseCapture()
        {
            if (_heatmapSmartWindow?.IsMouseCaptured == true)
            {
                _heatmapSmartWindow.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// 重置热力图鼠标交互状态和临时图形。
        /// </summary>
        private void ResetHeatmapInteractionState(bool clearTemporaryShapes)
        {
            _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.Idle;
            _firstMeasureRectangle = null;
            _firstMeasureRectangle2 = null;
            _rectangleStartPixel = default;
            _rectangleEndPixel = default;
            ReleaseHeatmapMouseCapture();
            HeatmapHalconPreview.DrawModel = false;

            if (clearTemporaryShapes)
            {
                _hasSelectionLine = false;
            }
        }

        /// <summary>
        /// 延迟刷新热力图叠加层，合并连续界面变化。
        /// </summary>
        private void ScheduleHeatmapOverlayRefresh()
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            int requestId = ++_heatmapOverlayRefreshRequestId;
            Dispatcher.BeginInvoke(new Action(() => RunScheduledHeatmapOverlayRefresh(requestId)), DispatcherPriority.Background);
            Dispatcher.BeginInvoke(new Action(() => RunScheduledHeatmapOverlayRefresh(requestId)), DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 热力图平移后延迟恢复叠加图形显示。
        /// </summary>
        private void ScheduleHeatmapNavigationRefresh()
        {
            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            int requestId = ++_heatmapOverlayRefreshRequestId;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (requestId == _heatmapOverlayRefreshRequestId)
                {
                    RefreshHeatmapInteractiveVectorPreview();
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// 请求刷新热力图交互过程中的矢量预览。
        /// </summary>
        private void RequestHeatmapInteractiveOverlayRefresh()
        {
            if (_isHeatmapInteractiveRefreshQueued)
            {
                return;
            }

            _isHeatmapInteractiveRefreshQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isHeatmapInteractiveRefreshQueued = false;
                RefreshHeatmapInteractiveVectorPreview();
            }), DispatcherPriority.Render);
        }

        /// <summary>
        /// 在 HALCON 窗口中重画当前交互预览图形。
        /// </summary>
        private void RefreshHeatmapInteractiveVectorPreview()
        {
            if (_model?.HeatmapPreviewObject == null || HeatmapHalconPreview?.HWindow == null)
            {
                return;
            }

            try
            {
                HWindow window = HeatmapHalconPreview.HWindow;
                window.GetPart(out int row1, out int col1, out int row2, out int col2);
                window.ClearWindow();
                window.SetPart(row1, col1, row2, col2);
                window.DispObj(_model.HeatmapPreviewObject);
                DrawHeatmapVectorOverlays();
            }
            catch
            {
                RefreshHeatmapPreviewOverlays();
            }
        }

        /// <summary>
        /// 执行排队的热力图叠加层刷新请求。
        /// </summary>
        private void RunScheduledHeatmapOverlayRefresh(int requestId)
        {
            if (requestId != _heatmapOverlayRefreshRequestId)
            {
                return;
            }

            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 提交当前焦点输入框的绑定值，避免测量使用旧参数。
        /// </summary>
        private void CommitFocusedInputValue()
        {
            if (Keyboard.FocusedElement is not DependencyObject focusedElement)
            {
                return;
            }

            BindingExpression? textBinding = BindingOperations.GetBindingExpression(focusedElement, System.Windows.Controls.TextBox.TextProperty);
            textBinding?.UpdateSource();

            HeatmapHalconPreview.Focusable = true;
            Keyboard.Focus(HeatmapHalconPreview);
            Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);

            DependencyProperty? valueProperty = focusedElement.GetType().GetField(
                "ValueProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)?.GetValue(null) as DependencyProperty;
            if (valueProperty != null)
            {
                BindingOperations.GetBindingExpression(focusedElement, valueProperty)?.UpdateSource();
            }

            UpdateValueBindings(this);
        }

        /// <summary>
        /// 递归更新界面内可编辑控件的绑定源。
        /// </summary>
        private static void UpdateValueBindings(DependencyObject root)
        {
            DependencyProperty? valueProperty = root.GetType().GetField(
                "ValueProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)?.GetValue(null) as DependencyProperty;
            if (valueProperty != null)
            {
                BindingOperations.GetBindingExpression(root, valueProperty)?.UpdateSource();
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                UpdateValueBindings(VisualTreeHelper.GetChild(root, i));
            }
        }

        /// <summary>
        /// 把 HALCON 窗口坐标转换为热力图图像像素坐标。
        /// </summary>
        private bool TryGetImagePixelFromHalcon(double column, double row, bool clampToImage, out Point pixelPoint)
        {
            pixelPoint = default;
            if (!TryGetPreviewImageSize(out int width, out int height))
            {
                return false;
            }

            if (!clampToImage && (column < 0 || row < 0 || column > width - 1 || row > height - 1))
            {
                return false;
            }

            double x = clampToImage ? Math.Clamp(column, 0, width - 1) : column;
            double y = clampToImage ? Math.Clamp(row, 0, height - 1) : row;
            int pixelX = Math.Clamp((int)Math.Round(x), 0, width - 1);
            int pixelY = Math.Clamp((int)Math.Round(y), 0, height - 1);
            pixelPoint = new Point(pixelX, pixelY);
            return true;
        }

        /// <summary>
        /// 读取当前热力图预览图像宽高。
        /// </summary>
        private bool TryGetPreviewImageSize(out int width, out int height)
        {
            width = _model?.HeatmapPreviewImage?.PixelWidth ?? _model?.DepthImageWidth ?? 0;
            height = _model?.HeatmapPreviewImage?.PixelHeight ?? _model?.DepthImageHeight ?? 0;
            return width > 0 && height > 0;
        }

        /// <summary>
        /// 把两个拖拽点整理为左上到右下的矩形。
        /// </summary>
        private static Rect NormalizePixelRect(Point start, Point end)
        {
            double left = Math.Min(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double right = Math.Max(start.X, end.X);
            double bottom = Math.Max(start.Y, end.Y);
            return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        /// <summary>
        /// 计算并保存两块水平矩形区域的平均高度差。
        /// </summary>
        private void AddRectangle1MeasurementResult(Rect rect1, Rect rect2)
        {
            if (_model?.DepthRawValues == null || _model.DepthImageWidth <= 0 || _model.DepthImageHeight <= 0)
            {
                _model!.ResultMessage = "当前没有可用的原始高度数据，请先生成热力图。";
                return;
            }

            DepthImageData imageData = new(
                _model.DepthPixelType,
                _model.DepthImageWidth,
                _model.DepthImageHeight,
                _model.DepthRawValues);

            DepthProfileRegionStats rect1Stats = DepthProfileAnalysisHelper.EvaluateRectangle(
                imageData,
                (int)Math.Round(rect1.Left),
                (int)Math.Round(rect1.Top),
                (int)Math.Round(rect1.Right),
                (int)Math.Round(rect1.Bottom),
                _model.ZValueToMillimeterFactor,
                _model.InvalidGrayCenter,
                _model.InvalidGrayTolerance,
                _model.TrimRatio);

            DepthProfileRegionStats rect2Stats = DepthProfileAnalysisHelper.EvaluateRectangle(
                imageData,
                (int)Math.Round(rect2.Left),
                (int)Math.Round(rect2.Top),
                (int)Math.Round(rect2.Right),
                (int)Math.Round(rect2.Bottom),
                _model.ZValueToMillimeterFactor,
                _model.InvalidGrayCenter,
                _model.InvalidGrayTolerance,
                _model.TrimRatio);

            if (rect1Stats.ValidSamples <= 0
                || rect2Stats.ValidSamples <= 0
                || double.IsNaN(rect1Stats.MeanHeight)
                || double.IsNaN(rect2Stats.MeanHeight))
            {
                _model.ResultMessage = "矩形区域内有效高度点不足，请重新框选。";
                return;
            }

            double heightDiff = Math.Abs(rect2Stats.MeanHeight - rect1Stats.MeanHeight);
            int totalSamples = rect1Stats.TotalSamples + rect2Stats.TotalSamples;
            int validSamples = rect1Stats.ValidSamples + rect2Stats.ValidSamples;
            _model.AddRectangleMeasureItem(
                HeightDifferenceMeasureKind.Rectangle1,
                rect1,
                rect2,
                rect1Stats.MeanHeight,
                rect2Stats.MeanHeight,
                heightDiff,
                totalSamples,
                validSamples);
            _model.UpdateProfileSummary(
                double.NaN,
                totalSamples,
                validSamples,
                rect1Stats.MeanHeight,
                rect2Stats.MeanHeight,
                heightDiff,
                $"矩形1测量：区域1均值 = {FormatMeasurement(rect1Stats.MeanHeight)}，区域2均值 = {FormatMeasurement(rect2Stats.MeanHeight)}，高度差 = {FormatMeasurement(heightDiff)}，有效点 = {validSamples}/{totalSamples}。");
            _model.ResultMessage = $"矩形1测量完成，高度差 = {FormatMeasurement(heightDiff)}。";
            RedrawProfileChart();
        }

        /// <summary>
        /// 启动 HALCON 旋转矩形交互并等待两块区域。
        /// </summary>
        private void BeginRectangle2Measurement()
        {
            if (_model?.DepthRawValues == null || HeatmapHalconPreview?.HWindow == null)
            {
                if (_model != null)
                {
                    _model.ResultMessage = "当前没有可用的原始高度数据，请先生成热力图。";
                }

                return;
            }

            _firstMeasureRectangle = null;
            _firstMeasureRectangle2 = null;
            _heatmapInteractionState = HeightDifferenceHeatmapInteractionState.Idle;
            _isDrawingRectangle2Interactively = true;
            _model.ClearAutomaticMeasurementResult();
            HeatmapHalconPreview.DrawModel = true;
            HWindow window = HeatmapHalconPreview.HWindow;

            _ = Task.Run(() =>
            {
                try
                {
                    window.SetColor(Segment1OverlayColor);
                    window.SetLineWidth(3);
                    HOperatorSet.DrawRectangle2(
                        window,
                        out HTuple row1,
                        out HTuple col1,
                        out HTuple phi1,
                        out HTuple length11,
                        out HTuple length12);

                    RotatedMeasureRectangle rect1 = new(col1.D, row1.D, phi1.D, length11.D, length12.D);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _firstMeasureRectangle2 = rect1;
                        if (_model != null)
                        {
                            _model.ResultMessage = "第一个旋转矩形已选择，请继续绘制第二个旋转矩形区域。";
                        }

                    }), DispatcherPriority.Background);

                    window.SetColor(Segment1OverlayColor);
                    window.SetLineWidth(3);
                    window.DispRectangle2(row1.D, col1.D, phi1.D, length11.D, length12.D);
                    window.SetColor(Segment2OverlayColor);
                    window.SetLineWidth(3);
                    HOperatorSet.DrawRectangle2(
                        window,
                        out HTuple row2,
                        out HTuple col2,
                        out HTuple phi2,
                        out HTuple length21,
                        out HTuple length22);

                    RotatedMeasureRectangle rect2 = new(col2.D, row2.D, phi2.D, length21.D, length22.D);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _firstMeasureRectangle2 = null;
                        _isDrawingRectangle2Interactively = false;
                        HeatmapHalconPreview.DrawModel = false;
                        AddRectangle2MeasurementResult(rect1, rect2);
                        RefreshHeatmapPreviewOverlays();
                    }), DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _firstMeasureRectangle2 = null;
                        _isDrawingRectangle2Interactively = false;
                        HeatmapHalconPreview.DrawModel = false;
                        if (_model != null)
                        {
                            _model.ResultMessage = $"矩形2测量已取消或失败：{ex.Message}";
                        }

                        RefreshHeatmapPreviewOverlays();
                    }), DispatcherPriority.Background);
                }
            });
        }

        /// <summary>
        /// 计算并保存两块旋转矩形区域的平均高度差。
        /// </summary>
        private void AddRectangle2MeasurementResult(RotatedMeasureRectangle rect1, RotatedMeasureRectangle rect2)
        {
            if (_model?.DepthRawValues == null || _model.DepthImageWidth <= 0 || _model.DepthImageHeight <= 0)
            {
                _model!.ResultMessage = "当前没有可用的原始高度数据，请先生成热力图。";
                return;
            }

            DepthImageData imageData = new(
                _model.DepthPixelType,
                _model.DepthImageWidth,
                _model.DepthImageHeight,
                _model.DepthRawValues);

            DepthProfileRegionStats rect1Stats = DepthProfileAnalysisHelper.EvaluateRectangle2(
                imageData,
                rect1.CenterX,
                rect1.CenterY,
                rect1.Phi,
                rect1.Length1,
                rect1.Length2,
                _model.ZValueToMillimeterFactor,
                _model.InvalidGrayCenter,
                _model.InvalidGrayTolerance,
                _model.TrimRatio);

            DepthProfileRegionStats rect2Stats = DepthProfileAnalysisHelper.EvaluateRectangle2(
                imageData,
                rect2.CenterX,
                rect2.CenterY,
                rect2.Phi,
                rect2.Length1,
                rect2.Length2,
                _model.ZValueToMillimeterFactor,
                _model.InvalidGrayCenter,
                _model.InvalidGrayTolerance,
                _model.TrimRatio);

            if (rect1Stats.ValidSamples <= 0
                || rect2Stats.ValidSamples <= 0
                || double.IsNaN(rect1Stats.MeanHeight)
                || double.IsNaN(rect2Stats.MeanHeight))
            {
                _model.ResultMessage = "矩形2区域内有效高度点不足，请重新绘制。";
                return;
            }

            double heightDiff = Math.Abs(rect2Stats.MeanHeight - rect1Stats.MeanHeight);
            int totalSamples = rect1Stats.TotalSamples + rect2Stats.TotalSamples;
            int validSamples = rect1Stats.ValidSamples + rect2Stats.ValidSamples;
            _model.AddRectangle2MeasureItem(
                rect1.CenterX,
                rect1.CenterY,
                rect1.Phi,
                rect1.Length1,
                rect1.Length2,
                rect2.CenterX,
                rect2.CenterY,
                rect2.Phi,
                rect2.Length1,
                rect2.Length2,
                rect1Stats.MeanHeight,
                rect2Stats.MeanHeight,
                heightDiff,
                totalSamples,
                validSamples);
            _model.UpdateProfileSummary(
                double.NaN,
                totalSamples,
                validSamples,
                rect1Stats.MeanHeight,
                rect2Stats.MeanHeight,
                heightDiff,
                $"矩形2测量：区域1均值 = {FormatMeasurement(rect1Stats.MeanHeight)}，区域2均值 = {FormatMeasurement(rect2Stats.MeanHeight)}，高度差 = {FormatMeasurement(heightDiff)}，有效点 = {validSamples}/{totalSamples}。");
            _model.ResultMessage = $"矩形2测量完成，高度差 = {FormatMeasurement(heightDiff)}。";
            RedrawProfileChart();
        }

        /// <summary>
        /// 根据当前选中结果重建热力图手动测量叠加图形。
        /// </summary>
        private void RebuildManualMeasureLineOverlays()
        {
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 清除热力图窗口中的手动测量叠加图形。
        /// </summary>
        private void ClearManualMeasureLineOverlays()
        {
            _model?.ClearHeatmapPreviewDrawObjects();
        }

        /// <summary>
        /// 选中测量结果变化时刷新热力图回显图形。
        /// </summary>
        private void UpdateHeatmapSelectionOverlay()
        {
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 隐藏当前选中结果的热力图回显图形。
        /// </summary>
        private void HideHeatmapSelectionOverlay()
        {
            RefreshHeatmapPreviewOverlays();
        }

        /// <summary>
        /// 按当前选中结果重建热力图覆盖层，只显示对应线、区间或矩形。
        /// </summary>
        private void RefreshHeatmapPreviewOverlays()
        {
            if (_model == null)
            {
                return;
            }

            if (_isDrawingRectangle2Interactively)
            {
                return;
            }

            _suppressHeatmapPreviewSync = true;
            try
            {
                _model.ClearHeatmapPreviewDrawObjects();
                if (_model.HeatmapPreviewObject == null)
                {
                    return;
                }

                double overlayThickness = GetAdaptiveHeatmapOverlayThickness();
                double segmentOverlayThickness = GetAdaptiveHeatmapOverlayThickness(2.0);
                foreach (HeightDifferenceMeasureItem item in GetVisibleMeasureItems())
                {
                    if (item.HasSelectionLine)
                    {
                        AddLineOverlay(
                            new Point(item.StartPixelX, item.StartPixelY),
                            new Point(item.EndPixelX, item.EndPixelY),
                            ProfileLineOverlayColor,
                            overlayThickness);
                        AddPersistedProfileSegmentOverlay(item, firstSegment: true, segmentOverlayThickness);
                        AddPersistedProfileSegmentOverlay(item, firstSegment: false, segmentOverlayThickness);
                    }

                    if (item.HasRectanglePair)
                    {
                        AddRectanglePairOverlay(item, overlayThickness);
                    }
                }

                AddTemporaryRectangleOverlays(overlayThickness);

                if (!_hasSelectionLine)
                {
                    return;
                }

                AddLineOverlay(_selectionStartPixel, _selectionEndPixel, ProfileLineOverlayColor, overlayThickness);
                AddCrossOverlay(_selectionStartPixel, Segment1OverlayColor);
                AddCrossOverlay(_selectionEndPixel, Segment2OverlayColor);
                AddProfileSegmentOverlay(_profileSegment1, Segment1OverlayColor, segmentOverlayThickness);
                AddProfileSegmentOverlay(_profileSegment2, Segment2OverlayColor, segmentOverlayThickness);
            }
            finally
            {
                _suppressHeatmapPreviewSync = false;
                SyncHeatmapPreviewDrawObjects();
            }
        }

        /// <summary>
        /// 筛选当前需要在热力图上回显的测量结果。
        /// </summary>
        private IEnumerable<HeightDifferenceMeasureItem> GetVisibleMeasureItems()
        {
            HeightDifferenceMeasureItem? selectedItem = _viewModel?.SelectedMeasureItem;
            if (_model == null || selectedItem == null || !_model.MeasureItems.Contains(selectedItem))
            {
                yield break;
            }

            yield return selectedItem;
        }

        /// <summary>
        /// 把当前回显图形同步到模型预览绘制集合。
        /// </summary>
        private void SyncHeatmapPreviewDrawObjects()
        {
            if (HeatmapHalconPreview?.DrawObjectList == null)
            {
                return;
            }

            ClearMirroredHeatmapPreviewObjects();
            if (_model == null)
            {
                return;
            }

            foreach (HalconDrawingObject drawObject in _model.HeatmapPreviewDrawObjects.ToList())
            {
                if (drawObject == null)
                {
                    continue;
                }

                try
                {
                    HeatmapHalconPreview.DrawObjectList.Add(new HalconDrawingObject
                    {
                        ShapeType = drawObject.ShapeType,
                        Hobject = drawObject.Hobject?.IsInitialized() == true
                            ? drawObject.Hobject.Clone()
                            : null,
                        HTuples = drawObject.HTuples,
                        Color = drawObject.Color,
                        IsFillDisplay = drawObject.IsFillDisplay
                    });
                }
                catch
                {
                }
            }

            DrawHeatmapVectorOverlays();
        }

        /// <summary>
        /// 删除由视图镜像生成的热力图预览绘制对象。
        /// </summary>
        private void ClearMirroredHeatmapPreviewObjects()
        {
            if (HeatmapHalconPreview?.DrawObjectList == null)
            {
                return;
            }

            foreach (HalconDrawingObject item in HeatmapHalconPreview.DrawObjectList.ToList())
            {
                try
                {
                    item.Hobject?.Dispose();
                }
                catch
                {
                }
            }

            HeatmapHalconPreview.DrawObjectList.Clear();
        }

        /// <summary>
        /// 在热力图上绘制曲线区间对应的线段叠加。
        /// </summary>
        private void AddProfileSegmentOverlay((int Start, int End)? range, string color, double thickness)
        {
            if (!range.HasValue || _profilePoints.Count == 0)
            {
                return;
            }

            int startIndex = Math.Clamp(range.Value.Start, 0, _profilePoints.Count - 1);
            int endIndex = Math.Clamp(range.Value.End, 0, _profilePoints.Count - 1);
            DepthProfilePoint startPoint = _profilePoints[startIndex];
            DepthProfilePoint endPoint = _profilePoints[endIndex];
            AddLineOverlay(
                new Point(startPoint.PixelX, startPoint.PixelY),
                new Point(endPoint.PixelX, endPoint.PixelY),
                color,
                thickness);
        }

        /// <summary>
        /// 把已保存测量项的曲线区间回显到热力图。
        /// </summary>
        private void AddPersistedProfileSegmentOverlay(HeightDifferenceMeasureItem item, bool firstSegment, double thickness)
        {
            bool hasSegment = firstSegment ? item.HasProfileSegment1 : item.HasProfileSegment2;
            if (!hasSegment)
            {
                return;
            }

            Point start = firstSegment
                ? new Point(item.Segment1StartPixelX, item.Segment1StartPixelY)
                : new Point(item.Segment2StartPixelX, item.Segment2StartPixelY);
            Point end = firstSegment
                ? new Point(item.Segment1EndPixelX, item.Segment1EndPixelY)
                : new Point(item.Segment2EndPixelX, item.Segment2EndPixelY);
            AddLineOverlay(start, end, firstSegment ? Segment1OverlayColor : Segment2OverlayColor, thickness);
        }

        /// <summary>
        /// 把矩形测量结果的两块区域回显到热力图。
        /// </summary>
        private void AddRectanglePairOverlay(HeightDifferenceMeasureItem item, double thickness)
        {
            if (item.IsRectangle2Pair)
            {
                AddRotatedRectangleOverlay(CreateRotatedRectFromItem(item, firstRectangle: true), Segment1OverlayColor, thickness);
                AddRotatedRectangleOverlay(CreateRotatedRectFromItem(item, firstRectangle: false), Segment2OverlayColor, thickness);
                return;
            }

            AddRectangleOverlay(CreateRectFromItem(item, firstRectangle: true), Segment1OverlayColor, thickness);
            AddRectangleOverlay(CreateRectFromItem(item, firstRectangle: false), Segment2OverlayColor, thickness);
        }

        /// <summary>
        /// 从测量结果中还原水平矩形区域。
        /// </summary>
        private static Rect CreateRectFromItem(HeightDifferenceMeasureItem item, bool firstRectangle)
        {
            return firstRectangle
                ? NormalizePixelRect(
                    new Point(item.Rect1StartPixelX, item.Rect1StartPixelY),
                    new Point(item.Rect1EndPixelX, item.Rect1EndPixelY))
                : NormalizePixelRect(
                    new Point(item.Rect2StartPixelX, item.Rect2StartPixelY),
                    new Point(item.Rect2EndPixelX, item.Rect2EndPixelY));
        }

        /// <summary>
        /// 从测量结果中还原旋转矩形区域。
        /// </summary>
        private static RotatedMeasureRectangle CreateRotatedRectFromItem(HeightDifferenceMeasureItem item, bool firstRectangle)
        {
            return firstRectangle
                ? new RotatedMeasureRectangle(
                    item.Rect1CenterPixelX,
                    item.Rect1CenterPixelY,
                    item.Rect1Phi,
                    item.Rect1Length1,
                    item.Rect1Length2)
                : new RotatedMeasureRectangle(
                    item.Rect2CenterPixelX,
                    item.Rect2CenterPixelY,
                    item.Rect2Phi,
                    item.Rect2Length1,
                    item.Rect2Length2);
        }

        /// <summary>
        /// 绘制当前拖拽中的临时矩形预览。
        /// </summary>
        private void AddTemporaryRectangleOverlays(double thickness)
        {
            if (_firstMeasureRectangle.HasValue)
            {
                AddRectangleOverlay(_firstMeasureRectangle.Value, Segment1OverlayColor, thickness);
            }

            if (_firstMeasureRectangle2.HasValue)
            {
                AddRotatedRectangleOverlay(_firstMeasureRectangle2.Value, Segment1OverlayColor, thickness);
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                || _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingSecondRectangle)
            {
                string color = _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                    ? Segment1OverlayColor
                    : Segment2OverlayColor;
                AddRectangleOverlay(NormalizePixelRect(_rectangleStartPixel, _rectangleEndPixel), color, thickness);
            }
        }

        /// <summary>
        /// 在热力图叠加层添加水平矩形轮廓。
        /// </summary>
        private void AddRectangleOverlay(Rect rect, string color, double thickness = 1)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            Point topLeft = new(rect.Left, rect.Top);
            Point topRight = new(rect.Right, rect.Top);
            Point bottomRight = new(rect.Right, rect.Bottom);
            Point bottomLeft = new(rect.Left, rect.Bottom);
            AddLineOverlay(topLeft, topRight, color, thickness);
            AddLineOverlay(topRight, bottomRight, color, thickness);
            AddLineOverlay(bottomRight, bottomLeft, color, thickness);
            AddLineOverlay(bottomLeft, topLeft, color, thickness);
        }

        /// <summary>
        /// 在热力图叠加层添加旋转矩形轮廓。
        /// </summary>
        private void AddRotatedRectangleOverlay(RotatedMeasureRectangle rect, string color, double thickness = 1)
        {
            if (rect.Length1 <= 0 || rect.Length2 <= 0)
            {
                return;
            }

            Point[] corners = GetRotatedRectangleCorners(rect);
            AddLineOverlay(corners[0], corners[1], color, thickness);
            AddLineOverlay(corners[1], corners[2], color, thickness);
            AddLineOverlay(corners[2], corners[3], color, thickness);
            AddLineOverlay(corners[3], corners[0], color, thickness);
        }

        /// <summary>
        /// 根据中心、角度和半轴长度计算旋转矩形四个角点。
        /// </summary>
        private static Point[] GetRotatedRectangleCorners(RotatedMeasureRectangle rect)
        {
            double cos = Math.Cos(rect.Phi);
            double sin = Math.Sin(rect.Phi);
            double axis1X = cos * rect.Length1;
            double axis1Y = -sin * rect.Length1;
            double axis2X = sin * rect.Length2;
            double axis2Y = cos * rect.Length2;
            Point center = new(rect.CenterX, rect.CenterY);

            return
            [
                new Point(center.X - axis1X - axis2X, center.Y - axis1Y - axis2Y),
                new Point(center.X + axis1X - axis2X, center.Y + axis1Y - axis2Y),
                new Point(center.X + axis1X + axis2X, center.Y + axis1Y + axis2Y),
                new Point(center.X - axis1X + axis2X, center.Y - axis1Y + axis2Y)
            ];
        }

        /// <summary>
        /// 在热力图叠加层添加一条测量线。
        /// </summary>
        private void AddLineOverlay(Point start, Point end, string color, double thickness = 1)
        {
            if (_model == null)
            {
                return;
            }

            HObject? contour = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(
                    out contour,
                    new HTuple(new[] { start.Y, end.Y }),
                    new HTuple(new[] { start.X, end.X }));
                _model.AddHeatmapPreviewDrawObject(contour!, color);
                contour = null;
            }
            finally
            {
                contour?.Dispose();
            }
        }

        /// <summary>
        /// 按当前窗口缩放比例计算热力图叠加线宽。
        /// </summary>
        private double GetAdaptiveHeatmapOverlayThickness(double extraScreenPixels = 0)
        {
            double screenPixels = HeatmapOverlayTargetScreenPixels + extraScreenPixels;
            try
            {
                HWindow window = HeatmapHalconPreview.HWindow;
                window.GetPart(out int row1, out int col1, out int row2, out int col2);

                double width = Math.Max(1.0, _heatmapSmartWindow?.ActualWidth ?? HeatmapHalconPreview.ActualWidth);
                double height = Math.Max(1.0, _heatmapSmartWindow?.ActualHeight ?? HeatmapHalconPreview.ActualHeight);
                double unitX = (Math.Abs(col2 - col1) + 1.0) / width;
                double unitY = (Math.Abs(row2 - row1) + 1.0) / height;
                double imageThickness = screenPixels * Math.Max(unitX, unitY);

                return Math.Clamp(imageThickness, HeatmapOverlayMinImageThickness, HeatmapOverlayMaxImageThickness);
            }
            catch
            {
                return Math.Clamp(screenPixels, HeatmapOverlayMinImageThickness, HeatmapOverlayMaxImageThickness);
            }
        }

        /// <summary>
        /// 使用 HALCON 矢量绘制当前需要显示的热力图叠加图形。
        /// </summary>
        private void DrawHeatmapVectorOverlays()
        {
            if (_model?.HeatmapPreviewObject == null || HeatmapHalconPreview?.HWindow == null)
            {
                return;
            }

            try
            {
                HWindow window = HeatmapHalconPreview.HWindow;
                foreach (HeightDifferenceMeasureItem item in GetVisibleMeasureItems())
                {
                    if (item.HasSelectionLine)
                    {
                        DrawHeatmapVectorLine(
                            window,
                            new Point(item.StartPixelX, item.StartPixelY),
                            new Point(item.EndPixelX, item.EndPixelY),
                            ProfileLineOverlayColor,
                            HeatmapOverlayTargetScreenPixels);
                        DrawHeatmapVectorSegment(window, item, firstSegment: true);
                        DrawHeatmapVectorSegment(window, item, firstSegment: false);
                    }

                    if (item.HasRectanglePair)
                    {
                        DrawHeatmapVectorRectanglePair(window, item);
                    }
                }

                if (_hasSelectionLine)
                {
                    DrawHeatmapVectorLine(window, _selectionStartPixel, _selectionEndPixel, ProfileLineOverlayColor, HeatmapOverlayTargetScreenPixels);
                    DrawHeatmapVectorProfileSegment(window, _profileSegment1, Segment1OverlayColor);
                    DrawHeatmapVectorProfileSegment(window, _profileSegment2, Segment2OverlayColor);
                }

                DrawHeatmapTemporaryVectorRectangle(window);
            }
            catch
            {
            }
            finally
            {
                try
                {
                    HeatmapHalconPreview.HWindow.SetLineWidth(1);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 在 HALCON 窗口绘制选中结果的两块矩形区域。
        /// </summary>
        private void DrawHeatmapVectorRectanglePair(HWindow window, HeightDifferenceMeasureItem item)
        {
            if (item.IsRectangle2Pair)
            {
                DrawHeatmapVectorRotatedRectangle(window, CreateRotatedRectFromItem(item, firstRectangle: true), Segment1OverlayColor);
                DrawHeatmapVectorRotatedRectangle(window, CreateRotatedRectFromItem(item, firstRectangle: false), Segment2OverlayColor);
                return;
            }

            DrawHeatmapVectorRectangle(window, CreateRectFromItem(item, firstRectangle: true), Segment1OverlayColor);
            DrawHeatmapVectorRectangle(window, CreateRectFromItem(item, firstRectangle: false), Segment2OverlayColor);
        }

        /// <summary>
        /// 在 HALCON 窗口绘制鼠标拖拽中的临时矩形。
        /// </summary>
        private void DrawHeatmapTemporaryVectorRectangle(HWindow window)
        {
            if (_firstMeasureRectangle.HasValue)
            {
                DrawHeatmapVectorRectangle(window, _firstMeasureRectangle.Value, Segment1OverlayColor);
            }

            if (_firstMeasureRectangle2.HasValue)
            {
                DrawHeatmapVectorRotatedRectangle(window, _firstMeasureRectangle2.Value, Segment1OverlayColor);
            }

            if (_heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                || _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingSecondRectangle)
            {
                string color = _heatmapInteractionState == HeightDifferenceHeatmapInteractionState.DrawingFirstRectangle
                    ? Segment1OverlayColor
                    : Segment2OverlayColor;
                DrawHeatmapVectorRectangle(window, NormalizePixelRect(_rectangleStartPixel, _rectangleEndPixel), color);
            }
        }

        /// <summary>
        /// 在 HALCON 窗口绘制测量结果中的一段曲线区间。
        /// </summary>
        private void DrawHeatmapVectorSegment(HWindow window, HeightDifferenceMeasureItem item, bool firstSegment)
        {
            bool hasSegment = firstSegment ? item.HasProfileSegment1 : item.HasProfileSegment2;
            if (!hasSegment)
            {
                return;
            }

            Point start = firstSegment
                ? new Point(item.Segment1StartPixelX, item.Segment1StartPixelY)
                : new Point(item.Segment2StartPixelX, item.Segment2StartPixelY);
            Point end = firstSegment
                ? new Point(item.Segment1EndPixelX, item.Segment1EndPixelY)
                : new Point(item.Segment2EndPixelX, item.Segment2EndPixelY);
            DrawHeatmapVectorLine(
                window,
                start,
                end,
                firstSegment ? Segment1OverlayColor : Segment2OverlayColor,
                HeatmapOverlayTargetScreenPixels + 2.0);
        }

        /// <summary>
        /// 按曲线索引范围在热力图上绘制对应线段。
        /// </summary>
        private void DrawHeatmapVectorProfileSegment(HWindow window, (int Start, int End)? range, string color)
        {
            if (!range.HasValue || _profilePoints.Count == 0)
            {
                return;
            }

            int startIndex = Math.Clamp(range.Value.Start, 0, _profilePoints.Count - 1);
            int endIndex = Math.Clamp(range.Value.End, 0, _profilePoints.Count - 1);
            DepthProfilePoint startPoint = _profilePoints[startIndex];
            DepthProfilePoint endPoint = _profilePoints[endIndex];
            DrawHeatmapVectorLine(
                window,
                new Point(startPoint.PixelX, startPoint.PixelY),
                new Point(endPoint.PixelX, endPoint.PixelY),
                color,
                HeatmapOverlayTargetScreenPixels + 2.0);
        }

        /// <summary>
        /// 用 HALCON 线条绘制水平矩形轮廓。
        /// </summary>
        private static void DrawHeatmapVectorRectangle(HWindow window, Rect rect, string color)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            Point topLeft = new(rect.Left, rect.Top);
            Point topRight = new(rect.Right, rect.Top);
            Point bottomRight = new(rect.Right, rect.Bottom);
            Point bottomLeft = new(rect.Left, rect.Bottom);
            DrawHeatmapVectorLine(window, topLeft, topRight, color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, topRight, bottomRight, color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, bottomRight, bottomLeft, color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, bottomLeft, topLeft, color, HeatmapOverlayTargetScreenPixels);
        }

        /// <summary>
        /// 用 HALCON 线条绘制旋转矩形轮廓。
        /// </summary>
        private static void DrawHeatmapVectorRotatedRectangle(HWindow window, RotatedMeasureRectangle rect, string color)
        {
            if (rect.Length1 <= 0 || rect.Length2 <= 0)
            {
                return;
            }

            Point[] corners = GetRotatedRectangleCorners(rect);
            DrawHeatmapVectorLine(window, corners[0], corners[1], color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, corners[1], corners[2], color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, corners[2], corners[3], color, HeatmapOverlayTargetScreenPixels);
            DrawHeatmapVectorLine(window, corners[3], corners[0], color, HeatmapOverlayTargetScreenPixels);
        }

        /// <summary>
        /// 用 HALCON 直接绘制图像坐标线段。
        /// </summary>
        private static void DrawHeatmapVectorLine(HWindow window, Point start, Point end, string color, double screenPixels)
        {
            window.SetColor(string.IsNullOrWhiteSpace(color) ? "yellow" : color);
            window.SetLineWidth(Math.Max(1, (int)Math.Round(screenPixels)));
            window.DispLine(start.Y, start.X, end.Y, end.X);
        }

        /// <summary>
        /// 在指定像素位置添加十字标记辅助定位。
        /// </summary>
        private void AddCrossOverlay(Point point, string color)
        {
            if (_model == null)
            {
                return;
            }

            HObject? cross = null;
            try
            {
                HOperatorSet.GenCrossContourXld(out cross, point.Y, point.X, 16, 0);
                _model.AddHeatmapPreviewDrawObject(cross, color);
                cross = null;
            }
            finally
            {
                cross?.Dispose();
            }
        }

        /// <summary>
        /// 根据画线测量主线生成高度曲线采样数据。
        /// </summary>
        private void BuildProfileFromSelectionLine()
        {
            if (_model?.DepthRawValues == null || _model.DepthImageWidth <= 0 || _model.DepthImageHeight <= 0)
            {
                ResetProfileState(clearSelectionLine: false);
                _model?.UpdateProfileSummary(double.NaN, 0, 0, null, null, null, "当前没有可用的原始高度数据，请先生成热力图。");
                return;
            }

            DepthImageData imageData = new(
                _model.DepthPixelType,
                _model.DepthImageWidth,
                _model.DepthImageHeight,
                _model.DepthRawValues);

            _profilePoints = DepthProfileAnalysisHelper.ExtractLineProfile(
                imageData,
                (int)Math.Round(_selectionStartPixel.X),
                (int)Math.Round(_selectionStartPixel.Y),
                (int)Math.Round(_selectionEndPixel.X),
                (int)Math.Round(_selectionEndPixel.Y),
                _model.IntervalX,
                _model.IntervalY,
                _model.ZValueToMillimeterFactor,
                _model.InvalidGrayCenter,
                _model.InvalidGrayTolerance);

            _profileSegment1 = null;
            _profileSegment2 = null;
            _currentSelectionTarget = 1;
            RefreshProfileSummary();
            RedrawProfileChart();
        }

        /// <summary>
        /// 清空高度曲线、区间选择和曲线提示信息。
        /// </summary>
        private void ResetProfileState(bool clearSelectionLine)
        {
            _profilePoints = Array.Empty<DepthProfilePoint>();
            _profileSegment1 = null;
            _profileSegment2 = null;
            _profilePreviewSegment = null;
            _currentSelectionTarget = 1;
            _isSelectingProfileRange = false;
            _profileSelectionStartIndex = -1;
            ClearProfileDrawObjects();
            _model?.ClearProfileAnalysis();

            if (clearSelectionLine)
            {
                _hasSelectionLine = false;
                HideHeatmapSelectionOverlay();
            }
        }

        /// <summary>
        /// 响应按钮点击，清空高度曲线上的两段区间选择。
        /// </summary>
        private void ResetProfileSelectionsButton_Click(object sender, RoutedEventArgs e)
        {
            _profileSegment1 = null;
            _profileSegment2 = null;
            _profilePreviewSegment = null;
            _currentSelectionTarget = 1;
            RefreshProfileSummary();
            RedrawProfileChart();
        }

        /// <summary>
        /// 响应按钮点击，清空当前高度曲线和画线结果。
        /// </summary>
        private void ClearProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetProfileState(clearSelectionLine: true);
            RedrawProfileChart();
        }

        /// <summary>
        /// 曲线区域尺寸变化时重新绘制高度曲线。
        /// </summary>
        private void ProfileChartHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawProfileChart();
        }

        /// <summary>
        /// 绑定高度曲线 HALCON 窗口鼠标事件。
        /// </summary>
        private void AttachProfileSmartWindow()
        {
            DetachProfileSmartWindow();
            _profileSmartWindow = ProfileHalconPreview.getHWindowControl();
            if (_profileSmartWindow == null)
            {
                return;
            }

            _profileSmartWindow.HMouseDown += ProfileSmartWindow_HMouseDown;
            _profileSmartWindow.HMouseMove += ProfileSmartWindow_HMouseMove;
            _profileSmartWindow.HMouseUp += ProfileSmartWindow_HMouseUp;
            _profileSmartWindow.HZoomContent = HSmartWindowControlWPF.ZoomContent.Off;
            RedrawProfileChart();
        }

        /// <summary>
        /// 解绑高度曲线 HALCON 窗口鼠标事件。
        /// </summary>
        private void DetachProfileSmartWindow()
        {
            if (_profileSmartWindow == null)
            {
                return;
            }

            _profileSmartWindow.HMouseDown -= ProfileSmartWindow_HMouseDown;
            _profileSmartWindow.HMouseMove -= ProfileSmartWindow_HMouseMove;
            _profileSmartWindow.HMouseUp -= ProfileSmartWindow_HMouseUp;
            _profileSmartWindow = null;
            ProfileHalconPreview.DrawModel = false;
        }

        /// <summary>
        /// 在高度曲线窗口开始选择一段统计区间。
        /// </summary>
        private void ProfileSmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (e.Button != MouseButton.Left || _profilePoints.Count == 0)
            {
                return;
            }

            Rect plotRect = GetProfilePlotRect();
            if (!plotRect.Contains(new Point(e.Column, e.Row)))
            {
                return;
            }

            if (_profileSegment1.HasValue && _profileSegment2.HasValue)
            {
                _profileSegment1 = null;
                _profileSegment2 = null;
                _currentSelectionTarget = 1;
            }
            else
            {
                _currentSelectionTarget = _profileSegment1.HasValue ? 2 : 1;
            }

            _isSelectingProfileRange = true;
            _profileSelectionStartIndex = GetProfileIndexFromCanvasX(e.Column);
            _profilePreviewSegment = NormalizeRange(_profileSelectionStartIndex, _profileSelectionStartIndex);
            ProfileHalconPreview.DrawModel = true;
            _profileSmartWindow?.CaptureMouse();
            RedrawProfileChart();
        }

        /// <summary>
        /// 拖动高度曲线窗口时更新区间选择预览。
        /// </summary>
        private void ProfileSmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!_isSelectingProfileRange)
            {
                return;
            }

            int currentIndex = GetProfileIndexFromCanvasX(e.Column);
            _profilePreviewSegment = NormalizeRange(_profileSelectionStartIndex, currentIndex);
            RedrawProfileChart();
        }

        /// <summary>
        /// 结束高度曲线区间选择并刷新统计结果。
        /// </summary>
        private void ProfileSmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!_isSelectingProfileRange)
            {
                return;
            }

            _isSelectingProfileRange = false;
            ProfileHalconPreview.DrawModel = false;
            if (_profileSmartWindow != null)
            {
                _profileSmartWindow.HZoomContent = HSmartWindowControlWPF.ZoomContent.Off;
            }

            if (_profileSmartWindow?.IsMouseCaptured == true)
            {
                _profileSmartWindow.ReleaseMouseCapture();
            }

            int endIndex = GetProfileIndexFromCanvasX(e.Column);
            (int Start, int End) range = NormalizeRange(_profileSelectionStartIndex, endIndex);
            if (_currentSelectionTarget == 1)
            {
                _profileSegment1 = range;
                _currentSelectionTarget = 2;
            }
            else
            {
                _profileSegment2 = range;
            }

            _profilePreviewSegment = null;
            RefreshProfileSummary();
            RedrawProfileChart();
        }

        /// <summary>
        /// 根据两段曲线区间刷新均值、高度差和结果列表。
        /// </summary>
        private void RefreshProfileSummary()
        {
            if (_model == null)
            {
                return;
            }

            if (_profilePoints.Count == 0)
            {
                _model.ClearProfileAnalysis();
                return;
            }

            double profileLength = _profilePoints[^1].Distance;
            int sampleCount = _profilePoints.Count;
            int validCount = _profilePoints.Count(point => point.IsValid);

            if (!_profileSegment1.HasValue)
            {
                _model.UpdateProfileSummary(
                    profileLength,
                    sampleCount,
                    validCount,
                    null,
                    null,
                    null,
                    "已提取高度曲线，请在下方曲线上拖动选择第一段。");
                return;
            }

            DepthProfileSegmentStats segment1Stats = DepthProfileAnalysisHelper.EvaluateSegment(
                _profilePoints,
                _profileSegment1.Value.Start,
                _profileSegment1.Value.End,
                _model.TrimRatio);

            if (!_profileSegment2.HasValue)
            {
                string status = segment1Stats.ValidSamples > 0
                    ? $"第一段已选中，范围 {FormatPlainNumber(segment1Stats.StartDistance)} ~ {FormatPlainNumber(segment1Stats.EndDistance)}，请继续选择第二段。"
                    : "第一段内没有有效高度点，请重新选择。";

                _model.UpdateProfileSummary(
                    profileLength,
                    sampleCount,
                    validCount,
                    segment1Stats.ValidSamples > 0 ? segment1Stats.MeanHeight : null,
                    null,
                    null,
                    status);
                return;
            }

            DepthProfileSegmentStats segment2Stats = DepthProfileAnalysisHelper.EvaluateSegment(
                _profilePoints,
                _profileSegment2.Value.Start,
                _profileSegment2.Value.End,
                _model.TrimRatio);

            double? heightDiff = null;
            if (segment1Stats.ValidSamples > 0 && segment2Stats.ValidSamples > 0)
            {
                heightDiff = Math.Abs(segment2Stats.MeanHeight - segment1Stats.MeanHeight);
            }

            string summary = segment1Stats.ValidSamples == 0 || segment2Stats.ValidSamples == 0
                ? "选区内存在无效段，请重新选择两段。"
                : $"两段均值已计算完成，高度差 = {FormatMeasurement(heightDiff.GetValueOrDefault())}。";

            _model.UpdateProfileSummary(
                profileLength,
                sampleCount,
                validCount,
                segment1Stats.ValidSamples > 0 ? segment1Stats.MeanHeight : null,
                segment2Stats.ValidSamples > 0 ? segment2Stats.MeanHeight : null,
                heightDiff,
                summary);

            if (heightDiff.HasValue && !double.IsNaN(heightDiff.Value))
            {
                Point segment1Start = GetProfileSegmentPixelPoint(_profileSegment1.Value.Start);
                Point segment1End = GetProfileSegmentPixelPoint(_profileSegment1.Value.End);
                Point segment2Start = GetProfileSegmentPixelPoint(_profileSegment2.Value.Start);
                Point segment2End = GetProfileSegmentPixelPoint(_profileSegment2.Value.End);
                _model.AddManualMeasureItem(
                    _selectionStartPixel,
                    _selectionEndPixel,
                    segment1Start,
                    segment1End,
                    segment2Start,
                    segment2End,
                    segment1Stats.MeanHeight,
                    segment2Stats.MeanHeight,
                    heightDiff.Value,
                    profileLength,
                    sampleCount,
                    validCount);
                _hasSelectionLine = false;
                RefreshHeatmapPreviewOverlays();
            }
        }

        /// <summary>
        /// 把高度曲线采样点索引转换为原图像素坐标。
        /// </summary>
        private Point GetProfileSegmentPixelPoint(int index)
        {
            if (_profilePoints.Count == 0)
            {
                return default;
            }

            int normalizedIndex = Math.Clamp(index, 0, _profilePoints.Count - 1);
            DepthProfilePoint point = _profilePoints[normalizedIndex];
            return new Point(point.PixelX, point.PixelY);
        }

        /// <summary>
        /// 重新生成高度曲线 HALCON 图像并刷新窗口显示。
        /// </summary>
        private void RedrawProfileChart()
        {
            if (!EnsureProfileChartImage())
            {
                return;
            }

            ClearProfileDrawObjects();
            Rect plotRect = GetProfilePlotRect();
            DrawProfileAxes(plotRect);

            if (_profilePoints.Count == 0 || plotRect.Width <= 1 || plotRect.Height <= 1)
            {
                HideProfileAxisLabels();
                ShowProfileChartMessage(_model?.ProfileStatusText ?? "暂无高度曲线。");
                return;
            }

            List<DepthProfilePoint> validPoints = _profilePoints.Where(point => point.IsValid).ToList();
            if (validPoints.Count == 0)
            {
                HideProfileAxisLabels();
                ShowProfileChartMessage("当前高度曲线没有有效高度点。");
                return;
            }

            HideProfileChartMessage();
            double minDistance = _profilePoints.First().Distance;
            double maxDistance = _profilePoints.Last().Distance;
            double minValue = validPoints.Min(point => point.HeightValue);
            double maxValue = validPoints.Max(point => point.HeightValue);
            if (Math.Abs(maxValue - minValue) < 0.000001)
            {
                minValue -= 1.0;
                maxValue += 1.0;
            }

            if (Math.Abs(maxDistance - minDistance) < 0.000001)
            {
                maxDistance = minDistance + 1.0;
            }

            UpdateProfileAxisLabels(plotRect, minDistance, maxDistance, minValue, maxValue);
            DrawProfileCurve(plotRect, minDistance, maxDistance, minValue, maxValue);
            DrawProfileSelectionRange(_profileSegment1, plotRect, minDistance, maxDistance, Segment1OverlayColor);
            DrawProfileSelectionRange(_profileSegment2, plotRect, minDistance, maxDistance, Segment2OverlayColor);
            DrawProfileSelectionRange(_profilePreviewSegment, plotRect, minDistance, maxDistance, PreviewOverlayColor);
            UpdateHeatmapSelectionOverlay();
        }

        /// <summary>
        /// 确保高度曲线绘制目标图像已按窗口尺寸创建。
        /// </summary>
        private bool EnsureProfileChartImage()
        {
            int width = Math.Max(320, (int)Math.Round(ProfileChartHost.ActualWidth));
            int height = Math.Max(160, (int)Math.Round(ProfileChartHost.ActualHeight));
            if (_profileChartImage != null
                && _profileChartImage.IsInitialized()
                && _profileChartWidth == width
                && _profileChartHeight == height)
            {
                return true;
            }

            HObject? image = null;
            try
            {
                HOperatorSet.GenImageConst(out image, "byte", width, height);
                HObject? oldImage = _profileChartImage;
                _profileChartImage = image;
                _profileChartWidth = width;
                _profileChartHeight = height;
                ProfileHalconPreview.Image = _profileChartImage;
                DisposeHObject(oldImage);
                image = null;
                Dispatcher.BeginInvoke(new Action(() => ProfileHalconPreview.DispImageFitImage()), DispatcherPriority.Background);
                return true;
            }
            catch
            {
                DisposeHObject(image);
                return false;
            }
        }

        /// <summary>
        /// 绘制高度曲线坐标轴和背景网格。
        /// </summary>
        private void DrawProfileAxes(Rect plotRect)
        {
            HObject? axes = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(
                    out axes,
                    new HTuple(new[] { plotRect.Top, plotRect.Bottom, plotRect.Bottom }),
                    new HTuple(new[] { plotRect.Left, plotRect.Left, plotRect.Right }));
                AddProfileDrawObject(axes, "gray");
                axes = null;
            }
            finally
            {
                axes?.Dispose();
            }
        }

        /// <summary>
        /// 计算高度曲线在窗口图像中的绘图区矩形。
        /// </summary>
        private Rect GetProfilePlotRect()
        {
            EnsureProfileChartImage();
            double width = Math.Max(320, _profileChartWidth);
            double height = Math.Max(160, _profileChartHeight);
            return new Rect(
                ProfilePlotLeftMargin,
                ProfilePlotTopMargin,
                Math.Max(1, width - ProfilePlotLeftMargin - ProfilePlotRightMargin),
                Math.Max(1, height - ProfilePlotTopMargin - ProfilePlotBottomMargin));
        }

        /// <summary>
        /// 根据采样点绘制高度曲线折线。
        /// </summary>
        private void DrawProfileCurve(
            Rect plotRect,
            double minDistance,
            double maxDistance,
            double minValue,
            double maxValue)
        {
            List<double> rows = [];
            List<double> columns = [];
            bool hasOpenSegment = false;

            foreach (DepthProfilePoint point in _profilePoints)
            {
                if (!point.IsValid)
                {
                    FlushProfileCurveSegment(rows, columns);
                    hasOpenSegment = false;
                    continue;
                }

                Point chartPoint = GetProfileCanvasPoint(point.Distance, point.HeightValue, plotRect, minDistance, maxDistance, minValue, maxValue);
                rows.Add(chartPoint.Y);
                columns.Add(chartPoint.X);
                hasOpenSegment = true;
            }

            if (hasOpenSegment)
            {
                FlushProfileCurveSegment(rows, columns);
            }
        }

        /// <summary>
        /// 把连续有效采样点刷新为一段 HALCON 曲线。
        /// </summary>
        private void FlushProfileCurveSegment(List<double> rows, List<double> columns)
        {
            if (rows.Count < 2 || columns.Count < 2)
            {
                rows.Clear();
                columns.Clear();
                return;
            }

            HObject? curve = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out curve, new HTuple(rows.ToArray()), new HTuple(columns.ToArray()));
                AddProfileDrawObject(curve, "cyan");
                curve = null;
            }
            finally
            {
                curve?.Dispose();
                rows.Clear();
                columns.Clear();
            }
        }

        /// <summary>
        /// 把曲线采样距离和高度值映射到绘图坐标。
        /// </summary>
        private Point GetProfileCanvasPoint(
            double distance,
            double value,
            Rect plotRect,
            double minDistance,
            double maxDistance,
            double minValue,
            double maxValue)
        {
            double x = GetProfileCanvasX(distance, plotRect, minDistance, maxDistance);
            double normalized = (value - minValue) / (maxValue - minValue);
            double y = plotRect.Bottom - normalized * plotRect.Height;
            return new Point(x, y);
        }

        /// <summary>
        /// 把剖面距离映射到曲线绘图区横坐标。
        /// </summary>
        private double GetProfileCanvasX(double distance, Rect plotRect, double minDistance, double maxDistance)
        {
            if (Math.Abs(maxDistance - minDistance) < 0.000001)
            {
                return plotRect.Left;
            }

            double normalized = (distance - minDistance) / Math.Max(maxDistance - minDistance, 0.000001);
            return plotRect.Left + plotRect.Width * Math.Clamp(normalized, 0.0, 1.0);
        }

        /// <summary>
        /// 根据曲线窗口横坐标反算最近的采样点索引。
        /// </summary>
        private int GetProfileIndexFromCanvasX(double x)
        {
            Rect plotRect = GetProfilePlotRect();
            if (_profilePoints.Count <= 1)
            {
                return 0;
            }

            double clampedX = Math.Clamp(x, plotRect.Left, plotRect.Right);
            double normalized = (clampedX - plotRect.Left) / Math.Max(plotRect.Width, 1d);
            double minDistance = _profilePoints.First().Distance;
            double maxDistance = _profilePoints.Last().Distance;
            double targetDistance = minDistance + normalized * (maxDistance - minDistance);

            int nearestIndex = 0;
            double nearestDistanceGap = double.MaxValue;
            for (int i = 0; i < _profilePoints.Count; i++)
            {
                double distanceGap = Math.Abs(_profilePoints[i].Distance - targetDistance);
                if (distanceGap < nearestDistanceGap)
                {
                    nearestDistanceGap = distanceGap;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        /// <summary>
        /// 在高度曲线中绘制已选区间的半透明范围。
        /// </summary>
        private void DrawProfileSelectionRange(
            (int Start, int End)? range,
            Rect plotRect,
            double minDistance,
            double maxDistance,
            string color)
        {
            if (!range.HasValue || _profilePoints.Count == 0)
            {
                return;
            }

            double x1 = GetProfileCanvasX(_profilePoints[range.Value.Start].Distance, plotRect, minDistance, maxDistance);
            double x2 = GetProfileCanvasX(_profilePoints[range.Value.End].Distance, plotRect, minDistance, maxDistance);
            double left = Math.Min(x1, x2);
            double right = Math.Max(x1, x2);
            HObject? rectangle = null;
            try
            {
                HOperatorSet.GenRectangle1(out rectangle, plotRect.Top, left, plotRect.Bottom, Math.Max(left + 2, right));
                AddProfileDrawObject(rectangle, color);
                rectangle = null;
            }
            finally
            {
                rectangle?.Dispose();
            }
        }

        /// <summary>
        /// 把高度曲线绘制对象显示到 HALCON 窗口并记录生命周期。
        /// </summary>
        private void AddProfileDrawObject(HObject hObject, string color, bool isFillDisplay = false)
        {
            if (hObject == null || !hObject.IsInitialized() || ProfileHalconPreview?.DrawObjectList == null)
            {
                hObject?.Dispose();
                return;
            }

            ProfileHalconPreview.DrawObjectList.Add(new HalconDrawingObject
            {
                ShapeType = HalconShapeType.Region,
                Hobject = hObject,
                Color = color,
                IsFillDisplay = isFillDisplay
            });
        }

        /// <summary>
        /// 释放并清空高度曲线窗口的全部 HALCON 绘制对象。
        /// </summary>
        private void ClearProfileDrawObjects()
        {
            if (ProfileHalconPreview?.DrawObjectList == null)
            {
                return;
            }

            foreach (HalconDrawingObject item in ProfileHalconPreview.DrawObjectList.ToList())
            {
                try
                {
                    item.Hobject?.Dispose();
                }
                catch
                {
                }
            }

            ProfileHalconPreview.DrawObjectList.Clear();
        }

        /// <summary>
        /// 释放高度曲线底图 HALCON 对象。
        /// </summary>
        private void DisposeProfileChartImage()
        {
            DisposeHObject(_profileChartImage);
            _profileChartImage = null;
            _profileChartWidth = 0;
            _profileChartHeight = 0;
        }

        /// <summary>
        /// 安全释放 HALCON 对象并忽略重复释放异常。
        /// </summary>
        private static void DisposeHObject(HObject? hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 拖拽高度曲线区间时更新预览范围。
        /// </summary>
        private void UpdateProfileSelectionPreview(int startIndex, int endIndex)
        {
            _profilePreviewSegment = NormalizeRange(startIndex, endIndex);
            RedrawProfileChart();
        }

        /// <summary>
        /// 根据曲线范围更新坐标轴刻度文字。
        /// </summary>
        private void UpdateProfileAxisLabels(
            Rect plotRect,
            double minDistance,
            double maxDistance,
            double minValue,
            double maxValue)
        {
            ProfileYAxisTopLabel.Text = FormatMeasurement(maxValue);
            ProfileYAxisBottomLabel.Text = FormatMeasurement(minValue);
            ProfileXAxisStartLabel.Text = FormatMeasurement(minDistance);
            ProfileXAxisEndLabel.Text = FormatMeasurement(maxDistance);

            Canvas.SetLeft(ProfileYAxisTopLabel, 4);
            Canvas.SetTop(ProfileYAxisTopLabel, Math.Max(0, plotRect.Top - 10));
            Canvas.SetLeft(ProfileYAxisBottomLabel, 4);
            Canvas.SetTop(ProfileYAxisBottomLabel, Math.Max(0, plotRect.Bottom - 12));
            Canvas.SetLeft(ProfileXAxisStartLabel, plotRect.Left);
            Canvas.SetTop(ProfileXAxisStartLabel, plotRect.Bottom + 8);
            Canvas.SetLeft(ProfileXAxisEndLabel, Math.Max(plotRect.Left, plotRect.Right - 78));
            Canvas.SetTop(ProfileXAxisEndLabel, plotRect.Bottom + 8);
            Canvas.SetLeft(ProfileYAxisTitleLabel, 4);
            Canvas.SetTop(ProfileYAxisTitleLabel, 4);
            Canvas.SetLeft(ProfileXAxisTitleLabel, Math.Max(plotRect.Left, plotRect.Left + (plotRect.Width * 0.5) - 24));
            Canvas.SetTop(ProfileXAxisTitleLabel, plotRect.Bottom + 24);

            ProfileYAxisTopLabel.Visibility = Visibility.Visible;
            ProfileYAxisBottomLabel.Visibility = Visibility.Visible;
            ProfileXAxisStartLabel.Visibility = Visibility.Visible;
            ProfileXAxisEndLabel.Visibility = Visibility.Visible;
            ProfileYAxisTitleLabel.Visibility = Visibility.Visible;
            ProfileXAxisTitleLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏高度曲线坐标轴刻度文字。
        /// </summary>
        private void HideProfileAxisLabels()
        {
            ProfileYAxisTopLabel.Visibility = Visibility.Collapsed;
            ProfileYAxisBottomLabel.Visibility = Visibility.Collapsed;
            ProfileXAxisStartLabel.Visibility = Visibility.Collapsed;
            ProfileXAxisEndLabel.Visibility = Visibility.Collapsed;
            ProfileYAxisTitleLabel.Visibility = Visibility.Collapsed;
            ProfileXAxisTitleLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 在高度曲线区域显示空数据或操作提示。
        /// </summary>
        private void ShowProfileChartMessage(string message)
        {
            ProfileChartMessageLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "暂无高度曲线。"
                : message;
            ProfileChartMessageLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏高度曲线区域的提示信息。
        /// </summary>
        private void HideProfileChartMessage()
        {
            ProfileChartMessageLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 隐藏高度曲线两段区间的数值标签。
        /// </summary>
        private void HideProfileSegmentLabels()
        {
        }

        /// <summary>
        /// 尝试统计当前曲线区间的有效高度均值。
        /// </summary>
        private DepthProfileSegmentStats? TryEvaluateSegment((int Start, int End)? range)
        {
            if (!range.HasValue || _model == null || _profilePoints.Count == 0)
            {
                return null;
            }

            return DepthProfileAnalysisHelper.EvaluateSegment(
                _profilePoints,
                range.Value.Start,
                range.Value.End,
                _model.TrimRatio);
        }

        /// <summary>
        /// 初始化高度曲线绘制使用的画笔、字体和网格线样式。
        /// </summary>
        private static (int Start, int End) NormalizeRange(int index1, int index2)
        {
            return (Math.Min(index1, index2), Math.Max(index1, index2));
        }

        /// <summary>
        /// 按测量精度格式化带单位的高度值。
        /// </summary>
        private string FormatMeasurement(double value)
        {
            return _model?.FormatMeasurement(value) ?? $"{value:F3} mm";
        }

        /// <summary>
        /// 按测量精度格式化不带单位的数值。
        /// </summary>
        private string FormatPlainNumber(double value)
        {
            return _model?.FormatPlainNumber(value) ?? $"{value:F3}";
        }

        #endregion

        #region 原生窗口声明

        private delegate bool EnumChildWindowsCallback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hwndParent,
            EnumChildWindowsCallback callback,
            IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(
            IntPtr hwnd,
            IntPtr lprcUpdate,
            IntPtr hrgnUpdate,
            RedrawWindowFlags flags);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hwnd,
            WindowMessages msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr hwndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            SetWindowPosFlags flags);

        [Flags]
        private enum RedrawWindowFlags : uint
        {
            Invalidate = 0x0001,
            UpdateNow = 0x0100,
            AllChildren = 0x0080
        }

        private enum WindowMessages : uint
        {
            MouseMove = 0x0200
        }

        [Flags]
        private enum SetWindowPosFlags : uint
        {
            NoSize = 0x0001,
            NoMove = 0x0002,
            NoZOrder = 0x0004,
            NoActivate = 0x0010,
            ShowWindow = 0x0040,
            FrameChanged = 0x0020
        }

        #endregion
    }
}
