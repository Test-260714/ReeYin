using Prism.Mvvm;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.Models
{
    public sealed class ZMotionIoDiagnosticItem : BindableBase
    {
        private bool state;

        public ZMotionIoDiagnosticItem(int port, bool isOutput)
        {
            Port = port;
            IsOutput = isOutput;
        }

        public int Port { get; }

        public bool IsOutput { get; }

        public string Name => $"{(IsOutput ? "OUT" : "IN")}{Port}";

        public bool State
        {
            get => state;
            set => SetProperty(ref state, value);
        }
    }
}
