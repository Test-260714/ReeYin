using DryIoc;
using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.NodifyRalated;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Events;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class NodifyEditorViewModel : ObservableObject
    {

        #region Fields

        [JsonIgnore]
        public TransitionViewModel PendingTransition { get; }

        [JsonIgnore]
        public HandleFlowRunnerViewModel Runner { get; }

        [JsonIgnore]
        public BlackboardViewModel Blackboard { get; }

        [JsonIgnore]
        public bool IsRunning => Runner.State != MachineState.Stopped;

        [JsonIgnore]
        public bool IsPaused => Runner.State == MachineState.Paused;

        [JsonIgnore]
        private int _restorableDeleteDepth;
        #endregion

        #region Properties
        [JsonIgnore]
        public ObservableCollection<ToolCategory> ToolCategories { get; set; } = new ObservableCollection<ToolCategory>();

        /// <summary>
        /// 记录了当前的连接
        /// </summary>
        [JsonIgnore]
        public PendingConnectionViewModel PendingConnection { get; set; }

        public GraphSchema Schema { get; set; }

        [JsonIgnore]
        private NodifyObservableCollection<NodeViewModel> _nodes = new NodifyObservableCollection<NodeViewModel>();
        public NodifyObservableCollection<NodeViewModel> Nodes
        {
            //get => _nodes;
            get
            {
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeCaches = _nodes;
                return _nodes;
            }
            set => SetProperty(ref _nodes, value);
        }

        [JsonIgnore]
        private NodifyObservableCollection<NodeViewModel> _selectedNodes = new NodifyObservableCollection<NodeViewModel>();
        [JsonIgnore]
        public NodifyObservableCollection<NodeViewModel> SelectedNodes
        {
            get => _selectedNodes;
            set => SetProperty(ref _selectedNodes, value);
        }

        [JsonIgnore]
        private NodifyObservableCollection<ConnectionViewModel> _selectedConnections = new NodifyObservableCollection<ConnectionViewModel>();
        [JsonIgnore]
        public NodifyObservableCollection<ConnectionViewModel> SelectedConnections
        {
            get => _selectedConnections;
            set => SetProperty(ref _selectedConnections, value);
        }

        [JsonIgnore]
        private NodifyObservableCollection<ConnectionViewModel> _connections = new NodifyObservableCollection<ConnectionViewModel>();
        public NodifyObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        [JsonIgnore]
        private Size _viewportSize;
        public Size ViewportSize
        {
            get => _viewportSize;
            set => SetProperty(ref _viewportSize, value);
        }

        [JsonIgnore]
        private ConnectionViewModel? _selectedConnection;
        [JsonIgnore]
        public ConnectionViewModel? SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        [JsonIgnore]
        private NodeViewModel? _selectedNode;
        [JsonIgnore]
        public NodeViewModel? SelectedNode
        {
            get 
            {
                //PrismProvider.EventAggregator.GetEvent<NodifySelecteChangedEvent>().Publish(_selectedNode);
                return _selectedNode;
            }
            set => SetProperty(ref _selectedNode, value);
        }

        /// <summary>
        /// 切换到下一个节点
        /// </summary>
        [JsonIgnore]
        private NodifyObservableCollection<ConnectionViewModel> _transitions = new NodifyObservableCollection<ConnectionViewModel>();
        public NodifyObservableCollection<ConnectionViewModel> Transitions
        {
            get => _transitions;
            set => SetProperty(ref _transitions, value);
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand DeleteSelectionCommand 
        { 
            get
            {
                return new DelegateCommand
                    (DeleteSelection, () => SelectedNodes.Count > 0 || SelectedConnections.Count > 0);
            } 
        }

        [JsonIgnore]
        public ICommand UndoCommand
        {
            get
            {
                return new RequeryCommand(
                    () =>
                    {
                        ActionsHistory.Global.Undo();
                        CommandManager.InvalidateRequerySuggested();
                    },
                    () => ActionsHistory.Global.CanUndo);
            }
        }

        [JsonIgnore]
        public ICommand RedoCommand
        {
            get
            {
                return new RequeryCommand(
                    () =>
                    {
                        ActionsHistory.Global.Redo();
                        CommandManager.InvalidateRequerySuggested();
                    },
                    () => ActionsHistory.Global.CanRedo);
            }
        }

        [JsonIgnore]
        public ICommand DisconnectConnectorCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<ConnectorViewModel>
                    (c => 
                    {
                        c.Disconnect();
                    } );
            }
        }

        [JsonIgnore]
        public ICommand CreateConnectionCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<object>
                    (target =>
                    {
                        if (PendingConnection.Source == null || target == null)
                        {
                            return;
                        }

                        if (TryResolveConnectedNodes(PendingConnection.Source, target, out _, out var inputNode)
                            && inputNode is FlowNodeViewModel flowNode
                            && flowNode.MenuInfo?.NodeType != NodeType.Merge
                            && Connections.Any(connection => connection.Input?.Node == flowNode))
                        {
                            MessageBox.Show("非“合并分支”不允许有多个输入");
                            return;
                        }

                        Schema.TryAddConnection(PendingConnection.Source, target);
                    }
                    , target => PendingConnection.Source != null && target != null);
            }
        }

        [JsonIgnore]
        public ICommand CommentSelectionCommand 
        {
            get 
            {
                return new RequeryCommand(() =>
                Schema.AddCommentAroundNodes(SelectedNodes, "New comment"), () => SelectedNodes.Count > 0);
            }
        }

        [JsonIgnore]
        public INodifyCommand RenameStateCommand 
        {
            get
            {
                return new RequeryCommand(() =>
                {
                    MessageBox.Show("Please enter your ideal name");
                });
            }
        }

        [JsonIgnore]
        public INodifyCommand FlowRunCommand
        {
            get
            {
                return new RequeryCommand(() =>
                {
                    if(SelectedNodes.Count > 1)
                    {
                        MessageBox.Show("Please select only one node");
                    }

                    PrismProvider.ProjectManager.SltCurSolutionItem.IsManual = true;

                    //找到哪些是最后的节点
                    PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Clear();
                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(SelectedNodes[0].MenuInfo.Serial))
                        PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Add(SelectedNodes[0].MenuInfo.Serial, new Dictionary<int, bool>());

                    NodeViewModel.MarkLastOfEachBranch(SelectedNodes[0].MenuInfo.Serial, SelectedNodes[0].NextNodes);

                    SelectedNodes[0].ExecuteMulti();
                });
            }
        }

        [JsonIgnore]
        public INodifyCommand SingleStepRunCommand
        {
            get
            {
                return new RequeryCommand(() =>
                {
                    if(SelectedNodes.Count > 1)
                    {
                        MessageBox.Show("Please select only one node");
                    }

                    PrismProvider.ProjectManager.SltCurSolutionItem.IsManual = true;

                    //找到最后哪些是最后的节点
                    PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Clear();
                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(SelectedNodes[0].MenuInfo.Serial))
                        PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Add(SelectedNodes[0].MenuInfo.Serial, new Dictionary<int, bool>());

                    NodeViewModel.MarkLastOfEachBranch(SelectedNodes[0].MenuInfo.Serial, SelectedNodes[0].NextNodes);

                    SelectedNodes[0].ExecuteMulti();
                });
            }
        }

        [JsonIgnore]
        public INodifyCommand CancelRunCommand
        {
            get
            {
                return new RequeryCommand(() =>
                {
                    if(SelectedNodes.Count > 1)
                    {
                        MessageBox.Show("Please select only one node");
                    }

                    PrismProvider.ProjectManager.SltCurSolutionItem.IsManual = true;

                    //找到最后哪些是最后的节点
                    PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Clear();
                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(SelectedNodes[0].MenuInfo.Serial))
                        PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Add(SelectedNodes[0].MenuInfo.Serial, new Dictionary<int, bool>());

                    NodeViewModel.MarkLastOfEachBranch(SelectedNodes[0].MenuInfo.Serial, SelectedNodes[0].NextNodes);

                    SelectedNodes[0].CancelExecution();
                });
            }
        }

        [JsonIgnore]
        public INodifyCommand CreateTransitionCommand 
        {
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<(object Source, object? Target)>(s => Transitions.Add(new ConnectionViewModel
                {
                    Source = (NodeViewModel)s.Source,
                    Target = (NodeViewModel)s.Target!
                }), s => !IsRunning && s.Source is NodeViewModel source && s.Target is NodeViewModel target && target != s.Source && target != Nodes[0] && !source.Transitions.Contains(s.Target));
            }
        }

        [JsonIgnore]
        public INodifyCommand SelectionChangedCommand
        {
            get
            {
                return new RequeryCommand(() =>
                {                
                    PrismProvider.EventAggregator.GetEvent<NodifySelecteChangedEvent>().Publish(SelectedNode);
                    
                });
            }
        }

        [JsonIgnore]
        public INodifyCommand DeleteTransitionCommand 
        {
            get
            {
                return new RequeryCommand<ConnectionViewModel>(t => 
                Transitions.Remove(t), t => !IsRunning);
            }
        }
        #endregion

        #region Constructor 
        public NodifyEditorViewModel()
        {
            ActionsHistory.Global.Clear();

            PendingTransition = new TransitionViewModel();
            Runner = new HandleFlowRunnerViewModel(this);

            Blackboard = new BlackboardViewModel()
            {
                Actions = new NodifyObservableCollection<BlackboardItemReferenceViewModel>(BlackboardDescriptor.GetAvailableItems<IBlackboardAction>()),
                Conditions = new NodifyObservableCollection<BlackboardItemReferenceViewModel>(BlackboardDescriptor.GetAvailableItems<IBlackboardCondition>())
            };

            //注册状态改变事件
            PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Subscribe(TriggerWork, ThreadOption.BackgroundThread);
            //if(!AppView.IsOpenSolution)

            //注册语言切换事件
            InitOperationMenu();
            PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Subscribe(() =>
            {
                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    InitOperationMenu();
                });
            }, ThreadOption.UIThread);

            Schema = new GraphSchema();

            PendingConnection = new PendingConnectionViewModel
            {
                Graph = this
            };

            Transitions.WhenAdded(c =>
            {
                c.Source.Transitions.Add(c.Target);
                c.Target.Transitions.Add(c.Source);
            })
            .WhenRemoved(c =>
            {
                c.Source.Transitions.Remove(c.Target);
                c.Target.Transitions.Remove(c.Source);
            })
            .WhenCleared(c => c.ForEach(i =>
            {
                i.Source.Transitions.Clear();
                i.Target.Transitions.Clear();
            }));

            Connections.WhenAdded(c =>
            {
                AttachConnection(c);
            })
            // Called when the collection is cleared
            .WhenRemoved(c =>
            {
                DetachConnection(c);
            });

            Nodes.WhenAdded(x => 
            x.Graph = this)
                 // Not called when the collection is cleared
                 .WhenRemoved(x =>
                 {
                     if (_restorableDeleteDepth > 0)
                     {
                         return;
                     }

                     if (x.MenuInfo != null)
                     {
                         //通知删除，释放相关资源
                         PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Publish($"{x.MenuInfo.Serial}");
                     }

                     if (x is FlowNodeViewModel flow)
                     {
                         flow.Disconnect();
                         flow.LastNodes.Clear();
                         flow.NextNodes.Clear();
                         flow.LastNode = null!;
                         flow.NextNode = null!;
                         ClearNodeInputParameters(flow);
                     }
                     else if (x is KnotNodeViewModel knot)
                     {
                         knot.Connector.Disconnect();
                     }
                 })
                 .WhenCleared(x => 
                 {
                     Connections.Clear();
                     Transitions.Clear();
                 });
        }

        #endregion

        #region Methos
        public void Release()
        {
            PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Unsubscribe(TriggerWork);
            ActionsHistory.Global.Clear();
        }

        private void AttachConnection(ConnectionViewModel connection)
        {
            if (connection == null)
            {
                return;
            }

            connection.Graph = this;
            AttachConnectorConnection(connection.Input, connection);
            AttachConnectorConnection(connection.Output, connection);

            if (TryGetConnectedNodes(connection, out var outputNode, out var inputNode))
            {
                AddNodeRelationship(outputNode, inputNode);
                RefreshNodeInputParameters(inputNode);
            }
        }

        private void DetachConnection(ConnectionViewModel connection)
        {
            if (connection == null)
            {
                return;
            }

            DetachConnectorConnection(connection.Input, connection);
            DetachConnectorConnection(connection.Output, connection);

            if (TryGetConnectedNodes(connection, out var outputNode, out var inputNode))
            {
                if (!HasConnectionBetween(outputNode, inputNode))
                {
                    RemoveNodeRelationship(outputNode, inputNode);
                }

                RefreshNodeInputParameters(inputNode);
            }
        }

        private static void AttachConnectorConnection(ConnectorViewModel connector, ConnectionViewModel connection)
        {
            if (connector == null || connection == null)
            {
                return;
            }

            if (!connector.Connections.Contains(connection))
            {
                connector.Connections.Add(connection);
            }
        }

        private static void DetachConnectorConnection(ConnectorViewModel connector, ConnectionViewModel connection)
        {
            if (connector == null || connection == null)
            {
                return;
            }

            if (connector.Connections.Contains(connection))
            {
                connector.Connections.Remove(connection);
            }
        }

        private bool HasConnectionBetween(NodeViewModel outputNode, NodeViewModel inputNode)
        {
            return Connections.Any(connection =>
                connection?.Output?.Node == outputNode
                && connection.Input?.Node == inputNode);
        }

        private static void AddNodeRelationship(NodeViewModel outputNode, NodeViewModel inputNode)
        {
            if (outputNode == null || inputNode == null || ReferenceEquals(outputNode, inputNode))
            {
                return;
            }

            if (!outputNode.NextNodes.Contains(inputNode))
            {
                outputNode.NextNodes.Add(inputNode);
            }

            if (!inputNode.LastNodes.Contains(outputNode))
            {
                inputNode.LastNodes.Add(outputNode);
            }

            outputNode.NextNode = outputNode.NextNodes.LastOrDefault()!;
            inputNode.LastNode = inputNode.LastNodes.LastOrDefault()!;
        }

        private static void RemoveNodeRelationship(NodeViewModel outputNode, NodeViewModel inputNode)
        {
            if (outputNode == null || inputNode == null)
            {
                return;
            }

            outputNode.NextNodes.Remove(inputNode);
            inputNode.LastNodes.Remove(outputNode);

            outputNode.NextNode = outputNode.NextNodes.LastOrDefault()!;
            inputNode.LastNode = inputNode.LastNodes.LastOrDefault()!;
        }

        private static bool TryGetConnectedNodes(ConnectionViewModel connection, out NodeViewModel outputNode, out NodeViewModel inputNode)
        {
            outputNode = null!;
            inputNode = null!;

            if (connection?.Output?.Node == null || connection.Input?.Node == null)
            {
                return false;
            }

            outputNode = connection.Output.Node;
            inputNode = connection.Input.Node;
            return !ReferenceEquals(outputNode, inputNode);
        }

        private static bool TryResolveConnectedNodes(ConnectorViewModel sourceConnector, object target, out NodeViewModel outputNode, out NodeViewModel inputNode)
        {
            outputNode = null!;
            inputNode = null!;

            if (sourceConnector == null || target == null)
            {
                return false;
            }

            if (target is ConnectorViewModel targetConnector)
            {
                bool sourceIsInput = sourceConnector.Flow == ConnectorFlow.Input;
                inputNode = sourceIsInput ? sourceConnector.Node : targetConnector.Node;
                outputNode = sourceIsInput ? targetConnector.Node : sourceConnector.Node;
                return outputNode != null && inputNode != null && !ReferenceEquals(outputNode, inputNode);
            }

            if (target is NodeViewModel targetNode)
            {
                if (sourceConnector.Flow == ConnectorFlow.Input)
                {
                    inputNode = sourceConnector.Node;
                    outputNode = targetNode;
                }
                else
                {
                    inputNode = targetNode;
                    outputNode = sourceConnector.Node;
                }

                return outputNode != null && inputNode != null && !ReferenceEquals(outputNode, inputNode);
            }

            return false;
        }

        internal void RefreshNodeInputParameters(NodeViewModel node)
        {
            if (node == null)
            {
                return;
            }

            EnsureNodeModuleParam(node);

            if (node.LastNodes.Count == 0)
            {
                ClearNodeInputParameters(node);
                return;
            }

            if (node.MenuInfo?.NodeType == NodeType.Merge)
            {
                var mergedParams = new Dictionary<string, object>();

                foreach (var lastNode in node.LastNodes.Distinct())
                {
                    var outputParams = GetNodeOutputParameters(lastNode);
                    if (outputParams == null)
                    {
                        continue;
                    }

                    foreach (var pair in outputParams)
                    {
                        string key = mergedParams.ContainsKey(pair.Key)
                            ? $"{lastNode.MenuInfo?.Serial ?? 0}_{pair.Key}"
                            : pair.Key;
                        mergedParams[key] = pair.Value;
                    }
                }
                node.ModuleParam.moduleInputParam.TransmitParams = mergedParams;
                return;
            }

            var previousNode = node.LastNode ?? node.LastNodes.LastOrDefault();
            var transmitParams = GetNodeOutputParameters(previousNode);

            node.ModuleParam.moduleInputParam.TransmitParams = transmitParams != null
                ? new Dictionary<string, object>(transmitParams)
                : new Dictionary<string, object>();
        }

        private static Dictionary<string, object> GetNodeOutputParameters(NodeViewModel node)
        {
            if (node?.ModuleParam == null)
            {
                return null;
            }

            FlowNodeViewModel.SyncDialogOutputParameters(node.ModuleParam);
            return node.ModuleParam.moduleOutputParam?.TransmitParams;
        }

        private static void EnsureNodeModuleParam(NodeViewModel node)
        {
            if (node.ModuleParam == null)
            {
                node.ModuleParam = new ModuleParamBase
                {
                    Serial = node.MenuInfo?.Serial ?? 0
                };
            }

            node.ModuleParam.moduleInputParam ??= new ModuleParam();
            node.ModuleParam.moduleOutputParam ??= new ModuleParam();
        }

        private static void ClearNodeInputParameters(NodeViewModel node)
        {
            if (node?.ModuleParam == null)
            {
                return;
            }

            node.ModuleParam.moduleInputParam ??= new ModuleParam();
            node.ModuleParam.moduleInputParam.TransmitParams = new Dictionary<string, object>();
        }

        public NodifyObservableCollection<OperationInfoViewModel> InitOperationMenu()
        {
            try
            {
                ToolCategories.Clear();

                var menus = PrismProvider.NodifyMenuManager.AvailableMenus
                    .Where(p => p.IsUsing)
                    .ToList();

                if (menus.Count == 0)
                    return new NodifyObservableCollection<OperationInfoViewModel>();

                // ===== 1️⃣ 分组 + 排序（按 Header 前两位数字排序） =====
                var groupedMenus = menus
                    .GroupBy(menu => menu.Type)
                    .OrderBy(group => ParseOrder(group.Key))   // 核心排序逻辑
                    .ToList();

                // ===== 2️⃣ 构建分类菜单 =====
                foreach (var group in groupedMenus)
                {
                    var category = new ToolCategory
                    {
                        Header = PrismProvider.LanguageManager.GetStringResource(group.Key),
                        Icon = "icon-common",
                        Tools = new NodifyObservableCollection<OperationInfoViewModel>(
                            group.Select(CreateOperationInfo)
                        )
                    };

                    ToolCategories.Add(category);
                }

                // ===== 3️⃣ 返回全部 OperationInfo 列表 =====
                return new NodifyObservableCollection<OperationInfoViewModel>(
                    menus
                        .OrderBy(m => ParseOrder(m.Type))
                        .Select(CreateOperationInfo)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new NodifyObservableCollection<OperationInfoViewModel>();
            }
        }

        private OperationInfoViewModel CreateOperationInfo(MenuInfo menu)
        {
            return new OperationInfoViewModel
            {
                MenuInfo = menu,
                Type = OperationType.Graph,
                Title = menu.Title,
                BindingView = menu.TargetType.Name,
                Icon = menu.Icon,
                CurStatus = NodeStatus.None
            };
        }

        private int ParseOrder(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return int.MaxValue;

            // 取前两位数字
            var match = System.Text.RegularExpressions.Regex.Match(header, @"^\d{2}");

            if (match.Success && int.TryParse(match.Value, out int order))
                return order;

            return int.MaxValue; // 没有数字前缀的排到最后
        }


        /// <summary>
        /// 删除节点连线
        /// </summary>
        private void DeleteSelection()
        {
            RequestDelete(SelectedNodes.ToList(), SelectedConnections.ToList(), true);
        }

        public void DeleteNode(NodeViewModel node)
        {
            if (node == null)
            {
                return;
            }

            RequestDelete(new[] { node }, Array.Empty<ConnectionViewModel>(), true);
        }

        public void DeleteConnection(ConnectionViewModel connection)
        {
            if (connection == null)
            {
                return;
            }

            RequestDelete(Array.Empty<NodeViewModel>(), new[] { connection }, false);
        }

        private void RequestDelete(
            IEnumerable<NodeViewModel> nodes,
            IEnumerable<ConnectionViewModel> connections,
            bool confirmNodes)
        {
            var selectedNodes = nodes
                .Where(node => node != null && Nodes.Contains(node))
                .Distinct()
                .ToList();

            var selectedConnections = connections
                .Where(connection => connection != null && Connections.Contains(connection))
                .Distinct()
                .ToList();

            if (selectedNodes.Count == 0 && selectedConnections.Count == 0)
            {
                return;
            }

            if (confirmNodes && selectedNodes.Count > 0 && !ConfirmDeleteNodes(selectedNodes.Count))
            {
                return;
            }

            ActionsHistory.Global.ExecuteAction(new DeleteSelectionAction(this, selectedNodes, selectedConnections));
            CommandManager.InvalidateRequerySuggested();
        }

        private static bool ConfirmDeleteNodes(int nodeCount)
        {
            string message = nodeCount == 1
                ? "确定要删除选中的节点吗？删除后会同时移除相关连线。"
                : $"确定要删除选中的 {nodeCount} 个节点吗？删除后会同时移除相关连线。";

            MessageBoxResult result = HandyControl.Controls.MessageBox.Show(
                message,
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private sealed class DeleteSelectionAction : IAction
        {
            private readonly NodifyEditorViewModel _graph;
            private readonly List<NodeRestoreRecord> _nodeRecords;
            private readonly List<ConnectionRestoreRecord> _connectionRecords;
            private readonly List<NodeViewModel> _selectedNodes;
            private readonly List<ConnectionViewModel> _selectedConnections;

            public string? Label { get; }

            public DeleteSelectionAction(
                NodifyEditorViewModel graph,
                IReadOnlyCollection<NodeViewModel> selectedNodes,
                IReadOnlyCollection<ConnectionViewModel> selectedConnections)
            {
                _graph = graph;
                _nodeRecords = selectedNodes
                    .Select(node => new NodeRestoreRecord(graph, node))
                    .OrderBy(record => record.Index)
                    .ToList();

                var selectedNodeSet = selectedNodes.ToHashSet();
                _connectionRecords = graph.Connections
                    .Where(connection => IsConnectedToAnyNode(connection, selectedNodeSet))
                    .Concat(selectedConnections)
                    .Distinct()
                    .Select(connection => new ConnectionRestoreRecord(graph, connection))
                    .OrderBy(record => record.Index)
                    .ToList();

                _selectedNodes = graph.SelectedNodes.ToList();
                _selectedConnections = graph.SelectedConnections.ToList();
                Label = _nodeRecords.Count > 0 ? "删除节点" : "删除连线";
            }

            public void Execute()
            {
                _graph._restorableDeleteDepth++;
                try
                {
                    foreach (var record in _connectionRecords)
                    {
                        record.Remove(_graph);
                    }

                    foreach (var record in _nodeRecords)
                    {
                        record.Remove(_graph);
                    }
                }
                finally
                {
                    _graph._restorableDeleteDepth--;
                }

                foreach (var record in _nodeRecords)
                {
                    record.ProjectCache.Remove();
                }

                RemoveDeletedItemsFromSelection();
            }

            public void Undo()
            {
                foreach (var record in _nodeRecords)
                {
                    record.RestoreNodeState();
                    record.Restore(_graph);
                }

                foreach (var record in _connectionRecords)
                {
                    record.Restore(_graph);
                }

                foreach (var record in _nodeRecords)
                {
                    record.ProjectCache.Restore();
                }

                RestoreSelection();
            }

            private static bool IsConnectedToAnyNode(ConnectionViewModel connection, HashSet<NodeViewModel> nodes)
            {
                return connection?.Input?.Node != null && nodes.Contains(connection.Input.Node)
                    || connection?.Output?.Node != null && nodes.Contains(connection.Output.Node);
            }

            private void RemoveDeletedItemsFromSelection()
            {
                foreach (var record in _connectionRecords)
                {
                    _graph.SelectedConnections.Remove(record.Connection);
                }

                foreach (var record in _nodeRecords)
                {
                    _graph.SelectedNodes.Remove(record.Node);
                }

                if (_graph.SelectedNode != null && _nodeRecords.Any(record => ReferenceEquals(record.Node, _graph.SelectedNode)))
                {
                    _graph.SelectedNode = null;
                }

                if (_graph.SelectedConnection != null && _connectionRecords.Any(record => ReferenceEquals(record.Connection, _graph.SelectedConnection)))
                {
                    _graph.SelectedConnection = null;
                }
            }

            private void RestoreSelection()
            {
                _graph.SelectedNodes.Clear();
                foreach (var node in _selectedNodes.Where(node => _graph.Nodes.Contains(node)))
                {
                    _graph.SelectedNodes.Add(node);
                }

                _graph.SelectedConnections.Clear();
                foreach (var connection in _selectedConnections.Where(connection => _graph.Connections.Contains(connection)))
                {
                    _graph.SelectedConnections.Add(connection);
                }

                _graph.SelectedNode = _selectedNodes.LastOrDefault(node => _graph.Nodes.Contains(node));
                _graph.SelectedConnection = _selectedConnections.LastOrDefault(connection => _graph.Connections.Contains(connection));
            }
        }

        private sealed class NodeRestoreRecord
        {
            private readonly List<ConnectorViewModel>? _inputConnectors;
            private readonly List<ConnectorViewModel>? _outputConnectors;
            private readonly ConnectorViewModel? _knotConnector;
            private readonly List<NodeViewModel> _lastNodes;
            private readonly List<NodeViewModel> _nextNodes;
            private readonly NodeViewModel _lastNode;
            private readonly NodeViewModel _nextNode;

            public NodeViewModel Node { get; }
            public int Index { get; }
            public NodeProjectCacheSnapshot ProjectCache { get; }

            public NodeRestoreRecord(NodifyEditorViewModel graph, NodeViewModel node)
            {
                Node = node;
                Index = graph.Nodes.IndexOf(node);
                _lastNodes = node.LastNodes.ToList();
                _nextNodes = node.NextNodes.ToList();
                _lastNode = node.LastNode;
                _nextNode = node.NextNode;
                ProjectCache = NodeProjectCacheSnapshot.Capture(node);

                if (node is FlowNodeViewModel flow)
                {
                    _inputConnectors = flow.Input.ToList();
                    _outputConnectors = flow.Output.ToList();
                }
                else if (node is KnotNodeViewModel knot)
                {
                    _knotConnector = knot.Connector;
                }
            }

            public void Remove(NodifyEditorViewModel graph)
            {
                if (!graph.Nodes.Contains(Node))
                {
                    return;
                }

                graph.Nodes.Remove(Node);

                if (Node.MenuInfo != null)
                {
                    Logs.LogInfo($"节点{Node.MenuInfo.Serial}_{Node.MenuInfo.Title}，已被移除!");
                }
            }

            public void Restore(NodifyEditorViewModel graph)
            {
                if (graph.Nodes.Contains(Node))
                {
                    return;
                }

                graph.Nodes.Insert(Clamp(Index, 0, graph.Nodes.Count), Node);
            }

            public void RestoreNodeState()
            {
                if (Node is FlowNodeViewModel flow)
                {
                    RestoreConnectors(flow.Input, _inputConnectors);
                    RestoreConnectors(flow.Output, _outputConnectors);
                }
                else if (Node is KnotNodeViewModel knot && _knotConnector != null)
                {
                    knot.Connector = _knotConnector;
                }

                Node.LastNodes = _lastNodes.ToList();
                Node.NextNodes = _nextNodes.ToList();
                Node.LastNode = _lastNode;
                Node.NextNode = _nextNode;
            }

            private static void RestoreConnectors(
                NodifyObservableCollection<ConnectorViewModel> target,
                List<ConnectorViewModel>? source)
            {
                if (source == null)
                {
                    return;
                }

                target.Clear();
                foreach (var connector in source)
                {
                    target.Add(connector);
                }
            }
        }

        private sealed class ConnectionRestoreRecord
        {
            private readonly ConnectorViewModel _input;
            private readonly ConnectorViewModel _output;

            public ConnectionViewModel Connection { get; }
            public int Index { get; }

            public ConnectionRestoreRecord(NodifyEditorViewModel graph, ConnectionViewModel connection)
            {
                Connection = connection;
                Index = graph.Connections.IndexOf(connection);
                _input = connection.Input;
                _output = connection.Output;
            }

            public void Remove(NodifyEditorViewModel graph)
            {
                if (graph.Connections.Contains(Connection))
                {
                    graph.Connections.Remove(Connection);
                }
            }

            public void Restore(NodifyEditorViewModel graph)
            {
                if (graph.Connections.Contains(Connection))
                {
                    return;
                }

                Connection.Input = _input;
                Connection.Output = _output;
                Connection.Graph = graph;
                graph.Connections.Insert(Clamp(Index, 0, graph.Connections.Count), Connection);
            }
        }

        private sealed class NodeProjectCacheSnapshot
        {
            private readonly int _serial;
            private readonly ModelParamBase? _modelParam;
            private readonly bool _hasOutputCache;
            private readonly ObservableCollection<TransmitParam>? _outputCache;
            private readonly List<IndexedTransmitParam> _globalParams;

            private NodeProjectCacheSnapshot(
                int serial,
                ModelParamBase? modelParam,
                bool hasOutputCache,
                ObservableCollection<TransmitParam>? outputCache,
                List<IndexedTransmitParam> globalParams)
            {
                _serial = serial;
                _modelParam = modelParam;
                _hasOutputCache = hasOutputCache;
                _outputCache = outputCache;
                _globalParams = globalParams;
            }

            public static NodeProjectCacheSnapshot Capture(NodeViewModel node)
            {
                int serial = node.MenuInfo?.Serial ?? node.ModuleParam?.Serial ?? -1;
                var modelParam = node.ModuleParam as ModelParamBase;
                var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
                bool hasOutputCache = false;
                ObservableCollection<TransmitParam>? outputCache = null;
                var globalParams = new List<IndexedTransmitParam>();

                if (serial >= 0 && solution != null)
                {
                    hasOutputCache = solution.NodesOutputCache.TryGetValue(serial.ToString(), out outputCache);

                    if (solution.GlobalParams != null)
                    {
                        for (int i = 0; i < solution.GlobalParams.Count; i++)
                        {
                            if (solution.GlobalParams[i].Serial == serial)
                            {
                                globalParams.Add(new IndexedTransmitParam(i, solution.GlobalParams[i]));
                            }
                        }
                    }
                }

                return new NodeProjectCacheSnapshot(serial, modelParam, hasOutputCache, outputCache, globalParams);
            }

            public void Remove()
            {
                if (_modelParam != null)
                {
                    PrismProvider.ProjectManager?.RemoveNodeParamCache(_modelParam);
                }

                if (_serial < 0)
                {
                    return;
                }

                var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solution == null)
                {
                    return;
                }

                solution.NodesOutputCache.Remove(_serial.ToString());

                if (solution.GlobalParams == null)
                {
                    return;
                }

                for (int i = solution.GlobalParams.Count - 1; i >= 0; i--)
                {
                    if (solution.GlobalParams[i].Serial == _serial)
                    {
                        solution.GlobalParams.RemoveAt(i);
                    }
                }
            }

            public void Restore()
            {
                if (_modelParam != null)
                {
                    PrismProvider.ProjectManager?.AddNodeParamCache(_modelParam);
                }

                if (_serial < 0)
                {
                    return;
                }

                var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solution == null)
                {
                    return;
                }

                if (_hasOutputCache && _outputCache != null)
                {
                    solution.NodesOutputCache[_serial.ToString()] = _outputCache;
                }

                if (solution.GlobalParams == null)
                {
                    return;
                }

                foreach (var item in _globalParams.OrderBy(item => item.Index))
                {
                    if (solution.GlobalParams.Contains(item.Param))
                    {
                        continue;
                    }

                    solution.GlobalParams.Insert(Clamp(item.Index, 0, solution.GlobalParams.Count), item.Param);
                }
            }
        }

        private sealed class IndexedTransmitParam
        {
            public IndexedTransmitParam(int index, TransmitParam param)
            {
                Index = index;
                Param = param;
            }

            public int Index { get; }
            public TransmitParam Param { get; }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
        
        /// <summary>
        /// 触发工作
        /// </summary>
        /// <param name="args"></param>
        private void TriggerWork((eRunStatus status, int serial) args)
        {
            //MarkTheEnd();
            switch (args.status)
            {
                case eRunStatus.Running:
                    {
                        PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Running);

                        PrismProvider.ProjectManager.SltCurSolutionItem.IsManual = false;
                        foreach (var item in Nodes)
                        {

                            //重置之后节点的所有状态
                            //item.CurStatus = NodeStatus.None;
                            //NodeViewModel.SetAllStatusToNone(item.NextNodes);

                            if ((item.MenuInfo.Serial == args.serial) || (args.serial == -1 && item.MenuInfo.NodeType == NodeType.Monitor))
                            {
                                //只执行顶级节点
                                //if (item.MenuInfo.NodeType == NodeType.Start || item.MenuInfo.NodeType == NodeType.Monitor)
                                {
                                    //寻找并标记最后一个节点
                                    //NodeViewModel.MarkLastOfLongestBranch(item.NextNodes);
                                    PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Clear();
                                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(item.MenuInfo.Serial))
                                        PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Add(item.MenuInfo.Serial, new Dictionary<int, bool>());

                                    NodeViewModel.MarkLastOfEachBranch(item.MenuInfo.Serial,item.NextNodes);

                                    ////判断当前节点是否成功执行
                                    //if (item.CurStatus != NodeStatus.Success) 
                                    //    return;
                                    item.ExecuteMulti();

                                }
                            }
                        }
                    }
                    break;
                case eRunStatus.NotRun:
                    {

                    }
                    break;
                case eRunStatus.NG:
                    {

                    }
                    break;
                case eRunStatus.OK:
                    {

                    }
                    break;
            }
        }

        #endregion

    }


    public class ToolCategory
    {
        public string Header { get; set; }
        public string Icon { get; set; }
        public NodifyObservableCollection<OperationInfoViewModel> Tools { get; set; }

    }
}
