namespace ReeYin_V.License.Models
{
    /// <summary>授权校验状态。</summary>
    public enum LicenseValidationStatus
    {
        /// <summary>校验通过。</summary>
        Valid = 0,
        /// <summary>未找到授权文件。</summary>
        NotFound = 1,
        /// <summary>授权内容格式错误。</summary>
        InvalidJson = 2,
        /// <summary>签名校验失败。</summary>
        InvalidSignature = 3,
        /// <summary>机器码不匹配。</summary>
        MachineCodeMismatch = 4,
        /// <summary>授权已过期。</summary>
        Expired = 5,
        /// <summary>发生未分类异常。</summary>
        UnknownError = 6,
        /// <summary>检测到系统时间回拨。</summary>
        ClockRollbackDetected = 7
    }
}
