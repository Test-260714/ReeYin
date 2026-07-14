using Prism.Mvvm;
using ReeYin_V.Core.Models.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models
{
    public class CurrentUser :BindableBase
    {
        private int userId;

        public int UserId
        {
            get { return userId; }
            set { userId = value; RaisePropertyChanged(); }
        }

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value; RaisePropertyChanged(); }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; RaisePropertyChanged(); }
        }
        /// <summary>
        /// 当前角色ID
        /// </summary>
        public int RoleID { get; set; }

        /// <summary>
        /// 当前权限ID
        /// </summary>
        public int PermissionID { get; set; }

        private DateTime loginTime = DateTime.Now;

        public DateTime LoginTime
        {
            get { return loginTime; }
            set { loginTime = value; RaisePropertyChanged(); }
        }

        private int updateBy;

        public int UpdateBy
        {
            get { return updateBy; }
            set { updateBy = value; RaisePropertyChanged(); }
        }

    }
}
