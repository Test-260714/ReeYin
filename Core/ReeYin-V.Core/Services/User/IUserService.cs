using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Tables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// 当前用户
        /// </summary>
        CurrentUser CurUser { get; set; }


        /// <summary>
        /// 所有用户名
        /// </summary>
        /// <returns></returns>
        ObservableCollection<string> AllUserName { get; set; }


        /// <summary>
        /// 所有用户
        /// </summary>
        ObservableCollection<ReeYin_V.Core.Models.Database.Tables.User> AllUser { get; set; }

        /// <summary>
        /// 所有角色
        /// </summary>
        ObservableCollection<Role> AllRole { get; set; }

        /// <summary>
        /// 所有权限
        /// </summary>
        ObservableCollection<Permission> AllPermisson { get; set; }

        /// <summary>
        /// 所有菜单
        /// </summary>
        ObservableCollection<Menu> AllMenu { get; set; }

        /// <summary>
        /// 验证用户身份
        /// </summary>
        /// <returns></returns>
        bool VerifyIdentity();

        /// <summary>
        /// 登出当前用户
        /// </summary>
        void Logout();

        /// <summary>
        /// 判断执行某操作是否有权限
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        bool VerifyCurUserPermission(UserPermission permission);
    }
}
