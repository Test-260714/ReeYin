using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Custom.EVEMFDJC.ViewModels
{
    public class EvePlcControlViewModel : DialogViewModelBase
    {
        private string _plcAddress { get; set; }

        public PLCBase _curPlc { get; set; }
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "启动":
                    if (_curPlc == null)
                    {
                        Console.WriteLine($"亿纬PLC操作页面没连上PLC");
                        return;
                    }
                    _plcAddress = "MR905";
                    var Parama = new PLCParaInfoModel
                    {
                        PLCAddress = _plcAddress,
                        ParaType = EnumParaInfoModelParaType.Bool,
                        ParaValue = true
                    };
                    if (_curPlc.WritePLCPara(Parama))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Parama.ParaValue}成功！");
                    }
                    Thread.Sleep(100);
                    Parama.ParaValue = false;
                    if (_curPlc.WritePLCPara(Parama))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Parama.ParaValue}成功！");
                    }
                    break;
                case "复位":
                    if (_curPlc == null)
                    {
                        Console.WriteLine($"亿纬PLC操作页面没连上PLC");
                        return;
                    }
                    _plcAddress = "MR904";
                    var Paramb = new PLCParaInfoModel
                    {
                        PLCAddress = _plcAddress,
                        ParaType = EnumParaInfoModelParaType.Bool,
                        ParaValue = true
                    };
                    if (_curPlc.WritePLCPara(Paramb))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Paramb.ParaValue}成功！");
                    }
                    Thread.Sleep(100);
                    Paramb.ParaValue = false;
                    if (_curPlc.WritePLCPara(Paramb))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Paramb.ParaValue}成功！");
                    }
                    break;
                case "光栅屏蔽":
                    if (_curPlc == null)
                    {
                        Console.WriteLine($"亿纬PLC操作页面没连上PLC");
                        return;
                    }
                    _plcAddress = "MR910";
                    var Paramc = new PLCParaInfoModel
                    {
                        PLCAddress = _plcAddress,
                        ParaType = EnumParaInfoModelParaType.Bool,
                        ParaValue = true
                    };
                    if (_curPlc.WritePLCPara(Paramc))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Paramc.ParaValue}成功！");
                    }
                    break;
                case "停止":
                    if (_curPlc == null)
                    {
                        Console.WriteLine($"亿纬PLC操作页面没连上PLC");
                        return;
                    }
                    _plcAddress = "MR906";
                    var Paramd = new PLCParaInfoModel
                    {
                        PLCAddress = _plcAddress,
                        ParaType = EnumParaInfoModelParaType.Bool,
                        ParaValue = true
                    };
                    if (_curPlc.WritePLCPara(Paramd))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Paramd.ParaValue}成功！");
                    }
                    Thread.Sleep(100);
                    Paramd.ParaValue = false;
                    if (_curPlc.WritePLCPara(Paramd))
                    {
                        Console.WriteLine($"地址{_plcAddress}写入{Paramd.ParaValue}成功！");
                    }
                    break;
                default:
                    break;
            }
        });


        public EvePlcControlViewModel()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();

            if (models.Models.Count > 0)
            {
                _curPlc = models.Models[0];
            }
        }

    }


}
