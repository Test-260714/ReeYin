using Nodify.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Nodify.FlowApp
{
    /// <summary>
    /// FlowStateView.xaml 的交互逻辑
    /// </summary>
    public partial class FlowStateView : UserControl
    {
        public FlowStateView()
        {
            InitializeComponent();

            ConnectorState.EnableToggledConnectingMode = true;
            NodifyEditor.EnableCuttingLinePreview = true;

            EditorGestures.Mappings.Connection.Disconnect.Unbind();
            EditorGestures.Mappings.Editor.ZoomModifierKey = ModifierKeys.Control;
            EditorGestures.Mappings.Editor.PanWithMouseWheel = true;
        }

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Shift)
                return;

            var scrollViewer = (ScrollViewer)sender;

            if (e.Key == Key.PageUp)
            {
                scrollViewer.PageLeft();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                scrollViewer.PageRight();
                e.Handled = true;
            }
        }
    }
}
