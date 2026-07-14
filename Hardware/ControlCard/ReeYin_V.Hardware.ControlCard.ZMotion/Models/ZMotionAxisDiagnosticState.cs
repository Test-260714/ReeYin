using Prism.Mvvm;
using ReeYin_V.Core.Services.Project;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.Models
{
    public sealed class ZMotionAxisDiagnosticState : BindableBase
    {
        private double position;
        private bool isEnabled;
        private bool isIdle;

        public ZMotionAxisDiagnosticState(En_AxisNum axisNum, short axisNo, string? nickName)
        {
            AxisNum = axisNum;
            AxisNo = axisNo;
            NickName = string.IsNullOrWhiteSpace(nickName) ? axisNum.ToString() : nickName;
        }

        public En_AxisNum AxisNum { get; }

        public short AxisNo { get; }

        public short ControllerAxisId => (short)(AxisNo - 1);

        public string NickName { get; }

        public string DisplayName => $"{AxisNum}轴 · {NickName} · 控制器轴{ControllerAxisId}";

        public double Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public bool IsIdle
        {
            get => isIdle;
            set => SetProperty(ref isIdle, value);
        }

        public string MotionStateText => IsIdle ? "空闲" : "运动中";

        public string EnableStateText => IsEnabled ? "已使能" : "未使能";

        public void Update(double currentPosition, bool enabled, bool idle)
        {
            Position = currentPosition;
            IsEnabled = enabled;
            IsIdle = idle;
            RaisePropertyChanged(nameof(MotionStateText));
            RaisePropertyChanged(nameof(EnableStateText));
        }
    }
}
