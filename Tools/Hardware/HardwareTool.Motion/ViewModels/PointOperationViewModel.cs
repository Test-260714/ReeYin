namespace HardwareTool.Motion.ViewModels
{
    public class PointOperationViewModel : MotionOperationViewModelBase
    {
        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            ExecuteSelectedMovement();
        });
    }
}
