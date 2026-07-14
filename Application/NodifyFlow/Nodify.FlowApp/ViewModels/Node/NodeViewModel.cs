using Azure;
using Dm.util;
using ImageTool.Halcon.Config;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.NodifyManager;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;
using Point = System.Windows.Point;

namespace Nodify.FlowApp
{
    /// <summary>
    /// 节点基类
    /// </summary>
    [Serializable]
    public class NodeViewModel : ObservableObject
    {
        #region Fields
        public bool IsLastNode = false;

        /// <summary>
        /// Merge节点执行标志位，0=未执行，1=已执行
        /// 用于保证Merge节点在多个输入分支触发时只执行一次
        /// </summary>
        private int _mergeExecuteFlag = 0;

        #endregion

        #region Properties
        [JsonIgnore]
        private MenuInfo _menuInfo;

        public MenuInfo MenuInfo
        {
            get => _menuInfo;
            set => SetProperty(ref _menuInfo, value);
        }

        [JsonIgnore]
        private bool _isInput;
        public bool IsInput
        {
            get => _isInput;
            set => SetProperty(ref _isInput, value);
        }

        public Guid Id { get; set; }

        [JsonIgnore]
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        [JsonIgnore]
        private NodeStatus _curStatus;
        [JsonIgnore]
        public NodeStatus CurStatus 
        { 
            get => _curStatus;
            set => SetProperty(ref _curStatus, value);
        }

        [JsonIgnore]
        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [JsonIgnore]
        private string? _icon;
        public string? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        [JsonIgnore]
        private IModuleParam _moduleParam;
        public IModuleParam ModuleParam
        {
            get { return _moduleParam; }
            set { _moduleParam = value; }
        }

        [JsonIgnore]
        private NodifyEditorViewModel _graph = default!;
        public NodifyEditorViewModel Graph
        {
            get => _graph;
            internal set => SetProperty(ref _graph, value);
        }

        [JsonIgnore]
        private Point _location;
        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        [JsonIgnore]
        private OperationViewModel _operation = default!;
        public OperationViewModel Operation
        {
            get => _operation;
            set => SetProperty(ref _operation, value);
        }

        public Orientation Orientation { get; protected set; }

        public BlackboardItemViewModel? Action { get; private set; }

        public bool IsEditable { get; set; } = true;
        
        public NodifyObservableCollection<NodeViewModel> Transitions { get; } = new NodifyObservableCollection<NodeViewModel>();

        [JsonIgnore]
        private BlackboardItemReferenceViewModel? _actionReference;
        public BlackboardItemReferenceViewModel? ActionReference
        {
            get => _actionReference;
            set
            {
                if (SetProperty(ref _actionReference, value))
                {
                    SetAction(_actionReference);
                }
            }
        }

        [JsonIgnore]
        private NodeViewModel _lastNode = default!;
        public NodeViewModel LastNode
        {
            get => _lastNode;
            set => SetProperty(ref _lastNode, value);
        }

        [JsonIgnore]
        private List<NodeViewModel> _lastNodes = new List<NodeViewModel>();
        public List<NodeViewModel> LastNodes
        {
            get => _lastNodes;
            set => SetProperty(ref _lastNodes, value);
        }

        [JsonIgnore]
        private NodeViewModel _nextNode = default!;
        public NodeViewModel NextNode
        {
            get => _nextNode;
            set => SetProperty(ref _nextNode, value);
        }

        [JsonIgnore]
        private List<NodeViewModel> _nextNodes = new List<NodeViewModel>();
        public List<NodeViewModel> NextNodes
        {
            get => _nextNodes;
            set => SetProperty(ref _nextNodes, value);
        }

        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand DeleteCommand 
        {
            get
            {
                return new DelegateCommand(() => Graph.DeleteNode(this));
            }
        }

        #endregion

        #region Constructor
        //public NodeViewModel(Guid id) => Id = id;

        public NodeViewModel()
        {
            if(Id == null)
                Id = Guid.NewGuid();
        }
        #endregion

        #region Methods
        private void SetAction(BlackboardItemReferenceViewModel? actionRef)
        {
            Action = BlackboardDescriptor.GetItem(actionRef);

            OnPropertyChanged(nameof(Action));
        }

        /// <summary>
        /// 执行单个节点的操作
        /// </summary>
        public void Execute()
        {
            Console.WriteLine($"{Title}触发执行...");
            CurStatus = NodeStatus.Running;
            if(NextNode != null)
            {
                NextNode.CurStatus = NodeStatus.Waiting;
                //Console.WriteLine($"{NextNode.Title}等待执行...");
                Task.Run(() =>
                {
                    CurStatus = ModuleParam.TriggerModuleRun.Invoke().RunStatus;
                    if(CurStatus != NodeStatus.Success)
                    {
                        Console.WriteLine($"{Title}执行失败！！！");
                        return;
                    }

                    Console.WriteLine($"{Title}成功执行，状态为{CurStatus}！！！");
                    if (NextNode.ModuleParam != null && ModuleParam != null)
                        NextNode.ModuleParam.moduleInputParam.TransmitParams = ModuleParam.moduleOutputParam.TransmitParams;

                    if (this == NextNode)
                    {
                        MessageBox.Show("不能循环执行！");
                        return;
                    }
                    NextNode.Execute();
                });
            }
            else
            {
                Task.Run(() =>
                {
                    CurStatus = ModuleParam.TriggerModuleRun.Invoke().RunStatus;
                    Console.WriteLine($"{Title}成功执行！！！");
                });
            }
        }

        /// <summary>
        /// 执行多个节点（支持外部停止）
        /// </summary>
        public void ExecuteMulti()
        {
            try
            {
                SetAllStatusToNone(NextNodes);

                if (ShouldStopExecution())
                {
                    CurStatus = NodeStatus.Cancelled;
                    HandleStopCleanup();
                    return;
                }

                Console.WriteLine($"{Title},{MenuInfo.Serial}触发执行...");
                if (ModuleParam.InputNodeStatus == null)
                    ModuleParam.InputNodeStatus = new List<(int, NodeStatus)>();
                ModuleParam.InputNodeStatus.Clear();
                //是多输入的合并节点需要等所有输入节点执行成功
                if (MenuInfo.NodeType == NodeType.Merge)
                {
                    foreach (var node in LastNodes)
                    {
                        ModuleParam.InputNodeStatus.Add((node.MenuInfo.Serial, node.CurStatus));
                    }
                }
                //else
                {
                    CurStatus = NodeStatus.Running;

                    // 关键：当前节点只执行一次（不放到每个 NextNode 的 Task 里）
                    Task.Run(() =>
                    {
                        if (ShouldStopExecution())
                        {
                            CurStatus = NodeStatus.Cancelled;
                            HandleStopCleanup();
                            return;
                        }

                        if (ModuleParam?.TriggerModuleRun == null)
                        {
                            CurStatus = NodeStatus.Failed;
                            Console.WriteLine($"{Title},{MenuInfo.Serial}没有执行方法！！！");
                            return;
                        }

                        var runResult = ModuleParam.TriggerModuleRun.Invoke();
                        CurStatus = runResult.RunStatus;

                        if (ShouldStopExecution())
                        {
                            CurStatus = NodeStatus.Cancelled;
                            HandleStopCleanup();
                            return;
                        }

                        if (CurStatus == NodeStatus.NotRun)
                        {
                            // NotRun 表示当前分支未命中，只跳过本分支，不切错误状态。
                            MarkDownstreamAsNotRun(NextNodes);
                            return;
                        }

                        if (CurStatus != NodeStatus.Success)
                        {
                            if (CurStatus == NodeStatus.Circle)
                            {
                                Console.WriteLine($"{Title},{MenuInfo.Serial}继续循环");
                            }
                            else
                            {
                                Console.WriteLine($"{Title},{MenuInfo.Serial}执行失败，后面就不执行了哦！！！");
                                PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Error);
                            }

                            if (NextNodes != null)
                            {
                                foreach (var n in NextNodes)
                                    n.CurStatus = NodeStatus.None;
                            }
                            if (PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.Keys.Contains(MenuInfo.RootSerial))
                                PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[MenuInfo.RootSerial]?.Clear();
                            return;
                        }

                        Console.WriteLine($"{Title}成功执行，状态为{CurStatus}！！！");

                        // 执行成功后，再并发触发多个 NextNodes；分支路由节点只放行命中的已连接节点。
                        List<NodeViewModel> executableNextNodes = GetExecutableNextNodes();
                        if (executableNextNodes != null && executableNextNodes.Count != 0)
                        {
                            foreach (var next in executableNextNodes)
                            {
                                if (ShouldStopExecution())
                                {
                                    CurStatus = NodeStatus.Cancelled;
                                    HandleStopCleanup();
                                    return;
                                }

                                next.CurStatus = NodeStatus.Waiting;

                                if (ReferenceEquals(this, next))
                                {
                                    MessageBox.Show("不能循环执行！");
                                    continue;
                                }

                                // Merge节点：等所有前驱节点都完成后，由最后到达的分支触发执行
                                if (next.MenuInfo.NodeType == NodeType.Merge)
                                {
                                    // 检查Merge节点的所有前驱是否都已完成
                                    bool allLastNodesCompleted = next.LastNodes.All(n =>
                                        n.CurStatus == NodeStatus.Success ||
                                        n.CurStatus == NodeStatus.NotRun ||
                                        n.CurStatus == NodeStatus.Failed ||
                                        n.CurStatus == NodeStatus.Error);

                                    if (!allLastNodesCompleted)
                                    {
                                        Console.WriteLine($"Merge节点{next.Title},{next.MenuInfo.Serial}：前驱节点尚未全部完成，当前分支{Title},{MenuInfo.Serial}到达后等待其他分支");
                                        continue;
                                    }

                                    // 所有前驱都完成了，用CAS保证只有一个线程触发（防止两个前驱同时完成的竞态）
                                    if (Interlocked.CompareExchange(ref next._mergeExecuteFlag, 1, 0) != 0)
                                    {
                                        Console.WriteLine($"Merge节点{next.Title},{next.MenuInfo.Serial}已被其他分支触发，跳过重复执行");
                                        continue;
                                    }

                                    // 将所有前驱节点的输出参数合并为Merge节点的输入参数
                                    if (next.ModuleParam?.moduleInputParam != null)
                                    {
                                        var mergedParams = new Dictionary<string, object>();
                                        foreach (var lastNode in next.LastNodes)
                                        {
                                            if (lastNode.ModuleParam?.moduleOutputParam?.TransmitParams == null) continue;
                                            foreach (var kv in lastNode.ModuleParam.moduleOutputParam.TransmitParams)
                                            {
                                                // 键冲突时加前缀区分来源节点
                                                var key = mergedParams.ContainsKey(kv.Key)
                                                    ? $"{lastNode.MenuInfo.Serial}_{kv.Key}"
                                                    : kv.Key;
                                                mergedParams[key] = kv.Value;
                                            }
                                        }
                                        next.ModuleParam.moduleInputParam.TransmitParams = mergedParams;
                                    }
                                    Console.WriteLine($"Merge节点{next.Title},{next.MenuInfo.Serial}：所有前驱节点已完成，触发执行");
                                }
                                else
                                {
                                    if (next.ModuleParam?.moduleInputParam != null && ModuleParam?.moduleOutputParam != null)
                                    {
                                        next.ModuleParam.moduleInputParam.TransmitParams =
                                            new Dictionary<string, object>(ModuleParam.moduleOutputParam.TransmitParams);
                                    }
                                }

                                Task.Run(() => next.ExecuteMulti());
                            }
                        }
                        else
                        {
                            if (IsLastNode && !PrismProvider.ProjectManager.SltCurSolutionItem.IsManual)
                            {
                                PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[MenuInfo.RootSerial].Remove(MenuInfo.Serial);
                                PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Idle);
                            }
                        }
                    });
                }

            }
            catch (Exception ex)
            {
                PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Error);
                Console.WriteLine($"ExecuteMulti()_执行异常：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取当前节点本次允许执行的后续节点。
        /// </summary>
        private List<NodeViewModel> GetExecutableNextNodes()
        {
            if (NextNodes == null)
            {
                return new List<NodeViewModel>();
            }

            if (ModuleParam is IBranchRouter branchRouter && branchRouter.SelectedNextSerials != null && branchRouter.SelectedNextSerials.Count > 0)
            {
                return NextNodes
                    .Where(node => branchRouter.SelectedNextSerials.Contains(node.MenuInfo.Serial))
                    .ToList();
            }

            return NextNodes;
        }

        /// <summary>
        /// 将未命中的分支整条后续链标记为 NotRun，避免流程把跳过分支当失败。
        /// </summary>
        private void MarkDownstreamAsNotRun(IEnumerable<NodeViewModel> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                node.CurStatus = NodeStatus.NotRun;
                if (node.IsLastNode
                    && PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.TryGetValue(MenuInfo.RootSerial, out var processEnds))
                {
                    processEnds.Remove(node.MenuInfo.Serial);
                }

                node.MarkDownstreamAsNotRun(node.NextNodes);
            }
        }

        /// <summary>
        /// 检查是否应该停止执行（通过工作状态判断）
        /// </summary>
        private bool ShouldStopExecution()
        {
            try
            {
                var currentStatus = PrismProvider.WorkStatusManager.CurStatus;
                // 如果工作状态为暂停或停止，则应该停止执行
                return currentStatus == ReeYin_V.Core.Services.WorkStatus.WorkStatus.Paused ||
                       currentStatus == ReeYin_V.Core.Services.WorkStatus.WorkStatus.Stopped;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理停止后的清理工作（恢复空闲状态）
        /// </summary>
        private void HandleStopCleanup()
        {
            try
            {
                //延时一会在恢复
                Task.Delay(100).Wait();
                // 将工作状态恢复为空闲，方便下次运行
                PrismProvider.WorkStatusManager.SwitchWorkStatus(ReeYin_V.Core.Services.WorkStatus.WorkStatus.Idle);
                Console.WriteLine("执行已停止，状态已恢复为空闲");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止清理异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 重载方法，接收取消令牌
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void ExecuteMulti(CancellationToken cancellationToken)
        {
            try
            {
                #region 重置节点状态
                SetAllStatusToNone(NextNodes);
                #endregion

                #region 条件分支判断
                if (MenuInfo.Title == "ConditionalBranching" && LastNodes.Count > 1)
                {
                    int temp = 0;
                    foreach (var lastNode in LastNodes)
                    {
                        // 检查取消状态
                        if (cancellationToken.IsCancellationRequested)
                        {
                            CurStatus = NodeStatus.Cancelled;
                            Console.WriteLine($"{Title},{MenuInfo.Serial}检测到取消请求，终止条件分支判断");
                            return;
                        }

                        if (lastNode.CurStatus != NodeStatus.Success)
                        {
                            temp++;
                            Console.WriteLine($"条件分支前置节点{lastNode.MenuInfo.Serial}未执行完成！");
                        }
                    }
                }
                #endregion

                // 检查取消状态（核心判断点1）
                if (cancellationToken.IsCancellationRequested)
                {
                    CurStatus = NodeStatus.Cancelled;
                    Console.WriteLine($"{Title},{MenuInfo.Serial}检测到取消请求，终止执行");
                    return;
                }

                Console.WriteLine($"{Title},{MenuInfo.Serial}触发执行...");
                CurStatus = NodeStatus.Running;

                if (NextNodes != null && NextNodes.Count != 0)
                {
                    foreach (var node in NextNodes)
                    {
                        // 检查取消状态（核心判断点2）
                        if (cancellationToken.IsCancellationRequested)
                        {
                            CurStatus = NodeStatus.Cancelled;
                            Console.WriteLine($"{Title},{MenuInfo.Serial}检测到取消请求，停止处理后续节点");
                            return;
                        }

                        node.CurStatus = NodeStatus.Waiting;

                        // 传递取消令牌到子任务
                        var token = cancellationToken;
                        Task.Run(() => {
                            // 子任务内检查取消
                            if (token.IsCancellationRequested)
                            {
                                node.CurStatus = NodeStatus.Cancelled;
                                Console.WriteLine($"{node.Title},{node.MenuInfo.Serial}子任务检测到取消请求");
                                return;
                            }

                            if (ModuleParam.TriggerModuleRun != null)
                            {
                                CurStatus = ModuleParam.TriggerModuleRun.Invoke().RunStatus;
                            }

                            // 子任务内检查取消
                            if (token.IsCancellationRequested)
                            {
                                node.CurStatus = NodeStatus.Cancelled;
                                return;
                            }

                            if (CurStatus == NodeStatus.NotRun)
                            {
                                // NotRun 表示当前分支未命中，只跳过本分支，不按失败处理。
                                MarkDownstreamAsNotRun(NextNodes);
                                return;
                            }

                            if (CurStatus != NodeStatus.Success)
                            {
                                if (CurStatus == NodeStatus.Circle)
                                {
                                    Console.WriteLine($"{Title},{MenuInfo.Serial}继续循环");
                                }
                                else
                                {
                                    Console.WriteLine($"{Title},{MenuInfo.Serial}执行失败，后面就不执行了哦！！！");
                                }
                                node.CurStatus = NodeStatus.None;

                                PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[MenuInfo.RootSerial]?.Clear();
                                return;
                            }

                            if (ModuleParam is IBranchRouter branchRouter
                                && branchRouter.SelectedNextSerials != null
                                && branchRouter.SelectedNextSerials.Count > 0
                                && !branchRouter.SelectedNextSerials.Contains(node.MenuInfo.Serial))
                            {
                                // 取消令牌版本是逐后续节点起任务，这里补一次路由过滤。
                                MarkDownstreamAsNotRun(new[] { node });
                                return;
                            }

                            Console.WriteLine($"{Title}成功执行，状态为{CurStatus}！！！");
                            if (node.ModuleParam != null && ModuleParam != null)
                            {
                                node.ModuleParam.moduleInputParam.TransmitParams = ModuleParam.moduleOutputParam.TransmitParams;
                            }

                            if (this == node)
                            {
                                MessageBox.Show("不能循环执行！");
                                return;
                            }

                            // 递归执行时传递取消令牌
                            node.ExecuteMulti(token);
                        }, token); // 将令牌传入Task.Run，支持任务取消
                    }
                }
                else
                {
                    // 无后续节点时的处理
                    Task.Run(() => {
                        // 检查取消状态
                        if (cancellationToken.IsCancellationRequested)
                        {
                            CurStatus = NodeStatus.Cancelled;
                            Console.WriteLine($"{Title},{MenuInfo.Serial}无后续节点任务检测到取消请求");
                            return;
                        }

                        if (ModuleParam.TriggerModuleRun == null)
                        {
                            Console.WriteLine($"{Title},{MenuInfo.Serial}没有执行方法！！！");
                            return;
                        }

                        CurStatus = ModuleParam.TriggerModuleRun.Invoke().RunStatus;

                        // 检查取消状态
                        if (cancellationToken.IsCancellationRequested)
                        {
                            CurStatus = NodeStatus.Cancelled;
                            return;
                        }

                        Console.WriteLine($"{Title}成功执行！！！");

                        if (IsLastNode && !PrismProvider.ProjectManager.SltCurSolutionItem.IsManual)
                        {
                            Console.WriteLine($"{Title},{MenuInfo.Serial}是最后一个节点！！！");
                            PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[MenuInfo.RootSerial]?.Remove(MenuInfo.Serial);
                        }
                    }, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 捕获取消操作异常
                CurStatus = NodeStatus.Cancelled;
                Console.WriteLine($"{Title},{MenuInfo.Serial}任务已被取消");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecuteMulti()_执行异常，异常信息：{ex.StackTrace}");
            }
        }

        // 取消令牌源（可根据实际情况从外部传入或在类内部管理）
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// 取消运行
        /// </summary>
        /// <returns></returns>
        public void CancelExecution()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                CurStatus = NodeStatus.Cancelled;
                Console.WriteLine($"{Title},{MenuInfo.Serial}任务已取消");
            }
        }
        #endregion

        #region Some static assist methods
        /// <summary>
        /// 深度优先遍历子元素
        /// </summary>
        private static void Traverse(NodeViewModel current, HashSet<NodeViewModel> visited, ref NodeViewModel lastElement)
        {
            // 若元素为空或已访问（循环引用），则跳过
            if (current == null || visited.Contains(current))
                return;

            // 标记为已访问
            visited.Add(current);

            // 更新最后访问的元素
            lastElement = current;

            // 递归遍历所有子元素（深度优先）
            foreach (var child in current.NextNodes)
            {
                Traverse(child, visited, ref lastElement);
            }
        }

        /// <summary>
        /// 批量设置所有元素（含嵌套和循环引用）的状态为 "none"
        /// </summary>
        /// <param name="list">根列表</param>
        public static void SetAllStatusToNone(List<NodeViewModel> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "列表不能为 null");

            var processed = new HashSet<NodeViewModel>(); // 记录已处理的元素，解决循环引用

            foreach (var item in list)
            {
                ProcessItem(item, processed);
            }
        }

        /// <summary>
        /// 递归处理单个元素及其子元素
        /// </summary>
        private static void ProcessItem(NodeViewModel item, HashSet<NodeViewModel> processed)
        {
            // 若元素为空或已处理（循环引用），则跳过
            if (item == null || processed.Contains(item))
                return;

            // 标记为已处理
            processed.Add(item);

            // 设置状态为 "none"
            item.CurStatus = NodeStatus.None;
            // 重置Merge节点的执行标志位
            Interlocked.Exchange(ref item._mergeExecuteFlag, 0);

            // 递归处理所有子元素
            foreach (var child in item.NextNodes)
            {
                ProcessItem(child, processed);
            }
        }

        /// <summary>
        /// 找到最长分支的最后一个节点，并将其IsLastNode设为true，其他所有节点设为false
        /// （若存在多个相同长度的最长分支，其最后节点均设为true，其余为false）
        /// </summary>
        /// <param name="rootList">根节点列表（含循环引用）</param>
        public static void MarkLastOfLongestBranch(List<NodeViewModel> rootList)
        {
            if (rootList == null || rootList.Count == 0)
                return;

            // 步骤1：收集所有节点（用于后续统一设为false）
            var allNodes = new HashSet<NodeViewModel>();
            CollectAllNodes(rootList, allNodes);

            // 步骤2：将所有节点的IsLastNode先设为false
            foreach (var node in allNodes)
            {
                node.IsLastNode = false;
            }

            // 步骤3：存储所有分支的信息：(分支长度, 最后一个节点)
            var branchInfos = new List<(int Length, NodeViewModel LastNode)>();

            // 遍历所有根节点，作为分支起点
            foreach (var root in rootList)
            {
                var visited = new HashSet<NodeViewModel>(); // 记录当前分支已访问节点（防循环）
                TraverseBranch(root, visited, 1, branchInfos);
            }

            if (branchInfos.Count == 0)
                return;

            // 步骤4：找到最长分支的长度
            int maxLength = branchInfos.Max(info => info.Length);

            // 步骤5：找到所有最长分支的最后节点并标记为true
            var longestLastNodes = branchInfos
                .Where(info => info.Length == maxLength)
                .Select(info => info.LastNode)
                .Distinct() // 去重（避免同一节点被多条最长分支共享）
                .ToList();

            foreach (var node in longestLastNodes)
            {
                node.IsLastNode = true;
                Console.WriteLine($"{node.Title},{node.MenuInfo.Serial}是最长分支的最后节点");
            }
        }

        public static void MarkLastOfEachBranch(int LastRoot, List<NodeViewModel> rootList)
        {
            if (rootList == null || rootList.Count == 0)
                return;

            // 步骤1：收集所有节点并初始化为false
            var allNodes = new HashSet<NodeViewModel>();
            CollectAllNodes(rootList, allNodes);
            foreach (var node in allNodes)
            {
                node.IsLastNode = false;
                node.MenuInfo.RootSerial = LastRoot;
            }

            // 步骤2：遍历所有分支，标记每个分支的最后一个节点（叶子节点）
            foreach (var root in rootList)
            {
                var visited = new HashSet<NodeViewModel>(); // 防循环引用
                TraverseEachBranch(LastRoot, root, visited);
            }
        }

        /// <summary>
        /// 遍历分支，标记当前分支的最后一个节点（叶子节点）
        /// </summary>
        private static void TraverseEachBranch(int LastRoot, NodeViewModel currentNode, HashSet<NodeViewModel> visited)
        {
            // 避免循环引用导致死循环
            if (visited.Contains(currentNode))
                return;
            visited.Add(currentNode);

            // 判断当前节点是否为分支的最后一个节点（没有子节点）
            if (currentNode.NextNodes == null || currentNode.NextNodes.Count == 0)
            {
                // 标记为分支的最后一个节点
                currentNode.IsLastNode = true;
                currentNode.MenuInfo.RootSerial = LastRoot;
                if (!PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[LastRoot].Keys.Contains(currentNode.MenuInfo.Serial))
                    PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[LastRoot].Add(currentNode.MenuInfo.Serial, false);
                else
                    Console.WriteLine($"已经包含了相同的键，无法重复添加，应该是由于父节点和子节点有同一个最后节点导致！！！");

                Console.WriteLine($"{currentNode.Title},{currentNode.MenuInfo.Serial}是当前分支的最后节点");
                visited.Remove(currentNode); // 回溯时移除当前节点，不影响其他分支
                return;
            }

            // 若有子节点，递归遍历所有子节点（每个子节点都是新的分支起点）
            foreach (var child in currentNode.NextNodes)
            {
                // 传递新的visited副本，避免不同分支互相干扰
                var childVisited = new HashSet<NodeViewModel>(visited);
                TraverseEachBranch(LastRoot, child, childVisited);
            }

            // 回溯时移除当前节点
            visited.Remove(currentNode);
        }

        /// <summary>
        /// 收集所有节点（包括嵌套和循环引用的节点）
        /// </summary>
        private static void CollectAllNodes(List<NodeViewModel> nodes, HashSet<NodeViewModel> allNodes)
        {
            foreach (var node in nodes)
            {
                if (node == null || allNodes.Contains(node))
                    continue;

                allNodes.Add(node);
                // 递归收集子节点
                CollectAllNodes(node.NextNodes, allNodes);
            }
        }

        /// <summary>
        /// 递归遍历分支，收集分支长度和最后节点信息
        /// </summary>
        private static void TraverseBranch(NodeViewModel currentNode, HashSet<NodeViewModel> visited, int currentLength, List<(int, NodeViewModel)> branchInfos)
        {
            // 若节点为空或已访问（循环引用），当前分支终止
            if (currentNode == null || visited.Contains(currentNode))
                return;

            // 标记当前节点为已访问（当前分支内）
            visited.Add(currentNode);

            // 检查是否有未访问的子节点（判断是否为分支终点）
            var unvisitedChildren = currentNode.NextNodes
                .Where(child => child != null && !visited.Contains(child))
                .ToList();

            if (!unvisitedChildren.Any())
            {
                // 无未访问子节点，当前节点为分支终点
                branchInfos.Add((currentLength, currentNode));
                return;
            }

            // 递归遍历所有未访问子节点（每个子节点形成新的分支）
            foreach (var child in unvisitedChildren)
            {
                // 复制已访问集合（避免不同分支互相干扰）
                var newVisited = new HashSet<NodeViewModel>(visited);
                TraverseBranch(child, newVisited, currentLength + 1, branchInfos);
            }
        }
        #endregion
    }
}
