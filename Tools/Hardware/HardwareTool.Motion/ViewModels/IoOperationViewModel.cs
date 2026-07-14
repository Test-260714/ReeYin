namespace HardwareTool.Motion.ViewModels
{
    public class IoOperationViewModel : MotionOperationViewModelBase
    {
        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            ExecuteSelectedMovement();
        });
    }
}
