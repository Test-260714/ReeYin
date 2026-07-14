using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Database.Repository
{
    [ExposedService(Lifetime.Singleton)]
    public class MenuRepository : BaseRepository<Menu>
    {
        public MenuRepository(ISqlSugarClient context) : base(context)
        {


        }
    }
}
