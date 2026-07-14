using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Update
{
    /// <summary>
    /// 更新包信息
    /// </summary>
    public class UpdatePackageInfo
    {
        /// <summary>
        /// 包ID
        /// </summary>
        public string PackageId { get; set; }

        /// <summary>
        /// 包名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 发布日期
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// 下载地址
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件哈希值（用于校验）
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// 是否为必须更新
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// 更新类型（组件/核心/补丁）
        /// </summary>
        public UpdateType UpdateType { get; set; }

        /// <summary>
        /// 依赖的最低版本
        /// </summary>
        public string MinRequiredVersion { get; set; }

        /// <summary>
        /// 更新日志
        /// </summary>
        public string ChangeLog { get; set; }
    }

    /// <summary>
    /// 更新类型
    /// </summary>
    public enum UpdateType
    {
        /// <summary>
        /// 组件更新
        /// </summary>
        Component,

        /// <summary>
        /// 核心更新
        /// </summary>
        Core,

        /// <summary>
        /// 补丁更新
        /// </summary>
        Patch,

        /// <summary>
        /// 完整包
        /// </summary>
        Full
    }

    /// <summary>
    /// 更新查询条件
    /// </summary>
    public class UpdateQueryCondition
    {
        /// <summary>
        /// 当前版本
        /// </summary>
        public string CurrentVersion { get; set; }

        /// <summary>
        /// 组件名称（可选，用于查询特定组件）
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// 更新类型筛选
        /// </summary>
        public UpdateType? UpdateType { get; set; }

        /// <summary>
        /// 是否包含预发布版本
        /// </summary>
        public bool IncludePreRelease { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseKey { get; set; }

        /// <summary>
        /// 客户端标识
        /// </summary>
        public string ClientId { get; set; }
    }

    /// <summary>
    /// 更新进度信息
    /// </summary>
    public class UpdateProgress
    {
        /// <summary>
        /// 当前阶段
        /// </summary>
        public UpdateStage Stage { get; set; }

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// 当前处理的文件名
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// 已下载字节数
        /// </summary>
        public long BytesDownloaded { get; set; }

        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 更新阶段
    /// </summary>
    public enum UpdateStage
    {
        /// <summary>
        /// 检查更新
        /// </summary>
        Checking,

        /// <summary>
        /// 下载中
        /// </summary>
        Downloading,

        /// <summary>
        /// 校验中
        /// </summary>
        Verifying,

        /// <summary>
        /// 解压中
        /// </summary>
        Extracting,

        /// <summary>
        /// 安装中
        /// </summary>
        Installing,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 更新服务接口
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        string ServerUrl { get; set; }

        /// <summary>
        /// 是否正在更新
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="condition">查询条件</param>
        /// <returns>可用的更新包列表</returns>
        Task<List<UpdatePackageInfo>> CheckForUpdatesAsync(UpdateQueryCondition condition);

        /// <summary>
        /// 下载更新包
        /// </summary>
        /// <param name="package">更新包信息</param>
        /// <param name="progress">进度回调</param>
        /// <returns>下载的本地文件路径</returns>
        Task<string> DownloadUpdateAsync(UpdatePackageInfo package, IProgress<UpdateProgress> progress = null);

        /// <summary>
        /// 安装更新
        /// </summary>
        /// <param name="packagePath">更新包本地路径</param>
        /// <param name="progress">进度回调</param>
        /// <returns>是否安装成功</returns>
        Task<bool> InstallUpdateAsync(string packagePath, IProgress<UpdateProgress> progress = null);

        /// <summary>
        /// 取消当前更新操作
        /// </summary>
        void CancelUpdate();

        /// <summary>
        /// 获取当前应用版本
        /// </summary>
        string GetCurrentVersion();

        /// <summary>
        /// 验证更新包完整性
        /// </summary>
        /// <param name="packagePath">包路径</param>
        /// <param name="expectedHash">期望的哈希值</param>
        /// <returns>是否验证通过</returns>
        Task<bool> VerifyPackageAsync(string packagePath, string expectedHash);
    }
}
