using ReeYin_V.Core.Services.License;
using ReeYin_V.License.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReeYin_V.License.Services
{
    /// <summary>负责授权校验、导入和模块权限判断。</summary>
    public sealed class LicenseService : ILicenseService
    {
        /// <summary>默认试用天数。</summary>
        private const int DefaultTrialDays = 30;
        /// <summary>试用起始时间文件名。</summary>
        private const string TrialMarkerFileName = "trial.start";
        /// <summary>本地时钟状态文件名。</summary>
        private const string ClockStateFileName = "license.clock";
        /// <summary>允许的时钟回拨容差。</summary>
        private static readonly TimeSpan ClockRollbackTolerance = TimeSpan.FromMinutes(5);
        /// <summary>保护本地时钟状态时使用的附加熵。</summary>
        private static readonly byte[] ClockStateEntropy = Encoding.UTF8.GetBytes("ReeYin.V.License.ClockState.v1");

        /// <summary>无论授权如何都允许访问的基础模块。</summary>
        private static readonly HashSet<string> AlwaysAllowedModules = new(StringComparer.OrdinalIgnoreCase)
        {
            "CoreModule",
            "ShareModule",
            "ApplicationLoginModule",
            "ApplicationUserManagerModule",
            "ApplicationPermissionModule",
            "ApplicationLicenseModule",
            "ApplicationConfigModule",
            "ApplicationInitializeModule",
            "ApplicatoinMainModule",
            "AppRootManagerModule",
            "ApplicationNotifyFlowAPPModule"
        };

        /// <summary>默认试用授权开放的模块。</summary>
        private static readonly IReadOnlyCollection<string> DefaultTrialModules = new[]
        {
            "ApplicationLoginModule",
            "ApplicationUserManagerModule",
            "ApplicationPermissionModule",
            "ApplicationConfigModule",
            "ApplicationInitializeModule",
            "ApplicatoinMainModule",
        };

        /// <summary>用于校验正式授权的公钥。</summary>
        private const string PublicKeyXml =
            "<RSAKeyValue><Modulus>vi0KtsCUiZT6cXM8XHjgEfVJEAbYYbY69eyonXHS8cUJY0C723iSt9zkKvxU33yOKwVQGhqoUipe4FvOVaby5jDQgETooiD8Rw6rqq6SLRyLGgrEjh3A4Vbbm3rdANvzp/AIiqZWEOTyQbv5YGrBrwtb5VqemaiFJ8vRtKbJZdHRwejFRbXhwi3VuE97gZjX7U62wOB3tikR0fW7IRxDHaJdTKo9AVLRNM0IykE4v+8+XugitnpFPyL134mXsWJBAD5k2fSk5NeaR9FgRzmxGObOr6klDNsE5/YaR3ILr8ewXmfW7dU5ZoaVbINKwQoozvNqFB4Uhv+cGSYOAE/voQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>授权文件序列化配置。</summary>
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>保护运行时状态的同步锁。</summary>
        private readonly object _syncRoot = new();
        /// <summary>硬件信息读取服务。</summary>
        private readonly IHardwareInfoService _hardwareInfoService;
        /// <summary>当前授权允许的模块集合。</summary>
        private readonly HashSet<string> _authorizedModules = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>当前缓存的授权状态。</summary>
        private LicenseValidationResult _currentStatus =
            LicenseValidationResult.Create(LicenseValidationStatus.NotFound, "License 未初始化", string.Empty);

        /// <summary>当前缓存的授权文件内容。</summary>
        private LicenseDocument? _currentLicense;
        /// <summary>当前试用授权到期时间。</summary>
        private DateTime? _trialExpireTime;
        /// <summary>是否允许所有业务模块。</summary>
        private bool _allowAllModules;

        /// <summary>创建授权服务实例。</summary>
        public LicenseService(IHardwareInfoService hardwareInfoService)
        {
            _hardwareInfoService = hardwareInfoService;
        }

        /// <summary>授权目录。</summary>
        public string LicenseDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ReeYin",
                "Project",
                "License");

        /// <summary>当前授权文件路径。</summary>
        public string LicenseFilePath => Path.Combine(LicenseDirectory, "license.json");

        /// <summary>本地时钟状态文件路径。</summary>
        private string ClockStateFilePath => Path.Combine(LicenseDirectory, ClockStateFileName);

        /// <summary>当前试用授权到期时间。</summary>
        public DateTime? TrialExpireTime
        {
            get
            {
                lock (_syncRoot)
                {
                    return _trialExpireTime;
                }
            }
        }

        /// <summary>当前缓存的授权状态。</summary>
        public LicenseValidationResult CurrentStatus
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentStatus;
                }
            }
        }

        /// <summary>当前缓存的授权内容。</summary>
        public LicenseDocument? CurrentLicense
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentLicense;
                }
            }
        }

        /// <summary>获取当前设备机器码。</summary>
        public string GetCurrentMachineCode()
        {
            return _hardwareInfoService.GetMachineCode(MachineCodeHashAlgorithm.Sha256);
        }

        /// <summary>按当前运行环境决定是否允许试用回退。</summary>
        public LicenseValidationResult ValidateCurrentLicense()
        {
#if DEBUG
            return ValidateCurrentLicense(allowTrialFallback: true);
#else
            return ValidateCurrentLicense(allowTrialFallback: false);
#endif
        }

        /// <summary>校验当前授权文件，不存在时可选回退到试用授权。</summary>
        public LicenseValidationResult ValidateCurrentLicense(bool allowTrialFallback)
        {
            EnsureDirectory();

            string machineCode = GetCurrentMachineCode();

            if (!File.Exists(LicenseFilePath))
            {
                var result1 = allowTrialFallback
                    ? BuildTrialResult(machineCode)
                    : LicenseValidationResult.Create(
                        LicenseValidationStatus.NotFound,
                        "未检测到已注册 License，发布版本必须导入有效 License 后才能登录。",
                        machineCode);
                UpdateRuntimeStatus(result1);
                return result1;
            }

            var result = ValidateLicenseFile(LicenseFilePath);
            UpdateRuntimeStatus(result);
            return result;
        }

        /// <summary>校验指定授权文件内容。</summary>
        public LicenseValidationResult ValidateLicenseFile(string filePath)
        {
            string machineCode = GetCurrentMachineCode();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.NotFound,
                    "License 文件不存在",
                    machineCode);
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var license = JsonSerializer.Deserialize<LicenseDocument>(json, SerializerOptions);

                if (license == null)
                {
                    return LicenseValidationResult.Create(
                        LicenseValidationStatus.InvalidJson,
                        "License 格式无效",
                        machineCode);
                }

                return ValidateLicenseDocument(license, machineCode);
            }
            catch (JsonException ex)
            {
                Logs.LogError($"License JSON 解析失败: {ex}");
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.InvalidJson,
                    "License JSON 解析失败",
                    machineCode);
            }
            catch (Exception ex)
            {
                Logs.LogError($"License 校验异常: {ex}");
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.UnknownError,
                    $"License 校验异常: {ex.Message}",
                    machineCode);
            }
        }

        /// <summary>导入授权文件并刷新运行时状态。</summary>
        public LicenseValidationResult ImportLicense(string filePath)
        {
            EnsureDirectory();

            var result = ValidateLicenseFile(filePath);
            if (!result.IsValid)
            {
                UpdateRuntimeStatus(result);
                return result;
            }

            try
            {
                File.Copy(filePath, LicenseFilePath, true);
                var persisted = ValidateLicenseFile(LicenseFilePath);
                UpdateRuntimeStatus(persisted);
                return persisted;
            }
            catch (Exception ex)
            {
                Logs.LogError($"导入 License 失败: {ex}");
                var failed = LicenseValidationResult.Create(
                    LicenseValidationStatus.UnknownError,
                    $"导入 License 失败: {ex.Message}",
                    GetCurrentMachineCode());
                UpdateRuntimeStatus(failed);
                return failed;
            }
        }

        /// <summary>判断指定模块当前是否具备授权。</summary>
        public bool HasModulePermission(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return true;
            }

            if (AlwaysAllowedModules.Contains(moduleName))
            {
                return true;
            }

            lock (_syncRoot)
            {
                if (!_currentStatus.IsValid)
                {
                    return false;
                }

                if (_allowAllModules)
                {
                    return true;
                }

                return _authorizedModules.Contains(moduleName);
            }
        }

        /// <summary>确保授权目录存在。</summary>
        private void EnsureDirectory()
        {
            if (!Directory.Exists(LicenseDirectory))
            {
                Directory.CreateDirectory(LicenseDirectory);
            }
        }

        /// <summary>更新运行时缓存，并同步全局权限判断器。</summary>
        private void UpdateRuntimeStatus(LicenseValidationResult result)
        {
            lock (_syncRoot)
            {
                _currentStatus = result;
                _currentLicense = result.License;
                _authorizedModules.Clear();
                _allowAllModules = false;
                _trialExpireTime = null;

                if (result.License != null)
                {
                    _trialExpireTime = result.License.ExpireTime;

                    if (result.License.NormalizedModules.Count == 0)
                    {
                        _allowAllModules = true;
                    }
                    else
                    {
                        foreach (string module in result.License.NormalizedModules)
                        {
                            _authorizedModules.Add(module);
                        }
                    }
                }
            }

            LicensePermissionHub.ModulePermissionEvaluator = HasModulePermission;
        }

        /// <summary>校验授权文档的完整性和生效条件。</summary>
        private LicenseValidationResult ValidateLicenseDocument(LicenseDocument license, string currentMachineCode)
        {
            if (string.IsNullOrWhiteSpace(license.MachineCode))
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.InvalidJson,
                    "License 缺少 MachineCode",
                    currentMachineCode);
            }

            if (string.IsNullOrWhiteSpace(license.Signature))
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.InvalidSignature,
                    "License 缺少签名",
                    currentMachineCode);
            }

            if (license.IsTrial && !license.ExpireTime.HasValue)
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.InvalidJson,
                    "试用 License 必须包含到期时间",
                    currentMachineCode);
            }

            string canonicalPayload = BuildCanonicalPayload(license);
            if (!VerifySignature(canonicalPayload, license.Signature))
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.InvalidSignature,
                    "License RSA 签名校验失败",
                    currentMachineCode);
            }

            string normalizedMachineCode = Normalize(license.MachineCode);
            if (!string.Equals(normalizedMachineCode, Normalize(currentMachineCode), StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.MachineCodeMismatch,
                    "License 机器码不匹配",
                    currentMachineCode,
                    license);
            }

            DateTime nowUtc = DateTime.UtcNow;
            ClockValidationResult? clockValidation = null;
            if (license.ExpireTime.HasValue)
            {
                clockValidation = ValidateClockState(nowUtc);
                if (!clockValidation.IsValid)
                {
                    return LicenseValidationResult.Create(
                        LicenseValidationStatus.ClockRollbackDetected,
                        clockValidation.Message,
                        currentMachineCode,
                        license,
                        license.NormalizedModules);
                }
            }

            if (license.ExpireTime.HasValue && license.ExpireTime.Value.ToUniversalTime() < nowUtc)
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.Expired,
                    $"License 已过期（{license.ExpireTime:yyyy-MM-dd HH:mm:ss}）",
                    currentMachineCode,
                    license,
                    license.NormalizedModules);
            }

            if (license.ExpireTime.HasValue)
            {
                ClockValidationResult clockUpdate = SaveClockState(currentMachineCode, nowUtc, clockValidation?.LastSeenUtc);
                if (!clockUpdate.IsValid)
                {
                    return LicenseValidationResult.Create(
                        LicenseValidationStatus.ClockRollbackDetected,
                        clockUpdate.Message,
                        currentMachineCode,
                        license,
                        license.NormalizedModules);
                }
            }

            string message = license.Type switch
            {
                LicenseType.Permanent => "永久授权有效",
                LicenseType.Trial => "试用授权有效",
                LicenseType.TimeLimited => "时效授权有效",
                _ => "授权有效"
            };

            return LicenseValidationResult.Create(
                LicenseValidationStatus.Valid,
                message,
                currentMachineCode,
                license,
                license.NormalizedModules);
        }

        /// <summary>构造默认试用授权结果。</summary>
        private LicenseValidationResult BuildTrialResult(string machineCode)
        {
            DateTime nowUtc = DateTime.UtcNow;
            DateTime startTime = LoadOrCreateTrialStartTimeUtc();
            DateTime expireTime = startTime.AddDays(DefaultTrialDays);

            var trialLicense = new LicenseDocument
            {
                MachineCode = machineCode,
                ExpireTime = expireTime,
                CustomerName = "Trial",
                Modules = DefaultTrialModules.ToList(),
                Version = "Trial",
                IsTrial = true,
                Signature = string.Empty
            };

            ClockValidationResult clockValidation = ValidateClockState(nowUtc);
            if (!clockValidation.IsValid)
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.ClockRollbackDetected,
                    clockValidation.Message,
                    machineCode,
                    trialLicense,
                    trialLicense.NormalizedModules);
            }

            if (nowUtc > expireTime)
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.Expired,
                    $"试用已过期（{expireTime:yyyy-MM-dd HH:mm:ss}）",
                    machineCode,
                    trialLicense,
                    trialLicense.NormalizedModules);
            }

            ClockValidationResult clockUpdate = SaveClockState(machineCode, nowUtc, clockValidation.LastSeenUtc);
            if (!clockUpdate.IsValid)
            {
                return LicenseValidationResult.Create(
                    LicenseValidationStatus.ClockRollbackDetected,
                    clockUpdate.Message,
                    machineCode,
                    trialLicense,
                    trialLicense.NormalizedModules);
            }

            int remainingDays = Math.Max(0, (int)Math.Ceiling((expireTime - nowUtc).TotalDays));
            return LicenseValidationResult.Create(
                LicenseValidationStatus.Valid,
                $"试用授权有效，剩余 {remainingDays} 天",
                machineCode,
                trialLicense,
                trialLicense.NormalizedModules);
        }

        /// <summary>读取或创建试用起始时间。</summary>
        private DateTime LoadOrCreateTrialStartTimeUtc()
        {
            EnsureDirectory();
            string markerFilePath = Path.Combine(LicenseDirectory, TrialMarkerFileName);
            if (File.Exists(markerFilePath))
            {
                string raw = File.ReadAllText(markerFilePath, Encoding.UTF8).Trim();
                if (DateTime.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out DateTime parsed))
                {
                    return parsed.ToUniversalTime();
                }
            }

            DateTime now = DateTime.UtcNow;
            File.WriteAllText(markerFilePath, now.ToString("O", CultureInfo.InvariantCulture), Encoding.UTF8);
            return now;
        }

        /// <summary>校验本地时钟状态，防止回拨绕过过期限制。</summary>
        private ClockValidationResult ValidateClockState(DateTime nowUtc)
        {
            try
            {
                ClockState? state = LoadClockState();
                if (state == null)
                {
                    return ClockValidationResult.Valid(null);
                }

                if (state.LastSeenUtc == default)
                {
                    return ClockValidationResult.Invalid("License 时钟状态无效，请联系管理员重新激活。");
                }

                DateTime lastSeenUtc = EnsureUtc(state.LastSeenUtc);
                if (nowUtc.Add(ClockRollbackTolerance) < lastSeenUtc)
                {
                    return ClockValidationResult.Invalid(
                        $"检测到系统时间异常，当前时间早于上次授权校验时间（{lastSeenUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}）。请恢复系统时间后重试。");
                }

                return ClockValidationResult.Valid(lastSeenUtc);
            }
            catch (Exception ex) when (ex is FormatException or JsonException or CryptographicException or IOException or UnauthorizedAccessException)
            {
                Logs.LogError($"License 时钟状态校验失败: {ex}");
                return ClockValidationResult.Invalid("License 时钟状态异常，请恢复系统时间或联系管理员重新激活。");
            }
        }

        /// <summary>保存本地时钟状态。</summary>
        private ClockValidationResult SaveClockState(string machineCode, DateTime nowUtc, DateTime? previousLastSeenUtc)
        {
            try
            {
                EnsureDirectory();
                DateTime lastSeenUtc = previousLastSeenUtc.HasValue && EnsureUtc(previousLastSeenUtc.Value) > nowUtc
                    ? EnsureUtc(previousLastSeenUtc.Value)
                    : nowUtc;

                var state = new ClockState
                {
                    MachineCode = Normalize(machineCode),
                    LastSeenUtc = EnsureUtc(lastSeenUtc)
                };

                string json = JsonSerializer.Serialize(state, SerializerOptions);
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] protectedPayload = ProtectedData.Protect(payload, ClockStateEntropy, DataProtectionScope.LocalMachine);
                File.WriteAllText(ClockStateFilePath, Convert.ToBase64String(protectedPayload), Encoding.UTF8);

                return ClockValidationResult.Valid(lastSeenUtc);
            }
            catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
            {
                Logs.LogError($"License 时钟状态保存失败: {ex}");
                return ClockValidationResult.Invalid("License 时钟状态无法更新，请检查授权目录写入权限后重试。");
            }
        }

        /// <summary>读取并解密本地时钟状态。</summary>
        private ClockState? LoadClockState()
        {
            if (!File.Exists(ClockStateFilePath))
            {
                return null;
            }

            string raw = File.ReadAllText(ClockStateFilePath, Encoding.UTF8).Trim();
            byte[] protectedPayload = Convert.FromBase64String(raw);
            byte[] payload = ProtectedData.Unprotect(protectedPayload, ClockStateEntropy, DataProtectionScope.LocalMachine);
            return JsonSerializer.Deserialize<ClockState>(Encoding.UTF8.GetString(payload), SerializerOptions)
                ?? throw new JsonException("License 时钟状态为空");
        }

        /// <summary>确保时间为 UTC。</summary>
        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        /// <summary>生成参与签名校验的规范化文本。</summary>
        public static string BuildCanonicalPayload(LicenseDocument license)
        {
            string expire = license.ExpireTime.HasValue
                ? license.ExpireTime.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : "PERMANENT";

            string modules = string.Join(",", license.NormalizedModules);

            return string.Join(
                "\n",
                $"MachineCode={Normalize(license.MachineCode)}",
                $"ExpireTime={expire}",
                $"CustomerName={(license.CustomerName ?? string.Empty).Trim()}",
                $"Modules={modules}",
                $"Version={(license.Version ?? string.Empty).Trim()}",
                $"IsTrial={license.IsTrial}");
        }

        /// <summary>使用内置公钥校验授权签名。</summary>
        private static bool VerifySignature(string canonicalPayload, string signatureBase64)
        {
            try
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using var rsa = new RSACryptoServiceProvider();
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(PublicKeyXml);
                return rsa.VerifyData(payloadBytes, SHA256.Create(), signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>去除空白和连接符，统一授权比较格式。</summary>
        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            foreach (char c in value.Trim().ToUpperInvariant())
            {
                if (!char.IsWhiteSpace(c) && c != '-')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>本地保存的时钟状态。</summary>
        private sealed class ClockState
        {
            /// <summary>保存时对应的机器码。</summary>
            public string MachineCode { get; set; } = string.Empty;

            /// <summary>最近一次通过校验的 UTC 时间。</summary>
            public DateTime LastSeenUtc { get; set; }
        }

        /// <summary>时钟状态校验结果。</summary>
        private sealed class ClockValidationResult
        {
            /// <summary>创建时钟状态结果。</summary>
            private ClockValidationResult(bool isValid, DateTime? lastSeenUtc, string message)
            {
                IsValid = isValid;
                LastSeenUtc = lastSeenUtc;
                Message = message;
            }

            /// <summary>校验是否通过。</summary>
            public bool IsValid { get; }

            /// <summary>最近一次有效校验时间。</summary>
            public DateTime? LastSeenUtc { get; }

            /// <summary>失败时的提示信息。</summary>
            public string Message { get; }

            /// <summary>创建成功结果。</summary>
            public static ClockValidationResult Valid(DateTime? lastSeenUtc)
            {
                return new ClockValidationResult(true, lastSeenUtc, string.Empty);
            }

            /// <summary>创建失败结果。</summary>
            public static ClockValidationResult Invalid(string message)
            {
                return new ClockValidationResult(false, null, message);
            }
        }
    }
}
