using Custom.EVEMFDJC.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Custom.EVEMFDJC.ViewModels
{
    public class ConfigureaAlarmViewModel : DialogViewModelBase
    {
        #region Properties
        /// <summary>
        /// 整个页面的全局变量
        /// </summary>
        private ConfigureaAlarmModel _modelParam = new ConfigureaAlarmModel();
        public ConfigureaAlarmModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }


        private bool _editCustomParam = false;
        /// <summary>
        /// 编辑页面打开开关
        /// </summary>
        public bool EditCustomParam
        {
            get { return _editCustomParam; }
            set { _editCustomParam = value; RaisePropertyChanged(); }
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
                case "编辑参数":
                    {
                        if(ModelParam.SltConfigureaAlarm == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选中要编辑的项，然后再双击对其进行编辑", eMsgType.Info);
                            return;
                        }
                        EditCustomParam = true;
                    }
                    break;

                case "参数关闭":
                    {
                        if(!IsJudgeValueMatchParamType(ModelParam.SltConfigureaAlarm))
                        {
                            MessageView.Ins.MessageBoxShow($"参数的触发值{ModelParam.SltConfigureaAlarm.JudgeValue}和类型{ModelParam.SltConfigureaAlarm.ParamType}不匹配", eMsgType.Info);
                            EditCustomParam = true;
                        }
                    }
                    break;
                case "参数打开":
                    {

                    }
                    break;
                case "取消":
                    {
                        CloseDialog(ButtonResult.No, new DialogParameters()
                        {

                        });
                    }
                    break;
                case "添加新项":
                    {
                        ModelParam.ConfigureaAlarm.Add(new ConfigureAlarm()
                        {
                            ParamType = EnumParaInfoModelParaType.Bool,
                        });
                    }
                    break;

                case "删除选中项":
                    {
                        if(ModelParam.SltConfigureaAlarm == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选中要删除的项，然后再鼠标右键删除选中项", eMsgType.Info);
                            return;
                        }

                        ModelParam.ConfigureaAlarm.Remove(ModelParam.SltConfigureaAlarm);

                        CollectionViewSource.GetDefaultView(ModelParam.ConfigureaAlarm).Refresh();  //手动调用DataGrid_LoadingRow方法
                    }
                    break;
                case "确认":
                    {
                        MessageBoxResult result = MessageView.Ins.MessageBoxShow("确定要保存吗?", eMsgType.Info, MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            if(ModelParam.ConfigureaAlarm.Any(x => (x.Addr == "" || x.Addr == null) || (x.JudgeValue == "" || x.JudgeValue == null) || (x.AlarmContent == "" || x.AlarmContent == null)))
                            {
                                MessageView.Ins.MessageBoxShow("每条报警信息的地址,触发值，报警信息内容不能为空", eMsgType.Info);
                                return;
                            }
                            CloseDialog(ButtonResult.OK, new DialogParameters()
                            {
                            { "Param", ModelParam },
                            });
                        }
                        else
                        {
                            CloseDialog(ButtonResult.No, new DialogParameters()
                            {

                            });
                        }
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion


        #region Methods
        public override void InitParam()
        {
            if (Param != null && (Param is ObservableCollection<ConfigureAlarm>))
            {
                ModelParam.ConfigureaAlarm = base.Param as ObservableCollection<ConfigureAlarm>;
            }
        }

        /// <summary>
        /// 判断参数的触发值和类型是否匹配
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsJudgeValueMatchParamType(ConfigureAlarm item)
        {
            if (item == null) return false;

            var text = item.JudgeValue?.Trim();
            if (string.IsNullOrEmpty(text)) return false;

            // 统一使用不受系统区域影响的解析规则
            var ci = CultureInfo.InvariantCulture;

            switch (item.ParamType)
            {
                case EnumParaInfoModelParaType.Float:
                    return float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, ci, out _);

                case EnumParaInfoModelParaType.Bool:
                    return TryParseBool(text, out _);

                case EnumParaInfoModelParaType.Short:
                    return short.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Ushort:
                    return ushort.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Int:
                    return int.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Uint:
                    return uint.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Long:
                    return long.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Ulong:
                    return ulong.TryParse(text, NumberStyles.Integer, ci, out _);

                case EnumParaInfoModelParaType.Double:
                    return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, ci, out _);

                case EnumParaInfoModelParaType.String:
                case EnumParaInfoModelParaType.StringUTF8:
                    // 字符串类型：非空即可
                    return true;

                case EnumParaInfoModelParaType.FloatArray:
                    return TryParseFloatArray(text, out _);

                case EnumParaInfoModelParaType.BoolArray:
                    return TryParseBoolArray(text, out _);

                default:
                    return false;
            }
        }

        private bool TryParseBool(string text, out bool value)
        {
            text = text?.Trim().ToLowerInvariant();
            switch (text)
            {
                case "true":
                case "1":
                case "on":
                case "yes":
                    value = true;
                    return true;
                case "false":
                case "0":
                case "off":
                case "no":
                    value = false;
                    return true;
                default:
                    return bool.TryParse(text, out value);
            }
        }

        // 允许格式: "true,false,1,0" 或 "on;off;yes;no"
        private bool TryParseBoolArray(string text, out bool[] values)
        {
            values = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            var arr = new bool[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var token = parts[i].Trim();
                if (!TryParseBool(token, out arr[i]))
                    return false;
            }

            values = arr;
            return true;
        }

        // 允许格式: "1.2,3.4,5" 或 "1.2;3.4;5"
        private bool TryParseFloatArray(string text, out float[] values)
        {
            values = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            var arr = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var token = parts[i].Trim();
                if (!float.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out arr[i]))
                    return false;
            }

            values = arr;
            return true;
        }
        #endregion
    }
}
