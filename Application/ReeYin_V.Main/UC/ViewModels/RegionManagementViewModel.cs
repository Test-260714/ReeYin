using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Main.UC.Models;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LayoutSplitDirection = ReeYin_V.Main.UC.Models.SplitDirection;

namespace ReeYin_V.Main.UC.ViewModels
{
    public class DynamicViewInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public int NodeSerial { get; set; } = -1;
        public string Subjection { get; set; } = string.Empty;
        public DynamicViewType Type { get; set; } = DynamicViewType.General;
        public string TypeDisplayName { get; set; } = string.Empty;
        public string TypeDescription { get; set; } = string.Empty;
        public string TypeAccentColor { get; set; } = "#607D8B";
        public string ControlInfo { get; set; } = string.Empty;
        public string AvailabilityText { get; set; } = string.Empty;
        public string AvailabilityShortText { get; set; } = string.Empty;
        public string AvailabilityForeground { get; set; } = "#1E6F45";
        public string AvailabilityBackground { get; set; } = "#E8F5EC";
        public string ListIdentityText { get; set; } = string.Empty;
        public bool CanLoad { get; set; }
        public int TypeSortOrder { get; set; }

        public DynamicRegionViewLoadRequest ToLoadRequest(string regionName)
        {
            return new DynamicRegionViewLoadRequest
            {
                Serial = NodeSerial,
                RegionName = regionName ?? string.Empty,
                ViewName = ViewName,
                DisplayName = DisplayName,
                Subjection = Subjection ?? string.Empty,
                Type = Type
            };
        }
    }

    public class RegionManagementViewModel : DialogViewModelBase
    {
        #region Fields
        private RegionLayoutNode _currentNode;
        private RegionLayoutNode _rootNode;
        private Action<DynamicRegionViewLoadRequest> _onViewLoaded;
        private Action _onLayoutChanged;
        #endregion

        #region Properties

        private string _currentRegionName = "未知";
        public string CurrentRegionName
        {
            get => _currentRegionName;
            set
            {
                _currentRegionName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedViewLoadHint));
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
                FilterViews();
            }
        }

        private string _selectedTypeFilter = "全部";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                _selectedTypeFilter = value;
                RaisePropertyChanged();
                FilterViews();
            }
        }

        private ObservableCollection<DynamicViewInfo> _allViews;
        public ObservableCollection<DynamicViewInfo> AllViews
        {
            get => _allViews;
            set { _allViews = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<DynamicViewInfo> _filteredViews;
        public ObservableCollection<DynamicViewInfo> FilteredViews
        {
            get => _filteredViews;
            set
            {
                _filteredViews = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilteredViewCount));
            }
        }

        private ObservableCollection<string> _typeFilters;
        public ObservableCollection<string> TypeFilters
        {
            get => _typeFilters;
            set { _typeFilters = value; RaisePropertyChanged(); }
        }

        private DynamicViewInfo _selectedView;
        public DynamicViewInfo SelectedView
        {
            get => _selectedView;
            set
            {
                _selectedView = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedView));
                RaisePropertyChanged(nameof(SelectedViewDisplayName));
                RaisePropertyChanged(nameof(SelectedViewTypeDisplayName));
                RaisePropertyChanged(nameof(SelectedViewTypeDescription));
                RaisePropertyChanged(nameof(SelectedViewControlInfo));
                RaisePropertyChanged(nameof(SelectedViewAvailabilityText));
                RaisePropertyChanged(nameof(SelectedViewName));
                RaisePropertyChanged(nameof(SelectedViewSubjection));
                RaisePropertyChanged(nameof(SelectedViewIdentityTitle));
                RaisePropertyChanged(nameof(SelectedViewLoadHint));
                RaisePropertyChanged(nameof(CanLoadSelectedView));
                RaisePropertyChanged(nameof(CanExecuteLoadSelectedView));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; RaisePropertyChanged(); }
        }

        public int GeneralViewCount => AllViews.Count(v => v.Type == DynamicViewType.General);
        public int CustomViewCount => AllViews.Count(v => v.Type == DynamicViewType.Custom);
        public int NodeMapViewCount => AllViews.Count(v => v.Type == DynamicViewType.NodeMap);
        public int FilteredViewCount => FilteredViews?.Count ?? 0;

        public bool HasSelectedView => SelectedView != null;
        public bool CanLoadSelectedView => SelectedView?.CanLoad ?? false;
        public bool CanLoadIntoCurrentRegion => _currentNode?.SplitDirection == LayoutSplitDirection.None;
        public bool CanExecuteLoadSelectedView => CanLoadIntoCurrentRegion && CanLoadSelectedView;
        public bool CanSplitCurrentRegion => _currentNode?.SplitDirection == LayoutSplitDirection.None;
        public bool CanMergeCurrentRegion => _currentNode?.SplitDirection != LayoutSplitDirection.None;

        public string CurrentRegionState =>
            _currentNode?.SplitDirection == LayoutSplitDirection.None ? "当前区域可直接加载单视图" : "当前区域已拆分，请先合并后再加载视图";

        public string CurrentLoadedViewDisplayName =>
            _currentNode?.SplitDirection != LayoutSplitDirection.None
                ? "容器节点（由子区域承载视图）"
                : (string.IsNullOrWhiteSpace(_currentNode?.LoadedDisplayName)
                    ? (string.IsNullOrWhiteSpace(_currentNode?.LoadedViewName) ? "未加载视图" : _currentNode.LoadedViewName)
                    : _currentNode.LoadedDisplayName);

        public string CurrentLoadedViewTypeDisplayName =>
            _currentNode?.SplitDirection != LayoutSplitDirection.None
                ? "容器节点"
                : string.IsNullOrWhiteSpace(_currentNode?.LoadedViewName) ? "无" : GetTypeDisplayName(_currentNode.Type);

        public string CurrentLoadedViewControlInfo
        {
            get
            {
                if (_currentNode == null)
                    return "当前区域未初始化";

                if (_currentNode.SplitDirection != LayoutSplitDirection.None)
                    return "当前区域已拆分为子区域，当前节点不直接承载视图控件";

                if (string.IsNullOrWhiteSpace(_currentNode.LoadedViewName))
                    return "当前区域尚未绑定任何控件";

                return BuildControlInfo(_currentNode.CreateLoadRequest());
            }
        }

        public string SelectedViewDisplayName => SelectedView?.DisplayName ?? "未选择视图";
        public string SelectedViewTypeDisplayName => SelectedView?.TypeDisplayName ?? "无";
        public string SelectedViewTypeDescription => SelectedView?.TypeDescription ?? "请在列表中选择一个可用视图";
        public string SelectedViewControlInfo => SelectedView?.ControlInfo ?? "暂无控件信息";
        public string SelectedViewAvailabilityText => SelectedView?.AvailabilityText ?? "未进行视图校验";
        public string SelectedViewName => SelectedView?.ViewName ?? "-";
        public string SelectedViewSubjection => SelectedView switch
        {
            null => "-",
            { Type: DynamicViewType.Custom } view => string.IsNullOrWhiteSpace(view.Subjection) ? "未设置归属模块" : view.Subjection,
            { Type: DynamicViewType.NodeMap } view => view.NodeSerial < 0 ? "节点序号未设置" : $"节点 {view.NodeSerial:D3}",
            _ => "通用视图，无额外归属"
        };

        public string SelectedViewIdentityTitle => SelectedView switch
        {
            { Type: DynamicViewType.Custom } => "归属模块",
            { Type: DynamicViewType.NodeMap } => "节点信息",
            { Type: DynamicViewType.General } => "适用范围",
            _ => "归属 / 节点"
        };

        public string SelectedViewLoadHint
        {
            get
            {
                if (SelectedView == null)
                    return "请先在左侧列表中选择一个视图，再查看详细信息或执行加载。";

                if (!CanLoadIntoCurrentRegion)
                    return "当前区域已拆分为容器节点，请先合并区域后再加载视图。";

                if (!SelectedView.CanLoad)
                    return $"{SelectedView.AvailabilityText}，暂时不能加载到当前区域。";

                return $"当前区域满足加载条件，可将该视图直接加载到区域 {CurrentRegionName}。";
            }
        }

        #endregion

        #region Constructor

        public RegionManagementViewModel()
        {
            AllViews = new ObservableCollection<DynamicViewInfo>();
            FilteredViews = new ObservableCollection<DynamicViewInfo>();
            TypeFilters = new ObservableCollection<string> { "全部", "通用", "定制", "节点映射" };
        }

        #endregion

        #region Methods

        private static string GetTypeDisplayName(DynamicViewType type)
        {
            return type switch
            {
                DynamicViewType.General => "通用",
                DynamicViewType.Custom => "定制",
                DynamicViewType.NodeMap => "节点映射",
                _ => "未知"
            };
        }

        private static string GetTypeDescription(DynamicViewType type)
        {
            return type switch
            {
                DynamicViewType.General => "全局可复用视图，适合日志、状态、操作面板等公共模块。",
                DynamicViewType.Custom => "业务定制视图，可按模块归属快速定位对应页面。",
                DynamicViewType.NodeMap => "节点映射视图，依赖节点序号和节点缓存信息。",
                _ => "未识别的视图类型。"
            };
        }

        private static string GetTypeAccentColor(DynamicViewType type)
        {
            return type switch
            {
                DynamicViewType.General => "#546E7A",
                DynamicViewType.Custom => "#00897B",
                DynamicViewType.NodeMap => "#FB8C00",
                _ => "#607D8B"
            };
        }

        private static int GetTypeSortOrder(DynamicViewType type)
        {
            return type switch
            {
                DynamicViewType.General => 0,
                DynamicViewType.Custom => 1,
                DynamicViewType.NodeMap => 2,
                _ => 99
            };
        }

        private static bool MatchesCurrentTypeFilter(string filter, DynamicViewInfo view)
        {
            return filter switch
            {
                "通用" => view.Type == DynamicViewType.General,
                "定制" => view.Type == DynamicViewType.Custom,
                "节点映射" => view.Type == DynamicViewType.NodeMap,
                _ => true
            };
        }

        private static string BuildControlInfo(DynamicRegionViewLoadRequest request)
        {
            return request.Type switch
            {
                DynamicViewType.General => "控件信息：通用视图，无额外绑定要求",
                DynamicViewType.Custom => string.IsNullOrWhiteSpace(request.Subjection)
                    ? "控件信息：定制视图，未设置归属模块"
                    : $"控件信息：定制视图，归属 {request.Subjection}",
                DynamicViewType.NodeMap => request.Serial < 0
                    ? "控件信息：节点映射视图，但未提供节点序号"
                    : $"控件信息：节点映射视图，节点序号 {request.Serial:D3}",
                _ => "控件信息：未知"
            };
        }

        private static string BuildListIdentityText(DynamicView view)
        {
            return view.Type switch
            {
                DynamicViewType.General => "公共复用",
                DynamicViewType.Custom => string.IsNullOrWhiteSpace(view.Subjection) ? "未设置归属" : view.Subjection,
                DynamicViewType.NodeMap => view.NodeSerial < 0 ? "节点未设置" : $"节点 {view.NodeSerial:D3}",
                _ => "-"
            };
        }

        private static (string ShortText, string Foreground, string Background) BuildAvailabilityBadge(
            DynamicViewType type,
            bool canLoad)
        {
            return type switch
            {
                DynamicViewType.NodeMap when canLoad => ("已就绪", "#9A3412", "#FFF1E7"),
                DynamicViewType.NodeMap => ("缺缓存", "#B42318", "#FEECEC"),
                DynamicViewType.Custom => ("可加载", "#0F766E", "#E8F7F3"),
                DynamicViewType.General => ("可加载", "#1D4ED8", "#EAF1FF"),
                _ when canLoad => ("可加载", "#1E6F45", "#E8F5EC"),
                _ => ("待检查", "#6B7280", "#F3F4F6")
            };
        }

        private bool HasNodeParamCache(int serial)
        {
            if (serial < 0)
                return false;

            var caches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
            if (caches == null)
                return false;

            return caches.ContainsKey(serial.ToString("D3")) || caches.ContainsKey(serial.ToString());
        }

        private DynamicViewInfo BuildDynamicViewInfo(DynamicView view)
        {
            var request = new DynamicRegionViewLoadRequest
            {
                Serial = view.NodeSerial,
                ViewName = view.ViewName,
                DisplayName = view.DisplayName,
                Subjection = view.Subjection,
                Type = view.Type
            };

            bool canLoad = true;
            string availabilityText = "状态：可直接加载";

            switch (view.Type)
            {
                case DynamicViewType.Custom:
                    availabilityText = string.IsNullOrWhiteSpace(view.Subjection)
                        ? "状态：定制视图，可加载，未标记归属模块"
                        : $"状态：定制视图，可加载，归属 {view.Subjection}";
                    break;
                case DynamicViewType.NodeMap:
                    canLoad = view.NodeSerial >= 0 && HasNodeParamCache(view.NodeSerial);
                    availabilityText = canLoad
                        ? $"状态：节点 {view.NodeSerial:D3} 已就绪，可加载"
                        : $"状态：节点 {view.NodeSerial:D3} 缓存缺失，当前不可加载";
                    break;
            }

            var availabilityBadge = BuildAvailabilityBadge(view.Type, canLoad);

            return new DynamicViewInfo
            {
                DisplayName = string.IsNullOrWhiteSpace(view.DisplayName) ? view.ViewName : view.DisplayName,
                ViewName = view.ViewName,
                NodeSerial = view.NodeSerial,
                Subjection = view.Subjection ?? string.Empty,
                Type = view.Type,
                TypeDisplayName = GetTypeDisplayName(view.Type),
                TypeDescription = GetTypeDescription(view.Type),
                TypeAccentColor = GetTypeAccentColor(view.Type),
                TypeSortOrder = GetTypeSortOrder(view.Type),
                ListIdentityText = BuildListIdentityText(view),
                ControlInfo = BuildControlInfo(request),
                AvailabilityText = availabilityText,
                AvailabilityShortText = availabilityBadge.ShortText,
                AvailabilityForeground = availabilityBadge.Foreground,
                AvailabilityBackground = availabilityBadge.Background,
                CanLoad = canLoad
            };
        }

        private void RefreshCurrentNodeState()
        {
            RaisePropertyChanged(nameof(CurrentRegionState));
            RaisePropertyChanged(nameof(CurrentLoadedViewDisplayName));
            RaisePropertyChanged(nameof(CurrentLoadedViewTypeDisplayName));
            RaisePropertyChanged(nameof(CurrentLoadedViewControlInfo));
            RaisePropertyChanged(nameof(CanLoadIntoCurrentRegion));
            RaisePropertyChanged(nameof(CanExecuteLoadSelectedView));
            RaisePropertyChanged(nameof(CanSplitCurrentRegion));
            RaisePropertyChanged(nameof(CanMergeCurrentRegion));
            RaisePropertyChanged(nameof(SelectedViewLoadHint));
        }

        private void FilterViews()
        {
            var filtered = AllViews
                .Where(v => MatchesCurrentTypeFilter(SelectedTypeFilter, v))
                .Where(v =>
                    string.IsNullOrWhiteSpace(SearchText) ||
                    v.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    v.ViewName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    v.ListIdentityText.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    v.ControlInfo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    v.TypeDisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.TypeSortOrder)
                .ThenBy(v => v.DisplayName)
                .ThenBy(v => v.ViewName)
                .ToList();

            FilteredViews = new ObservableCollection<DynamicViewInfo>(filtered);

            if (SelectedView != null && FilteredViews.Any(v =>
                    v.ViewName == SelectedView.ViewName &&
                    v.Type == SelectedView.Type &&
                    v.NodeSerial == SelectedView.NodeSerial))
            {
                SelectedView = FilteredViews.First(v =>
                    v.ViewName == SelectedView.ViewName &&
                    v.Type == SelectedView.Type &&
                    v.NodeSerial == SelectedView.NodeSerial);
            }
            else
            {
                SelectedView = FilteredViews.FirstOrDefault();
            }
        }

        private void LoadAvailableViews()
        {
            try
            {
                AllViews.Clear();

                var dynamicViews = PrismProvider.DynamicViewManager?.DynamicViews ?? new List<DynamicView>();
                var distinctViews = dynamicViews
                    .Where(view => view != null && !string.IsNullOrWhiteSpace(view.ViewName))
                    .GroupBy(view => $"{view.Type}|{view.ViewName}|{view.NodeSerial}|{view.Subjection}")
                    .Select(group => group.First())
                    .OrderBy(view => GetTypeSortOrder(view.Type))
                    .ThenBy(view => view.DisplayName)
                    .ThenBy(view => view.ViewName)
                    .ToList();

                foreach (var view in distinctViews)
                {
                    AllViews.Add(BuildDynamicViewInfo(view));
                }

                RaisePropertyChanged(nameof(GeneralViewCount));
                RaisePropertyChanged(nameof(CustomViewCount));
                RaisePropertyChanged(nameof(NodeMapViewCount));

                FilterViews();
                TrySelectCurrentLoadedView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载视图列表失败：{ex.Message}");
            }
        }

        private void TrySelectCurrentLoadedView()
        {
            if (_currentNode == null || string.IsNullOrWhiteSpace(_currentNode.LoadedViewName))
                return;

            var currentView = FilteredViews.FirstOrDefault(view =>
                string.Equals(view.ViewName, _currentNode.LoadedViewName, StringComparison.OrdinalIgnoreCase) &&
                view.Type == _currentNode.Type &&
                (view.Type != DynamicViewType.NodeMap || view.NodeSerial == _currentNode.Serial) &&
                (view.Type != DynamicViewType.Custom || string.Equals(view.Subjection, _currentNode.Subjection, StringComparison.OrdinalIgnoreCase)));

            if (currentView != null)
            {
                SelectedView = currentView;
            }
        }

        private void ExecuteLoadView(DynamicViewInfo viewInfo)
        {
            TryExecuteLoadView(viewInfo);
        }

        private static bool ConfirmOperation(
            string message,
            string title = "操作确认",
            MessageBoxImage icon = MessageBoxImage.Question)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                icon) == MessageBoxResult.Yes;
        }

        private void CloseCurrentPage()
        {
            CloseDialog(ButtonResult.Cancel);
        }

        private void ExecuteLoadSelectedView()
        {
            if (!ConfirmOperation("确定要将选中视图加载到当前区域吗？"))
                return;

            if (TryExecuteLoadView(SelectedView))
            {
                CloseCurrentPage();
            }
        }

        private bool TryExecuteLoadView(DynamicViewInfo viewInfo)
        {
            if (viewInfo == null)
            {
                MessageView.Ins.MessageBoxShow("视图信息为空", eMsgType.Warn);
                return false;
            }

            SelectedView = viewInfo;

            if (_currentNode == null)
            {
                MessageView.Ins.MessageBoxShow("当前节点为空", eMsgType.Warn);
                return false;
            }

            if (_currentNode.SplitDirection != LayoutSplitDirection.None)
            {
                MessageView.Ins.MessageBoxShow("当前区域已拆分，请先合并后再加载视图", eMsgType.Warn);
                return false;
            }

            if (!viewInfo.CanLoad)
            {
                MessageView.Ins.MessageBoxShow(viewInfo.AvailabilityText, eMsgType.Warn);
                return false;
            }

            try
            {
                var request = viewInfo.ToLoadRequest(_currentNode.RegionName);
                _onViewLoaded?.Invoke(request);
                _currentNode.ApplyLoadRequest(request);
                RefreshCurrentNodeState();
                MessageView.Ins.MessageBoxShow($"视图 '{viewInfo.DisplayName}' 已加载", eMsgType.Success);
                return true;
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"加载视图失败：{ex.Message}", eMsgType.Error);
                Console.WriteLine($"加载视图失败：{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        #endregion

        #region Commands

        public DelegateCommand SplitHorizontalCommand => new DelegateCommand(() =>
        {
            if (_currentNode == null)
            {
                MessageView.Ins.MessageBoxShow("当前节点为空", eMsgType.Warn);
                return;
            }

            if (!ConfirmOperation("确定要上下拆分当前区域吗？"))
                return;

            try
            {
                _currentNode.Split(LayoutSplitDirection.Horizontal);

                _onLayoutChanged?.Invoke();
                RefreshCurrentNodeState();

                MessageView.Ins.MessageBoxShow("区域已上下拆分", eMsgType.Success);
                CloseCurrentPage();
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"拆分失败：{ex.Message}", eMsgType.Error);
            }
        });

        public DelegateCommand SplitVerticalCommand => new DelegateCommand(() =>
        {
            if (_currentNode == null)
            {
                MessageView.Ins.MessageBoxShow("当前节点为空", eMsgType.Warn);
                return;
            }

            if (!ConfirmOperation("确定要左右拆分当前区域吗？"))
                return;

            try
            {
                _currentNode.Split(LayoutSplitDirection.Vertical);

                _onLayoutChanged?.Invoke();
                RefreshCurrentNodeState();

                MessageView.Ins.MessageBoxShow("区域已左右拆分", eMsgType.Success);
                CloseCurrentPage();
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"拆分失败：{ex.Message}", eMsgType.Error);
            }
        });

        public DelegateCommand MergeRegionCommand => new DelegateCommand(() =>
        {
            if (_currentNode == null)
            {
                MessageView.Ins.MessageBoxShow("当前节点为空", eMsgType.Warn);
                return;
            }

            if (_currentNode.SplitDirection == LayoutSplitDirection.None)
            {
                MessageView.Ins.MessageBoxShow("当前区域未拆分，无需合并", eMsgType.Warn);
                return;
            }

            if (!ConfirmOperation("确定要合并当前区域吗？"))
                return;

            try
            {
                _currentNode.Merge();

                _onLayoutChanged?.Invoke();
                RefreshCurrentNodeState();

                MessageView.Ins.MessageBoxShow("区域已合并", eMsgType.Success);
                CloseCurrentPage();
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"合并失败：{ex.Message}", eMsgType.Error);
            }
        });

        public DelegateCommand ResetLayoutCommand => new DelegateCommand(() =>
        {
            if (!ConfirmOperation(
                    "确定要重置整个布局吗？此操作将清除所有拆分和加载的视图。",
                    "确认重置",
                    MessageBoxImage.Warning))
                return;

            try
            {
                if (_rootNode != null)
                {
                    _rootNode.Merge();
                }

                _onLayoutChanged?.Invoke();
                RefreshCurrentNodeState();

                MessageView.Ins.MessageBoxShow("布局已重置", eMsgType.Success);
                CloseCurrentPage();
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"重置失败：{ex.Message}", eMsgType.Error);
            }
        });

        public DelegateCommand<DynamicViewInfo> SelectViewCommand => new DelegateCommand<DynamicViewInfo>(viewInfo =>
        {
            if (viewInfo != null)
            {
                SelectedView = viewInfo;
            }
        });

        public DelegateCommand<DynamicViewInfo> LoadViewCommand => new DelegateCommand<DynamicViewInfo>(ExecuteLoadView);

        public DelegateCommand LoadSelectedViewCommand => new DelegateCommand(() =>
        {
            ExecuteLoadSelectedView();
        });

        public DelegateCommand CloseCommand => new DelegateCommand(() =>
        {
            CloseDialog(ButtonResult.Cancel);
        });

        #endregion

        #region IDialogAware


        public override async void OnDialogOpened(IDialogParameters parameters)
        {
            IsLoading = true;
            try
            {
                base.OnDialogOpened(parameters);
                _currentNode = parameters.GetValue<RegionLayoutNode>("Node");
                _rootNode = parameters.GetValue<RegionLayoutNode>("RootNode");
                _onViewLoaded = parameters.GetValue<Action<DynamicRegionViewLoadRequest>>("OnViewLoaded");
                _onLayoutChanged = parameters.GetValue<Action>("OnLayoutChanged");

                CurrentRegionName = _currentNode?.RegionName ?? "未知";
                RefreshCurrentNodeState();
                await Task.Delay(50);
                LoadAvailableViews();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化对话框失败：{ex.Message}\n{ex.StackTrace}");
                MessageView.Ins.MessageBoxShow($"初始化失败：{ex.Message}", eMsgType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}
