using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Share.Events;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ReeYin_V.UserManager.ViewModels
{
    public class UserSwitchViewModel : DialogViewModelBase
    {
        private ObservableCollection<string> _modelParam;
        private UserRepository _userRepository { get; }
        private RoleRepository _roleRepository { get; }

        private IUserService _userService { get; }
        public ObservableCollection<string> ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; RaisePropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; RaisePropertyChanged(); }
        }

        private CurrentUser _currentUser;
        public CurrentUser CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; RaisePropertyChanged(); }
        }

        public UserSwitchViewModel(IUserService userService, UserRepository userRepository,RoleRepository roleRepository)
        {
            CurrentUser = userService.CurUser;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userService = userService;
            ModelParam = userService.AllUser.Select(x => x.Username).ToObservableCollection();
        }

        public DelegateCommand LoginCommand => new DelegateCommand(() =>
        {
            var Result = _userService.AllUser.Where(p => p.Username == UserName && p.PasswordHash == Password).ToList();
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
            {
                MessageView.Ins.MessageBoxShow("用户名或密码不能为空！", eMsgType.Info);
                return;
            }
            else if (Result.Count == 0)
            {
                MessageView.Ins.MessageBoxShow("您输入用户名或密码不正确！", eMsgType.Info);
                return;
            }
            else if (Result.Count != 0 && Result[0].Status == 0)
            {
                MessageView.Ins.MessageBoxShow("该用户已被禁用，请联系管理员！", eMsgType.Info);
                return;
            }
            else
            {
                CurrentUser.UserId = Result[0].UserId;
                CurrentUser.UserName = Result[0].Username;
                CurrentUser.Password = Result[0].PasswordHash;
                CurrentUser.RoleID = Result[0].RoleId;
                CurrentUser.PermissionID = _roleRepository.GetList(p => p.RoleId == Result[0].RoleId).FirstOrDefault().PermissionID;
                CurrentUser.LoginTime = DateTime.Now;
                CurrentUser.UpdateBy = (int)Result[0].UpdateBy;
                this.CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                });
                ServiceProvider.MenuService.OnLoadMenu();
                PrismProvider.EventAggregator.GetEvent<CurrentUserChangedEvent>().Publish();
                MessageView.Ins.MessageBoxShow($"用户切换成功，当前用户名为{CurrentUser.UserName}！", eMsgType.Success);
            }
        });

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            CloseDialog(ButtonResult.No, new DialogParameters()
            {

            });
        });

    }
}
