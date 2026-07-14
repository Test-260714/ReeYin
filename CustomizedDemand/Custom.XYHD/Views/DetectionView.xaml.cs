using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Custom.XYHD.Views
{
    public partial class DetectionView : UserControl
    {
        private static FrameworkElement _runHost;
        private static int _runHostGuardRefCount;
        private static bool _updatingRunHostState;

        private FrameworkElement _attachedRunHost;

        public DetectionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachRunHostGuard();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachRunHostGuard();
        }

        private void AttachRunHostGuard()
        {
            var runHost = FindRunMainHost();
            if (runHost == null)
                return;

            _attachedRunHost = runHost;

            if (!ReferenceEquals(_runHost, runHost))
            {
                if (_runHost != null)
                {
                    _runHost.IsEnabledChanged -= OnRunHostIsEnabledChanged;
                    RestoreRunHostBinding(_runHost);
                }

                _runHost = runHost;
                _runHostGuardRefCount = 0;
                _runHost.IsEnabledChanged += OnRunHostIsEnabledChanged;
            }

            _runHostGuardRefCount++;
            ForceRunHostEnabled(runHost);
        }

        private void DetachRunHostGuard()
        {
            if (_attachedRunHost == null || !ReferenceEquals(_attachedRunHost, _runHost))
            {
                _attachedRunHost = null;
                return;
            }

            _runHostGuardRefCount = Math.Max(0, _runHostGuardRefCount - 1);
            if (_runHostGuardRefCount == 0 && _runHost != null)
            {
                _runHost.IsEnabledChanged -= OnRunHostIsEnabledChanged;
                RestoreRunHostBinding(_runHost);
                _runHost = null;
            }

            _attachedRunHost = null;
        }

        private static void OnRunHostIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement runHost && !runHost.IsEnabled)
                ForceRunHostEnabled(runHost);
        }

        private static void ForceRunHostEnabled(FrameworkElement runHost)
        {
            if (runHost == null || _updatingRunHostState)
                return;

            try
            {
                _updatingRunHostState = true;
                BindingOperations.ClearBinding(runHost, IsEnabledProperty);
                runHost.IsEnabled = true;
            }
            finally
            {
                _updatingRunHostState = false;
            }
        }

        private static void RestoreRunHostBinding(FrameworkElement runHost)
        {
            if (runHost == null)
                return;

            BindingOperations.ClearBinding(runHost, IsEnabledProperty);
            BindingOperations.SetBinding(runHost, IsEnabledProperty, new Binding("IsEnable")
            {
                Mode = BindingMode.OneWay
            });
        }

        private FrameworkElement FindRunMainHost()
        {
            DependencyObject current = this;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    string.Equals(element.GetType().Name, "RunMainView", StringComparison.Ordinal))
                {
                    return element;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
