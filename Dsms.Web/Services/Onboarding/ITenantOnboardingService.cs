using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Onboarding;

public interface ITenantOnboardingService
{
    Task EnsureDefaultTasksForTenantAsync(int tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantOnboardingTask>> GetTasksForCurrentTenantAsync(CancellationToken ct = default);

    Task<TenantOnboardingTask?> ToggleTaskCompletedAsync(int taskId, bool isCompleted, CancellationToken ct = default);
}
