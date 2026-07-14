using Dm;
using DryIoc.ImTools;
using NetTaste;
using OpenCvSharp;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using ReeYin_V.Core.Cache;
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
using ReeYin_V.Core.Services.User;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using ReeYin_V.UserManager.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ReeYin_V.UserManager.ViewModels
{
    public class UserManagerViewModel : DialogViewModelBase
    {


        #region Properties
        private IUserService _modelParam;
        private RoleRepository _roleRepository = null;
        private UserRepository _userRepository = null;

        public IUserService ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户名
        /// </summary>
        private string _userName = "";
        public string UserName
        {
            get { return _userName; }
            set { _userName = value.Trim(); RaisePropertyChanged(); }
        }

        private string _passwordHash = "";
        /// <summary>
        /// 密码
        /// </summary>
        public string PasswordHash 
        {
            get { return _passwordHash; }
            set { _passwordHash = value.Trim(); RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _allRoleName;
        /// <summary>
        /// 所有角色名称
        /// </summary>
        public ObservableCollection<string> AllRoleName
        {
            get { return _allRoleName; }
            set
            {
                if (value.Contains("管理员"))
                {
                    value.Remove("管理员");
                }
                _allRoleName = value;
                RaisePropertyChanged(); 
            }
        }

        /// <summary>
        /// 角色名称
        /// </summary>
        private string _userRoleName = "";
        public string UserRoleName
        {
            get { return _userRoleName; }
            set { _userRoleName = value; RaisePropertyChanged(); }
        }   

        /// <summary>
        /// 用户状态
        /// </summary>
        private byte _status = 1;
        public byte Status
        {
            get { return _status; }
            set { _status = value; RaisePropertyChanged(); }
        }


        /// <summary>
        /// 卡号
        /// </summary>
        private string _cardNo = "";
        public string CardNo
        {
            get { return _cardNo; }
            set { _cardNo = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public UserManagerViewModel(IConfigManager configManager, RoleRepository roleRepository, UserRepository userRepository)
        {
            ModelParam = PrismProvider.User;
            AllRoleName = PrismProvider.User.AllRole.Select(x => x.RoleName).ToObservableCollection();
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Subscribe(RefreshRoleName, ThreadOption.UIThread,false);
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "添加用户":
                    {
                        int CardNo = 0;
                        if (this.UserName == "")
                        {
                            MessageView.Ins.MessageBoxShow("用户名称不能为空", eMsgType.Info);
                            return;
                        }
                        if (this.PasswordHash == "")
                        {
                            MessageView.Ins.MessageBoxShow("密码不能为空", eMsgType.Info);
                            return;
                        }
                        if (this.UserRoleName == "")
                        {
                            MessageView.Ins.MessageBoxShow("角色名称不能为空", eMsgType.Info);
                            return;
                        }
                        if (this.CardNo != "" && !int.TryParse(this.CardNo,out CardNo))
                        {
                            MessageView.Ins.MessageBoxShow("卡号只能是纯数字", eMsgType.Info);
                            return;
                        }
                        if (Regex.IsMatch(this.PasswordHash, @"[\u4e00-\u9fa5]"))
                        {
                            MessageView.Ins.MessageBoxShow("密码中不能包含中文字符", eMsgType.Info);
                            return;
                        }

                        if(ModelParam.AllUser.Count(x => x.Username == this.UserName) > 0)
                        {
                            MessageView.Ins.MessageBoxShow("不能添加重复的用户", eMsgType.Info);
                            return;
                        }

                        RoleIdToRoleNameConverter roleConverter = new RoleIdToRoleNameConverter();
                        object rolename = roleConverter.ConvertBack(UserRoleName, typeof(int), new object(), CultureInfo.CurrentCulture);
                        User user = new User()
                        {
                            Username = this.UserName,
                            PasswordHash = this.PasswordHash,
                            CardNo = this.CardNo,
                            RoleId = (int)rolename,
                            Status = this.Status,
                            CreateBy = ModelParam.CurUser.UserId,
                            CreateTime = DateTime.Now,
                            UpdateBy = ModelParam.CurUser.UserId,
                            UpdateTime = DateTime.Now,
                            RealName = ""
                        };
                        _userRepository.Insert(user);
                        int idmax = _userRepository.GetList().Max(x => x.UserId);
                        user.UserId = idmax;
                        ModelParam.AllUser.Add(user);
                        this.UserName = "";
                        this.PasswordHash = "";
                        this.CardNo = "";
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
                case "权限管理":
                    //弹窗权限管理页面
                    PrismProvider.DialogService.Show("PermissionManageView", new DialogParameters
                        {
                            { "Title", "权限管理" },
                            { "Icon", "\ue65f" },
                        }, result =>
                        {
                        }, nameof(DialogWindowView));
                    break;
                case "修改":
                    //弹窗用户修改页面
                    PrismProvider.DialogService.Show("UserEditView", new DialogParameters
                        {
                            { "Title", "用户修改" },
                            { "Icon", "\ue6af" },
                        }, result =>
                        {
                        }, nameof(DialogWindowView));
                    break;
            }
        });

        public DelegateCommand<User> EditCommand => new DelegateCommand<User>((user) =>
        {
            PrismProvider.DialogService.Show("EditUserView", new DialogParameters
            {
                 { "Title", "用户修改" },
                 { "Icon", "\ue6af" },
                 { "Param", user },
            }, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    User edituser = result.Parameters.GetValue<object>("Param") as User;
                    ModelParam.AllUser.Remove(user);
                    ModelParam.AllUser.Add(edituser);
                }
            }, nameof(DialogWindowView));
        });

        public DelegateCommand<User> DeleteCommand => new DelegateCommand<User>((user) =>
        {
            if(user.Username == "admin")
            {
                MessageView.Ins.MessageBoxShow("管理员用户不可以删除", eMsgType.Info);
                return;
            }
            ModelParam.AllUser.Remove(user);
            _userRepository.Delete(user);
        });
        #endregion



        #region Methods
        public void RefreshRoleName()
        {
             AllRoleName = PrismProvider.User.AllRole.Select(x => x.RoleName).ToObservableCollection();
        }

        /// <summary>
        /// 关闭时执行的方法
        /// </summary>
        public virtual void OnDialogClosed()
        {
            PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Unsubscribe(RefreshRoleName);
        }
        #endregion
    }
}
