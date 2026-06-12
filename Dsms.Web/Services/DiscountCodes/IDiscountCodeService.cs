using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.DiscountCodes;

public interface IDiscountCodeService
{
    Task<IReadOnlyList<DiscountCodeListItemDto>> GetListAsync(
        string? search = null,
        bool? activeFilter = null,
        DiscountCodeType? typeFilter = null);

    Task<DiscountCodeDetailDto?> GetByIdAsync(Guid id);

    Task<DiscountCodeEditDto> GetForEditAsync(Guid? id);

    Task<SaveDiscountCodeResult> SaveAsync(DiscountCodeEditDto dto);

    Task<bool> ActivateAsync(Guid id);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);

    Task<IReadOnlyList<DiscountCodePlanOptionDto>> GetPlanOptionsAsync();
}
