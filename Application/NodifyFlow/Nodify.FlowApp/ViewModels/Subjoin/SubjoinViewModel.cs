using HalconDotNet;
using ImageTool.Halcon;
using MathNet.Numerics;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.NodifyRalated;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.NodifyManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Nodify.FlowApp.ViewModels.Subjoin
{
    public class SubjoinViewModel : BindableBase
    {
        #region Fileds

        #endregion

        #region Properties
        private ModelParamBase _curModelParam;
        /// <summary>
        /// 选中模块参数
        /// </summary>
        public ModelParamBase CurModelParam
        {
            get { return _curModelParam; }
            set { _curModelParam = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _displayableNames = new ObservableCollection<string>();
        /// <summary>
        /// 所有展示名称
        /// </summary>
        public ObservableCollection<string> DisplayableNames
        {
            get { return _displayableNames; }
            set { _displayableNames = value; RaisePropertyChanged(); }
        }

        private string _sltDisplayableName;
        /// <summary>
        /// 选中展示名称
        /// </summary>
        public string SltDisplayableName
        {
            get { return _sltDisplayableName; }
            set { _sltDisplayableName = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _displayable = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 可显示的数据（输入/输出参数）
        /// </summary>
        public ObservableCollection<TransmitParam> Displayable
        {
            get { return _displayable; }
            set { _displayable = value; RaisePropertyChanged(); }
        }

        private HImage image;
        /// <summary>
        /// 展示图片
        /// </summary>
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public SubjoinViewModel()
        {
            PrismProvider.EventAggregator.GetEvent<NodifySelecteChangedEvent>().Subscribe((obj) =>
            {
                Displayable.Clear();
                DisplayableNames.Clear();
                SltDisplayableName = "";
                var temp = obj as NodeViewModel;
                if (temp == null || !PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Keys.Contains(temp.MenuInfo.Serial.ToString("D3"))) return;
                
                var modelparam = PrismProvider.ProjectManager.GetNodeParamCacheValue($"{temp.MenuInfo.Serial.ToString("D3")}");

                if (modelparam == null) return;
                CurModelParam = modelparam as ModelParamBase;

                foreach (var item in CurModelParam.InputParams)
                {
                    if (item.Type == DataType.HObject)
                    {
                        Displayable.Add(item);
                        DisplayableNames.Add(item.Name);
                    }
                }
                foreach (var item in CurModelParam.OutputParams)
                {
                    if (item.Type == DataType.HObject)
                    {
                        Displayable.Add(item);
                        DisplayableNames.Add(item.Name);
                    }
                }

            }, ThreadOption.UIThread);
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public Prism.Commands.DelegateCommand<string> GeneralCommand => new Prism.Commands.DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "切换图片":
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        if (Image != null && Image.IsInitialized())
                            Image.Dispose();
                        if (SltDisplayableName == null || SltDisplayableName == "") return;
                        var showimg = Displayable.Where(p => p.Name == SltDisplayableName).FirstOrDefault();
                        if (showimg.Value == null || !(showimg.Value is HObject)) return;
                        var temp = (showimg.Value as HObject).Clone();

                        var list = new List<HImage>();

                        for (int i = 1; i <= temp.CountObj(); i++)
                        {
                            list.Add(new HImage(temp.SelectObj(i)));
                        }
                        Image = list[0].Clone();
                    });
                    
                    break;
                default:
                    break;
            }

        });
        #endregion

        #region Methods

        #endregion

    }
}
