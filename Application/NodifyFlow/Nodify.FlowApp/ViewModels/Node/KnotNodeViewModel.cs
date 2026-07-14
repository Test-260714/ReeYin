using Newtonsoft.Json;
using ReeYin_V.NodifyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Nodify.FlowApp
{
    [Serializable]
    public class KnotNodeViewModel : NodeViewModel
    {
        public KnotNodeViewModel(Orientation orientation)
        {
            Orientation = orientation;
        }

        public KnotNodeViewModel() : this(Orientation.Horizontal)
        {
        }
        [JsonIgnore]
        private ConnectorViewModel _connector = default!;
        public ConnectorViewModel Connector
        {
            get => _connector;
            set
            {
                if (SetProperty(ref _connector, value))
                {
                    _connector.Node = this;
                }
            }
        }

        public ConnectorFlow Flow { get; set; }
    }
}
