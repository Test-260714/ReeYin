using DryIoc.ImTools;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace ReeYin_V.Hardware.ControlCard.ViewModels
{
    public class IOManagerViewModel : DialogViewModelBase
    {
        #region Fields
        private Timer _timer = null;

        #endregion

        #region Properties
        private IControlCard ControlCard { get; }

        private IConfigManager ConfigManager { get; }

        private IOManagerModel _modelParam;
        /// <summary>
        /// 模块参数
        /// </summary>
        public IOManagerModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public IOManagerViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;

            ModelParam = ConfigManager.Read<IOManagerModel>(ConfigKey.IOManagerModel) ?? new IOManagerModel();

            this.ControlCard = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel).CurSltCard;
            InitTimer();
            _timer?.Start();
        }
        #endregion

        #region Methods
        public void InitTimer()
        {
            _timer = new Timer(50);
            _timer.Elapsed += async (sender, e) => await Task.Run(() =>
            {
                try
                {
                    if(ControlCard.GetAllInput(out bool[] inputStatus))
                    {
                        foreach (var input in ModelParam.AllInput)
                        {
                            input.State = inputStatus[input.Port];
                        }
                    }
                    else
                    {
                        Console.WriteLine($"GetAllInput()_获取所有输入IO失败");
                    }

                    if (ControlCard.GetAllOutput(out bool[] outStatus))
                    {
                        foreach (var output in ModelParam.AllOutput)
                        {
                            output.State = outStatus[output.Port];
                        }
                    }
                    else
                    {
                        Console.WriteLine($"GetAllOutput()_获取所有输出IO失败");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + ex.StackTrace);
                }
            });
            _timer.Enabled = true;
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "关闭":
                    {
                        //存一下参数
                        ConfigManager.Write(ConfigKey.IOManagerModel, ModelParam);

                        _timer.Stop();
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        //{ "Param", ModelParam },
                    });
                    break;
                case "切换状态":
                    {


                        ControlCard.SetSpecifiedIO((int)ModelParam.SltIO.Port, ModelParam.SltIO.State);
                    }
                    break;
                case "输入添加新项":
                    {
                        ModelParam.AllInput.Add(new IOParam());
                    }
                    break;
                case "输入删除选中项":
                    {
                        ModelParam.AllInput.Remove(ModelParam.SltIO);
                        ModelParam.SltIO = null;
                    }
                    break;
                case "输出添加新项":
                    {
                        ModelParam.AllOutput.Add(new IOParam());
                    }
                    break;
                case "输出删除选中项":
                    {
                        ModelParam.AllOutput.Remove(ModelParam.SltIO);
                        ModelParam.SltIO = null;
                    }
                    break;
                default:
                    break;
            }

        });
        #endregion

    }
}
