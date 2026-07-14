using ReeYin_V.Hardware.ControlCard.Models;

namespace HardwareTool.Motion.ViewModels
{
    public class PosCompareOperationViewModel : MotionOperationViewModelBase
    {
        public DelegateCommand ApplyCommand => new DelegateCommand(() =>
        {
            ApplyPosCompare();
        });

        public DelegateCommand AddCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.SltMovementLocus?.PosComparisonParam == null) return;

            ModelParam.SltMovementLocus.PosComparisonParam.PosCompareDatas.Add(new PosCompareData());
        });

        public DelegateCommand RemoveCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.SltMovementLocus?.PosComparisonParam?.SltPosCompareData == null) return;

            ModelParam.SltMovementLocus.PosComparisonParam.PosCompareDatas.Remove(ModelParam.SltMovementLocus.PosComparisonParam.SltPosCompareData);
            ModelParam.SltMovementLocus.PosComparisonParam.SltPosCompareData = null!;
        });

        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            ExecuteSelectedMovement();
        });
    }
}
