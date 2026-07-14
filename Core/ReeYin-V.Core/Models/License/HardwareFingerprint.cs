namespace ReeYin_V.License.Models
{
    /// <summary>组成机器码的硬件指纹信息。</summary>
    public sealed class HardwareFingerprint
    {
        /// <summary>CPU 标识。</summary>
        public string CpuId { get; init; } = string.Empty;

        /// <summary>主板序列号。</summary>
        public string MainboardSerial { get; init; } = string.Empty;

        /// <summary>磁盘序列号。</summary>
        public string DiskSerial { get; init; } = string.Empty;

        /// <summary>物理网卡 MAC 地址。</summary>
        public string MacAddress { get; init; } = string.Empty;
    }
}
