using ReeYin_V.Core.IOC;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Events;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Nodify.FlowApp
{
    public partial class EditorView : UserControl
    {
        public NodifyEditor EditorInstance => Editor;

        public EditorView()
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
                if(calculator.SelectedOperations.Count == 0)
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
            if(e.Source is NodifyEditor editor && editor.DataContext is CalculatorViewModel calculator
                && e.Data.GetData(typeof(OperationInfoViewModel)) is OperationInfoViewModel operation)
            {
                OperationViewModel op = OperationFactory.GetOperation(operation);
                op.Location = editor.GetLocationInsideEditor(e);
                calculator.Operations.Add(op);

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
            if(e.LeftButton == MouseButtonState.Pressed && ((FrameworkElement)sender).DataContext is OperationInfoViewModel operation)
            { 
                var data = new DataObject(typeof(OperationInfoViewModel), operation);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
            }
        }

        private void Minimap_Zoom(object sender, Events.ZoomEventArgs e)
        {
            EditorInstance.ZoomAtPosition(e.Zoom, e.Location);
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
    }
}
