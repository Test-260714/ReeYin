using DryIoc;
using OpenCvSharp;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Share.Helper;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace ReeYin_V.UserManager.ViewModels
{
    public class RoleManageViewModel : DialogViewModelBase
    {

        #region Fields
        public IConfigManager ConfigManager = null;

        private RoleRepository _roleRepository = null;

        private UserRepository _userRepository = null;
        #endregion

        #region Properties
        private IUserService _roleManagementModel;

        public IUserService RoleManagementModel
        {
            get { return _roleManagementModel; }
            set { _roleManagementModel = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 角色名称
        /// </summary>
        private string _roleName;
        public string RoleName
        {
            get { return _roleName; }
            set { _roleName = value.Trim(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 权限等级
        /// </summary>
        private int _permissionID;
        public int PermissionID
        {
            get { return _permissionID; }
            set { _permissionID = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 权限等级数据源
        /// </summary>
        private List<int> _permissionIDSource;
        public List<int> PermissionIDSource
        {
            get { return _permissionIDSource; }
            set
            { 
                if(value.Contains(1))
                {
                    value.Remove(1);
                }
                _permissionIDSource = value;
                RaisePropertyChanged(); 
            }
        }
        #endregion

        #region Constructor
        public RoleManageViewModel(IConfigManager configManager, RoleRepository roleRepository, UserRepository userRepository)
        {
            RoleManagementModel = PrismProvider.User;
            ConfigManager = configManager;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            PermissionIDSource = PrismProvider.User.AllPermisson.Select(x => x.PermId).ToList();
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralRoleCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "添加角色":
                    {
                        if (this.RoleName == null || this.RoleName == "")
                        {
                            MessageView.Ins.MessageBoxShow("角色名称不能为空",eMsgType.Info);
                            return;
                        }
                        if (this.PermissionID == 0)
                        {
                            MessageView.Ins.MessageBoxShow("权限等级不能为空", eMsgType.Info);
                            return;
                        }

                        if (RoleManagementModel.AllRole.Count(x => x.RoleName == this.RoleName) > 0)
                        {
                            MessageView.Ins.MessageBoxShow("不能添加重复的角色", eMsgType.Info);
                            return;
                        }

                        Role role = new Role()
                        {
                            RoleName = this.RoleName,
                            PermissionID = this.PermissionID,
                            CreateBy = RoleManagementModel.CurUser.UserId,
                            CreateTime = DateTime.Now,
                            UpdateTime = DateTime.Now,
                            UpdateBy = RoleManagementModel.CurUser.UpdateBy 
                        };
                        _roleRepository.Insert(role);
                        int idmax = _roleRepository.GetList().Max(x => x.RoleId);
                        role.RoleId = idmax;
                        RoleManagementModel.AllRole.Add(role);
                        UpdateTableEvent();
                        this.RoleName = "";
                    }
                    break;
                case "角色管理":
                    //弹窗角色管理页面
                    PrismProvider.DialogService.Show("RoleManageView", new DialogParameters
                        {
                            { "Title", "角色管理" },
                            { "Icon", "\ue694" },
                        }, result =>
                        {

                        }, nameof(DialogWindowView));
                    break;
                case "确认":

                    break;
                case "修改":
                    break;
            }
        });

        public DelegateCommand<Role> EditCommand => new DelegateCommand<Role>((order) =>
        {
            PrismProvider.DialogService.Show("RoleModificationView", new DialogParameters
            {
                 { "Title", "角色修改" },
                 { "Icon", "\ue6a3" },
                 { "Param", order },
            }, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    Role editrole = result.Parameters.GetValue<object>("Param") as Role;
                    RoleManagementModel.AllRole.Remove(order);
                    RoleManagementModel.AllRole.Add(editrole);
                }
            }, nameof(DialogWindowView));
        });


        public DelegateCommand<Role> DeleteCommand => new DelegateCommand<Role>((order) =>
        {
            Role deleterole = order as Role;
            if(deleterole.PermissionID == 1)
            {
                MessageView.Ins.MessageBoxShow("管理员角色不可删除", eMsgType.Info);
                return;
            }

            if(_userRepository.GetList(x => x.RoleId == deleterole.RoleId).Count > 0)
            {
                MessageView.Ins.MessageBoxShow("当前用户表中，有用户使用该角色名称，不能进行删除", eMsgType.Info);
                return;
            }

            RoleManagementModel.AllRole.Remove(order);
            _roleRepository.Delete(order);
            UpdateTableEvent();
        });
        #endregion

        /// <summary>
        /// 发布角色表更新事件
        /// </summary>
        private void UpdateTableEvent()
        {
            PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Publish();
        }
    }
}
