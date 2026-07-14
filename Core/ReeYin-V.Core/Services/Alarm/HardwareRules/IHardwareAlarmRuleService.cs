#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public interface IHardwareAlarmRuleService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HardwareAlarmRuleInfo>> GetRulesAsync(HardwareAlarmRuleQuery query, CancellationToken cancellationToken = default);
        Task<HardwareAlarmRuleInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
        Task SaveAsync(HardwareAlarmRuleInfo rule, string operatorName, CancellationToken cancellationToken = default);
        Task SetEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default);
        IReadOnlyList<HardwareAlarmRuleInfo> GetEnabledRulesSnapshot();
    }
}
