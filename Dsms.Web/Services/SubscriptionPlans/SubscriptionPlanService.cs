using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.SubscriptionPlans;

public sealed class SubscriptionPlanService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ILogService logService) : ISubscriptionPlanService
{
    public async Task<IReadOnlyList<SubscriptionPlanListDto>> GetAllPlansAsync(
        string? search = null,
        bool? activeFilter = null,
        bool sortDescending = false)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.SubscriptionPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term)
                || p.DisplayName.Contains(term));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(p => p.IsActive == activeFilter.Value);
        }

        query = sortDescending
            ? query.OrderByDescending(p => p.SortOrder).ThenByDescending(p => p.DisplayName)
            : query.OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayName);

        return await query
            .Select(p => new SubscriptionPlanListDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                IsActive = p.IsActive,
                IsFree = p.IsFree,
                SortOrder = p.SortOrder,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                Currency = p.Currency,
                MaxTenants = p.MaxTenants,
                MaxAdmins = p.MaxAdmins,
                MaxUsersPerTenant = p.MaxUsersPerTenant
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SubscriptionPlanOptionDto>> GetActivePlansAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => MapToOption(p))
            .ToListAsync();
    }

    public async Task<SubscriptionPlanDetailsDto?> GetPlanByIdAsync(Guid id)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var plan = await db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return plan is null ? null : MapToDetails(plan);
    }

    public async Task<SubscriptionPlanDetailsDto?> GetPlanByNameAsync(string name)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var normalized = NormalizeName(name);
        var plan = await db.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == normalized);
        return plan is null ? null : MapToDetails(plan);
    }

    public async Task<Guid> CreatePlanAsync(SubscriptionPlanEditDto dto)
    {
        await EnsureSuperuserAsync();
        ValidateDto(dto);

        await using var db = await dbFactory.CreateDbContextAsync();

        var name = NormalizeName(dto.Name);
        if (await db.SubscriptionPlans.AnyAsync(p => p.Name == name))
        {
            throw new InvalidOperationException($"Ein Tarif mit dem Namen „{name}“ existiert bereits.");
        }

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = dto.DisplayName.Trim(),
            Description = NormalizeOptional(dto.Description),
            IsActive = dto.IsActive,
            IsFree = dto.IsFree,
            SortOrder = dto.SortOrder,
            PriceMonthly = dto.PriceMonthly,
            PriceYearly = dto.PriceYearly,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            ExternalProductId = NormalizeOptional(dto.ExternalProductId),
            ExternalMonthlyPriceId = NormalizeOptional(dto.ExternalMonthlyPriceId),
            ExternalYearlyPriceId = NormalizeOptional(dto.ExternalYearlyPriceId),
            InternalNote = NormalizeOptional(dto.InternalNote),
            CreatedAt = DateTime.UtcNow,
            MaxTenants = dto.MaxTenants,
            MaxAdmins = dto.MaxAdmins,
            MaxUsersPerTenant = dto.MaxUsersPerTenant,
            MaxAuditorsPerTenant = dto.MaxAuditorsPerTenant,
            MaxCustomAuditTemplatesPerTenant = dto.MaxCustomAuditTemplatesPerTenant,
            MaxActiveAuditsPerTenant = dto.MaxActiveAuditsPerTenant,
            MaxProcessingActivitiesPerTenant = dto.MaxProcessingActivitiesPerTenant,
            MaxDpiaPerTenant = dto.MaxDpiaPerTenant,
            MaxTomsPerTenant = dto.MaxTomsPerTenant,
            MaxProcessorsPerTenant = dto.MaxProcessorsPerTenant,
            MaxActiveMeasuresPerTenant = dto.MaxActiveMeasuresPerTenant,
            MaxStorageMb = dto.MaxStorageMb,
            MaxEmailRemindersPerMonth = dto.MaxEmailRemindersPerMonth
        };

        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "SubscriptionPlanCreated",
            description: "Tarif wurde erstellt.",
            plan,
            newValues: MapPlanSnapshot(plan));

        return plan.Id;
    }

    public async Task<bool> UpdatePlanAsync(Guid id, SubscriptionPlanEditDto dto)
    {
        await EnsureSuperuserAsync();
        ValidateDto(dto);

        await using var db = await dbFactory.CreateDbContextAsync();

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return false;
        }

        var name = NormalizeName(dto.Name);
        if (await db.SubscriptionPlans.AnyAsync(p => p.Id != id && p.Name == name))
        {
            throw new InvalidOperationException($"Ein Tarif mit dem Namen „{name}“ existiert bereits.");
        }

        var oldSnapshot = MapPlanSnapshot(plan);
        var oldIsActive = plan.IsActive;

        plan.Name = name;
        plan.DisplayName = dto.DisplayName.Trim();
        plan.Description = NormalizeOptional(dto.Description);
        plan.IsActive = dto.IsActive;
        plan.IsFree = dto.IsFree;
        plan.SortOrder = dto.SortOrder;
        plan.PriceMonthly = dto.PriceMonthly;
        plan.PriceYearly = dto.PriceYearly;
        plan.Currency = dto.Currency.Trim().ToUpperInvariant();
        plan.ExternalProductId = NormalizeOptional(dto.ExternalProductId);
        plan.ExternalMonthlyPriceId = NormalizeOptional(dto.ExternalMonthlyPriceId);
        plan.ExternalYearlyPriceId = NormalizeOptional(dto.ExternalYearlyPriceId);
        plan.InternalNote = NormalizeOptional(dto.InternalNote);
        plan.UpdatedAt = DateTime.UtcNow;
        plan.MaxTenants = dto.MaxTenants;
        plan.MaxAdmins = dto.MaxAdmins;
        plan.MaxUsersPerTenant = dto.MaxUsersPerTenant;
        plan.MaxAuditorsPerTenant = dto.MaxAuditorsPerTenant;
        plan.MaxCustomAuditTemplatesPerTenant = dto.MaxCustomAuditTemplatesPerTenant;
        plan.MaxActiveAuditsPerTenant = dto.MaxActiveAuditsPerTenant;
        plan.MaxProcessingActivitiesPerTenant = dto.MaxProcessingActivitiesPerTenant;
        plan.MaxDpiaPerTenant = dto.MaxDpiaPerTenant;
        plan.MaxTomsPerTenant = dto.MaxTomsPerTenant;
        plan.MaxProcessorsPerTenant = dto.MaxProcessorsPerTenant;
        plan.MaxActiveMeasuresPerTenant = dto.MaxActiveMeasuresPerTenant;
        plan.MaxStorageMb = dto.MaxStorageMb;
        plan.MaxEmailRemindersPerMonth = dto.MaxEmailRemindersPerMonth;

        await db.SaveChangesAsync();

        var newSnapshot = MapPlanSnapshot(plan);
        await TryLogAuditAsync(
            action: "SubscriptionPlanUpdated",
            description: "Tarif wurde geändert.",
            plan,
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        if (oldIsActive != plan.IsActive)
        {
            await TryLogAuditAsync(
                action: plan.IsActive ? "SubscriptionPlanActivated" : "SubscriptionPlanDeactivated",
                description: plan.IsActive ? "Tarif wurde aktiviert." : "Tarif wurde deaktiviert.",
                plan,
                oldValues: new { IsActive = oldIsActive },
                newValues: new { IsActive = plan.IsActive });
        }

        return true;
    }

    public async Task<IReadOnlyList<SubscriptionPlanOptionDto>> GetPlanOptionsAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => MapToOption(p))
            .ToListAsync();
    }

    private static void ValidateDto(SubscriptionPlanEditDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Name ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            throw new InvalidOperationException("Anzeigename ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            throw new InvalidOperationException("Währung ist erforderlich.");
        }

        if (dto.SortOrder < 0)
        {
            throw new InvalidOperationException("Sortierung muss 0 oder größer sein.");
        }

        if (dto.PriceMonthly is < 0)
        {
            throw new InvalidOperationException("Preis monatlich darf nicht negativ sein.");
        }

        if (dto.PriceYearly is < 0)
        {
            throw new InvalidOperationException("Preis jährlich darf nicht negativ sein.");
        }

        ValidateLimit(dto.MaxTenants, "Max. Mandanten");
        ValidateLimit(dto.MaxAdmins, "Max. Admins");
        ValidateLimit(dto.MaxUsersPerTenant, "Max. Benutzer pro Mandant");
        ValidateLimit(dto.MaxAuditorsPerTenant, "Max. Auditoren pro Mandant");
        ValidateLimit(dto.MaxCustomAuditTemplatesPerTenant, "Max. eigene Auditvorlagen pro Mandant");
        ValidateLimit(dto.MaxActiveAuditsPerTenant, "Max. laufende Audits pro Mandant");
        ValidateLimit(dto.MaxProcessingActivitiesPerTenant, "Max. Verarbeitungstätigkeiten pro Mandant");
        ValidateLimit(dto.MaxDpiaPerTenant, "Max. DSFA pro Mandant");
        ValidateLimit(dto.MaxTomsPerTenant, "Max. TOMs pro Mandant");
        ValidateLimit(dto.MaxProcessorsPerTenant, "Max. Dienstleister pro Mandant");
        ValidateLimit(dto.MaxActiveMeasuresPerTenant, "Max. laufende Maßnahmen pro Mandant");
        ValidateLimit(dto.MaxStorageMb, "Max. Speicher");
        ValidateLimit(dto.MaxEmailRemindersPerMonth, "Max. E-Mail-Erinnerungen pro Monat");
    }

    private static void ValidateLimit(int? value, string fieldName)
    {
        if (value is < 0)
        {
            throw new InvalidOperationException($"{fieldName} darf nicht negativ sein.");
        }
    }

    private static string NormalizeName(string name) =>
        name.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SubscriptionPlanDetailsDto MapToDetails(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        DisplayName = plan.DisplayName,
        Description = plan.Description,
        IsActive = plan.IsActive,
        IsFree = plan.IsFree,
        SortOrder = plan.SortOrder,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt,
        PriceMonthly = plan.PriceMonthly,
        PriceYearly = plan.PriceYearly,
        Currency = plan.Currency,
        ExternalProductId = plan.ExternalProductId,
        ExternalMonthlyPriceId = plan.ExternalMonthlyPriceId,
        ExternalYearlyPriceId = plan.ExternalYearlyPriceId,
        InternalNote = plan.InternalNote,
        MaxTenants = plan.MaxTenants,
        MaxAdmins = plan.MaxAdmins,
        MaxUsersPerTenant = plan.MaxUsersPerTenant,
        MaxAuditorsPerTenant = plan.MaxAuditorsPerTenant,
        MaxCustomAuditTemplatesPerTenant = plan.MaxCustomAuditTemplatesPerTenant,
        MaxActiveAuditsPerTenant = plan.MaxActiveAuditsPerTenant,
        MaxProcessingActivitiesPerTenant = plan.MaxProcessingActivitiesPerTenant,
        MaxDpiaPerTenant = plan.MaxDpiaPerTenant,
        MaxTomsPerTenant = plan.MaxTomsPerTenant,
        MaxProcessorsPerTenant = plan.MaxProcessorsPerTenant,
        MaxActiveMeasuresPerTenant = plan.MaxActiveMeasuresPerTenant,
        MaxStorageMb = plan.MaxStorageMb,
        MaxEmailRemindersPerMonth = plan.MaxEmailRemindersPerMonth
    };

    private static SubscriptionPlanOptionDto MapToOption(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        DisplayName = plan.DisplayName,
        Description = plan.Description,
        PriceMonthly = plan.PriceMonthly,
        PriceYearly = plan.PriceYearly,
        Currency = plan.Currency,
        IsFree = plan.IsFree,
        SortOrder = plan.SortOrder
    };

    private static object MapPlanSnapshot(SubscriptionPlan plan) => new
    {
        plan.Name,
        plan.DisplayName,
        plan.Description,
        plan.IsActive,
        plan.IsFree,
        plan.SortOrder,
        plan.PriceMonthly,
        plan.PriceYearly,
        plan.Currency,
        plan.ExternalProductId,
        plan.ExternalMonthlyPriceId,
        plan.ExternalYearlyPriceId,
        plan.MaxTenants,
        plan.MaxAdmins,
        plan.MaxUsersPerTenant,
        plan.MaxAuditorsPerTenant,
        plan.MaxCustomAuditTemplatesPerTenant,
        plan.MaxActiveAuditsPerTenant,
        plan.MaxProcessingActivitiesPerTenant,
        plan.MaxDpiaPerTenant,
        plan.MaxTomsPerTenant,
        plan.MaxProcessorsPerTenant,
        plan.MaxActiveMeasuresPerTenant,
        plan.MaxStorageMb,
        plan.MaxEmailRemindersPerMonth
    };

    private async Task TryLogAuditAsync(
        string action,
        string description,
        SubscriptionPlan plan,
        object? oldValues = null,
        object? newValues = null)
    {
        try
        {
            await logService.LogAuditAsync(
                action: action,
                description: description,
                entityType: "SubscriptionPlan",
                entityId: plan.Id.ToString(),
                entityName: plan.DisplayName,
                oldValues: oldValues,
                newValues: newValues,
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für die Tarifverwaltung.");
        }
    }
}
