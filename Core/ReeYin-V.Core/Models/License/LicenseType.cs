namespace ReeYin_V.License.Models
{
    /// <summary>授权类型。</summary>
    public enum LicenseType
    {
        /// <summary>未知类型。</summary>
        Unknown = 0,
        /// <summary>永久授权。</summary>
        Permanent = 1,
        /// <summary>时效授权。</summary>
        TimeLimited = 2,
        /// <summary>试用授权。</summary>
        Trial = 3
    }
}
