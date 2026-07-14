using System.Windows;

namespace HardwareTool.Motion.ViewModels
{
    public class ArcOperationViewModel : MotionOperationViewModelBase
    {
        public DelegateCommand ValidateCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.SltMovementLocus == null)
            {
                MessageBox.Show("当前没有可校验的圆弧轨迹。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ModelParam.TryValidateArcMovement(ModelParam.SltMovementLocus, out string errorMessage))
            {
                MessageBox.Show("当前圆弧参数有效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"当前圆弧参数无效：{errorMessage}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        });

        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            ExecuteSelectedMovement();
        });
    }
}
