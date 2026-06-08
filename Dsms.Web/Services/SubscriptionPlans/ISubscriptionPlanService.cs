namespace Dsms.Web.Services.SubscriptionPlans;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanListDto>> GetAllPlansAsync(
        string? search = null,
        bool? activeFilter = null,
        bool sortDescending = false);

    Task<IReadOnlyList<SubscriptionPlanOptionDto>> GetActivePlansAsync();

    Task<SubscriptionPlanDetailsDto?> GetPlanByIdAsync(Guid id);

    Task<SubscriptionPlanDetailsDto?> GetPlanByNameAsync(string name);

    Task<Guid> CreatePlanAsync(SubscriptionPlanEditDto dto);

    Task<bool> UpdatePlanAsync(Guid id, SubscriptionPlanEditDto dto);

    Task<IReadOnlyList<SubscriptionPlanOptionDto>> GetPlanOptionsAsync();
}
