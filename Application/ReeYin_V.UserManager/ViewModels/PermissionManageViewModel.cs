using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services.User;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.UserManager.ViewModels
{
    public class PermissionManageViewModel : DialogViewModelBase
    {
        #region Properties
        private IUserService _permissionModelParam;
        public IUserService PermissionModelParam
        {
            get { return _permissionModelParam; }
            set { _permissionModelParam = value; RaisePropertyChanged(); }
        }
        #endregion

        public PermissionManageViewModel()
        {
            PermissionModelParam = PrismProvider.User;
        }

        public DelegateCommand<int?> EditCommand => new DelegateCommand<int?>((order) =>
        {
            PrismProvider.DialogService.Show("EditPermissionView", new DialogParameters
            {
                 { "Title", "权限修改" },
                 { "Icon", "\ue6e8" },
                 { "Param", order },
            }, result =>
            {
                if (result.Result == ButtonResult.OK)
                {

                }
            }, nameof(DialogWindowView));
        });
    }
}
