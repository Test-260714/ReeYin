using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Models;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin_V.UserManager.ViewModels
{
    public class RoleModificationViewModel : DialogViewModelBase
    {
        #region Properties  
        private RoleRepository RoleRepository = null;

        private Role _currentRole;
        /// <summary>
        /// 当前编辑角色
        /// </summary>
        public Role CurrentRole
        {
            get { return _currentRole; }
            set { _currentRole = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 克隆当前的角色
        /// </summary>
        private Role _copyCurrentRole;
        public Role CopyCurrentRole
        {
            get { return _copyCurrentRole; }
            set { _copyCurrentRole = value;  RaisePropertyChanged(); }
        }
        private ObservableCollection<int> _allPermissionId;
        public ObservableCollection<int> AllPermissionId
        {
            get { return _allPermissionId; }
            set { _allPermissionId = value; RaisePropertyChanged(); }
        }

        private bool _isEnable;
        public bool IsEnable
        {
            get { return _isEnable; }
            set { _isEnable = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public RoleModificationViewModel(IConfigManager configManager, RoleRepository roleRepository)
        {
            AllPermissionId = PrismProvider.User.AllPermisson.Select(x => x.PermId).ToObservableCollection();
            RoleRepository = roleRepository;
        }
        #endregion


        #region Commands
        public DelegateCommand<string> GeneralRoleCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "确认":
                    CopyCurrentRole.RoleName = CopyCurrentRole.RoleName.Trim();

                    if(CopyCurrentRole.RoleName == "")
                    {
                        MessageView.Ins.MessageBoxShow("角色名称不能为空，请重新编写", eMsgType.Info);
                        return;
                    }

                    if (CopyCurrentRole.PermissionID == 1  && CopyCurrentRole.RoleName != "管理员") //只有管理员角色权限等级能设置为1，其余角色等级只能为2-10
                    {
                        MessageView.Ins.MessageBoxShow("只有管理员权限等级能设置为1，其余角色等级只能为2-10级", eMsgType.Info);
                        return;
                    }

                    if(CurrentRole.RoleName != CopyCurrentRole.RoleName && RoleRepository.GetList(x => x.RoleName == CopyCurrentRole.RoleName).Count > 0)
                    {
                        MessageView.Ins.MessageBoxShow("角色名称重复，请重新编写", eMsgType.Info);
                        return;
                    }

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param",CopyCurrentRole },
                    });
                    RoleRepository.Update(CopyCurrentRole);
                    PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Publish();
                    List<User> users = PrismProvider.User.AllUser.Where(x => x.RoleId == CopyCurrentRole.RoleId).ToList();                
                    foreach (var user in users)
                    {
                        PrismProvider.User.AllUser.Remove(user);
                        PrismProvider.User.AllUser.Add(user);
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No, new DialogParameters()
                    {

                    });
                    break;
            }
        });


        #endregion

        #region Methods

        public override void InitParam()
        {
            if (Param != null && (Param is Role))
            {
                CurrentRole = base.Param as Role;
                CopyCurrentRole = CurrentRole.DeepCopy();
                IsEnable = CopyCurrentRole.PermissionID <= 1 ? false : true; 
            }
        }
        #endregion
    }
}
