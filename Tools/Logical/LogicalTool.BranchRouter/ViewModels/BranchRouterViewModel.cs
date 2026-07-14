using LogicalTool.BranchRouter.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace LogicalTool.BranchRouter.ViewModels
{
    [Serializable]
    public class BranchTargetNodeInfo
    {
        public int Serial { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DisplayName => $"{Serial:D3}_{Title}";
    }

    [Serializable]
    public class BranchRouterViewModel : DialogViewModelBase, IViewModuleParam
    {
        public new BranchRouterModel ModelParam
        {
            get => base.ModelParam as BranchRouterModel;
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private BranchRouteRule _selectedRule;
        public BranchRouteRule SelectedRule
        {
            get => _selectedRule;
            set { _selectedRule = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public Array ParamSources { get; } = Enum.GetValues(typeof(BranchParamSource));

        [JsonIgnore]
        public Array CompareTypes { get; } = Enum.GetValues(typeof(BranchCompareType));

        [JsonIgnore]
        public Array MatchModes { get; } = Enum.GetValues(typeof(BranchMatchMode));

        [JsonIgnore]
        public Array DataTypes { get; } = Enum.GetValues(typeof(DataType));

        [JsonIgnore]
        private ObservableCollection<BranchTargetNodeInfo> _connectedNodes = new ObservableCollection<BranchTargetNodeInfo>();
        public ObservableCollection<BranchTargetNodeInfo> ConnectedNodes
        {
            get => _connectedNodes;
            set { _connectedNodes = value; RaisePropertyChanged(); }
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<BranchRouterModel>();
            RefreshConnectedNodes();
        }

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            RefreshConnectedNodes();
            ModelParam?.LoadKeyParam();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam }
                });
            }
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "添加规则":
                    AddRule();
                    break;
                case "删除规则":
                    RemoveRule();
                    break;
                case "刷新节点":
                    RefreshConnectedNodes();
                    break;
                case "取消":
                    CloseDialog(ButtonResult.Cancel, new DialogParameters());
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam }
                    });
                    break;
            }
        });

        private void AddRule()
        {
            var rule = new BranchRouteRule();
            if (ConnectedNodes.Count > 0)
            {
                rule.TargetNodeSerial = ConnectedNodes[0].Serial;
            }

            ModelParam.Rules.Add(rule);
            SelectedRule = rule;
        }

        private void RemoveRule()
        {
            if (SelectedRule == null)
            {
                return;
            }

            ModelParam.Rules.Remove(SelectedRule);
            SelectedRule = null;
        }

        private void RefreshConnectedNodes()
        {
            ConnectedNodes.Clear();
            var currentNode = FindCurrentNode();
            var nextNodes = GetPropertyValue(currentNode, "NextNodes") as IEnumerable;
            if (nextNodes == null)
            {
                return;
            }

            foreach (var node in nextNodes)
            {
                var menuInfo = GetPropertyValue(node, "MenuInfo");
                if (menuInfo == null)
                {
                    continue;
                }

                int serial = ResolveInt(GetPropertyValue(menuInfo, "Serial"));
                string title = GetPropertyValue(menuInfo, "Title") as string ?? string.Empty;
                if (serial > 0 && ConnectedNodes.All(item => item.Serial != serial))
                {
                    ConnectedNodes.Add(new BranchTargetNodeInfo
                    {
                        Serial = serial,
                        Title = title
                    });
                }
            }
        }

        private object FindCurrentNode()
        {
            var nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches as IEnumerable;
            if (nodeCaches == null)
            {
                return null;
            }

            foreach (var node in nodeCaches)
            {
                var menuInfo = GetPropertyValue(node, "MenuInfo");
                int serial = ResolveInt(GetPropertyValue(menuInfo, "Serial"));
                if (serial == Serial || serial == ModelParam?.Serial)
                {
                    return node;
                }
            }

            return null;
        }

        private object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null)
            {
                return null;
            }

            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(instance);
        }

        private int ResolveInt(object value)
        {
            return value is int serial ? serial : int.TryParse(value?.ToString(), out serial) ? serial : -1;
        }
    }
}
