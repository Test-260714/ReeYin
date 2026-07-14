using Microsoft.Data.Sqlite;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Governance;
using ReeYin_V.Core.Services.Alarm.HardwareRules;
using ReeYin_V.License.Services;
using ReeYin_V.Core.Services.License;

namespace ReeYin_V.Core
{
    public class CoreModule : IModule
    {
        static string url1 = PrismProvider.AppBasePath;
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
#if DEBUG
            LicensePermissionHub.ModulePermissionEvaluator = _ => true;
#else
            containerProvider.Resolve<ILicenseService>().ValidateCurrentLicense();
#endif
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterSingleton<IHardwareInfoService, HardwareInfoService>();
            containerRegistry.RegisterSingleton<ILicenseService, LicenseService>();
            #region 配置 SqlSugar 客户端
            SQLitePCL.Batteries_V2.Init();
            string connStr = new SqliteConnectionStringBuilder()
            {
                DataSource = $"{url1}\\Config\\DB_ReeYin.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = "ruiqi.12345"
            }.ToString();


            var sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connStr,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            sqlSugarClient.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine(sqlSugarClient.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
            };
            sqlSugarClient.DbMaintenance.CreateDatabase();

            // 关键：启用 CodeFirst 自动建表/迁移（表不存在时创建，存在时迁移）
            sqlSugarClient.CodeFirst
                .SetStringDefaultLength(200)  // 字符串默认长度（未显式设置时生效）
                .InitTables(
                    typeof(User),              // 用户表
                    typeof(Dict),              // 字典表
                    typeof(Menu),              // 菜单表
                    typeof(Role),              // 角色表
                    typeof(Permission),        // 权限表
                    typeof(PermMenuRelation),   // 权限表
                    typeof(ModuleLoadConfig),    // 模块加载配置表
                    typeof(AlarmRecordEntity),   // 报警记录表
                    typeof(AlarmDefinitionEntity), // 报警定义表
                    typeof(HardwareAlarmRuleEntity), // 硬件报警触发规则表
                    typeof(AlarmSuppressionRuleEntity), // 报警抑制规则表
                    typeof(AlarmShelveEntity), // 报警搁置表
                    typeof(AlarmNotificationRouteEntity), // 报警通知路由表
                    typeof(AlarmEventAuditEntity) // 报警事件审计表
                );

            // 初始化模块加载条件评估器
            DatabaseModuleConditionEvaluator.Initialize(sqlSugarClient);
            #endregion

            containerRegistry.RegisterInstance<ISqlSugarClient>(sqlSugarClient);
        }
    }
}
