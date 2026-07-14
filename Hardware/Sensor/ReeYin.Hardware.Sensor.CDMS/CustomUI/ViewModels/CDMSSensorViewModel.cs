using Prism.Commands;
using Prism.Dialogs;
using ReeYin.Hardware.Sensor.CDMS.CustomUI.Models;
using ReeYin_V.Core.IOC;
using System.Linq;

namespace ReeYin.Hardware.Sensor.CDMS.CustomUI.ViewModels
{
    public class CDMSSensorViewModel : DialogViewModelBase
    {
        private CDMSSensorModel _modelParam = new();
        public new CDMSSensorModel ModelParam
        {
            get => _modelParam;
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private string _sampleText = string.Empty;
        public string SampleText
        {
            get => _sampleText;
            set { _sampleText = value; RaisePropertyChanged(); }
        }

        public override void InitParam()
        {
            ModelParam.Sensor = Param is CDMSSensor sensor ? sensor : new CDMSSensor();
            UpdateSampleText();
        }

        public DelegateCommand<string> GeneralCommand => new(order =>
        {
            if (ModelParam.Sensor == null)
            {
                return;
            }

            switch (order)
            {
                case "Connect":
                    ModelParam.Sensor.Init();
                    break;
                case "Disconnect":
                    ModelParam.Sensor.Close();
                    break;
                case "Refresh":
                    ModelParam.Sensor.RefreshDeviceParameters();
                    break;
                case "RefreshChannel":
                    ModelParam.Sensor.RefreshChannelParameters();
                    break;
                case "ReadSingle":
                    ModelParam.Sensor.ReceiveSensorData();
                    UpdateSampleText();
                    break;
                case "StartCollect":
                    ModelParam.Sensor.StartCollect();
                    break;
                case "StopCollect":
                    ModelParam.Sensor.StopCollect();
                    break;
                case "Confirm":
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;
            }
        });

        public DelegateCommand<string> ApplyCommand => new(order =>
        {
            CDMSSensor sensor = ModelParam.Sensor;
            switch (order)
            {
                case "DeviceID":
                    sensor.SetDeviceID(sensor.Config.DeviceID);
                    break;
                case "DevicePN":
                    sensor.SetDevicePN(sensor.Config.DevicePN);
                    break;
                case "ChannelEnabled":
                    sensor.SetChannelEnabled(sensor.Config.ChannelEnabled);
                    break;
                case "FreqBW":
                    sensor.SetFreqBW(sensor.Config.FreqBW);
                    break;
                case "OutDataMode":
                    sensor.SetOutDataMode(sensor.Config.OutDataMode);
                    break;
                case "TriggerMode":
                    sensor.SetTriggerMode(sensor.Config.TriggerMode);
                    break;
                case "FSO":
                    sensor.SetFSO(sensor.Config.FSO);
                    break;
                case "IniDistance":
                    sensor.SetIniDistance(sensor.Config.IniDistance);
                    break;
                case "Zero":
                    sensor.SetZero(sensor.Config.Zero);
                    break;
                case "One":
                    sensor.SetOne(sensor.Config.One);
                    break;
                case "SSPN":
                    sensor.SetSSPN(sensor.Config.SSPN);
                    break;
                case "SSPNBackup":
                    sensor.SetSSPNBackup(sensor.Config.SSPNBackup);
                    break;
            }
        });

        private void UpdateSampleText()
        {
            SampleText = ModelParam.Sensor.Config.LastData.Length == 0
                ? string.Empty
                : string.Join(", ", ModelParam.Sensor.Config.LastData.Select(v => v.ToString("G6")));
        }
    }
}
