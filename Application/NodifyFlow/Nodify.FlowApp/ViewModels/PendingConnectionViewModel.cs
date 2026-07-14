using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System.Windows;
using System.Windows.Controls;

namespace Nodify.FlowApp
{
    [Serializable]
    public class PendingConnectionViewModel : ObservableObject
    {
        private NodifyEditorViewModel _graph = default!;
        public NodifyEditorViewModel Graph
        {
            get => _graph;
            internal set => SetProperty(ref _graph, value);
        }


        [JsonIgnore]
        private ConnectorViewModel _source = default!;
        public ConnectorViewModel Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }
        [JsonIgnore]
        private ConnectorViewModel? _target;
        public ConnectorViewModel? Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }
        [JsonIgnore]
        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
        [JsonIgnore]
        private Point _targetLocation;
        public Point TargetLocation
        {
            get => _targetLocation;
            set => SetProperty(ref _targetLocation, value);
        }

        private object? _previewTarget;
        public object? PreviewTarget
        {
            get => _previewTarget;
            set
            {
                if (SetProperty(ref _previewTarget, value))
                {
                    OnPreviewTargetChanged();
                }
            }
        }

        private string? _previewText;
        public string? PreviewText
        {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
        }

        private Orientation _targetOrientation;
        public Orientation TargetOrientation
        {
            get => _targetOrientation;
            set => SetProperty(ref _targetOrientation, value);
        }

        protected virtual void OnPreviewTargetChanged()
        {
            bool canConnect = PreviewTarget != null && Graph.Schema.CanAddConnection(Source!, PreviewTarget);
            PreviewText = PreviewTarget switch
            {
                ConnectorViewModel con when con == Source => $"Can't connect to self",
                ConnectorViewModel con => $"{(canConnect ? "Connect" : "Can't connect")} to {con.Title ?? "pin"}",
                FlowNodeViewModel flow => $"{(canConnect ? "Connect" : "Can't connect")} to {flow.Title ?? "node"}",
                _ => null
            };

            SetTargetOrientation();
        }

        private void SetTargetOrientation()
        {
            TargetOrientation = PreviewTarget switch
            {
                ConnectorViewModel con when con.Node is FlowNodeViewModel flow => flow.Orientation,
                FlowNodeViewModel flow => flow.Orientation,
                NodifyEditorViewModel editor when Source?.Node is FlowNodeViewModel flow => flow.Orientation,
                _ => Orientation.Horizontal,
            };
        }
    }
}
