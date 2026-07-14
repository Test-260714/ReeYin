using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using ReeYin_V.Core.Cache;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Logger;
using ReeYin_V.Login.Model;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Login.ViewModels
{
    public class LoginViewModel : DialogViewModelBase
    {
        #region Fields
        private IUserService _userService;

        private BarCodeHook _barCodeHook;
        private ICacheManager CacheManager { get; }
        #endregion

        #region Properties
        private string _loginIconPath;

        public string LoginIconPath
        {
            get { return _loginIconPath; }
            set { _loginIconPath = value; RaisePropertyChanged(); }
        }

        private bool _isRemember;

        public bool IsRemember
        {
            get { return _isRemember; }
            set { _isRemember = value; RaisePropertyChanged(); }
        }

        private bool _isAutoLogin;

        public bool IsAutoLogin
        {
            get { return _isAutoLogin; }
            set { _isAutoLogin = value; RaisePropertyChanged(); }
        }

        public CurrentUser CurUser
        {
            get { return _userService.CurUser; }
            set { _userService.CurUser = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> AllUserName
        {
            get { return _userService.AllUserName; }
            set { _userService.AllUserName = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor     
        public LoginViewModel(ICacheManager cacheManager, IUserService userService)
        {
            _userService = userService;
            CacheManager = cacheManager;
            LoadUserCache();

            LoginIconPath = PrismProvider.AppBasePath + "\\Resource\\Icon\\login_logo.png";

            if (!File.Exists(LoginIconPath))
            {
                LoginIconPath = PrismProvider.AppBasePath + "\\Resource\\Icon\\Default.png";
            }
        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //调试跳过登录页面
            if (Debugger.IsAttached)
            {
                CurUser.UserName = "admin";

                CurUser.Password = "12";
                Console.WriteLine("程序正在被调试器调试");
                Logs.LogInfo($"{CurUser.UserName}登录成功！");
                if (_userService.VerifyIdentity())
                {
                    Logs.LogInfo($"{CurUser.UserName}登录成功！");
                    //当用户登录成功后给出通知或触发一个事件
                    PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurUser);
                }
            }
            _barCodeHook = new BarCodeHook();
            _barCodeHook.BarCodeEvent += BarCode_BarCodeEvent;
            _barCodeHook.Start();
        });

        public DelegateCommand UnLoadedCommand => new DelegateCommand(() =>
        {
            _barCodeHook.BarCodeEvent -= BarCode_BarCodeEvent;
            _barCodeHook.Stop();
        });

        public DelegateCommand AutoLoginCommand => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(CurUser.UserName) || string.IsNullOrEmpty(CurUser.Password)) return;
            if (!_userService.VerifyIdentity()) return;

            User user = _userService.AllUser.FirstOrDefault(p => p.Username == CurUser.UserName);
            if (user?.Status == 0) return;

            Logs.LogInfo($"{CurUser.UserName}登录成功！");
            PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurUser);
        });

        public DelegateCommand LoginCommand => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(CurUser.UserName) || string.IsNullOrEmpty(CurUser.Password))
            {
                MessageView.Ins.MessageBoxShow("用户名或密码不能为空！", eMsgType.Info);
                return;
            }
            else
            {

                if (IsRemember)
                {
                    CacheManager.Set(CacheKey.User, CurUser);
                }
                else
                {
                    CacheManager.Delete(CacheKey.User);
                }

                CacheManager.Set(CacheKey.IsRemember, IsRemember);
                CacheManager.Set(CacheKey.IsAutoLogin, IsAutoLogin);

                //校验登陆用户是否正确
                if (_userService.VerifyIdentity())
                {
                    User Switchuser = _userService.AllUser.Where(p => p.Username == CurUser.UserName).FirstOrDefault();
                    if (Switchuser.Status == 0)
                    {
                        MessageView.Ins.MessageBoxShow("该用户已被禁用，请联系管理员！", eMsgType.Info);
                        return;
                    }
                    Logs.LogInfo($"{CurUser.UserName}登录成功！");
                    //当用户登录成功后给出通知或触发一个事件
                    PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurUser);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("您输入的用户名或密码不正确！", eMsgType.Info);
                    //PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurUser);
                    return;
                }
            }
        });

        #endregion

        #region Methods 
        /// <summary>
        /// 获取用户缓存
        /// </summary>
        private void LoadUserCache()
        {
            if (CacheManager.Get(CacheKey.User, out CurrentUser user))
            {
                CurUser.UserName = user.UserName;
                CurUser.Password = user.Password;
            }

            if (CacheManager.Get(CacheKey.IsRemember, out bool isRemember))
            {
                IsRemember = isRemember;
            }

            if (CacheManager.Get(CacheKey.IsAutoLogin, out bool isAutoLogin))
            {
                IsAutoLogin = isAutoLogin;
            }
        }

        /// <summary>
        /// 刷卡后对应的事件处理程序
        /// </summary>
        /// <param name="barCode"></param>
        public void BarCode_BarCodeEvent(BarCodeHook.BarCodes barCode)
        {
            string cardNo = barCode.BarCode;
            User user = _userService.AllUser.Where(x => x.CardNo == cardNo).FirstOrDefault();
            if(user != null)
            {
                if (user.Status == 0)
                {
                    MessageView.Ins.MessageBoxShow("该用户已被禁用，请联系管理员！", eMsgType.Info);
                    return;
                }

                CurUser.UserId = user.UserId;
                CurUser.UserName = user.Username;
                CurUser.Password = user.PasswordHash;
                CurUser.RoleID = user.RoleId;
                CurUser.PermissionID = _userService.AllRole.Where(p => p.RoleId == user.RoleId).FirstOrDefault()?.PermissionID ?? 10;
                CurUser.LoginTime = DateTime.Now;
                Logs.LogInfo($"{CurUser.UserName}登录成功！");
                //当用户登录成功后给出通知或触发一个事件
                PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurUser);
            }
            else
            {
                MessageView.Ins.MessageBoxShow($"卡号{cardNo}，没有对应用户。", eMsgType.Info);
                return;
            }
        }

        private void Callback(IDialogResult result)
        {
            //todo...
        }
        #endregion

    }
}
