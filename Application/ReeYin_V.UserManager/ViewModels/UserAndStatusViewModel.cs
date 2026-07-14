using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.UserManager.ViewModels
{
    public class UserAndStatusViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties
        public CurrentUser CurUser
        {
            get { return PrismProvider.User.CurUser; }
            set { RaisePropertyChanged(); }
        }

        public WorkStatus CurStatus
        {
            get { return PrismProvider.WorkStatusManager.CurStatus; }
            set { RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public UserAndStatusViewModel()
        {
            PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((obj) =>
            {
                CurStatus = obj;
            }, ThreadOption.UIThread);
        }
        #endregion

        #region Methods

        #endregion

        #region Commands
        public DelegateCommand<string> ButtonOperateCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "切换用户":
                    PrismProvider.DialogService.Show("UserSwitchView", new DialogParameters
                    {
                          { "Title", "切换用户" },
                          { "Icon", "\ue6c6" },

                    }, result =>
                    {
                       if (result.Result == ButtonResult.OK)
                       {
                       }
                    }, nameof(DialogWindowView));
                    break;
            }
        });
        #endregion

    }
}
