using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Database.Repository
{
    [ExposedService(Lifetime.Singleton)]
    public class PermissionRepository : BaseRepository<Permission>
    {
        private MenuRepository _menuRepository { get; }
        public PermissionRepository(ISqlSugarClient context,MenuRepository menuRepository) : base(context)
        {
            _menuRepository = menuRepository;

        }

        // 新增权限并关联多个菜单
        public async Task<DbResult<bool>> AddPermissionWithMenusAsync(Permission permission, List<int> menuIds)
        {
            return await Context.Ado.UseTranAsync(async () =>
            {
                // 1. 插入权限主表
                var permId = await InsertReturnIdentityAsync(permission);

                // 2. 插入关联表
                var relations = menuIds.Select(menuId => new PermMenuRelation
                {
                    PermId = permId,
                    MenuId = menuId,
                    IsVisible = true,
                    CreateBy = 1,
                    CreateTime = DateTime.Now,
                    CanRead = true,
                    CanWrite = true,
                    UpdateBy = 1,
                    UpdateTime = DateTime.Now

                }).ToList();

                await Context.Insertable(relations).ExecuteCommandAsync();
            });
        }

        // 查询权限及其关联菜单
        public async Task<Permission> GetPermissionWithMenusAsync(int permId)
        {
            return await Context.Queryable<Permission>()
                .LeftJoin<PermMenuRelation>((p, pm) => p.PermId == pm.PermId)
                .LeftJoin<Menu>((p, pm, m) => pm.MenuId == m.MenuId)
                .Where(p => p.PermId == permId)
                .Select((p, pm, m) => new { p, Menu = m })
                .MergeTable()
                .GroupBy(it => it.p.PermId)
                .Select(it => new Permission
                {
                    PermId = it.p.PermId,
                    PermName = it.p.PermName,
                    PermissionMenus = SqlFunc.Subqueryable<PermMenuRelation>()
                        .Where(pm => pm.PermId == it.p.PermId)
                        .ToList()
                })
                .FirstAsync();
        }


        // 新增菜单时，权限菜单表添加相应数据
        public async Task<DbResult<bool>> AddPermissionMissMenusAsync(Permission permission, List<int> menuIds)
        {
            return await Context.Ado.UseTranAsync(async () =>
            {
                // 1. 插入关联表
                var relations = menuIds.Select(menuId => new PermMenuRelation
                {
                    PermId = permission.PermId,
                    MenuId = menuId,
                    IsVisible = true,
                    IsEnabled = _menuRepository.GetList(x => x.MenuId == menuId).FirstOrDefault().Status == 0 ? false : true,
                    CanRead = true,
                    CanWrite = true,
                    CreateBy = 1,
                    CreateTime = DateTime.Now,
                    UpdateBy = 1,
                    UpdateTime = DateTime.Now

                }).ToList();

                await Context.Insertable(relations).ExecuteCommandAsync();
            });
        }




    }
}
