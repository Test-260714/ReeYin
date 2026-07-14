using HalconDotNet;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace ReeYin_V.UI.UserControls.WxLink
{
    public class WxLink : TextBox
    {
        static WxLink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WxLink), new FrameworkPropertyMetadata(typeof(WxLink)));
        }

        // 定义链接按钮点击事件
        public static readonly RoutedEvent ButtonLinkClickEvent =
            EventManager.RegisterRoutedEvent(
                "ButtonLinkClick",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(WxLink));

        // 链接按钮点击事件的 CLR 包装器
        public event RoutedEventHandler ButtonLinkClick
        {
            add => AddHandler(ButtonLinkClickEvent, value);
            remove => RemoveHandler(ButtonLinkClickEvent, value);
        }

        // 定义取消按钮点击事件
        public static readonly RoutedEvent ButtonCancelClickEvent =
            EventManager.RegisterRoutedEvent(
                "ButtonCancelClick",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(WxLink));

        // 取消按钮点击事件的 CLR 包装器
        public event RoutedEventHandler ButtonCancelClick
        {
            add => AddHandler(ButtonCancelClickEvent, value);
            remove => RemoveHandler(ButtonCancelClickEvent, value);
        }

        /// <summary>
        /// 权限等级
        /// </summary>
        public int AuthorityLevel
        {
            get => (int)GetValue(AuthorityLevelProperty);
            set => SetValue(AuthorityLevelProperty, value);
        }

        public static readonly DependencyProperty AuthorityLevelProperty =
            DependencyProperty.Register("AuthorityLevel", typeof(int), typeof(WxLink), new PropertyMetadata(1, OnAuthorityLevelChanged));

        /// <summary>
        /// 权限等级变化时触发，设置 IsEnabled 的值
        /// </summary>
        private static void OnAuthorityLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WxLink obj = d as WxLink;
            int value = (int)obj.GetValue(AuthorityLevelMinProperty);
            obj.SetValue(IsEnabledProperty, (int)e.NewValue >= value);
        }

        #region 输入参数
        /// <summary>
        /// 输入参数（从上一个节点传入的参数）
        /// </summary>
        public object InputParams
        {
            get => (object)GetValue(InputParamsProperty);
            set => SetValue(InputParamsProperty, value);
        }

        /// <summary>
        /// 输入参数依赖属性（支持外部绑定）
        /// </summary>
        public static readonly DependencyProperty InputParamsProperty =
        DependencyProperty.Register(
            name: "InputParams",
            propertyType: typeof(object),
            ownerType: typeof(WxLink),
            typeMetadata: new PropertyMetadata(
                defaultValue: new object(),
                propertyChangedCallback: InputParamsChanged,
                coerceValueCallback: CoerceInputParams
            )
        );

        /// <summary>
        /// 输入参数变化时的回调
        /// </summary>
        private static void InputParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WxLink wxLink) return;

            var oldValue = e.OldValue as ObservableCollection<object>;
            var newValue = e.NewValue as ObservableCollection<object>;

            if (oldValue != null)
                oldValue.CollectionChanged -= wxLink.OnInputParamsCollectionChanged;

            if (newValue != null)
            {
                newValue.CollectionChanged += wxLink.OnInputParamsCollectionChanged;
                wxLink.UpdateIsEnabledBasedOnInputParams(newValue);
            }
        }

        /// <summary>
        /// 确保InputParams不会为null
        /// </summary>
        private static object CoerceInputParams(DependencyObject d, object baseValue)
        {
            return baseValue ?? new ObservableCollection<TransmitParam>();
        }

        /// <summary>
        /// 集合内部元素变化时触发
        /// </summary>
        private void OnInputParamsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateIsEnabledBasedOnInputParams(InputParams);
        }

        /// <summary>
        /// 根据输入参数更新IsEnabled状态（预留扩展）
        /// </summary>
        private void UpdateIsEnabledBasedOnInputParams(object paramsCollection)
        {
        }
        #endregion

        #region 输出参数
        /// <summary>
        /// 输出参数（支持外部绑定）
        /// </summary>
        public object OutputParam
        {
            get => (object)GetValue(OutputParamProperty);
            set => SetValue(OutputParamProperty, value);
        }

        /// <summary>
        /// 输出参数依赖属性
        /// </summary>
        public static readonly DependencyProperty OutputParamProperty =
            DependencyProperty.Register(
                name: "OutputParam",
                propertyType: typeof(object),
                ownerType: typeof(WxLink),
                typeMetadata: new PropertyMetadata(
                    defaultValue: null,
                    propertyChangedCallback: OnOutputParamChanged
                )
            );

        /// <summary>
        /// OutputParam值变化时的回调
        /// </summary>
        private static void OnOutputParamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WxLink wxLink) return;

            var oldValue = e.OldValue as TransmitParam;
            var newValue = e.NewValue as TransmitParam;

            wxLink.HandleOutputParamChange(oldValue, newValue);
        }

        /// <summary>
        /// 处理OutputParam变化的业务逻辑（预留扩展）
        /// </summary>
        private void HandleOutputParamChange(TransmitParam oldValue, TransmitParam newValue)
        {
        }
        #endregion

        /// <summary>
        /// 操作类型（OpenFile / OpenParam / OpenPosParam）
        /// </summary>
        public string Order
        {
            get => (string)GetValue(OrderProperty);
            set => SetValue(OrderProperty, value);
        }

        public static readonly DependencyProperty OrderProperty =
            DependencyProperty.Register("Order", typeof(string), typeof(WxLink), new PropertyMetadata(""));

        /// <summary>
        /// 最低使用权限
        /// </summary>
        public int AuthorityLevelMin
        {
            get => (int)GetValue(AuthorityLevelMinProperty);
            set => SetValue(AuthorityLevelMinProperty, value);
        }

        public static readonly DependencyProperty AuthorityLevelMinProperty =
            DependencyProperty.Register("AuthorityLevelMin", typeof(int), typeof(WxLink), new PropertyMetadata(1, OnAuthorityLevelMinChanged));

        /// <summary>
        /// 最低权限变化时触发，设置 IsEnabled 的值
        /// </summary>
        private static void OnAuthorityLevelMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WxLink obj = d as WxLink;
            int value = (int)obj.GetValue(AuthorityLevelProperty);
            obj.SetValue(IsEnabledProperty, value >= (int)e.NewValue);
        }

        /// <summary>
        /// 应用控件模板时调用
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_ButtonLink") is Button btn_Link
                && GetTemplateChild("PART_ButtonCancel") is Button btn_Cancel)
            {
                btn_Link.Click += Btn_Link_Click;
                btn_Cancel.Click += Btn_Cancel_Click;
            }
        }

        private void Btn_Link_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (Order)
                {
                    case "OpenFile":
                        OpenFileLink();
                        break;
                    case "OpenParam":
                        OpenParamLink();
                        break;
                    case "OpenPosParam":
                        OpenPosParamLink();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 打开文件夹选择对话框
        /// </summary>
        private void OpenFileLink()
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "请选择文件夹路径";
            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folderDialog.ShowNewFolderButton = true;

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                Text = folderDialog.SelectedPath;
            }
        }

        /// <summary>
        /// 打开参数链接对话框
        /// </summary>
        private void OpenParamLink()
        {
            PrismProvider.DialogService.ShowDialog("ParamLinkView", new DialogParameters
            {
                { "Title", "变量链接" },
                { "Icon", "\ue647" },
                { "Param", InputParams },
            }, result =>
            {
                if (result.Result != ButtonResult.OK || result.Parameters == null)
                    return;

                if (result.Parameters.GetValue<object>("Param") is not TransmitParam sltParam) return;

                if (OutputParam != null && !VerifyParam(sltParam))
                    return;

                if (sltParam.Value is HObject hObj)
                {
                    var temp = sltParam.DeepClone();
                    temp.Value = hObj.Clone();
                    OutputParam = temp;
                }
                else
                {
                    OutputParam = sltParam;
                }
            }, nameof(DialogWindowView));
        }

        /// <summary>
        /// 打开位置参数链接对话框
        /// </summary>
        private void OpenPosParamLink()
        {
            PrismProvider.DialogService.Show("CoordinateCacheView", new DialogParameters
            {
                { "Title", "位置变量链接" },
                { "Icon", "\ue647" },
                { "Param", InputParams },
            }, result =>
            {
                if (result.Result != ButtonResult.OK || result.Parameters == null)
                    return;

                if (result.Parameters.GetValue<object>("Param") is not CoordinatePos sltParam) return;

                sltParam.Describe = $"{sltParam.Name}[{sltParam.TargetPos[0]:F2}/{sltParam.TargetPos[1]:F2}/{sltParam.TargetPos[2]:F2}/]";
                OutputParam = sltParam;
            }, nameof(DialogWindowView));
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定要取消链接吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            Text = "";
            OutputParam = null;
        }

        #region Methods

        /// <summary>
        /// 校验参数的有效性
        /// </summary>
        public bool VerifyParam(TransmitParam transmitParam)
        {
            try
            {
                var resourceParam = OutputParam as TransmitParam;
                if (resourceParam == null) return true;

                if (resourceParam.Type != DataType.None && resourceParam.Type != transmitParam.Type)
                {
                    MessageView.Ins.MessageBoxShow("参数类型不匹配，请重选!", eMsgType.Info);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"VerifyParam()_参数类型匹配异常，原因：{ex.StackTrace}", eMsgType.Error);
                return false;
            }
        }

        #endregion
    }
}
