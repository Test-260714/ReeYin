using HardwareTool.Motion.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Windows;

namespace HardwareTool.Motion.ViewModels
{
    public abstract class MotionOperationViewModelBase : BindableBase, INavigationAware
    {
        [JsonIgnore]
        private MotionModel _modelParam = null!;

        public MotionModel ModelParam
        {
            get => _modelParam;
            set => SetProperty(ref _modelParam, value);
        }

        protected bool EnsureControlCard()
        {
            if (ModelParam == null) return false;

            ModelParam.RefreshControlCardContext();

            if (ModelParam.ControlCard == null && !string.IsNullOrWhiteSpace(ModelParam.SltModelName))
            {
                var controlCard = ModelParam.Models?.FirstOrDefault(c => c.NickName == ModelParam.SltModelName);
                if (controlCard != null)
                {
                    ModelParam.ControlCard = controlCard;
                }
            }

            if (ModelParam.ControlCard != null)
            {
                return true;
            }

            MessageBox.Show("请先选择控制卡。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        protected void ExecuteSelectedMovement()
        {
            if (ModelParam?.SltMovementLocus == null) return;
            if (!EnsureControlCard()) return;

            ModelParam.CustomMoving(ModelParam.SltMovementLocus);
        }

        protected void ApplyPosCompare()
        {
            if (ModelParam?.SltMovementLocus == null) return;
            if (!EnsureControlCard()) return;

            ModelParam.SwitchPosCompare(ModelParam.SltMovementLocus);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<MotionModel>("ModelParam") ?? new MotionModel();
            ModelParam.RefreshControlCardContext();
        }
    }
}
