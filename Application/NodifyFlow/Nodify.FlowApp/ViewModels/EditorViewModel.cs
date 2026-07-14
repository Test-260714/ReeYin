using NetTaste;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.NodifyManager;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using DelegateCommand = ReeYin_V.NodifyManager.DelegateCommand;

namespace Nodify.FlowApp
{
    [Serializable]
    public class EditorViewModel : ObservableObject
    {
        #region Fields
        [JsonIgnore]
        private CalculatorViewModel _calculator = default!;

        [JsonIgnore]
        private string? _name;

        /// <summary>
        /// 存储路径
        /// </summary>
        private string FilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"CCD\Config\Other\");

        public event Action<EditorViewModel, CalculatorViewModel>? OnOpenInnerCalculator;

        public EditorViewModel? Parent { get; set; }

        public Guid Id { get; } = Guid.NewGuid();
        #endregion

        #region Properties

        public CalculatorViewModel Calculator
        {
            get => _calculator;
            set => SetProperty(ref _calculator, value);
        }

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand OpenCalculatorCommand 
        { 
            get
            {
                return new ReeYin_V.NodifyManager.DelegateCommand<CalculatorViewModel>(calculator =>
                {
                    string order = "急速模式";

                    switch (order)
                    {
                        case "新建方案":
                            {
                                var Param = Calculator.SelectedOperations[0].ModuleParam;

                                //调试初始化页面
                                PrismProvider.DialogService.ShowDialog("CollectImageView", new DialogParameters
                                {
                                    { "Title", "采图页面" },
                                    { "Icon", "\ue631" },
                                    { "Param", Param },
                                }, result =>
                                {
                                    if (result.Result == ButtonResult.OK)
                                    {
                                        var module = result.Parameters.GetValue<object>("Param") as IModuleParam;

                                        Calculator.SelectedOperations[0].ModuleParam = module;

                                    }
                                }, nameof(DialogWindowView));
                            }
                            break;
                        case "方案列表":
                            {
                                PrismProvider.RegionManager.RequestNavigate(RegionNames.PrimaryRegion, ViewNames.LoginView);
                            }
                            break;
                        case "打开":
                            {
                                //弹窗通用设置页面
                                PrismProvider.DialogService.ShowDialog("MenuManagerView", new DialogParameters
                                {
                                    { "Title", "菜单管理页面" },
                                    { "Icon", "\ue696" },
                                }, result =>
                                {

                                }, nameof(DialogWindowView));
                            }
                            break;
                        case "保存":
                            {
                                MessageView.Ins.MessageBoxShow("保存成功!", eMsgType.Info);
                            }
                            break;
                        case "急速模式":
                            {
                                
                            }
                            break;
                        case "运行一次":
                            {

                            }
                            break;
                        case "循环运行":
                            {

                            }
                            break;
                        case "停止":
                            {

                            }
                            break;
                        case "全局变量":
                            {
                                // 显示对话框时传递标题参数
                                var parameters = new DialogParameters
                                {

                                };

                                //弹窗初始化页面
                                PrismProvider.DialogService.ShowDialog(ViewNames.UserManagerView, parameters, result =>
                                {

                                });
                            }
                            break;
                        case "相机设置":
                            {
                                //弹窗初始化页面
                                PrismProvider.DialogService.Show("CameraSetView", new DialogParameters
                                {
                                    { "Title", "相机设置" },
                                    { "Icon", "\ue673" },
                                }, result =>
                                {
                                }, nameof(DialogWindowView));
                            }
                            break;
                        case "通信设置":
                            {
                                //弹窗初始化页面
                                PrismProvider.DialogService.ShowDialog("CommunicationSetView", new DialogParameters
                            {
                                { "Title", "通信设置" },
                                { "Icon", "\ue673" },
                            }, result =>
                            {
                            }, nameof(DialogWindowView));
                            }
                            break;
                    }

                    OnOpenInnerCalculator?.Invoke(this, calculator);
                });
            }
        
        }

        [JsonIgnore]
        public ICommand DeleteSelectionCommand { get; }
        #endregion

        #region Constructor
        public EditorViewModel()
        {
            Calculator = new CalculatorViewModel();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 操作解决方案
        /// </summary>
        /// <param name="order"></param>
        public void OperateSolution(string order)
        {
            //switch(order)
            //{
            //    case "打开":
            //        {
            //            //Calculator = JsonHelper.JsonDisObjectSerialize<CalculatorViewModel>(FilePath + "TestParam.json", out string str, TypeNameHandling.Auto);
            //            JsonHelper.JsonDisObjectSerialize<NodifyEditorViewModel>(FilePath + "TestParam.json", out string str, TypeNameHandling.Auto);
            //            PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Publish("打开");
            //        }
            //        break;

            //    case "保存":
            //        {
            //            JsonHelper.JsonObjectSerialize(Calculator, FilePath + "TestParam.json", TypeNameHandling.Auto);
            //        }
            //        break;
            //}
        }
        #endregion

        #region Commands
        public DelegateCommand loadCommand => new DelegateCommand(() =>
        {
            //订阅Nodify相关操作
            //PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Subscribe(OperateSolution, ThreadOption.UIThread);
        });
        #endregion
    }
}
