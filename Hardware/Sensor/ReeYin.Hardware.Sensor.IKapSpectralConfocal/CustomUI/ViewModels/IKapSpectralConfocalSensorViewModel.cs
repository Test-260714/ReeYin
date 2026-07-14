using Prism.Commands;
using ReeYin.Hardware.Sensor.IKapSpectralConfocal.CustomUI.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;

namespace ReeYin.Hardware.Sensor.IKapSpectralConfocal.CustomUI.ViewModels
{
    public class IKapSpectralConfocalSensorViewModel : DialogViewModelBase
    {
        private IKapSpectralConfocalSensorModel _modelParam = new IKapSpectralConfocalSensorModel();

        public new IKapSpectralConfocalSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public override void InitParam()
        {
            if (Param is IKapSpectralConfocalSensor sensor)
            {
                ModelParam.Sensor = sensor;
            }
            else
            {
                ModelParam.Sensor = new IKapSpectralConfocalSensor();
            }
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;

                case "Confirm":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;
            }
        });
    }
}