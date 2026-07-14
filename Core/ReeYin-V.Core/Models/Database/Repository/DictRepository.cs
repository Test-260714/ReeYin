using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Repository
{
    [ExposedService(Lifetime.Singleton)]
    public class DictRepository : BaseRepository<Dict>
    {
        public DictRepository(ISqlSugarClient context) : base(context)
        {


        }

        #region 查询操作
        /// <summary>
        /// 根据类型编码查询所有启用的字典项（支持多级）
        /// </summary>
        public async Task<List<Dict>> GetDictItemsByTypeAsync(string typeCode)
        {
            return await this.Context.Queryable<Dict>()
                .Where(d => d.DictType == typeCode && d.IsEnabled == 1)
                .OrderBy(d => d.Sort)
                .ToListAsync();
        }

        /// <summary>
        /// 根据父级ID查询子字典项（支持多级）
        /// </summary>
        public async Task<List<Dict>> GetChildrenByParentIdAsync(int parentId)
        {
            return await this.Context.Queryable<Dict>()
                .Where(d => d.ParentId == parentId && d.IsEnabled == 1)
                .OrderBy(d => d.Sort)
                .ToListAsync();
        }

        /// <summary>
        /// 根据类型编码和项编码查询字典项名称
        /// </summary>
        public async Task<string> GetItemNameAsync(string typeCode, string itemCode)
        {
            return await this.Context.Queryable<Dict>()
                .Where(d => d.DictType == typeCode && d.DictCode == itemCode && d.IsEnabled == 1)
                .Select(d => d.DictValue)
                .FirstAsync();
        }
        #endregion

        #region 新增/修改操作
        /// <summary>
        /// 新增字典项
        /// </summary>
        public async Task<bool> AddDictItemAsync(Dict dict)
        {
            return await this.Context.Insertable(dict).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 修改字典项
        /// </summary>
        public async Task<bool> UpdateDictItemAsync(Dict dict)
        {
            return await this.Context.Updateable(dict)
                .IgnoreColumns(it => new { it.CreateTime }) // 忽略创建时间
                .ExecuteCommandAsync() > 0;
        }
        #endregion
    }
}
