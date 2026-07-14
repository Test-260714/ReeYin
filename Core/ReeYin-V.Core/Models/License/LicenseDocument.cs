using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ReeYin_V.License.Models
{
    /// <summary>授权文件的数据结构。</summary>
    public sealed class LicenseDocument
    {
        /// <summary>授权绑定的机器码。</summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>授权到期时间，空表示永久。</summary>
        public DateTime? ExpireTime { get; set; }

        /// <summary>客户名称。</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>授权允许的模块列表。</summary>
        public List<string> Modules { get; set; } = new();

        /// <summary>授权版本标识。</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>是否为试用授权。</summary>
        public bool IsTrial { get; set; }

        /// <summary>授权文件签名。</summary>
        public string Signature { get; set; } = string.Empty;

        [JsonIgnore]
        /// <summary>根据字段推断的授权类型。</summary>
        public LicenseType Type
        {
            get
            {
                if (IsTrial)
                {
                    return LicenseType.Trial;
                }

                if (ExpireTime.HasValue)
                {
                    return LicenseType.TimeLimited;
                }

                return LicenseType.Permanent;
            }
        }

        [JsonIgnore]
        /// <summary>是否设置了到期时间。</summary>
        public bool HasExpireTime => ExpireTime.HasValue;

        [JsonIgnore]
        /// <summary>去重并标准化后的模块列表。</summary>
        public IReadOnlyCollection<string> NormalizedModules =>
            Modules
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Select(static item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
