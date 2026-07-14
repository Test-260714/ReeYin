using System;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Recipe.Interfaces
{
    /// <summary>
    /// 配方数据持久化接口
    /// </summary>
    public interface IRecipeRepository
    {
        /// <summary>
        /// 加载配方配置
        /// </summary>
        Task<ProjectRecipeConfig> LoadAsync();

        /// <summary>
        /// 保存配方配置
        /// </summary>
        Task<bool> SaveAsync(ProjectRecipeConfig config);

        /// <summary>
        /// 同步获取配方配置（阻塞操作）
        /// </summary>
        ProjectRecipeConfig Load();

        /// <summary>
        /// 同步保存配方配置（阻塞操作）
        /// </summary>
        bool Save(ProjectRecipeConfig config);
    }
}

