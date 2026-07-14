using Nodify.Interactivity;
using ReeYin_V.Core;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;

namespace Nodify.FlowApp
{
    /// <summary>
    /// ApplicationView.xaml 的交互逻辑
    /// </summary>
    public partial class AppView : UserControl
    {
        #region Fields  

        private readonly Random _rand = new Random();
        #endregion

        #region Constructor 
        public AppView()
        {
            InitializeComponent();
            EditorGestures.Mappings.Editor.Cutting.Unbind();
            //this.DataContext = new AppViewModel();

            CompositionTargetEx.Rendering += OnRendering;

            //订阅切换样式事件
            PrismProvider.EventAggregator.GetEvent<SwitchStyleEvent>().Subscribe(Switch, ThreadOption.UIThread);
            //PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Subscribe(OperateSolution, ThreadOption.UIThread);
            PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Subscribe(OperateSolution, ThreadOption.UIThread);
            PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Subscribe(LoadNodes, ThreadOption.UIThread);
            PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Subscribe(DelNodes, ThreadOption.UIThread);

            OperateSolution("打开");

            group1.ItemsSource = new List<object> { "日志", "轴", 2, 3 };
        }
        #endregion

        #region Commands
        private void BringIntoView_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppViewModel model)
            {
                NodifyObservableCollection<NodeViewModel> nodes = model.GraphViewModel.Nodes;
                int index = _rand.Next(nodes.Count);

                if (nodes.Count > index)
                {
                    NodeViewModel node = nodes[index];
                    EditorCommands.BringIntoView.Execute(node.Location, EditorView.Editor);
                }
            }
        }

        private void AnimateConnections_Click(object sender, RoutedEventArgs e)
        {
            EditorSettings.Instance.IsAnimatingConnections = !EditorSettings.Instance.IsAnimatingConnections;
        }

        private void UCWaterDropsButtonGroup_Click(object sender, RoutedEventArgs e)
        {
            PrismProvider.Dispatcher.BeginInvoke(() =>
            {
                FrameworkElement el = (FrameworkElement)sender;
                UCWaterDropsButtonGroupRoutedEventArgs EventArgs = (UCWaterDropsButtonGroupRoutedEventArgs)e;
                Console.WriteLine(el.Name);
                //表示日志
                if (EventArgs.Index == 1)
                    PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Publish($"AxisView");

                if (EventArgs.Index == 2)
                {
                    PrismProvider.DialogService.Show("ChartView", new DialogParameters
                        {
                            { "Title", "高度/灰度图预览" },
                            { "Icon", "\ue673" },
                        }, result =>
                        {

                        }, nameof(DialogWindowView));
                }


            });
        }
        #endregion

        #region Methods
        private void OnRendering(double fps)
        {
            FPSText.Text = fps.ToString("0");
        }

        public void LoadNodes(string solution)
        {
            if (solution != "加载")
                return;
            var start = DateTime.Now;

            try
            {
                if (DataContext is not AppViewModel model || model.GraphViewModel?.Nodes == null)
                    return;

                NodifyObservableCollection<NodeViewModel> nodes = model.GraphViewModel.Nodes;

                if (PrismProvider.ProjectManager.SltCurSolutionItem?.GlobalParams == null)
                    return;

                HashSet<Guid> nodeIds = nodes.Select(p => p.Id).ToHashSet();
                ObservableCollection<TransmitParam> temp = new ObservableCollection<TransmitParam>();
                foreach (var param in PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams)
                {
                    if (!nodeIds.Contains(param.LinkGuid))
                    {
                        temp.Add(param);
                    }
                }
                PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.RemoveRange(temp);
            }
            finally
            {
                PrismProvider.ProjectManager.IsOpenSolution = false;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：加载所有节点耗时：" + DateTime.Now.Subtract(start).TotalMilliseconds);
            }
        }

        /// <summary>
        /// 切换资源
        /// </summary>
        /// <param name="style"></param>
        private void Switch(string style)
        {
            switch (style)
            {
                case "Dark":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Dark.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Dark.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
                case "Light":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Light.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Light.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
                case "Nodify":
                    {
                        this.Resource.MergedDictionaries[0].Source = new Uri("pack://application:,,,/Nodify;component/Themes/Nodify.xaml", UriKind.RelativeOrAbsolute);
                        this.Resource.MergedDictionaries[1].Source = new Uri("pack://application:,,,/ReeYin_V.NodifyManager;component/UI/Style/Nodify.xaml", UriKind.RelativeOrAbsolute);
                    }
                    break;
            }
        }

        // 解决方案操作相关方法已移至 AppView.Solution.cs

        /// <summary>
        /// 删除节点时移除项目管理中的相关缓存
        /// 指令@节点号
        /// </summary>
        /// <param name="order"></param>
        public void DelNodes(string order)
        {
            if (order != "删除节点")
                return;
            var Num = int.Parse(order.Split('@')[1]);

            PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Remove(Num.ToString());

            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Remove(Num.ToString("D3") + "_" + $"{Name}");
        }
        #endregion


    }

    /// <summary>
    /// 获取FPS
    /// </summary>
    public static class CompositionTargetEx
    {
        private static TimeSpan _last = TimeSpan.Zero;
        private static event Action<double>? FrameUpdating;

        public static event Action<double> Rendering
        {
            add
            {
                if (FrameUpdating == null)
                {
                    CompositionTarget.Rendering += OnRendering;
                }
                FrameUpdating += value;
            }
            remove
            {
                FrameUpdating -= value;
                if (FrameUpdating == null)
                {
                    CompositionTarget.Rendering -= OnRendering;
                }
            }
        }

        private static void OnRendering(object? sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;
            var renderingTime = args.RenderingTime;
            if (renderingTime == _last)
                return;

            double fps = 1000 / (renderingTime - _last).TotalMilliseconds;
            _last = renderingTime;
            FrameUpdating?.Invoke(fps);
        }



    }
}
