using DryIoc;
using OpenCvSharp;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Logger;
using ReeYin_V.Main.UC.Models;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace ReeYin_V.Main.UC.Views
{
    public partial class DynamicRegionControl : UserControl
    {
        /// <summary>
        /// 存储文件
        /// </summary>
        private const string LayoutStoreKey = "RegionLayoutNode";
        private const string DefaultBusyMessage = "请等待主页面加载完成，正在准备区域视图...";
        private const string DefaultPageNamePrefix = "页面";
        private const double FloatingButtonMargin = 22d;
        private const double FloatingPanelGap = 14d;
        private const double FloatingBoundaryPadding = 12d;
        private Grid MainGrid;
        private readonly Grid _pageHostGrid = new();
        private readonly IRegionManager _regionManager;
        private RegionLayoutNode _rootNode = new();
        private DynamicRegionControlLayoutState _layoutState = new();
        private readonly HashSet<string> _registeredRegions = new();
        // Each page keeps its own visual tree so switching pages does not recreate live controls.
        private readonly Dictionary<string, Grid> _pageVisualTrees = [];
        // Track per-page region registrations so page removal or rebuild can clean up precisely.
        private readonly Dictionary<string, HashSet<string>> _pageRegisteredRegions = [];
        private readonly HashSet<string> _restoredPageIds = [];
        private readonly DispatcherTimer _saveLayoutTimer;
        private readonly DispatcherTimer _restoreLoadedViewsTimer;
        private static readonly TimeSpan SaveLayoutThrottleInterval = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan RestoreLoadedViewsRetryInterval = TimeSpan.FromMilliseconds(200);
        private const int RestoreLoadedViewsMaxAttempts = 15;
        private DateTime _lastLayoutSaveAt = DateTime.MinValue;
        private bool _hasPendingLayoutSave;
        private bool _isLoading;
        private int _restoreLoadedViewsAttempt;
        private List<RegionLayoutNode> _pendingLoadedNodes = [];
        private int _activeNavigationCount;
        private string _busyMessage = DefaultBusyMessage;
        private bool _isDynamicViewManagerHooked;
        private bool _suppressPageSelectionChanged;
        private bool _isUpdatingOperationPanelState;
        private bool _isFloatingDragPending;
        private bool _isDraggingFloatingHost;
        private bool _floatingDragMoved;
        private bool _isFloatingToggleClickCandidate;
        private string _pendingRestorePageId = string.Empty;
        private System.Windows.Point _floatingDragStartPoint;
        private double _floatingDragStartLeft;
        private double _floatingDragStartTop;
        private double _floatingDragCurrentLeft;
        private double _floatingDragCurrentTop;
        private UIElement? _floatingDragHandle;

        private bool IsInteractionLocked => _isLoading || _pendingLoadedNodes.Count > 0 || _activeNavigationCount > 0;

        public DynamicRegionControl() : this(ContainerLocator.Container.Resolve<IRegionManager>()) { }

        public DynamicRegionControl(IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager = regionManager;
            _saveLayoutTimer = new DispatcherTimer();
            _saveLayoutTimer.Tick += OnSaveLayoutTimerTick;
            _restoreLoadedViewsTimer = new DispatcherTimer
            {
                Interval = RestoreLoadedViewsRetryInterval
            };
            _restoreLoadedViewsTimer.Tick += OnRestoreLoadedViewsTimerTick;
            UpdateInteractionState("正在初始化区域布局，请稍候...");
            Loaded += (_, _) => TryHookDynamicViewManager();
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
            SizeChanged += OnControlSizeChanged;
            PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Subscribe((cmd) =>
            {
                if(cmd == "打开")
                {
                    LoadLayout();
                }
            }, ThreadOption.UIThread);
            TryHookDynamicViewManager();
            LoadLayout();
        }

        private DynamicRegionPageState CreateNewPageState(int index)
        {
            return DynamicRegionControlLayoutState.CreateNewPageState(index, DefaultPageNamePrefix);
        }

        private DynamicRegionControlLayoutState CreateDefaultLayoutState()
        {
            return DynamicRegionControlLayoutState.CreateDefault(DefaultPageNamePrefix);
        }

        private DynamicRegionControlLayoutState ConvertLegacyLayoutState(RegionLayoutNode? legacyRoot)
        {
            return DynamicRegionControlLayoutState.FromLegacy(legacyRoot, DefaultPageNamePrefix);
        }

        private void EnsureValidLayoutState()
        {
            _layoutState = DynamicRegionControlLayoutState.EnsureValid(_layoutState, DefaultPageNamePrefix);
        }

        private DynamicRegionPageState? GetCurrentPage()
        {
            EnsureValidLayoutState();
            return _layoutState.GetCurrentPage();
        }

        private int TryGetCurrentPageIndex()
        {
            EnsureValidLayoutState();
            return _layoutState.GetCurrentPageIndex();
        }

        private void SyncCurrentPageRootNode()
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
            {
                currentPage.RootNode = _rootNode ?? new RegionLayoutNode();
            }
        }

        private void EnsureLayoutHostInitialized()
        {
            if (!ReferenceEquals(LayoutHost.Content, _pageHostGrid))
            {
                LayoutHost.Content = _pageHostGrid;
            }
        }

        private Grid EnsurePageVisualTree(DynamicRegionPageState page, bool rebuild = false)
        {
            EnsureLayoutHostInitialized();

            if (rebuild)
            {
                RemovePageVisualTree(page.Id);
            }

            if (_pageVisualTrees.TryGetValue(page.Id, out var existingGrid))
            {
                return existingGrid;
            }

            var pageGrid = new Grid
            {
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            _pageHostGrid.Children.Add(pageGrid);
            _pageVisualTrees[page.Id] = pageGrid;

            // Build each page tree only once unless an explicit rebuild is requested.
            // Later page switches only toggle visibility, which keeps runtime state alive.
            BuildLayout(page.RootNode ??= new RegionLayoutNode(), pageGrid, page.Id);
            RegionManager.UpdateRegions();

            return pageGrid;
        }

        private void ActivatePageVisual(DynamicRegionPageState page, bool restoreLoadedViews)
        {
            var activeGrid = EnsurePageVisualTree(page);

            foreach (var pageVisual in _pageVisualTrees)
            {
                bool isActive = pageVisual.Key == page.Id;
                pageVisual.Value.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
                pageVisual.Value.IsHitTestVisible = isActive;
            }

            MainGrid = activeGrid;
            UpdateLayout();
            RegionManager.UpdateRegions();

            if (restoreLoadedViews && !_restoredPageIds.Contains(page.Id))
            {
                // Restore persisted views only the first time a page becomes active.
                QueueRestoreLoadedViews(page);
                return;
            }

            UpdateInteractionState();
        }

        private void RemovePageVisualTree(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                return;
            }

            if (string.Equals(_pendingRestorePageId, pageId, StringComparison.Ordinal))
            {
                StopRestoringLoadedViews();
            }

            RemoveRegisteredRegions(pageId);

            if (_pageVisualTrees.TryGetValue(pageId, out var pageGrid))
            {
                _pageHostGrid.Children.Remove(pageGrid);
                _pageVisualTrees.Remove(pageId);
            }

            _restoredPageIds.Remove(pageId);
        }

        private void ResetPageVisualTrees()
        {
            RemoveRegisteredRegions();
            _pageHostGrid.Children.Clear();
            _pageVisualTrees.Clear();
            _pageRegisteredRegions.Clear();
            _restoredPageIds.Clear();
            _pendingRestorePageId = string.Empty;
            MainGrid = new Grid();
            EnsureLayoutHostInitialized();
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => EnsureFloatingButtonPosition()), DispatcherPriority.Loaded);
        }

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureFloatingButtonPosition();
                UpdateFloatingPanelPlacement();
            }), DispatcherPriority.Loaded);
        }

        private void EnsureFloatingButtonPosition(bool forceDefault = false)
        {
            if (FloatingLayer == null
                || PageOperationBorder == null
                || FloatingLayer.ActualWidth <= 0
                || FloatingLayer.ActualHeight <= 0
                || PageOperationBorder.ActualWidth <= 0
                || PageOperationBorder.ActualHeight <= 0)
            {
                return;
            }

            EnsureValidLayoutState();

            double left = forceDefault || !_layoutState.FloatingButtonLeft.HasValue
                ? GetDefaultFloatingButtonLeft()
                : _layoutState.FloatingButtonLeft.Value;
            double top = forceDefault || !_layoutState.FloatingButtonTop.HasValue
                ? GetDefaultFloatingButtonTop()
                : _layoutState.FloatingButtonTop.Value;

            SetFloatingButtonPosition(left, top, persistLayout: false);
        }

        private double GetDefaultFloatingButtonLeft()
        {
            return Math.Max(
                FloatingBoundaryPadding,
                FloatingLayer.ActualWidth - PageOperationBorder.ActualWidth - FloatingButtonMargin);
        }

        private double GetDefaultFloatingButtonTop()
        {
            return Math.Max(
                FloatingBoundaryPadding,
                FloatingLayer.ActualHeight - PageOperationBorder.ActualHeight - FloatingButtonMargin);
        }

        private void SetFloatingButtonPosition(double left, double top, bool persistLayout)
        {
            if (FloatingLayer == null || PageOperationBorder == null)
            {
                return;
            }

            EnsureValidLayoutState();
            ClearFloatingDragVisual();

            double maxLeft = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualWidth - PageOperationBorder.ActualWidth - FloatingBoundaryPadding);
            double maxTop = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualHeight - PageOperationBorder.ActualHeight - FloatingBoundaryPadding);
            double clampedLeft = Math.Clamp(left, FloatingBoundaryPadding, maxLeft);
            double clampedTop = Math.Clamp(top, FloatingBoundaryPadding, maxTop);

            Canvas.SetLeft(PageOperationBorder, clampedLeft);
            Canvas.SetTop(PageOperationBorder, clampedTop);

            _layoutState.FloatingButtonLeft = clampedLeft;
            _layoutState.FloatingButtonTop = clampedTop;

            UpdateFloatingPanelPlacement();

            if (persistLayout)
            {
                SaveLayout();
            }
        }

        private static TranslateTransform EnsureTranslateTransform(UIElement? element)
        {
            if (element?.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                if (element != null)
                {
                    element.RenderTransform = transform;
                }
            }

            return transform;
        }

        private void ApplyFloatingDragVisual(double left, double top)
        {
            if (FloatingLayer == null || PageOperationBorder == null)
            {
                return;
            }

            double maxLeft = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualWidth - PageOperationBorder.ActualWidth - FloatingBoundaryPadding);
            double maxTop = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualHeight - PageOperationBorder.ActualHeight - FloatingBoundaryPadding);

            _floatingDragCurrentLeft = Math.Clamp(left, FloatingBoundaryPadding, maxLeft);
            _floatingDragCurrentTop = Math.Clamp(top, FloatingBoundaryPadding, maxTop);

            double offsetX = _floatingDragCurrentLeft - _floatingDragStartLeft;
            double offsetY = _floatingDragCurrentTop - _floatingDragStartTop;

            var buttonTransform = EnsureTranslateTransform(PageOperationBorder);
            buttonTransform.X = offsetX;
            buttonTransform.Y = offsetY;

            if (PageOperationPanel != null)
            {
                var panelTransform = EnsureTranslateTransform(PageOperationPanel);
                panelTransform.X = offsetX;
                panelTransform.Y = offsetY;
            }
        }

        private void ClearFloatingDragVisual()
        {
            if (PageOperationBorder?.RenderTransform is TranslateTransform buttonTransform)
            {
                buttonTransform.X = 0;
                buttonTransform.Y = 0;
            }

            if (PageOperationPanel?.RenderTransform is TranslateTransform panelTransform)
            {
                panelTransform.X = 0;
                panelTransform.Y = 0;
            }
        }

        private void UpdateFloatingPanelPlacement()
        {
            if (FloatingLayer == null
                || PageOperationPanel == null
                || PageOperationBorder == null
                || FloatingLayer.ActualWidth <= 0
                || FloatingLayer.ActualHeight <= 0
                || PageOperationBorder.ActualWidth <= 0
                || PageOperationBorder.ActualHeight <= 0)
            {
                return;
            }

            double buttonLeft = Canvas.GetLeft(PageOperationBorder);
            double buttonTop = Canvas.GetTop(PageOperationBorder);

            if (double.IsNaN(buttonLeft) || double.IsNaN(buttonTop))
            {
                return;
            }

            if (PageOperationPanel.Visibility != Visibility.Visible
                || PageOperationPanel.ActualWidth <= 0
                || PageOperationPanel.ActualHeight <= 0)
            {
                Canvas.SetLeft(PageOperationPanel, buttonLeft);
                Canvas.SetTop(PageOperationPanel, Math.Max(FloatingBoundaryPadding, buttonTop));
                return;
            }

            double desiredLeft = buttonLeft + PageOperationBorder.ActualWidth - PageOperationPanel.ActualWidth;
            double desiredTop = buttonTop - PageOperationPanel.ActualHeight - FloatingPanelGap;

            double maxLeft = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualWidth - PageOperationPanel.ActualWidth - FloatingBoundaryPadding);
            double maxTop = Math.Max(FloatingBoundaryPadding, FloatingLayer.ActualHeight - PageOperationPanel.ActualHeight - FloatingBoundaryPadding);

            double panelLeft = Math.Clamp(desiredLeft, FloatingBoundaryPadding, maxLeft);
            double panelTop = desiredTop >= FloatingBoundaryPadding
                ? desiredTop
                : Math.Min(
                    Math.Max(FloatingBoundaryPadding, buttonTop + PageOperationBorder.ActualHeight + FloatingPanelGap),
                    maxTop);

            panelTop = Math.Clamp(panelTop, FloatingBoundaryPadding, maxTop);

            Canvas.SetLeft(PageOperationPanel, panelLeft);
            Canvas.SetTop(PageOperationPanel, panelTop);
        }

        private void ResetFloatingDragState()
        {
            if (_floatingDragHandle != null && _floatingDragHandle.IsMouseCaptured)
            {
                _floatingDragHandle.ReleaseMouseCapture();
            }

            ClearFloatingDragVisual();
            _floatingDragHandle = null;
            _isFloatingDragPending = false;
            _isDraggingFloatingHost = false;
            _floatingDragMoved = false;
            _isFloatingToggleClickCandidate = false;
        }

        private void SetOperationPanelExpanded(bool isExpanded)
        {
            _isUpdatingOperationPanelState = true;
            PageOperationToggleButton.IsChecked = isExpanded;
            PageOperationPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            PageOperationPanel.IsHitTestVisible = isExpanded;
            _isUpdatingOperationPanelState = false;
            UpdateFloatingOperationButtonState();
            Dispatcher.BeginInvoke(new Action(UpdateFloatingPanelPlacement), DispatcherPriority.Loaded);
        }

        private void UpdateFloatingOperationButtonState()
        {
            if (PageFloatingStateText == null)
            {
                return;
            }

            bool isExpanded = PageOperationToggleButton.IsChecked == true;
            PageFloatingStateText.Text = isExpanded ? "收起" : "展开";
            PageOperationToggleButton.ToolTip = isExpanded ? "收起页面管理" : "展开页面管理";
        }

        private void RefreshPageSelector()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(RefreshPageSelector));
                return;
            }

            EnsureValidLayoutState();

            var currentPage = GetCurrentPage();
            int currentIndex = TryGetCurrentPageIndex();
            PagePanelSummaryText.Text = currentPage == null
                ? "当前页面：未初始化"
                : $"当前页面：{currentPage.Name}";
            PageCountText.Text = $"共 {_layoutState.Pages.Count} 页";
            PagePanelTitleText.Text = "页面管理";
            PageFloatingCurrentIndexText.Text = currentIndex >= 0 ? (currentIndex + 1).ToString() : "1";
            PageFloatingPageCountText.Text = $"/{_layoutState.Pages.Count}";
            UpdateFloatingOperationButtonState();

            _suppressPageSelectionChanged = true;
            PageListBox.ItemsSource = null;
            PageListBox.ItemsSource = _layoutState.Pages;
            PageListBox.SelectedItem = currentPage;
            _suppressPageSelectionChanged = false;

            UpdatePageOperationState();
        }

        private void UpdatePageOperationState()
        {
            if (PageOperationBorder == null)
            {
                return;
            }

            EnsureValidLayoutState();
            bool canInteract = !IsInteractionLocked;
            int currentIndex = TryGetCurrentPageIndex();

            PageOperationBorder.Opacity = canInteract ? 1.0 : 0.88;
            PageOperationPanel.Opacity = canInteract ? 1.0 : 0.82;
            PageListBox.IsEnabled = canInteract;
            PreviousPageButton.IsEnabled = canInteract && currentIndex > 0;
            NextPageButton.IsEnabled = canInteract && currentIndex >= 0 && currentIndex < _layoutState.Pages.Count - 1;
            AddPageButton.IsEnabled = canInteract;
            DeletePageButton.IsEnabled = canInteract && _layoutState.Pages.Count > 1;
        }

        private void SwitchToPage(DynamicRegionPageState? page, bool persistLayout = true)
        {
            if (page == null)
            {
                return;
            }

            EnsureValidLayoutState();
            StopRestoringLoadedViews();
            _layoutState.SelectedPageId = page.Id;
            _rootNode = page.RootNode ??= new RegionLayoutNode();

            RefreshPageSelector();
            ActivatePageVisual(page, restoreLoadedViews: true);

            if (persistLayout)
            {
                SaveLayout();
            }
        }

        private void SwitchPageByOffset(int offset)
        {
            if (!EnsureInteractionReady())
            {
                return;
            }

            EnsureValidLayoutState();
            int currentIndex = TryGetCurrentPageIndex();
            if (currentIndex < 0)
            {
                return;
            }

            int nextIndex = currentIndex + offset;
            if (nextIndex < 0 || nextIndex >= _layoutState.Pages.Count)
            {
                return;
            }

            SwitchToPage(_layoutState.Pages[nextIndex]);
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            ResetFloatingDragState();
            StopRestoringLoadedViews();
            FlushPendingLayoutSave();
            ResetNavigationTracking();
            TryUnhookDynamicViewManager();
        }

        private void UpdateInteractionState(string message = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateInteractionState(message)));
                return;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                _busyMessage = message;
            }

            bool isBusy = IsInteractionLocked;
            LayoutHost.IsHitTestVisible = !isBusy;
            BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            BusyMessageText.Text = isBusy ? BuildBusyMessage() : DefaultBusyMessage;
            UpdatePageOperationState();
        }

        private string BuildBusyMessage()
        {
            if (_isLoading)
            {
                return "请等待主页面加载完成，正在恢复区域布局和区域视图。";
            }

            int remainingCount = _pendingLoadedNodes.Count + _activeNavigationCount;
            if (remainingCount > 0)
            {
                return $"请等待主页面加载完成，当前还有 {remainingCount} 个区域视图正在加载。";
            }

            return string.IsNullOrWhiteSpace(_busyMessage) ? DefaultBusyMessage : _busyMessage;
        }

        private bool EnsureInteractionReady()
        {
            if (!IsInteractionLocked)
            {
                return true;
            }

            MessageView.Ins.MessageBoxShow("请等待主页面加载完成，当前区域视图仍在加载中。", eMsgType.Info);
            return false;
        }

        private void BeginNavigationTracking(string message = null)
        {
            _activeNavigationCount++;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _busyMessage = message;
            }

            UpdateInteractionState();
        }

        private void EndNavigationTracking()
        {
            if (_activeNavigationCount > 0)
            {
                _activeNavigationCount--;
            }

            UpdateInteractionState();
        }

        private void ResetNavigationTracking()
        {
            _activeNavigationCount = 0;
            UpdateInteractionState();
        }

        #region 构建布局
        private void BuildLayout(RegionLayoutNode node, Grid container, string pageId)
        {
            container.Children.Clear();
            container.RowDefinitions.Clear();
            container.ColumnDefinitions.Clear();

            if (node.SplitDirection == SplitDirection.None)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.LightGray
                };

                var content = new ContentControl();
                RegisterRegion(node, content, pageId);

                border.Child = content;
                container.Children.Add(border);

                var menuButton = new Button
                {
                    Content = "☰",
                    Width = 28,
                    Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(4),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    ToolTip = "操作菜单"
                };

                menuButton.Click += (_, _) =>
                {
                    if (!EnsureInteractionReady())
                    {
                        return;
                    }

                    if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
                    {
                        MessageView.Ins.MessageBoxShow("您无权限操作!", eMsgType.Info);
                        return;
                    }
                    OpenManagementPanel(node);
                    //var menu = CreateContextMenu(node, container);
                    //menuButton.ContextMenu = menu;
                    //menu.IsOpen = true;
                };

                container.Children.Add(menuButton);
                return;
            }

            if (node.SplitDirection == SplitDirection.Horizontal)
            {
                double top = node.Sizes?.ElementAtOrDefault(0) ?? 0;
                double bottom = node.Sizes?.ElementAtOrDefault(1) ?? 0;
                if (top == 0 && bottom == 0) { top = bottom = 1; }

                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(top, GridUnitType.Star) });
                container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(bottom, GridUnitType.Star) });

                var splitter = new GridSplitter
                {
                    Height = 4,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    ResizeBehavior = GridResizeBehavior.PreviousAndNext
                };

                Grid.SetRow(splitter, 1);
                container.Children.Add(splitter);

                var grid1 = new Grid();
                Grid.SetRow(grid1, 0);
                var grid2 = new Grid();
                Grid.SetRow(grid2, 2);

                container.Children.Add(grid1);
                container.Children.Add(grid2);

                BuildLayout(node.Child1 ?? new RegionLayoutNode(), grid1, pageId);
                BuildLayout(node.Child2 ?? new RegionLayoutNode(), grid2, pageId);

                AttachGridSplitterEvents(container, node, SplitDirection.Horizontal);
            }
            else
            {
                double left = node.Sizes?.ElementAtOrDefault(0) ?? 0;
                double right = node.Sizes?.ElementAtOrDefault(1) ?? 0;
                if (left == 0 && right == 0) { left = right = 1; }

                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(left, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(right, GridUnitType.Star) });

                var splitter = new GridSplitter
                {
                    Width = 4,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    ResizeBehavior = GridResizeBehavior.PreviousAndNext
                };

                Grid.SetColumn(splitter, 1);
                container.Children.Add(splitter);

                var grid1 = new Grid();
                Grid.SetColumn(grid1, 0);
                var grid2 = new Grid();
                Grid.SetColumn(grid2, 2);

                container.Children.Add(grid1);
                container.Children.Add(grid2);

                BuildLayout(node.Child1 ?? new RegionLayoutNode(), grid1, pageId);
                BuildLayout(node.Child2 ?? new RegionLayoutNode(), grid2, pageId);

                AttachGridSplitterEvents(container, node, SplitDirection.Vertical);
            }
        }
        #endregion

        #region GridSplitter 调整事件
        private void AttachGridSplitterEvents(Grid container, RegionLayoutNode node, SplitDirection direction)
        {
            if (container == null || node == null)
                return;

            var splitter = container.Children.OfType<GridSplitter>().FirstOrDefault();
            if (splitter == null) return;

            splitter.DragCompleted += (_, _) => UpdateAndSaveSizes(container, node, direction);
        }

        private void UpdateAndSaveSizes(Grid container, RegionLayoutNode node, SplitDirection direction)
        {
            if (direction == SplitDirection.Horizontal)
            {
                double top = container.RowDefinitions[0].Height.Value;
                double bottom = container.RowDefinitions[2].Height.Value;
                node.Sizes = [top, bottom];
            }
            else
            {
                double left = container.ColumnDefinitions[0].Width.Value;
                double right = container.ColumnDefinitions[2].Width.Value;
                node.Sizes = [left, right];
            }

            SaveLayout();
        }
        #endregion

        #region 上下文菜单
        private ContextMenu CreateContextMenu(RegionLayoutNode node)
        {
            if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
                return null;

            var menu = new ContextMenu
            {
                Style = Application.Current.TryFindResource("ContextMenuStyle") as Style
            };

            if (node.SplitDirection == SplitDirection.None)
            {
                menu.Items.Add(CreateMenuItem("⬍ 上下拆分", () => SplitRegion(node, SplitDirection.Horizontal)));
                menu.Items.Add(CreateMenuItem("⬌ 左右拆分", () => SplitRegion(node, SplitDirection.Vertical)));
            }
            else
            {
                menu.Items.Add(CreateMenuItem("⊕ 合并区域", () => MergeRegion(node)));
            }

            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("⚙ 打开管理面板", () => OpenManagementPanel(node)));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("↻ 重置布局", ResetLayout));

            return menu;
        }

        /// <summary>
        /// 拆分区域
        /// </summary>
        private void SplitRegion(RegionLayoutNode node, SplitDirection direction)
        {
            node.Split(direction);

            RebuildAndReloadLayout();
        }

        /// <summary>
        /// 合并区域
        /// </summary>
        private void MergeRegion(RegionLayoutNode node)
        {
            node.Merge();

            RebuildAndReloadLayout();
        }

        /// <summary>
        /// 重建布局并重新加载所有视图
        /// </summary>
        private void RebuildAndReloadLayout()
        {
            UpdateInteractionState("正在重新整理区域布局，请稍候...");
            RebuildLayoutVisualTree();
            SaveLayout();
            QueueRestoreLoadedViews();
        }

        /// <summary>
        /// 打开管理面板
        /// </summary>
        private void OpenManagementPanel(RegionLayoutNode node)
        {
            if (!EnsureInteractionReady())
            {
                return;
            }

            try
            {
                var dialogParams = new DialogParameters
                {
                    { "Title", "视图加载管理" },
                    { "Icon", "\ue673" },
                    { "Node", node },
                    { "RootNode", _rootNode },
                    { "OnViewLoaded", new Action<DynamicRegionViewLoadRequest>((request) =>
                        {
                            LoadView(request);
                        })
                    },
                    { "OnLayoutChanged", new Action(RebuildAndReloadLayout) }
                };

                PrismProvider.DialogService.Show("RegionManagementView", dialogParams, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 用户确认后的操作
                        RebuildAndReloadLayout();
                    }
                }, nameof(SingleInstanceDialogWindowView));
            }
            catch (Exception ex)
            {
                Logs.LogError($"打开管理面板失败：{ex.Message}");
                MessageView.Ins.MessageBoxShow($"打开管理面板失败：{ex.Message}", eMsgType.Error);
            }
        }

        private MenuItem CreateMenuItem(string header, Action onClick)
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, _) => onClick();
            return item;
        }
        #endregion

        #region 区域管理
        private void RegisterRegion(RegionLayoutNode node, ContentControl content, string pageId)
        {
            var regionName = node.RegionName;
            if (!_pageRegisteredRegions.TryGetValue(pageId, out var pageRegions))
            {
                pageRegions = [];
                _pageRegisteredRegions[pageId] = pageRegions;
            }

            try
            {
                if (_registeredRegions.Contains(regionName)
                    && !pageRegions.Contains(regionName))
                {
                    regionName = Guid.NewGuid().ToString();
                    node.RegionName = regionName;
                }

                _registeredRegions.Add(regionName);
                pageRegions.Add(regionName);
                RegionManager.SetRegionManager(content, _regionManager);
                RegionManager.SetRegionName(content, regionName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Region 注册失败: {regionName} - {ex.Message}");
            }
        }

        private void LoadView(DynamicRegionViewLoadRequest request, bool persistLayout = true)
        {
            TryBeginLoadView(request, persistLayout);
        }

        private bool TryBeginLoadView(
            DynamicRegionViewLoadRequest request,
            bool persistLayout = true,
            Action<bool> onCompleted = null)
        {
            if (_regionManager == null || request == null || !request.IsValid)
            {
                onCompleted?.Invoke(false);
                return false;
            }

            if (!CanLoadView(request, out string errorMessage))
            {
                Logs.LogWarning($"视图加载校验失败：{request.ViewName} - {errorMessage}");
                onCompleted?.Invoke(false);
                return false;
            }

            if (!_regionManager.Regions.ContainsRegionWithName(request.RegionName))
            {
                onCompleted?.Invoke(false);
                return false;
            }

            var node = _layoutState.FindNodeByRegionNameAcrossPages(request.RegionName, _rootNode);
            if (node == null)
            {
                onCompleted?.Invoke(false);
                return false;
            }

            BeginNavigationTracking();

            void CompleteNavigation(bool succeeded, Exception exception = null)
            {
                try
                {
                    if (succeeded)
                    {
                        node.ApplyLoadRequest(request);
                        if (persistLayout)
                        {
                            SaveLayout();
                        }
                    }
                    else if (exception != null)
                    {
                        Logs.LogWarning($"视图导航失败：{request.ViewName} -> {request.RegionName} - {exception.Message}");
                    }

                    onCompleted?.Invoke(succeeded);
                }
                finally
                {
                    EndNavigationTracking();
                }
            }

            void NavigateCore()
            {
                try
                {
                    if (!_regionManager.Regions.ContainsRegionWithName(request.RegionName))
                    {
                        CompleteNavigation(false);
                        return;
                    }

                    var region = _regionManager.Regions[request.RegionName];
                    region.RequestNavigate(
                        request.ViewName,
                        result =>
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                bool succeeded = result?.Success ?? false;
                                CompleteNavigation(succeeded, result?.Exception);
                            }));
                        },
                        new NavigationParameters()
                        {
                            { "Serial", request.Serial }
                        });
                }
                catch (Exception ex)
                {
                    CompleteNavigation(false, ex);
                }
            }

            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                NavigateCore();
            }
            else
            {
                PrismProvider.Dispatcher.BeginInvoke((Action)NavigateCore);
            }

            return true;
        }

        private void TryHookDynamicViewManager()
        {
            if (_isDynamicViewManagerHooked)
            {
                return;
            }

            var dynamicViewManager = PrismProvider.DynamicViewManager;
            if (dynamicViewManager == null)
            {
                return;
            }

            dynamicViewManager.DynamicViewsRemoved += OnDynamicViewsRemoved;
            _isDynamicViewManagerHooked = true;
        }

        private void TryUnhookDynamicViewManager()
        {
            if (!_isDynamicViewManagerHooked)
            {
                return;
            }

            var dynamicViewManager = PrismProvider.DynamicViewManager;
            if (dynamicViewManager != null)
            {
                dynamicViewManager.DynamicViewsRemoved -= OnDynamicViewsRemoved;
            }

            _isDynamicViewManagerHooked = false;
        }

        private void OnDynamicViewsRemoved(object? sender, DynamicViewsRemovedEventArgs e)
        {
            if (e?.RemovedViews == null || e.RemovedViews.Count == 0)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => HandleDynamicViewsRemoved(e.RemovedViews)));
                return;
            }

            HandleDynamicViewsRemoved(e.RemovedViews);
        }

        private void HandleDynamicViewsRemoved(IReadOnlyList<DynamicView> removedViews)
        {
            try
            {
                var matchedNodes = _layoutState.CollectLoadedNodesAcrossPages()
                    .Where(node => removedViews.Any(node.MatchesDynamicView))
                    .ToList();

                if (matchedNodes.Count == 0)
                {
                    return;
                }

                foreach (var node in matchedNodes)
                {
                    UnloadLoadedNode(node, persistLayout: false);
                }

                SaveLayout();
            }
            catch (Exception ex)
            {
                Logs.LogError($"同步移除动态视图失败：{ex.Message}");
            }
        }

        private void UnloadLoadedNode(RegionLayoutNode node, bool persistLayout = true)
        {
            if (node == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(node.RegionName)
                    && _regionManager.Regions.ContainsRegionWithName(node.RegionName))
                {
                    var region = _regionManager.Regions[node.RegionName];
                    foreach (var view in region.Views.Cast<object>().ToList())
                    {
                        region.Remove(view);
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.LogWarning($"卸载动态区域视图失败：{node.RegionName} - {ex.Message}");
            }

            node.ClearLoadedView();

            if (persistLayout)
            {
                SaveLayout();
            }
        }

        #endregion

        #region 保存与加载
        private void SaveLayout()
        {
            if (_isLoading) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(SaveLayout));
                return;
            }

            _hasPendingLayoutSave = true;
            var now = DateTime.UtcNow;
            var elapsed = now - _lastLayoutSaveAt;

            if (elapsed >= SaveLayoutThrottleInterval)
            {
                PersistLayout();
                return;
            }

            var remaining = SaveLayoutThrottleInterval - elapsed;
            _saveLayoutTimer.Stop();
            _saveLayoutTimer.Interval = remaining > TimeSpan.Zero ? remaining : SaveLayoutThrottleInterval;
            _saveLayoutTimer.Start();
        }

        private void OnSaveLayoutTimerTick(object? sender, EventArgs e)
        {
            _saveLayoutTimer.Stop();
            PersistLayout();
        }

        private void FlushPendingLayoutSave()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(FlushPendingLayoutSave);
                return;
            }

            _saveLayoutTimer.Stop();
            PersistLayout();
        }

        private void PersistLayout()
        {
            if (!_hasPendingLayoutSave)
                return;

            try
            {
                EnsureValidLayoutState();
                SyncCurrentPageRootNode();
                _layoutState.SelectedPageId = GetCurrentPage()?.Id ?? _layoutState.SelectedPageId;
                _layoutState.IsOperationPanelExpanded = PageOperationToggleButton.IsChecked == true;

                PrismProvider.ProjectManager.SolutionManager.UpdateItem(LayoutStoreKey, _layoutState);
                _lastLayoutSaveAt = DateTime.UtcNow;
                _hasPendingLayoutSave = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 保存布局失败: {ex.Message}");
            }
        }

        private void LoadLayout()
        {
            _isLoading = true;
            UpdateInteractionState("正在恢复区域布局，请稍候...");
            StopRestoringLoadedViews();
            ResetPageVisualTrees();

            try
            {
                object savedLayout = PrismProvider.ProjectManager.SolutionManager.GetItem(LayoutStoreKey);
                _layoutState = savedLayout switch
                {
                    DynamicRegionControlLayoutState layoutState => layoutState,
                    RegionLayoutNode legacyRoot => ConvertLegacyLayoutState(legacyRoot),
                    _ => CreateDefaultLayoutState()
                };

                EnsureValidLayoutState();
                var currentPage = GetCurrentPage();
                _rootNode = currentPage?.RootNode ?? new RegionLayoutNode();
                SetOperationPanelExpanded(_layoutState.IsOperationPanelExpanded);
                RefreshPageSelector();
                if (currentPage != null)
                {
                    ActivatePageVisual(currentPage, restoreLoadedViews: false);
                    QueueRestoreLoadedViews(currentPage);
                }
                Dispatcher.BeginInvoke(new Action(() => EnsureFloatingButtonPosition()), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 加载布局失败: {ex.Message}");
                _layoutState = CreateDefaultLayoutState();
                EnsureValidLayoutState();
                var currentPage = GetCurrentPage();
                _rootNode = currentPage?.RootNode ?? new RegionLayoutNode();
                SetOperationPanelExpanded(_layoutState.IsOperationPanelExpanded);
                RefreshPageSelector();
                if (currentPage != null)
                {
                    ActivatePageVisual(currentPage, restoreLoadedViews: false);
                    QueueRestoreLoadedViews(currentPage);
                }
                Dispatcher.BeginInvoke(new Action(() => EnsureFloatingButtonPosition()), DispatcherPriority.Loaded);
            }
            finally
            {
                _isLoading = false;
                UpdateInteractionState();
                InvalidateVisual();
                UpdateLayout();
            }
        }
        #endregion

        private void RebuildLayoutVisualTree()
        {
            var currentPage = GetCurrentPage();
            if (currentPage == null)
            {
                return;
            }

            currentPage.RootNode = _rootNode ?? new RegionLayoutNode();
            MainGrid = EnsurePageVisualTree(currentPage, rebuild: true);
            ActivatePageVisual(currentPage, restoreLoadedViews: false);
        }

        private void RemoveRegisteredRegions(string pageId = null)
        {
            IEnumerable<string> regionNames;
            if (string.IsNullOrWhiteSpace(pageId))
            {
                regionNames = _registeredRegions.ToList();
            }
            else if (_pageRegisteredRegions.TryGetValue(pageId, out var pageRegions))
            {
                regionNames = pageRegions.ToList();
            }
            else
            {
                return;
            }

            foreach (var regionName in regionNames)
            {
                try
                {
                    if (_regionManager.Regions.ContainsRegionWithName(regionName))
                    {
                        _regionManager.Regions.Remove(regionName);
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogWarning($"移除旧 Region 失败：{regionName} - {ex.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(pageId))
            {
                _registeredRegions.Clear();
                _pageRegisteredRegions.Clear();
                return;
            }

            if (_pageRegisteredRegions.TryGetValue(pageId, out var removedPageRegions))
            {
                foreach (var regionName in removedPageRegions)
                {
                    _registeredRegions.Remove(regionName);
                }

                _pageRegisteredRegions.Remove(pageId);
            }
        }

        private void QueueRestoreLoadedViews(DynamicRegionPageState? page = null)
        {
            StopRestoringLoadedViews();

            page ??= GetCurrentPage();
            if (page == null)
            {
                UpdateInteractionState();
                return;
            }

            _pendingRestorePageId = page.Id;
            _pendingLoadedNodes = page.RootNode?.CollectLoadedNodes() ?? [];
            UpdateInteractionState();
            if (_pendingLoadedNodes.Count == 0)
            {
                CompleteRestoreLoadedViews();
                return;
            }

            Dispatcher.BeginInvoke(new Action(TryRestoreLoadedViews), DispatcherPriority.Loaded);
        }

        private void OnRestoreLoadedViewsTimerTick(object? sender, EventArgs e)
        {
            _restoreLoadedViewsTimer.Stop();
            TryRestoreLoadedViews();
        }

        private void TryRestoreLoadedViews()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(TryRestoreLoadedViews));
                return;
            }

            if (_pendingLoadedNodes.Count == 0)
            {
                CompleteRestoreLoadedViews();
                return;
            }

            UpdateLayout();
            RegionManager.UpdateRegions();

            var pendingNodes = new List<RegionLayoutNode>();
            foreach (var node in _pendingLoadedNodes)
            {
                if (!TryRestoreLoadedNode(node))
                {
                    pendingNodes.Add(node);
                }
            }

            _pendingLoadedNodes = pendingNodes;
            UpdateInteractionState();
            if (_pendingLoadedNodes.Count == 0)
            {
                CompleteRestoreLoadedViews();
                return;
            }

            _restoreLoadedViewsAttempt++;
            if (_restoreLoadedViewsAttempt >= RestoreLoadedViewsMaxAttempts)
            {
                foreach (var node in _pendingLoadedNodes)
                {
                    Logs.LogWarning($"布局视图恢复超时：{node.LoadedViewName} -> {node.RegionName}");
                }

                StopRestoringLoadedViews();
                return;
            }

            _restoreLoadedViewsTimer.Start();
        }

        private bool TryRestoreLoadedNode(RegionLayoutNode node)
        {
            var request = node.CreateLoadRequest();
            if (!request.IsValid)
                return true;

            if (!_regionManager.Regions.ContainsRegionWithName(request.RegionName))
                return false;

            if (!CanRestoreNodeView(request))
                return false;

            try
            {
                return TryBeginLoadView(request, false);
            }
            catch (Exception ex)
            {
                Logs.LogError($"恢复布局视图失败：{request.ViewName} - {ex.Message}");
                return false;
            }
        }

        private bool CanRestoreNodeView(DynamicRegionViewLoadRequest request)
        {
            return CanLoadView(request, out _);
        }

        private void CompleteRestoreLoadedViews()
        {
            if (!string.IsNullOrWhiteSpace(_pendingRestorePageId))
            {
                _restoredPageIds.Add(_pendingRestorePageId);
            }

            _restoreLoadedViewsTimer.Stop();
            _restoreLoadedViewsAttempt = 0;
            _pendingLoadedNodes.Clear();
            _pendingRestorePageId = string.Empty;
            UpdateInteractionState();
        }

        private bool HasNodeParamCache(int serial)
        {
            if (serial < 0)
                return false;

            var caches = PrismProvider.ProjectManager.SltCurSolutionItem?.NodeParamCaches;
            if (caches == null)
                return false;

            return caches.ContainsKey(serial.ToString("D3")) || caches.ContainsKey(serial.ToString());
        }

        private bool CanLoadView(DynamicRegionViewLoadRequest request, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!request.IsValid)
            {
                errorMessage = "视图请求参数无效";
                return false;
            }

            switch (request.Type)
            {
                case DynamicViewType.General:
                    if (FindDynamicView(request) == null)
                    {
                        errorMessage = "未找到对应的通用视图";
                        return false;
                    }
                    return true;
                case DynamicViewType.Custom:
                    if (FindDynamicView(request) == null)
                    {
                        errorMessage = "未找到对应的自定义视图";
                        return false;
                    }
                    return true;
                case DynamicViewType.NodeMap:
                    if (request.Serial < 0)
                    {
                        errorMessage = "节点映射视图缺少节点序号";
                        return false;
                    }

                    if (!HasNodeParamCache(request.Serial))
                    {
                        errorMessage = $"未找到节点 {request.Serial:D3} 的缓存信息";
                        return false;
                    }

                    if (FindDynamicView(request) == null)
                    {
                        errorMessage = "未找到对应的节点映射视图";
                        return false;
                    }

                    return true;
                default:
                    errorMessage = "未知的视图类型";
                    return false;
            }
        }

        private DynamicView? FindDynamicView(DynamicRegionViewLoadRequest request)
        {
            var dynamicViews = PrismProvider.DynamicViewManager?.DynamicViews;
            if (dynamicViews == null)
                return null;

            return dynamicViews.FirstOrDefault(view =>
                view.Type == request.Type &&
                string.Equals(view.ViewName, request.ViewName, StringComparison.OrdinalIgnoreCase) &&
                (request.Type != DynamicViewType.Custom ||
                 string.Equals(view.Subjection ?? string.Empty, request.Subjection ?? string.Empty, StringComparison.OrdinalIgnoreCase)) &&
                (request.Type != DynamicViewType.NodeMap || view.NodeSerial == request.Serial));
        }

        private void StopRestoringLoadedViews()
        {
            _restoreLoadedViewsTimer.Stop();
            _restoreLoadedViewsAttempt = 0;
            _pendingLoadedNodes.Clear();
            _pendingRestorePageId = string.Empty;
            UpdateInteractionState();
        }

        private void PageListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressPageSelectionChanged)
            {
                return;
            }

            if (PageListBox.SelectedItem is not DynamicRegionPageState selectedPage)
            {
                return;
            }

            if (selectedPage.Id == GetCurrentPage()?.Id)
            {
                return;
            }

            if (!EnsureInteractionReady())
            {
                RefreshPageSelector();
                return;
            }

            SwitchToPage(selectedPage);
        }

        private void PreviousPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            SwitchPageByOffset(-1);
        }

        private void NextPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            SwitchPageByOffset(1);
        }

        private void AddPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!EnsureInteractionReady())
            {
                return;
            }

            if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
            {
                MessageView.Ins.MessageBoxShow("您无权限操作!", eMsgType.Info);
                return;
            }

            var addResult = MessageBox.Show(
                "确定要新建页面吗？此操作不可撤回。",
                "新建页面",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (addResult != MessageBoxResult.Yes)
            {
                RefreshPageSelector();
                return;
            }

            EnsureValidLayoutState();
            var newPage = CreateNewPageState(_layoutState.Pages.Count + 1);
            _layoutState.Pages.Add(newPage);
            EnsureValidLayoutState();
            SwitchToPage(newPage);

            MessageView.Ins.MessageBoxShow($"已新增 {newPage.Name}", eMsgType.Success);
        }

        private void DeletePageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!EnsureInteractionReady())
            {
                return;
            }

            if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
            {
                MessageView.Ins.MessageBoxShow("您无权限操作!", eMsgType.Info);
                return;
            }

            EnsureValidLayoutState();
            if (_layoutState.Pages.Count <= 1)
            {
                MessageView.Ins.MessageBoxShow("至少需要保留一个页面。", eMsgType.Info);
                return;
            }

            var currentPage = GetCurrentPage();
            if (currentPage == null)
            {
                return;
            }

            string pageName = currentPage.Name;
            var result = MessageBox.Show(
                $"确定要删除“{pageName}”吗？此操作不可撤回，页面布局和已加载视图状态都会被移除。",
                "删除页面",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                RefreshPageSelector();
                return;
            }

            int currentIndex = TryGetCurrentPageIndex();
            RemovePageVisualTree(currentPage.Id);
            _layoutState.Pages.Remove(currentPage);
            EnsureValidLayoutState();

            int targetIndex = Math.Min(currentIndex, _layoutState.Pages.Count - 1);
            SwitchToPage(_layoutState.Pages[targetIndex]);

            MessageView.Ins.MessageBoxShow($"已删除 {pageName}", eMsgType.Success);
        }

        private void FloatingLayer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureFloatingButtonPosition();
                UpdateFloatingPanelPlacement();
            }), DispatcherPriority.Loaded);
        }

        private void FloatingOverlayElement_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureFloatingButtonPosition();
                UpdateFloatingPanelPlacement();
            }), DispatcherPriority.Loaded);
        }

        private void FloatingDragHandle_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FloatingLayer == null || PageOperationBorder == null)
            {
                return;
            }

            if (sender is not UIElement dragHandle)
            {
                return;
            }

            EnsureFloatingButtonPosition();

            _floatingDragHandle = dragHandle;
            _isFloatingDragPending = true;
            _isDraggingFloatingHost = false;
            _floatingDragMoved = false;
            _isFloatingToggleClickCandidate = ReferenceEquals(dragHandle, PageOperationBorder);
            _floatingDragStartPoint = e.GetPosition(FloatingLayer);
            _floatingDragStartLeft = Canvas.GetLeft(PageOperationBorder);
            _floatingDragStartTop = Canvas.GetTop(PageOperationBorder);

            if (double.IsNaN(_floatingDragStartLeft))
            {
                _floatingDragStartLeft = GetDefaultFloatingButtonLeft();
            }

            if (double.IsNaN(_floatingDragStartTop))
            {
                _floatingDragStartTop = GetDefaultFloatingButtonTop();
            }

            _floatingDragCurrentLeft = _floatingDragStartLeft;
            _floatingDragCurrentTop = _floatingDragStartTop;
            ClearFloatingDragVisual();
            _floatingDragHandle.CaptureMouse();
            e.Handled = true;
        }

        private void FloatingDragHandle_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isFloatingDragPending || FloatingLayer == null || _floatingDragHandle == null)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                ResetFloatingDragState();
                return;
            }

            Point currentPoint = e.GetPosition(FloatingLayer);
            double deltaX = currentPoint.X - _floatingDragStartPoint.X;
            double deltaY = currentPoint.Y - _floatingDragStartPoint.Y;

            if (!_isDraggingFloatingHost)
            {
                if (Math.Abs(deltaX) < SystemParameters.MinimumHorizontalDragDistance
                    && Math.Abs(deltaY) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _isDraggingFloatingHost = true;
                _floatingDragMoved = true;
            }

            ApplyFloatingDragVisual(_floatingDragStartLeft + deltaX, _floatingDragStartTop + deltaY);
            e.Handled = true;
        }

        private void FloatingDragHandle_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool moved = _floatingDragMoved;
            bool isToggleClickCandidate = _isFloatingToggleClickCandidate && !moved;
            double finalLeft = _floatingDragCurrentLeft;
            double finalTop = _floatingDragCurrentTop;
            ResetFloatingDragState();

            if (moved)
            {
                SetFloatingButtonPosition(finalLeft, finalTop, persistLayout: false);
                SaveLayout();
                e.Handled = true;
                return;
            }

            if (isToggleClickCandidate)
            {
                PageOperationToggleButton.IsChecked = PageOperationToggleButton.IsChecked != true;
                e.Handled = true;
            }
        }

        private void FloatingDragHandle_OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            ResetFloatingDragState();
        }

        private void PageOperationToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingOperationPanelState || _isLoading)
            {
                return;
            }

            EnsureValidLayoutState();
            _layoutState.IsOperationPanelExpanded = true;
            PageOperationPanel.Visibility = Visibility.Visible;
            PageOperationPanel.IsHitTestVisible = true;
            UpdateFloatingOperationButtonState();
            Dispatcher.BeginInvoke(new Action(UpdateFloatingPanelPlacement), DispatcherPriority.Loaded);
            SaveLayout();
        }

        private void PageOperationToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingOperationPanelState || _isLoading)
            {
                return;
            }

            EnsureValidLayoutState();
            _layoutState.IsOperationPanelExpanded = false;
            PageOperationPanel.Visibility = Visibility.Collapsed;
            PageOperationPanel.IsHitTestVisible = false;
            UpdateFloatingOperationButtonState();
            Dispatcher.BeginInvoke(new Action(UpdateFloatingPanelPlacement), DispatcherPriority.Loaded);
            SaveLayout();
        }

        /// <summary>
        /// 重置面板
        /// </summary>
        private void ResetLayout()
        {
            _rootNode = new RegionLayoutNode();
            SyncCurrentPageRootNode();
            RebuildAndReloadLayout();
        }
    }

}
