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
    public class UserRepository : BaseRepository<User>
    {
        public UserRepository(ISqlSugarClient context): base(context)
        {


        }

        // 扩展方法示例：根据用户名查询
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await GetFirstAsync(it => it.Username == username);
        }
    }
}
