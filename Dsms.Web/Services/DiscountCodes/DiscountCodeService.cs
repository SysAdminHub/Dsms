using System.Text.RegularExpressions;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.DiscountCodes;

public sealed partial class DiscountCodeService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ILogService logService) : IDiscountCodeService
{
    private const int MaxFreeMonths = 36;

    public async Task<IReadOnlyList<DiscountCodeListItemDto>> GetListAsync(
        string? search = null,
        bool? activeFilter = null,
        DiscountCodeType? typeFilter = null)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.DiscountCodes
            .AsNoTracking()
            .Include(d => d.AppliesToPlan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Code.Contains(term)
                || d.Name.Contains(term));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(d => d.IsActive == activeFilter.Value);
        }

        if (typeFilter.HasValue)
        {
            query = query.Where(d => d.DiscountType == typeFilter.Value);
        }

        var items = await query
            .OrderBy(d => d.Code)
            .ToListAsync();

        return items.Select(MapToListItem).ToList();
    }

    public async Task<DiscountCodeDetailDto?> GetByIdAsync(Guid id)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.DiscountCodes
            .AsNoTracking()
            .Include(d => d.AppliesToPlan)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity is null ? null : await MapToDetailAsync(entity);
    }

    public async Task<DiscountCodeEditDto> GetForEditAsync(Guid? id)
    {
        await EnsureSuperuserAsync();

        if (!id.HasValue)
        {
            return new DiscountCodeEditDto
            {
                IsActive = true,
                DiscountType = DiscountCodeType.Percentage
            };
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entity = await db.DiscountCodes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id.Value);
        if (entity is null)
        {
            throw new InvalidOperationException("Rabattcode wurde nicht gefunden.");
        }

        return MapToEdit(entity);
    }

    public async Task<SaveDiscountCodeResult> SaveAsync(DiscountCodeEditDto dto)
    {
        await EnsureSuperuserAsync();

        var validationError = ValidateDto(dto);
        if (validationError is not null)
        {
            return SaveDiscountCodeResult.Failed(validationError);
        }

        var normalizedCode = NormalizeCode(dto.Code);
        await using var db = await dbFactory.CreateDbContextAsync();

        if (await db.DiscountCodes.AnyAsync(d =>
                d.Code == normalizedCode && d.Id != dto.Id))
        {
            return SaveDiscountCodeResult.Failed($"Ein Rabattcode mit dem Code „{normalizedCode}“ existiert bereits.");
        }

        if (dto.AppliesToPlanId.HasValue
            && !await db.SubscriptionPlans.AnyAsync(p => p.Id == dto.AppliesToPlanId.Value))
        {
            return SaveDiscountCodeResult.Failed("Der ausgewählte Plan existiert nicht.");
        }

        var userId = await currentUser.GetUserIdAsync();
        DiscountCode entity;

        if (dto.Id is Guid existingId)
        {
            entity = await db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == existingId)
                ?? throw new InvalidOperationException("Rabattcode wurde nicht gefunden.");

            if (dto.MaxRedemptions.HasValue && entity.CurrentRedemptions > dto.MaxRedemptions.Value)
            {
                return SaveDiscountCodeResult.Failed(
                    "Bisherige Nutzungen dürfen das maximale Nutzungslimit nicht überschreiten.");
            }

            var oldIsActive = entity.IsActive;
            var oldSnapshot = MapSnapshot(entity);
            ApplyDtoToEntity(entity, dto, normalizedCode);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = userId;

            await db.SaveChangesAsync();

            await TryLogAuditAsync(
                action: "DiscountCodeUpdated",
                description: "Rabattcode wurde geändert.",
                entity,
                oldValues: oldSnapshot,
                newValues: MapSnapshot(entity));

            if (oldIsActive != entity.IsActive)
            {
                await TryLogAuditAsync(
                    action: entity.IsActive ? "DiscountCodeActivated" : "DiscountCodeDeactivated",
                    description: entity.IsActive ? "Rabattcode wurde aktiviert." : "Rabattcode wurde deaktiviert.",
                    entity,
                    oldValues: new { IsActive = oldIsActive },
                    newValues: new { IsActive = entity.IsActive });
            }

            return SaveDiscountCodeResult.Succeeded(entity.Id);
        }

        entity = new DiscountCode
        {
            Id = Guid.NewGuid(),
            CurrentRedemptions = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        ApplyDtoToEntity(entity, dto, normalizedCode);
        db.DiscountCodes.Add(entity);
        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "DiscountCodeCreated",
            description: "Rabattcode wurde erstellt.",
            entity,
            newValues: MapSnapshot(entity));

        return SaveDiscountCodeResult.Succeeded(entity.Id);
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        await EnsureSuperuserAsync();
        return await SetActiveStateAsync(id, isActive: true, "DiscountCodeActivated", "Rabattcode wurde aktiviert.");
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        await EnsureSuperuserAsync();
        return await SetActiveStateAsync(id, isActive: false, "DiscountCodeDeactivated", "Rabattcode wurde deaktiviert.");
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
    {
        await EnsureSuperuserAsync();
        var normalized = NormalizeCode(code);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.DiscountCodes.AnyAsync(d =>
            d.Code == normalized && (!excludeId.HasValue || d.Id != excludeId.Value));
    }

    public async Task<IReadOnlyList<DiscountCodePlanOptionDto>> GetPlanOptionsAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => new DiscountCodePlanOptionDto
            {
                Id = p.Id,
                DisplayName = p.DisplayName
            })
            .ToListAsync();
    }

    private async Task<bool> SetActiveStateAsync(Guid id, bool isActive, string action, string description)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var entity = await db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
        {
            return false;
        }

        if (entity.IsActive == isActive)
        {
            return true;
        }

        var oldIsActive = entity.IsActive;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: action,
            description: description,
            entity,
            oldValues: new { IsActive = oldIsActive },
            newValues: new { IsActive = entity.IsActive });

        return true;
    }

    private static void ApplyDtoToEntity(DiscountCode entity, DiscountCodeEditDto dto, string normalizedCode)
    {
        entity.Code = normalizedCode;
        entity.Name = dto.Name.Trim();
        entity.Description = NormalizeOptional(dto.Description);
        entity.IsActive = dto.IsActive;
        entity.DiscountType = dto.DiscountType;
        entity.PercentageValue = dto.DiscountType == DiscountCodeType.Percentage ? dto.PercentageValue : null;
        entity.FixedAmountValue = dto.DiscountType == DiscountCodeType.FixedAmount ? dto.FixedAmountValue : null;
        entity.FreeMonths = dto.DiscountType == DiscountCodeType.FreeMonths ? dto.FreeMonths : null;
        entity.AppliesToPlanId = dto.AppliesToPlanId;
        entity.AppliesToBillingCycle = NormalizeBillingCycle(dto.AppliesToBillingCycle);
        entity.ValidFrom = NormalizeDate(dto.ValidFrom);
        entity.ValidUntil = NormalizeDate(dto.ValidUntil, endOfDay: true);
        entity.MaxRedemptions = dto.MaxRedemptions;
        entity.InternalNote = NormalizeOptional(dto.InternalNote);
    }

    private static string? ValidateDto(DiscountCodeEditDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            return "Code ist erforderlich.";
        }

        var normalizedCode = NormalizeCode(dto.Code);
        if (normalizedCode.Length > 64)
        {
            return "Code darf maximal 64 Zeichen lang sein.";
        }

        if (!CodePattern().IsMatch(normalizedCode))
        {
            return "Code darf nur Buchstaben, Ziffern, Bindestrich und Unterstrich enthalten (keine Leerzeichen).";
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name ist erforderlich.";
        }

        if (dto.Name.Trim().Length > 200)
        {
            return "Name darf maximal 200 Zeichen lang sein.";
        }

        switch (dto.DiscountType)
        {
            case DiscountCodeType.Percentage:
                if (!dto.PercentageValue.HasValue)
                {
                    return "Prozentwert ist erforderlich.";
                }

                if (dto.PercentageValue is <= 0 or > 100)
                {
                    return "Prozentwert muss größer als 0 und höchstens 100 sein.";
                }

                break;

            case DiscountCodeType.FixedAmount:
                if (!dto.FixedAmountValue.HasValue)
                {
                    return "Fester Betrag ist erforderlich.";
                }

                if (dto.FixedAmountValue is <= 0)
                {
                    return "Fester Betrag muss größer als 0 sein.";
                }

                break;

            case DiscountCodeType.FreeMonths:
                if (!dto.FreeMonths.HasValue)
                {
                    return "Anzahl kostenloser Monate ist erforderlich.";
                }

                if (dto.FreeMonths is <= 0)
                {
                    return "Anzahl kostenloser Monate muss größer als 0 sein.";
                }

                if (dto.FreeMonths > MaxFreeMonths)
                {
                    return $"Anzahl kostenloser Monate darf maximal {MaxFreeMonths} sein.";
                }

                break;

            default:
                return "Rabatt-Typ ist erforderlich.";
        }

        if (dto.ValidFrom.HasValue && dto.ValidUntil.HasValue
            && dto.ValidUntil.Value < dto.ValidFrom.Value)
        {
            return "Gültig bis darf nicht vor Gültig von liegen.";
        }

        if (dto.MaxRedemptions is <= 0)
        {
            return "Maximale Nutzungen muss größer als 0 sein, wenn gesetzt.";
        }

        if (dto.CurrentRedemptions < 0)
        {
            return "Bisherige Nutzungen dürfen nicht negativ sein.";
        }

        if (dto.MaxRedemptions.HasValue && dto.CurrentRedemptions > dto.MaxRedemptions.Value)
        {
            return "Bisherige Nutzungen dürfen das maximale Nutzungslimit nicht überschreiten.";
        }

        if (!string.IsNullOrWhiteSpace(dto.AppliesToBillingCycle)
            && !BillingCycles.IsValid(dto.AppliesToBillingCycle))
        {
            return "Ungültige Abrechnungsbindung.";
        }

        return null;
    }

    private static DiscountCodeListItemDto MapToListItem(DiscountCode entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        DiscountType = entity.DiscountType,
        DiscountDisplayText = DiscountCodeDisplayHelper.FormatDiscountValue(
            entity.DiscountType,
            entity.PercentageValue,
            entity.FixedAmountValue,
            entity.FreeMonths),
        IsActive = entity.IsActive,
        AppliesToPlanDisplayName = DiscountCodeDisplayHelper.FormatPlanBinding(entity.AppliesToPlan?.DisplayName),
        AppliesToBillingCycleDisplayText = DiscountCodeDisplayHelper.FormatBillingCycleScope(entity.AppliesToBillingCycle),
        ValidityDisplayText = DiscountCodeDisplayHelper.FormatValidity(entity.ValidFrom, entity.ValidUntil),
        UsageDisplayText = DiscountCodeDisplayHelper.FormatUsage(entity.CurrentRedemptions, entity.MaxRedemptions),
        ValidFrom = entity.ValidFrom,
        ValidUntil = entity.ValidUntil,
        MaxRedemptions = entity.MaxRedemptions,
        CurrentRedemptions = entity.CurrentRedemptions
    };

    private async Task<DiscountCodeDetailDto> MapToDetailAsync(DiscountCode entity)
    {
        var createdBy = await ResolveUserDisplayNameAsync(entity.CreatedByUserId);
        var updatedBy = await ResolveUserDisplayNameAsync(entity.UpdatedByUserId);

        return new DiscountCodeDetailDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            DiscountType = entity.DiscountType,
            DiscountTypeDisplayText = DiscountCodeLabels.GetTypeLabel(entity.DiscountType),
            DiscountDisplayText = DiscountCodeDisplayHelper.FormatDiscountValue(
                entity.DiscountType,
                entity.PercentageValue,
                entity.FixedAmountValue,
                entity.FreeMonths),
            PercentageValue = entity.PercentageValue,
            FixedAmountValue = entity.FixedAmountValue,
            FreeMonths = entity.FreeMonths,
            AppliesToPlanId = entity.AppliesToPlanId,
            AppliesToPlanDisplayName = DiscountCodeDisplayHelper.FormatPlanBinding(entity.AppliesToPlan?.DisplayName),
            AppliesToBillingCycle = entity.AppliesToBillingCycle,
            AppliesToBillingCycleDisplayText = DiscountCodeDisplayHelper.FormatBillingCycleScope(entity.AppliesToBillingCycle),
            ValidFrom = entity.ValidFrom,
            ValidUntil = entity.ValidUntil,
            ValidityDisplayText = DiscountCodeDisplayHelper.FormatValidity(entity.ValidFrom, entity.ValidUntil),
            MaxRedemptions = entity.MaxRedemptions,
            CurrentRedemptions = entity.CurrentRedemptions,
            UsageDisplayText = DiscountCodeDisplayHelper.FormatUsage(entity.CurrentRedemptions, entity.MaxRedemptions),
            InternalNote = entity.InternalNote,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedByDisplayName = createdBy,
            UpdatedByUserId = entity.UpdatedByUserId,
            UpdatedByDisplayName = updatedBy
        };
    }

    private static DiscountCodeEditDto MapToEdit(DiscountCode entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        DiscountType = entity.DiscountType,
        PercentageValue = entity.PercentageValue,
        FixedAmountValue = entity.FixedAmountValue,
        FreeMonths = entity.FreeMonths,
        AppliesToPlanId = entity.AppliesToPlanId,
        AppliesToBillingCycle = entity.AppliesToBillingCycle,
        ValidFrom = entity.ValidFrom,
        ValidUntil = entity.ValidUntil,
        MaxRedemptions = entity.MaxRedemptions,
        CurrentRedemptions = entity.CurrentRedemptions,
        InternalNote = entity.InternalNote
    };

    private static object MapSnapshot(DiscountCode entity) => new
    {
        entity.Code,
        entity.Name,
        entity.Description,
        entity.IsActive,
        entity.DiscountType,
        entity.PercentageValue,
        entity.FixedAmountValue,
        entity.FreeMonths,
        entity.AppliesToPlanId,
        entity.AppliesToBillingCycle,
        entity.ValidFrom,
        entity.ValidUntil,
        entity.MaxRedemptions,
        entity.CurrentRedemptions,
        entity.InternalNote
    };

    private async Task<string?> ResolveUserDisplayNameAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return userId;
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        return user.Email ?? userId;
    }

    private async Task TryLogAuditAsync(
        string action,
        string description,
        DiscountCode entity,
        object? oldValues = null,
        object? newValues = null)
    {
        try
        {
            await logService.LogAuditAsync(
                action: action,
                description: description,
                entityType: "DiscountCode",
                entityId: entity.Id.ToString(),
                entityName: entity.Code,
                oldValues: oldValues,
                newValues: newValues,
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeBillingCycle(string? cycle)
    {
        if (string.IsNullOrWhiteSpace(cycle))
        {
            return null;
        }

        var normalized = cycle.Trim();
        if (string.Equals(normalized, BillingCycles.Monthly, StringComparison.OrdinalIgnoreCase))
        {
            return BillingCycles.Monthly;
        }

        if (string.Equals(normalized, BillingCycles.Yearly, StringComparison.OrdinalIgnoreCase))
        {
            return BillingCycles.Yearly;
        }

        return null;
    }

    private static DateTime? NormalizeDate(DateTime? value, bool endOfDay = false)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value.Date;
        return endOfDay
            ? DateTime.SpecifyKind(date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für die Rabattcode-Verwaltung.");
        }
    }

    [GeneratedRegex("^[A-Z0-9_-]+$")]
    private static partial Regex CodePattern();
}
