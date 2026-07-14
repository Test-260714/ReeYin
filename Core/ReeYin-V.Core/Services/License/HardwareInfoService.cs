using ReeYin_V.License.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace ReeYin_V.License.Services
{
    /// <summary>
    /// 读取本机硬件信息并生成授权机器码。
    /// </summary>
    public sealed class HardwareInfoService : IHardwareInfoService
    {
        /// <summary>
        /// 读取 CPU、主板、磁盘和网卡信息组成硬件指纹。
        /// </summary>
        public HardwareFingerprint GetFingerprint()
        {
            return new HardwareFingerprint
            {
                CpuId = ReadWmiValue("Win32_Processor", "ProcessorId", "UnknownCPU"),
                MainboardSerial = ReadWmiValue("Win32_BaseBoard", "SerialNumber", "UnknownBoard"),
                DiskSerial = ReadDiskSerial(),
                MacAddress = ReadMacAddress()
            };
        }

        /// <summary>
        /// 将硬件指纹归一化后计算哈希机器码。
        /// </summary>
        public string GetMachineCode(MachineCodeHashAlgorithm algorithm = MachineCodeHashAlgorithm.Sha256)
        {
            HardwareFingerprint hardware = GetFingerprint();
            string payload =
                $"{Normalize(hardware.CpuId)}|{Normalize(hardware.MainboardSerial)}|{Normalize(hardware.DiskSerial)}|{Normalize(hardware.MacAddress)}";

            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            byte[] hash = algorithm switch
            {
                MachineCodeHashAlgorithm.Md5 => MD5.HashData(bytes),
                _ => SHA256.HashData(bytes)
            };

            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// 读取单个 WMI 字段，失败或无有效值时返回备用值。
        /// </summary>
        private static string ReadWmiValue(string wmiClass, string propertyName, string fallback)
        {
            string value = ReadWmiValues($"SELECT {propertyName} FROM {wmiClass}", propertyName)
                .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>
        /// 执行 WMI 查询并返回有效硬件字段值。
        /// </summary>
        private static IEnumerable<string> ReadWmiValues(string query, string propertyName)
        {
            var values = new List<string>();
            try
            {
                using var searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject obj in searcher.Get().OfType<ManagementObject>())
                {
                    object? raw = obj[propertyName];
                    string value = Normalize(raw?.ToString());
                    if (IsUsefulHardwareValue(value))
                    {
                        values.Add(value);
                    }
                }
            }
            catch
            {
            }

            return values;
        }

        /// <summary>
        /// 按稳定性优先级读取磁盘序列号。
        /// </summary>
        private static string ReadDiskSerial()
        {
            string serial = ReadDiskDriveSerial();
            if (!string.IsNullOrWhiteSpace(serial))
            {
                return serial;
            }

            serial = ReadPhysicalMediaSerial();
            if (!string.IsNullOrWhiteSpace(serial))
            {
                return serial;
            }

            serial = ReadLogicalDiskSerial();
            return string.IsNullOrWhiteSpace(serial) ? "UnknownDisk" : serial;
        }

        /// <summary>
        /// 从物理磁盘信息读取磁盘序列号。
        /// </summary>
        private static string ReadDiskDriveSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Index, SerialNumber FROM Win32_DiskDrive");
                return searcher.Get()
                    .OfType<ManagementObject>()
                    .Select(static item => new
                    {
                        Index = TryParseInt(item["Index"]?.ToString()),
                        Serial = Normalize(item["SerialNumber"]?.ToString())
                    })
                    .Where(static item => IsUsefulHardwareValue(item.Serial))
                    .OrderBy(static item => item.Index)
                    .ThenBy(static item => item.Serial, StringComparer.OrdinalIgnoreCase)
                    .Select(static item => item.Serial)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 从物理介质信息读取磁盘序列号。
        /// </summary>
        private static string ReadPhysicalMediaSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Tag, SerialNumber FROM Win32_PhysicalMedia");
                return searcher.Get()
                    .OfType<ManagementObject>()
                    .Select(static item => new
                    {
                        Tag = Normalize(item["Tag"]?.ToString()),
                        Serial = Normalize(item["SerialNumber"]?.ToString())
                    })
                    .Where(static item => IsUsefulHardwareValue(item.Serial))
                    .OrderBy(static item => item.Tag, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static item => item.Serial, StringComparer.OrdinalIgnoreCase)
                    .Select(static item => item.Serial)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 从本地逻辑磁盘读取卷序列号。
        /// </summary>
        private static string ReadLogicalDiskSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT DeviceID, VolumeSerialNumber FROM Win32_LogicalDisk WHERE DriveType = 3");
                return searcher.Get()
                    .OfType<ManagementObject>()
                    .Select(static item => new
                    {
                        DeviceId = Normalize(item["DeviceID"]?.ToString()),
                        Serial = Normalize(item["VolumeSerialNumber"]?.ToString())
                    })
                    .Where(static item => IsUsefulHardwareValue(item.Serial))
                    .OrderBy(static item => item.DeviceId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static item => item.Serial, StringComparer.OrdinalIgnoreCase)
                    .Select(static item => item.Serial)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取非虚拟网卡的 MAC 地址。
        /// </summary>
        private static string ReadMacAddress()
        {
            try
            {
                string mac = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(static item =>
                        item.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        item.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                        item.GetPhysicalAddress().GetAddressBytes().Length >= 6 &&
                        !LooksVirtualNetworkInterface(item))
                    .Select(static item => Normalize(item.GetPhysicalAddress().ToString()))
                    .Where(static item => IsUsefulHardwareValue(item))
                    .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(mac))
                {
                    return mac;
                }
            }
            catch
            {
            }

            return "UnknownMac";
        }

        /// <summary>
        /// 判断网卡名称或描述是否像虚拟网卡。
        /// </summary>
        private static bool LooksVirtualNetworkInterface(NetworkInterface item)
        {
            string value = $"{item.Name} {item.Description}";
            return value.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("VPN", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 过滤空值和常见无效硬件占位值。
        /// </summary>
        private static bool IsUsefulHardwareValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = Normalize(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return normalized != "TOBEFILLEDBYOEM" &&
                normalized != "DEFAULTSTRING" &&
                normalized != "NONE" &&
                normalized != "UNKNOWN" &&
                normalized != "0" &&
                normalized != "00000000";
        }

        /// <summary>
        /// 尝试解析排序用的整数。
        /// </summary>
        private static int TryParseInt(string? value)
        {
            return int.TryParse(value, out int result) ? result : int.MaxValue;
        }

        /// <summary>
        /// 去除硬件字段两端空白。
        /// </summary>
        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
