using ReeYin_V.License.Models;

namespace ReeYin_V.License.Services
{
    /// <summary>
    /// 机器码哈希算法。
    /// </summary>
    public enum MachineCodeHashAlgorithm
    {
        Sha256 = 0,
        Md5 = 1
    }

    /// <summary>
    /// 提供本机硬件指纹和机器码。
    /// </summary>
    public interface IHardwareInfoService
    {
        /// <summary>
        /// 读取当前设备的硬件指纹。
        /// </summary>
        HardwareFingerprint GetFingerprint();

        /// <summary>
        /// 根据硬件指纹生成机器码。
        /// </summary>
        string GetMachineCode(MachineCodeHashAlgorithm algorithm = MachineCodeHashAlgorithm.Sha256);
    }
}
