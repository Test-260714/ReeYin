using NetTaste;
using Newtonsoft.Json;
using NLog.Targets;
using ReeYin_V.Core;
using ReeYin_V.Core.Events.NodifyRalated;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.UI.Style.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using static Dm.net.buffer.ByteArrayBuffer;

namespace Nodify.FlowApp
{
    /// <summary>
    /// NodifyEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class NodifyEditorView : UserControl
    {
        //private readonly Random _rand = new Random();
        public NodifyEditor EditorInstance => Editor;

        public NodifyEditorView()
        {
            InitializeComponent();
            //// 注册鼠标事件
            EventManager.RegisterClassHandler(typeof(NodifyEditor), MouseLeftButtonDownEvent, new MouseButtonEventHandler(CloseOperationsMenu), true);
            EventManager.RegisterClassHandler(typeof(NodifyEditor), MouseRightButtonUpEvent, new MouseButtonEventHandler(OpenOperationsMenu));

            //订阅切换样式事件
            PrismProvider.EventAggregator.GetEvent<SwitchStyleEvent>().Subscribe(Switch, ThreadOption.UIThread);
        }

        /// <summary>
        /// 打开操作菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenOperationsMenu(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is NodifyEditor editor && editor.DataContext is CalculatorViewModel calculator)
            {
                if (calculator.SelectedOperations.Count == 0)
                {
                    e.Handled = true;
                    calculator.OperationsMenu.OpenAt(editor.MouseLocation);
                }
                else
                {
                    //选中了操作节点，打开对应操作菜单
                    e.Handled = true;
                    MessageBox.Show($"打开“{calculator.SelectedOperations[0].Title}”操作页面");
                }
            }
        }

        /// <summary>
        /// 关闭操作菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseOperationsMenu(object sender, MouseButtonEventArgs e)
        {
            ItemContainer? itemContainer = sender as ItemContainer;
            NodifyEditor? editor = sender as NodifyEditor ?? itemContainer?.Editor;

            if (editor?.DataContext is CalculatorViewModel calculator)
            {
                calculator.OperationsMenu.Close();
            }
        }

        /// <summary>
        /// 拖拽节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDropNode(object sender, DragEventArgs e)
        {
            if (e.Source is NodifyEditor editor && editor.DataContext is NodifyEditorViewModel MainGraph
                && e.Data.GetData(typeof(OperationInfoViewModel)) is OperationInfoViewModel operation)
            {
                var temp = operation.DeepClone();

                var NewNode = new List<FlowNodeViewModel>();
                //获取当前位置
                Point location = editor.GetLocationInsideEditor(e);
                if(temp.MenuInfo.NodeType == NodeType.Start || temp.MenuInfo.NodeType == NodeType.Monitor)
                {
                    //创建对应节点
                    NewNode = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(1, location)
                    {
                        MinNodesCount = 1,
                        MaxNodesCount = 1,
                        MinInputCount = 0,
                        MaxInputCount = 0,
                        MinOutputCount = 1,
                        MaxOutputCount = 1,
                        GridSnap = EditorSettings.Instance.GridSpacing
                    });
                }
                else if(temp.MenuInfo.NodeType == NodeType.General || temp.MenuInfo.NodeType == NodeType.Merge)
                {
                    //创建对应节点
                    NewNode = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(1, location)
                    {
                        MinNodesCount = 1,
                        MaxNodesCount = 1,
                        MinInputCount = 1,
                        MaxInputCount = 1,
                        MinOutputCount = 1,
                        MaxOutputCount = 1,
                        GridSnap = EditorSettings.Instance.GridSpacing
                    });
                }
                else if (temp.MenuInfo.NodeType == NodeType.Finish)
                {
                    //创建对应节点
                    NewNode = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(1, location)
                    {
                        MinNodesCount = 1,
                        MaxNodesCount = 1,
                        MinInputCount = 1,
                        MaxInputCount = 1,
                        MinOutputCount = 0,
                        MaxOutputCount = 0,
                        GridSnap = EditorSettings.Instance.GridSpacing
                    });
                }
                else if (temp.MenuInfo.NodeType == NodeType.Graph)
                {
                    //创建对应节点
                    MainGraph.Nodes.Add(new OperationGraphViewModel());
                }

                //获取节点所有的Serial，
                temp.MenuInfo.Serial = UniversalMethods.FindMissingNumber(MainGraph.Nodes.Select(x => x.MenuInfo.Serial).ToList());
                NewNode[0].MenuInfo = temp.MenuInfo;
                NewNode[0].Title = temp.Title;
                NewNode[0].Icon = temp.Icon;
                NewNode[0].Id = Guid.NewGuid();
                //添加对应节点类型至节点列表
                MainGraph.Nodes.AddRange(NewNode);

                var datetime = DateTime.Now;

                //拖拽完成后，初始化参数
                foreach (var node in NewNode)
                {
                    if (node.ModuleParam == null)
                        node.ModuleParam = new ModuleParamBase();
                    var Order = temp.MenuInfo;
                    node.ModuleParam.Serial = Order.Serial;
                    //拖出来就是得初始化配置参数
                    if (Order.NodeType == NodeType.Monitor)
                        PrismProvider.Dispatcher.InvokeAsync(() =>
                        {
                            PrismProvider.DialogService.Show(Order?.TargetType.Name, new DialogParameters
                            {
                            { "Title",Order.Serial.ToString("D3") + "_" + Order.Title },
                            { "Serial", Order.Serial },
                            { "Icon", Order.Icon },
                            { "Param", node.ModuleParam },
                            //设置隐藏
                            { "Visibility", Visibility.Hidden },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {

                                    node.ModuleParam = result.Parameters.GetValue<object>("Param") as IModuleParam;
                                }
                            }, nameof(SingleInstanceDialogWindowView));
                        });

                }
                Console.WriteLine($"组件加载完成{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}："+DateTime.Now.Subtract(datetime).TotalMilliseconds);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 开始拖拽节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNodeDrag(object sender, MouseEventArgs e)
        {
            var operation1 = ((FrameworkElement)sender).DataContext as OperationInfoViewModel;
            if (e.LeftButton == MouseButtonState.Pressed && ((FrameworkElement)sender).DataContext is OperationInfoViewModel operation)
            {
                var data = new DataObject(typeof(OperationInfoViewModel), operation);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
            }
        }

        private void Minimap_Zoom(object sender, Events.ZoomEventArgs e)
        {
            EditorInstance.ZoomAtPosition(e.Zoom, e.Location);
        }

        #region Methods
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

        #endregion

        private void Editor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            PrismProvider.EventAggregator.GetEvent<NodifySelecteChangedEvent>().Publish((this.DataContext as NodifyEditorViewModel).SelectedNode);
        }

        private void Editor_Selected(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 转为翻译后的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            var textbock = sender as TextBlock;
            textbock.Text = PrismProvider.LanguageManager.GetStringResource(textbock.ToolTip.ToString());
        }
    }
}
