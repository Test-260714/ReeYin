using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.WorkStatus
{
    [ExposedService(Lifetime.Singleton, 10, typeof(IWorkStatusManager))]
    public class WorkStatusManager : IWorkStatusManager
    {
        #region Fields
        private WorkStatus _curStatus = WorkStatus.None;
        #endregion

        #region Properties

        public WorkStatus CurStatus => _curStatus;
        #endregion

        #region Construcotr
        public WorkStatusManager()
        {

        }
        #endregion

        #region Override
        public bool SwitchWorkStatus(WorkStatus status)
        {
            try
            {
                Console.WriteLine($"修改了工作状态从{CurStatus}->{status}");
                _curStatus = status;
                PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Publish(_curStatus);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region Methods

        #endregion



    }
}
