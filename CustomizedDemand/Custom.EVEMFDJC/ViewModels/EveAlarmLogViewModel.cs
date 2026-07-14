using Custom.EVEMFDJC.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Custom.EVEMFDJC.ViewModels
{
    public class EveAlarmLogViewModel : DialogViewModelBase, INavigationAware
    {
        #region Properties
        /// <summary>
        /// plc对象
        /// </summary>
        public PLCBase CurPLC { get; set; }

        /// <summary>
        /// 取消线程的令牌
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 模型类
        /// </summary>
        private EveAlarmLogModel _modelParam = new EveAlarmLogModel();

        public EveAlarmLogModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
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
                case "开启监听":
                    StartListen();
                    break;
                case "停止监听":
                    StopListen();
                    break;
                case "添加报警":
                    PrismProvider.DialogService.Show("ConfigureaAlarm", new DialogParameters
                        {
                            { "Title", "配置PLC地址对应异常信息(右击页面可以添加和删除项，双击某一项可对其进行编辑)" },
                            { "Icon", "\ue694" },
                            { "Param", ModelParam.PlcConfigureAlarm.DeepClone() },
                        }, result =>
                        {
                            if (result.Result == ButtonResult.OK)
                            {
                                var configurealarmodel = result.Parameters.GetValue<object>("Param") as ConfigureaAlarmModel;
                                if (configurealarmodel != null)
                                {
                                    ModelParam.PlcConfigureAlarm = configurealarmodel.ConfigureaAlarm;
                                }
                            }
                        }, nameof(DialogWindowView));
                    break;
                case "历史信息":
                    OpenAlarmHistoryFile();
                    break;
                default:
                    break;
            }

        });
        #endregion

        #region Constructor
        public EveAlarmLogViewModel()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();

            if (models.Models.Count > 0)
                CurPLC = models.Models[0];
        }
        #endregion

        #region methods
        /// <summary>
        /// 开启监听
        /// </summary>
        private void StartListen()
        {
            StopListen(); // 确保之前的监听已停止
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => PollingLoopAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        private void StopListen()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 线程定时执行的方法
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task PollingLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<PlcAlarmInformation> alarms = new List<PlcAlarmInformation>();

                    if (CurPLC.State != HardwareState.Connected)
                    {
                        Console.WriteLine($"{DateTime.Now}: 亿纬报警信息页面连接PLC失败");
                        break;
                    }
                    IEnumerable<ConfigureAlarm> uesdItem = ModelParam.PlcConfigureAlarm.Where(x => x.IsUsing).ToList();//只要那些启用的监听地址

                    foreach (var order in uesdItem)
                    {
                        var param = new PLCParaInfoModel
                        {
                            PLCAddress = order.Addr,
                            ParaType = order.ParamType
                        };

                        CurPLC.ReadPLCPara(param);

                        if (param.ParaValue == null)
                        {
                            Console.WriteLine($"{DateTime.Now}: 亿纬报警信息页面,地址:{param.PLCAddress},类型{param.ParaType},获取的值为null");
                            continue;
                        }

                        var addressvalue = param.ParaValue;//从plc那里获取到的参数的值

                        if (addressvalue.ToString().ToLower() == order.JudgeValue.ToLower())//当获取到的点位置与设置的对应点位的判定值一样时
                        {
                            PlcAlarmInformation plcalarminformation = new PlcAlarmInformation
                            {
                                Triggertime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                Information = order.AlarmContent,
                                Address = order.Addr
                            };
                            alarms.Add(plcalarminformation);

                            AppendAlarmToHistoryFile(plcalarminformation);  // 追加到历史文件
                        }


                    }

                    //找出需要添加的数据
                    var toAdd = alarms.Where(a => !ModelParam.PlcAlarmInformation.Any(x => x.Address == a.Address))
                               .ToList();

                    //找出需要移除的数据
                    var toRemove = ModelParam.PlcAlarmInformation
                        .Where(x => !alarms.Any(a => a.Address == x.Address))
                        .ToList();


                    if (toAdd.Count > 0 || toRemove.Count > 0)
                    {
                        await PrismProvider.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var item in toRemove)
                                ModelParam.PlcAlarmInformation.Remove(item);

                            // 获取当前最大Id
                            int maxId = ModelParam.PlcAlarmInformation.Any()
                                ? ModelParam.PlcAlarmInformation.Max(x => x.Id)
                                : 0;

                            foreach (var item in toAdd)
                            {
                                item.Id = ++maxId;
                                ModelParam.PlcAlarmInformation.Add(item);
                            }
                        });
                    }

                    // 间隔一段时间再采
                    await Task.Delay(5000, token);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            int Serail = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serail = id;

            var temp = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[$"{Serail.ToString("D3")}"] as EveSensorDataCollectionModel;
            ModelParam = temp.EveAlarmLogModel;

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        /// <summary>
        /// 打开报警历史文本文件
        /// </summary>
        private void OpenAlarmHistoryFile()
        {
            try
            {
                string path = GetAlarmHistoryFilePath();
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开历史文件失败: {ex.Message}", "历史信息");
            }
        }

        /// <summary>
        /// 获取报警历史文件路径（所有记录写入同一文件）
        /// </summary>
        private string GetAlarmHistoryFilePath()
        {
            string dir = Path.Combine(PrismProvider.AppBasePath, "Config");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "EveAlarmLog.txt");  // 固定文件名，不按天
        }


        /// <summary>
        /// 将一条报警追加写入历史文件
        /// </summary>
        private void AppendAlarmToHistoryFile(PlcAlarmInformation item)
        {
            try
            {
                string path = GetAlarmHistoryFilePath();
                bool isNew = !File.Exists(path);
                using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    if (isNew)
                        sw.WriteLine("触发时间\t\t\t\t\t地址\t\t\t\t报警信息");
                    sw.WriteLine($"{item.Triggertime}\t\t{item.Address}\t\t\t\t{item.Information}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入报警历史失败: {ex.Message}");
            }
        }
        #endregion

    }
}
