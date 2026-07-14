using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;

namespace ReeYin_V.Core.Cache
{
    public class DictCacheService
    {
        //private readonly IMemoryCache _cache;
        //private readonly DictRepository _dictRepository;

        //public DictCacheService(IMemoryCache cache, DictRepository dictRepository)
        //{
        //    _cache = cache;
        //    _dictRepository = dictRepository;
        //}

        ///// <summary>
        ///// 获取字典项（优先从缓存读取）
        ///// </summary>
        //public async Task<List<Dict>> GetCachedDictItemsAsync(string typeCode)
        //{
        //    return await _cache.GetOrCreateAsync($"dict_{typeCode}", async entry =>
        //    {
        //        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // 缓存1小时
        //        return await _dictRepository.GetDictItemsByTypeAsync(typeCode);
        //    });
        //}
    }
}
