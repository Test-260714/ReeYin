using System;
using System.Collections.Generic;

namespace ReeYin_V.License.Models
{
    /// <summary>授权校验结果。</summary>
    public sealed class LicenseValidationResult
    {
        /// <summary>校验状态。</summary>
        public LicenseValidationStatus Status { get; init; }

        /// <summary>校验结果说明。</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>当前设备机器码。</summary>
        public string MachineCode { get; init; } = string.Empty;

        /// <summary>校验时间。</summary>
        public DateTime CheckedAt { get; init; } = DateTime.Now;

        /// <summary>解析出的授权信息。</summary>
        public LicenseDocument? License { get; init; }

        /// <summary>本次校验生效的模块集合。</summary>
        public IReadOnlyCollection<string> Modules { get; init; } = Array.Empty<string>();

        /// <summary>当前结果是否为有效授权。</summary>
        public bool IsValid => Status == LicenseValidationStatus.Valid;

        /// <summary>创建统一格式的校验结果。</summary>
        public static LicenseValidationResult Create(
            LicenseValidationStatus status,
            string message,
            string machineCode,
            LicenseDocument? license = null,
            IReadOnlyCollection<string>? modules = null)
        {
            return new LicenseValidationResult
            {
                Status = status,
                Message = message,
                MachineCode = machineCode,
                License = license,
                Modules = modules ?? Array.Empty<string>(),
                CheckedAt = DateTime.Now
            };
        }
    }
}
