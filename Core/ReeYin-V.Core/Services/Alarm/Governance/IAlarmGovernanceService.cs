#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    public interface IAlarmGovernanceService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmSuppressionRuleInfo>> GetSuppressionRulesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default);

        Task SaveSuppressionRuleAsync(AlarmSuppressionRuleInfo rule, string operatorName, CancellationToken cancellationToken = default);

        Task SetSuppressionRuleEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmShelveInfo>> GetShelvesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default);

        Task SaveShelveAsync(AlarmShelveInfo shelf, string operatorName, CancellationToken cancellationToken = default);

        Task ReleaseShelveAsync(string id, string operatorName, string? note = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmNotificationRouteInfo>> GetNotificationRoutesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default);

        Task SaveNotificationRouteAsync(AlarmNotificationRouteInfo route, string operatorName, CancellationToken cancellationToken = default);

        Task SetNotificationRouteEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmEventAuditInfo>> GetAuditsAsync(AlarmAuditQuery query, CancellationToken cancellationToken = default);

        Task AppendAuditAsync(AlarmEventAuditInfo audit, CancellationToken cancellationToken = default);

        bool TryMatchSuppression(AlarmRaiseRequest request, DateTime now, out AlarmSuppressionRuleInfo? rule);

        bool TryMatchShelve(AlarmRaiseRequest request, DateTime now, out AlarmShelveInfo? shelf);

        IReadOnlyList<AlarmNotificationRouteInfo> ResolveNotificationRoutes(AlarmRaiseRequest request, DateTime now);
    }
}
