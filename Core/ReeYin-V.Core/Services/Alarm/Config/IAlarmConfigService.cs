#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Config
{
    public interface IAlarmConfigService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<AlarmConfigSnapshot> LoadAsync(string keyword = "", CancellationToken cancellationToken = default);

        Task SaveDefinitionAsync(AlarmDefinition definition, string operatorName, CancellationToken cancellationToken = default);

        Task SetDefinitionEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default);

        Task SaveTriggerRuleAsync(AlarmTriggerRule rule, string operatorName, CancellationToken cancellationToken = default);

        Task SetTriggerRuleEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default);
    }
}
