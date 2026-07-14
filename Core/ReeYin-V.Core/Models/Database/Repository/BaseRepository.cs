using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ReeYin_V.Core.Models.Database.Tables;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Repository
{
    /// <summary>
    /// 参考链接：https://www.donet5.com/home/doc?masterId=1&typeId=1228
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseRepository<T> : SimpleClient<T> where T : BaseEntity, new()
    {
        public BaseRepository(ISqlSugarClient context) : base(context)
        {
            base.Context = context;
        }

        #region 新增操作
        /// <summary>
        /// 新增单条记录（自动填充创建/更新信息）
        /// </summary>
        public virtual async Task<bool> InsertAsync(T entity)
        {
            return await base.InsertAsync(entity);
        }

        /// <summary>
        /// 新增多条记录（自动填充创建/更新信息）
        /// </summary>
        public virtual async Task<bool> InsertRangeAsync(List<T> entities)
        {
            return await base.InsertRangeAsync(entities);
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新单条记录（自动填充更新信息）
        /// </summary>
        public virtual async Task<bool> UpdateAsync(T entity)
        {
            // 排除创建时间和创建人（避免被覆盖）
            return await base.Context.Updateable(entity)
                .IgnoreColumns(it => new { it.CreateBy, it.CreateTime })
                .ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 按条件更新（支持动态字段）
        /// </summary>
        public virtual async Task<bool> UpdateAsync(Expression<Func<T, bool>> where, Expression<Func<T, T>> updateColumns)
        {
            return await base.Context.Updateable<T>()
                .SetColumns(updateColumns)
                .Where(where)
                .ExecuteCommandAsync() > 0;
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 物理删除（根据ID）
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            return await base.Context.Deleteable<T>(id).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 物理删除（根据条件）
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> where)
        {
            return await base.Context.Deleteable(where).ExecuteCommandAsync() > 0;
        }

        ///// <summary>
        ///// 逻辑删除（需实体包含Status字段）
        ///// </summary>
        //public virtual async Task<bool> SoftDeleteAsync(int id)
        //{
        //    return await base.Context.Updateable<T>()
        //        .SetColumns(it => new T { Status = 0, UpdateBy = CurrentUserId, UpdateTime = DateTime.Now })
        //        .Where(it => it.GetId() == id)
        //        .ExecuteCommandAsync() > 0;
        //}
        #endregion

        #region 查询操作
        public virtual async Task<T> GetByIdAsync(int id) => await base.Context.Queryable<T>().In(id).FirstAsync();

        public virtual async Task<T> GetFirstAsync(Expression<Func<T, bool>> where) => await base.Context.Queryable<T>().Where(where).FirstAsync();

        public virtual async Task<List<T>> GetListAsync(Expression<Func<T, bool>> where) => await base.Context.Queryable<T>().Where(where).ToListAsync();

        public virtual async Task<List<T>> GetPageListAsync(
            Expression<Func<T, bool>> where,
            int pageIndex = 1,
            int pageSize = 20,
            Expression<Func<T, object>> orderBy = null,
            OrderByType orderType = OrderByType.Asc)
        {
            var query = base.Context.Queryable<T>().Where(where);
            if (orderBy != null) query = query.OrderBy(orderBy, orderType);
            return await query.ToPageListAsync(pageIndex, pageSize);
        }
        #endregion
    }
}
