using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.PendingSignups;

public sealed class PendingSignupService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    UserManager<ApplicationUser> userManager,
    ILogService logService) : IPendingSignupService
{
    public async Task<IReadOnlyList<PendingSignupListDto>> GetAllAsync(
        string? search = null,
        string? statusFilter = null,
        Guid? planIdFilter = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.PendingSignups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.CustomerName.Contains(term)
                || (p.CustomerEmail != null && p.CustomerEmail.Contains(term))
                || p.TenantName.Contains(term)
                || p.AdminEmail.Contains(term)
                || (p.PlanDisplayNameSnapshot != null && p.PlanDisplayNameSnapshot.Contains(term))
                || (p.ExternalPaymentId != null && p.ExternalPaymentId.Contains(term))
                || (p.ProvisionedLicenseNumber != null && p.ProvisionedLicenseNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(p => p.Status == statusFilter);
        }

        if (planIdFilter.HasValue)
        {
            query = query.Where(p => p.PlanId == planIdFilter.Value);
        }

        if (createdFrom.HasValue)
        {
            var from = createdFrom.Value.Date;
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (createdTo.HasValue)
        {
            var to = createdTo.Value.Date.AddDays(1);
            query = query.Where(p => p.CreatedAt < to);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToList(p))
            .ToListAsync();
    }

    public async Task<PendingSignupDetailsDto?> GetByIdAsync(Guid id)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return entity is null ? null : MapToDetails(entity);
    }

    public async Task<Guid> CreateAsync(CreatePendingSignupDto dto)
    {
        await EnsureSuperuserAsync();
        return await CreateInternalAsync(dto, requirePaidPlan: false);
    }

    public Task<Guid> CreatePublicAsync(CreatePendingSignupDto dto) =>
        CreateInternalAsync(dto, requirePaidPlan: true);

    private async Task<Guid> CreateInternalAsync(CreatePendingSignupDto dto, bool requirePaidPlan)
    {
        ValidateCreateDto(dto);

        await using var db = await dbFactory.CreateDbContextAsync();

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == dto.PlanId);
        if (plan is null)
        {
            throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");
        }

        if (requirePaidPlan)
        {
            ValidatePaidPlan(plan);
        }

        var adminEmail = dto.AdminEmail.Trim();

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            throw new InvalidOperationException("Für diese E-Mail-Adresse existiert bereits ein Benutzer.");
        }

        if (await HasOpenPendingSignupAsync(db, adminEmail))
        {
            throw new InvalidOperationException("Für diese E-Mail-Adresse existiert bereits eine offene Registrierung.");
        }

        var customerEmail = NormalizeOptional(dto.CustomerEmail);
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            customerEmail = adminEmail;
        }

        var entity = new PendingSignup
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            PlanId = plan.Id,
            PlanNameSnapshot = plan.Name,
            PlanDisplayNameSnapshot = plan.DisplayName,
            PlanPriceMonthlySnapshot = plan.PriceMonthly,
            PlanPriceYearlySnapshot = plan.PriceYearly,
            CurrencySnapshot = plan.Currency,
            CustomerName = dto.CustomerName.Trim(),
            CustomerEmail = customerEmail,
            TenantName = dto.TenantName.Trim(),
            TenantLegalName = NormalizeOptional(dto.TenantLegalName),
            TenantEmail = NormalizeOptional(dto.TenantEmail),
            TenantPhone = NormalizeOptional(dto.TenantPhone),
            TenantAddress = NormalizeOptional(dto.TenantAddress),
            AdminEmail = adminEmail,
            AdminDisplayName = dto.AdminDisplayName.Trim(),
            AdminFirstName = NormalizeOptional(dto.AdminFirstName),
            AdminLastName = NormalizeOptional(dto.AdminLastName),
            Status = PendingSignupStatuses.Draft,
            Amount = plan.PriceMonthly ?? plan.PriceYearly,
            Currency = plan.Currency,
            Source = NormalizeOptional(dto.Source) ?? "Manual",
            InternalNote = NormalizeOptional(dto.InternalNote)
        };

        db.PendingSignups.Add(entity);
        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupCreated",
            description: "Registrierung wurde vorgemerkt.",
            entity,
            metadata: new { entity.PlanId, entity.PlanDisplayNameSnapshot, entity.CustomerName, entity.AdminEmail, entity.Source });

        return entity.Id;
    }

    private static async Task<bool> HasOpenPendingSignupAsync(ApplicationDbContext db, string adminEmail)
    {
        var normalized = adminEmail.Trim();
        return await db.PendingSignups.AnyAsync(p =>
            p.AdminEmail == normalized && PendingSignupStatuses.Open.Contains(p.Status));
    }

    private static void ValidatePaidPlan(SubscriptionPlan plan)
    {
        if (!plan.IsActive)
        {
            throw new InvalidOperationException("Der ausgewählte Tarif ist nicht aktiv.");
        }

        if (plan.IsFree)
        {
            throw new InvalidOperationException("Der ausgewählte Tarif ist kein bezahlter Tarif.");
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, string? note = null)
    {
        await EnsureSuperuserAsync();

        if (!PendingSignupStatuses.IsValid(status))
        {
            throw new InvalidOperationException("Der Status ist ungültig.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        var oldStatus = entity.Status;
        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        ApplyStatusTimestamps(entity, status);
        AppendInternalNote(entity, note);

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupStatusChanged",
            description: "Status der Registrierung wurde geändert.",
            entity,
            metadata: new
            {
                OldStatus = oldStatus,
                NewStatus = status,
                entity.PlanId,
                entity.PlanDisplayNameSnapshot,
                entity.CustomerName,
                entity.AdminEmail
            });

        return true;
    }

    public async Task<bool> MarkAsPaidAsync(Guid id, string? externalPaymentId = null)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Status = PendingSignupStatuses.Paid;
        entity.PaidAt = DateTime.UtcNow;
        entity.ExternalPaymentId = NormalizeOptional(externalPaymentId) ?? entity.ExternalPaymentId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.ErrorMessage = null;

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupStatusChanged",
            description: "Status der Registrierung wurde geändert.",
            entity,
            metadata: new { NewStatus = PendingSignupStatuses.Paid, entity.ExternalPaymentId });

        return true;
    }

    public async Task<bool> MarkAsProvisionedAsync(
        Guid id,
        Guid licenseId,
        string? licenseNumber,
        int tenantId,
        string? tenantName,
        string adminUserId)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Status = PendingSignupStatuses.Provisioned;
        entity.ProvisionedAt = DateTime.UtcNow;
        entity.ProvisionedLicenseId = licenseId;
        entity.ProvisionedLicenseNumber = licenseNumber;
        entity.ProvisionedTenantId = tenantId;
        entity.ProvisionedTenantName = tenantName;
        entity.ProvisionedAdminUserId = adminUserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.ErrorMessage = null;

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupStatusChanged",
            description: "Status der Registrierung wurde geändert.",
            entity,
            metadata: new
            {
                NewStatus = PendingSignupStatuses.Provisioned,
                licenseId,
                licenseNumber,
                tenantId,
                adminUserId
            });

        return true;
    }

    public async Task<bool> MarkAsFailedAsync(Guid id, string errorMessage)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Status = PendingSignupStatuses.Failed;
        entity.FailedAt = DateTime.UtcNow;
        entity.ErrorMessage = errorMessage.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupMarkedFailed",
            description: "Registrierung wurde als fehlgeschlagen markiert.",
            entity,
            metadata: new { entity.ErrorMessage },
            severityWarning: true);

        return true;
    }

    public async Task<bool> MarkAsCancelledAsync(Guid id, string? note = null)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Status = PendingSignupStatuses.Cancelled;
        entity.CancelledAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        AppendInternalNote(entity, note);

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupCancelled",
            description: "Registrierung wurde abgebrochen.",
            entity);

        return true;
    }

    public async Task<bool> MarkAsExpiredAsync(Guid id, string? note = null)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Status = PendingSignupStatuses.Expired;
        entity.UpdatedAt = DateTime.UtcNow;
        AppendInternalNote(entity, note);

        await db.SaveChangesAsync();

        await TryLogAuditAsync(
            action: "PendingSignupExpired",
            description: "Registrierung wurde als abgelaufen markiert.",
            entity);

        return true;
    }

    public async Task<bool> UpdateInternalNoteAsync(Guid id, string? internalNote)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.InternalNote = NormalizeOptional(internalNote);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<PendingSignupListDto>> GetPendingPaymentAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.PendingSignups
            .AsNoTracking()
            .Where(p => p.Status == PendingSignupStatuses.PendingPayment)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToList(p))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PendingSignupListDto>> GetExpiredAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.PendingSignups
            .AsNoTracking()
            .Where(p => p.Status == PendingSignupStatuses.Expired)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToList(p))
            .ToListAsync();
    }

    public async Task<PendingSignupDetailsDto?> GetByExternalPaymentIdAsync(string externalPaymentId)
    {
        await EnsureSuperuserAsync();
        if (string.IsNullOrWhiteSpace(externalPaymentId))
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entity = await db.PendingSignups
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId.Trim());

        return entity is null ? null : MapToDetails(entity);
    }

    private static void ValidateCreateDto(CreatePendingSignupDto dto)
    {
        if (dto.PlanId == Guid.Empty)
        {
            throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            throw new InvalidOperationException("Bitte geben Sie den Unternehmensnamen ein.");
        }

        if (string.IsNullOrWhiteSpace(dto.TenantName))
        {
            throw new InvalidOperationException("Bitte geben Sie den Namen des ersten Mandanten ein.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminDisplayName))
        {
            throw new InvalidOperationException("Bitte geben Sie den Namen des Administrators ein.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminEmail))
        {
            throw new InvalidOperationException("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
        }

        if (!PlanToLicenseValidator.IsValidEmail(dto.AdminEmail))
        {
            throw new InvalidOperationException("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
        }

        if (!string.IsNullOrWhiteSpace(dto.CustomerEmail)
            && !PlanToLicenseValidator.IsValidEmail(dto.CustomerEmail))
        {
            throw new InvalidOperationException("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
        }
    }

    private static void ApplyStatusTimestamps(PendingSignup entity, string status)
    {
        switch (status)
        {
            case PendingSignupStatuses.Paid:
                entity.PaidAt ??= DateTime.UtcNow;
                break;
            case PendingSignupStatuses.Provisioned:
                entity.ProvisionedAt ??= DateTime.UtcNow;
                break;
            case PendingSignupStatuses.Cancelled:
                entity.CancelledAt ??= DateTime.UtcNow;
                break;
            case PendingSignupStatuses.Failed:
                entity.FailedAt ??= DateTime.UtcNow;
                break;
        }
    }

    private static void AppendInternalNote(PendingSignup entity, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        entity.InternalNote = string.IsNullOrWhiteSpace(entity.InternalNote)
            ? note.Trim()
            : $"{entity.InternalNote}\n{note.Trim()}";
    }

    private static PendingSignupListDto MapToList(PendingSignup p) => new()
    {
        Id = p.Id,
        CreatedAt = p.CreatedAt,
        Status = p.Status,
        CustomerName = p.CustomerName,
        CustomerEmail = p.CustomerEmail,
        TenantName = p.TenantName,
        AdminEmail = p.AdminEmail,
        AdminDisplayName = p.AdminDisplayName,
        PlanNameSnapshot = p.PlanNameSnapshot,
        PlanDisplayNameSnapshot = p.PlanDisplayNameSnapshot,
        Source = p.Source,
        Amount = p.Amount,
        Currency = p.Currency,
        ExternalPaymentId = p.ExternalPaymentId,
        ProvisionedLicenseId = p.ProvisionedLicenseId,
        ProvisionedLicenseNumber = p.ProvisionedLicenseNumber,
        ProvisionedAt = p.ProvisionedAt,
        ErrorMessage = p.ErrorMessage
    };

    private static PendingSignupDetailsDto MapToDetails(PendingSignup p) => new()
    {
        Id = p.Id,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Status = p.Status,
        Source = p.Source,
        PlanId = p.PlanId,
        PlanNameSnapshot = p.PlanNameSnapshot,
        PlanDisplayNameSnapshot = p.PlanDisplayNameSnapshot,
        PlanPriceMonthlySnapshot = p.PlanPriceMonthlySnapshot,
        PlanPriceYearlySnapshot = p.PlanPriceYearlySnapshot,
        CurrencySnapshot = p.CurrencySnapshot,
        CustomerName = p.CustomerName,
        CustomerEmail = p.CustomerEmail,
        TenantName = p.TenantName,
        TenantLegalName = p.TenantLegalName,
        TenantEmail = p.TenantEmail,
        TenantPhone = p.TenantPhone,
        TenantAddress = p.TenantAddress,
        AdminEmail = p.AdminEmail,
        AdminDisplayName = p.AdminDisplayName,
        AdminFirstName = p.AdminFirstName,
        AdminLastName = p.AdminLastName,
        PaymentProvider = p.PaymentProvider,
        ExternalPaymentId = p.ExternalPaymentId,
        ExternalCheckoutUrl = p.ExternalCheckoutUrl,
        Amount = p.Amount,
        Currency = p.Currency,
        PaidAt = p.PaidAt,
        ProvisionedAt = p.ProvisionedAt,
        CancelledAt = p.CancelledAt,
        FailedAt = p.FailedAt,
        ExpiresAt = p.ExpiresAt,
        ProvisionedLicenseId = p.ProvisionedLicenseId,
        ProvisionedLicenseNumber = p.ProvisionedLicenseNumber,
        ProvisionedTenantId = p.ProvisionedTenantId,
        ProvisionedTenantName = p.ProvisionedTenantName,
        ProvisionedAdminUserId = p.ProvisionedAdminUserId,
        ErrorMessage = p.ErrorMessage,
        InternalNote = p.InternalNote,
        MetadataJson = p.MetadataJson
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task TryLogAuditAsync(
        string action,
        string description,
        PendingSignup entity,
        object? metadata = null,
        bool severityWarning = false)
    {
        try
        {
            await logService.LogAuditAsync(
                action: action,
                description: description,
                entityType: "PendingSignup",
                entityId: entity.Id.ToString(),
                entityName: entity.CustomerName,
                metadata: metadata,
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }

        if (severityWarning)
        {
            try
            {
                await logService.LogSystemAsync(
                    action: action,
                    description: description,
                    severity: "Warning",
                    entityType: "PendingSignup",
                    entityId: entity.Id.ToString(),
                    metadata: metadata);
            }
            catch
            {
                // Protokollierung darf Fachfunktion nicht blockieren.
            }
        }
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für die Registrierungsverwaltung.");
        }
    }
}
