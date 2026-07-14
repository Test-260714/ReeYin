using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share.Helper;
using ReeYin_V.UI;
using ReeYin_V.UserManager.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.UserManager.ViewModels
{
    public class EditUserViewModel : DialogViewModelBase
    {
        private UserRepository UserRepository = null;

        private RoleRepository RoleRepository = null;
        /// <summary>
        /// 当前用户
        /// </summary>
        private User _editCurrentUser;
        public User EditCurrentUser
        {
            get { return _editCurrentUser; }
            set { _editCurrentUser = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 克隆当前的用户
        /// </summary>
        private User _copyCurrentUser;
        public User CopyCurrentUser
        {
            get { return _copyCurrentUser; }
            set { _copyCurrentUser = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 所有角色名称
        /// </summary>
        private ObservableCollection<string> _allRoleName;
        public ObservableCollection<string> AllRoleName
        {
            get { return _allRoleName; }
            set 
            {
                _allRoleName = value;
                RaisePropertyChanged(); 
            }
        }


        /// <summary>
        /// 编辑用户页面的一些控件是否可用
        /// </summary>
        private bool _isEnable;
        public bool IsEnable
        {
            get { return _isEnable; }
            set { _isEnable = value; RaisePropertyChanged(); }
        }

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No, new DialogParameters()
                    {

                    });
                    break;
                case "确认":
                    int roleid =  RoleRepository.GetList(x => x.RoleName == "管理员").FirstOrDefault()?.RoleId ?? 0;

                    CopyCurrentUser.Username =  CopyCurrentUser.Username.Trim();

                    if (CopyCurrentUser.Username == "" || CopyCurrentUser.PasswordHash == "")
                    {
                        MessageView.Ins.MessageBoxShow("用户名称或密码不能为空，请重新编写", eMsgType.Info);
                        return;
                    }


                    if (CopyCurrentUser.Username != EditCurrentUser.Username && UserRepository.GetList(x => x.Username == CopyCurrentUser.Username).Count > 0)
                    {
                        MessageView.Ins.MessageBoxShow($"用户{CopyCurrentUser.Username}已存在，不能有重复的用户名", eMsgType.Info);
                        return;
                    }

                    if(roleid == CopyCurrentUser.RoleId && CopyCurrentUser.Username != "admin")
                    {
                        MessageView.Ins.MessageBoxShow("程序中管理员只能有一个，不能设置为管理员", eMsgType.Info);
                        return;
                    }

                    if (Regex.IsMatch(CopyCurrentUser.PasswordHash, @"[\u4e00-\u9fa5]"))
                    {
                        MessageView.Ins.MessageBoxShow("密码中不能包含中文字符", eMsgType.Info);
                        return;
                    }

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param",CopyCurrentUser },
                    });
                    UserRepository.Update(CopyCurrentUser);
                    break;
            }

        });


        #endregion

        public EditUserViewModel(UserRepository userRepository,RoleRepository roleRepository)
        {
            AllRoleName =  PrismProvider.User.AllRole.Select(x => x.RoleName).ToObservableCollection();
            UserRepository = userRepository;
            RoleRepository = roleRepository;
        }


        public override void InitParam()
        {
            if (Param != null && (Param is User))
            {
                 EditCurrentUser = base.Param as User;
                 CopyCurrentUser = EditCurrentUser.DeepCopy();
                 IsEnable = EditCurrentUser.Username == "admin" ? false : true;
            }
        }
    }
}
