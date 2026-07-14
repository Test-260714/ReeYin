using ReeYin_V.License.Models;
using System;

namespace ReeYin_V.License.Services
{
    /// <summary>授权服务接口。</summary>
    public interface ILicenseService
    {
        /// <summary>授权文件目录。</summary>
        string LicenseDirectory { get; }

        /// <summary>当前授权文件路径。</summary>
        string LicenseFilePath { get; }

        /// <summary>获取当前设备机器码。</summary>
        string GetCurrentMachineCode();

        /// <summary>按默认策略校验当前授权。</summary>
        LicenseValidationResult ValidateCurrentLicense();

        /// <summary>按指定试用策略校验当前授权。</summary>
        LicenseValidationResult ValidateCurrentLicense(bool allowTrialFallback);

        /// <summary>校验指定授权文件。</summary>
        LicenseValidationResult ValidateLicenseFile(string filePath);

        /// <summary>导入并激活授权文件。</summary>
        LicenseValidationResult ImportLicense(string filePath);

        /// <summary>判断模块是否具备授权。</summary>
        bool HasModulePermission(string moduleName);

        /// <summary>当前缓存的授权状态。</summary>
        LicenseValidationResult CurrentStatus { get; }

        /// <summary>当前缓存的授权内容。</summary>
        LicenseDocument? CurrentLicense { get; }

        /// <summary>当前试用授权到期时间。</summary>
        DateTime? TrialExpireTime { get; }
    }
}
