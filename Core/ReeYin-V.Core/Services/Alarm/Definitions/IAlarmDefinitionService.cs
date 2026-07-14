#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public interface IAlarmDefinitionService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmDefinitionInfo>> GetDefinitionsAsync(AlarmDefinitionQuery query, CancellationToken cancellationToken = default);

        Task<AlarmDefinitionInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

        Task SaveAsync(AlarmDefinitionInfo definition, string operatorName, CancellationToken cancellationToken = default);

        Task SetEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default);

        AlarmRaiseRequest BuildRaiseRequest(AlarmReportRequest request);
    }
}

