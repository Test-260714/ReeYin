using System;

namespace ReeYin_V.Core.Services.License
{
    /// <summary>
    /// 提供模块授权判断入口。
    /// </summary>
    public static class LicensePermissionHub
    {
        /// <summary>
        /// 外部注入的模块权限判断委托。
        /// </summary>
        public static Func<string, bool>? ModulePermissionEvaluator { get; set; }

        /// <summary>
        /// 判断指定模块是否允许使用。
        /// </summary>
        public static bool IsModuleAllowed(string moduleName)
        {
            var evaluator = ModulePermissionEvaluator;
            if (evaluator == null)
            {
                return true;
            }

            try
            {
                return evaluator(moduleName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清空模块权限判断委托。
        /// </summary>
        public static void Reset()
        {
            ModulePermissionEvaluator = null;
        }
    }
}
