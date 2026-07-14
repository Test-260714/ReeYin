using ReeYin_V.NodifyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Nodify.FlowApp
{
    [Serializable]
    public class OperationGraphViewModel : InnerGraphViewModel
    {
        private Size _size;
        public Size DesiredSize
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        private Size _prevSize;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                {
                    if (_isExpanded)
                    {
                        DesiredSize = _prevSize;
                    }
                    else
                    {
                        //_prevSize = Size;
                        // Fit content
                        DesiredSize = new Size(double.NaN, double.NaN);
                    }
                }
            }
        }

        public OperationGraphViewModel()
        {
            //InnerCalculator.Operations[0].Location = new Point(50, 50);
            //InnerCalculator.Operations[1].Location = new Point(200, 50);
        }
    }

    public class InnerGraphViewModel : NodeViewModel
    {
        public NodifyEditorViewModel InnerView { get; } = new NodifyEditorViewModel();

        private NodeViewModel InnerOutput { get; } = new NodeViewModel
        {
            Title = "Output Parameters",

            Location = new Point(500, 300),
            //IsReadOnly = true
        };

        private NodeViewModel InnerInput { get; } = new NodeViewModel
        {
            Title = "Input Parameters",
            Location = new Point(300, 300),
            //IsReadOnly = true
        };

        public InnerGraphViewModel()
        {
            InnerView.Nodes.Add(InnerInput);
            InnerView.Nodes.Add(InnerOutput);

            //Output = new ConnectorViewModel();

            //InnerOutput.Input[0].ValueObservers.Add(Output);

            //InnerInput.Output.ForEach(x => Input.Add(new ConnectorViewModel
            //{
            //    Title = x.Title
            //}));

            //InnerInput.Output
            //    .WhenAdded(x => Input.Add(new ConnectorViewModel
            //    {
            //        Title = x.Title
            //    }))
            //    .WhenRemoved(x => Input.RemoveOne(i => i.Title == x.Title));
        }

        //protected override void OnInputValueChanged()
        //{
        //    for (var i = 0; i < Input.Count; i++)
        //    {
        //        //InnerInput.Output[i].Value = Input[i].Value;
        //    }
        //}
    }
}
