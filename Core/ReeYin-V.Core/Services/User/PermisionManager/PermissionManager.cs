using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services
{
    public static class PermissionManager
    {
        /// <summary>
        /// 权限表
        /// </summary>
        private static readonly HashSet<string> _permissions = [""];

        /// <summary>初始化用户权限</summary>
        public static void SetPermissions(IEnumerable<string> perms)
        {
            _permissions.Clear();
            foreach (var p in perms) _permissions.Add(p);
        }

        /// <summary>检查是否有权限</summary>
        public static bool Has(string perm) => _permissions.Contains(perm);

        /// <summary>权限更新事件，可用于刷新所有绑定</summary>
        public static event Action PermissionChanged;

        public static void NotifyChanged() => PermissionChanged?.Invoke();
    }

}
