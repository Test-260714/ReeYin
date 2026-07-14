using Custom.KCJC.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Custom.KCJC.ViewModels
{
    public class InfoConfigViewModel : DialogViewModelBase
    {
        private OtherConfigModel _model = new OtherConfigModel();
        public OtherConfigModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrooveSummaryTestItems));
                RaisePropertyChanged(nameof(ConvexSummaryTestItems));
            }
        }

        public IEnumerable<SummaryTestItemConfig> GrooveSummaryTestItems => Model.SummaryTestItems.Where(item => item.Category == "刻槽");

        public IEnumerable<SummaryTestItemConfig> ConvexSummaryTestItems => Model.SummaryTestItems.Where(item => item.Category == "压花");

        public DelegateCommand CloseCommand => new DelegateCommand(() =>
        {
            CloseDialog(ButtonResult.OK);
        });

        public DelegateCommand SaveCommand => new DelegateCommand(() =>
        {
            // 信息配置绑定的是方案模型，保存时直接保存当前解决方案。
            PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("保存");
            MessageBox.Show("保存成功！");
        });

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            string cacheKey = Serial.ToString("D3");
            if (PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches?.TryGetValue(cacheKey, out var param) == true &&
                param is SensorDataCollectionModel sensorModel)
            {
                // 信息配置直接绑定采集模块的 OtherConfig，关闭窗口后参数仍保留在当前方案模型中。
                Model = sensorModel.OtherConfig;
            }
        }
    }
}
