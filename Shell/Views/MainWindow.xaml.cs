using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.UI.Style.CustomWins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReeYin_V.Shell.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : VisionWindow
    {
        private bool _isClosingSubWindows;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyUp += MainWindow_KeyDown;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (e.Cancel || _isClosingSubWindows)
            {
                return;
            }

            CloseSubWindows();

            if (HasSubWindows())
            {
                e.Cancel = true;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否同时按下了Ctrl和Q键
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
                e.Key == Key.Q)
            {
                // 标记事件已处理，防止继续传播
                e.Handled = true;

                // 执行Ctrl+Q对应的操作
                HandleCtrlQAction();
            }
        }

        /// <summary>
        /// 进入开发者模式
        /// </summary>
        private void HandleCtrlQAction()
        {
            ConsoleHelper.ToggleConsole();
        }

        private void CloseSubWindows()
        {
            var subWindows = Application.Current.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, this))
                .ToList();

            if (subWindows.Count == 0)
            {
                return;
            }

            _isClosingSubWindows = true;
            try
            {
                foreach (var window in subWindows)
                {
                    window.Close();
                }
            }
            finally
            {
                _isClosingSubWindows = false;
            }
        }

        private bool HasSubWindows()
        {
            return Application.Current.Windows
                .OfType<Window>()
                .Any(window => !ReferenceEquals(window, this));
        }
    }
}
