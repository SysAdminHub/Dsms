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
        DateTime? createdTo = null,
        string? billingStatusFilter = null,
        DateOnly? nextInvoiceDateUntil = null)
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

        if (!string.IsNullOrWhiteSpace(billingStatusFilter))
        {
            var billingStatus = billingStatusFilter.Trim();
            query = query.Where(p => p.BillingStatus == billingStatus);
        }

        if (nextInvoiceDateUntil.HasValue)
        {
            var until = nextInvoiceDateUntil.Value;
            query = query.Where(p => p.NextInvoiceDate != null && p.NextInvoiceDate <= until);
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

    public Task<Guid> CreateForPublicSignupAsync(CreatePublicPendingSignupDto dto) =>
        CreatePublicSignupInternalAsync(dto);

    public Task<bool> SetStatusForPublicSignupAsync(Guid id, string status) =>
        UpdateStatusInternalAsync(id, status, requireSuperuser: false);

    public Task<bool> MarkAsProvisionedForPublicSignupAsync(
        Guid id,
        Guid licenseId,
        string? licenseNumber,
        int tenantId,
        string? tenantName,
        string adminUserId) =>
        MarkAsProvisionedInternalAsync(id, licenseId, licenseNumber, tenantId, tenantName, adminUserId, requireSuperuser: false);

    public Task<bool> MarkAsFailedForPublicSignupAsync(Guid id, string errorMessage) =>
        MarkAsFailedInternalAsync(id, errorMessage, requireSuperuser: false);

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
            InternalNote = NormalizeOptional(dto.InternalNote),
            BillingCompanyName = NormalizeOptional(dto.BillingCompanyName),
            BillingEmail = NormalizeOptional(dto.BillingEmail),
            BillingStreet = NormalizeOptional(dto.BillingStreet),
            BillingPostalCode = NormalizeOptional(dto.BillingPostalCode),
            BillingCity = NormalizeOptional(dto.BillingCity),
            BillingCountry = NormalizeOptional(dto.BillingCountry),
            BillingVatId = NormalizeOptional(dto.BillingVatId),
            BillingReference = NormalizeOptional(dto.BillingReference)
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

    private async Task<Guid> CreatePublicSignupInternalAsync(CreatePublicPendingSignupDto dto)
    {
        ValidateCreateDto(dto);

        await using var db = await dbFactory.CreateDbContextAsync();

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == dto.PlanId);
        if (plan is null)
        {
            throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");
        }

        if (!plan.IsActive)
        {
            throw new InvalidOperationException("Der ausgewählte Tarif ist nicht aktiv.");
        }

        if (!plan.IsPublicSignupEnabled)
        {
            throw new InvalidOperationException("Der ausgewählte Tarif ist nicht mehr verfügbar. Bitte wählen Sie einen anderen Tarif.");
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
            Amount = dto.Amount,
            Currency = dto.Currency ?? plan.Currency,
            PaymentProvider = NormalizeOptional(dto.PaymentProvider),
            Source = NormalizeOptional(dto.Source) ?? "PublicSignup",
            InternalNote = NormalizeOptional(dto.InternalNote),
            MetadataJson = NormalizeOptional(dto.MetadataJson),
            BillingCycle = NormalizeOptional(dto.BillingCycle),
            BillingCompanyName = NormalizeOptional(dto.BillingCompanyName),
            BillingEmail = NormalizeOptional(dto.BillingEmail),
            BillingStreet = NormalizeOptional(dto.BillingStreet),
            BillingPostalCode = NormalizeOptional(dto.BillingPostalCode),
            BillingCity = NormalizeOptional(dto.BillingCity),
            BillingCountry = NormalizeOptional(dto.BillingCountry),
            BillingVatId = NormalizeOptional(dto.BillingVatId),
            BillingReference = NormalizeOptional(dto.BillingReference),
            BillingStatus = NormalizeOptional(dto.BillingStatus),
            NextInvoiceDate = dto.NextInvoiceDate
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

    public Task<bool> UpdateStatusAsync(Guid id, string status, string? note = null)
    {
        return UpdateStatusInternalAsync(id, status, requireSuperuser: true, note);
    }

    private async Task<bool> UpdateStatusInternalAsync(
        Guid id,
        string status,
        bool requireSuperuser,
        string? note = null)
    {
        if (requireSuperuser)
        {
            await EnsureSuperuserAsync();
        }

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

    public Task<bool> MarkAsProvisionedAsync(
        Guid id,
        Guid licenseId,
        string? licenseNumber,
        int tenantId,
        string? tenantName,
        string adminUserId) =>
        MarkAsProvisionedInternalAsync(id, licenseId, licenseNumber, tenantId, tenantName, adminUserId, requireSuperuser: true);

    private async Task<bool> MarkAsProvisionedInternalAsync(
        Guid id,
        Guid licenseId,
        string? licenseNumber,
        int tenantId,
        string? tenantName,
        string adminUserId,
        bool requireSuperuser)
    {
        if (requireSuperuser)
        {
            await EnsureSuperuserAsync();
        }

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

    public Task<bool> MarkAsFailedAsync(Guid id, string errorMessage) =>
        MarkAsFailedInternalAsync(id, errorMessage, requireSuperuser: true);

    private async Task<bool> MarkAsFailedInternalAsync(
        Guid id,
        string errorMessage,
        bool requireSuperuser)
    {
        if (requireSuperuser)
        {
            await EnsureSuperuserAsync();
        }

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

    public async Task<bool> UpdateBillingDetailsAsync(UpdateBillingDetailsDto dto)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == dto.Id);
        if (entity is null)
        {
            return false;
        }

        if (IsPaidSignup(entity))
        {
            var oldDate = entity.NextInvoiceDate;
            entity.NextInvoiceDate = dto.NextInvoiceDate;
            entity.BillingNote = NormalizeOptional(dto.BillingNote);
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await TryLogBillingAuditAsync(
                entity,
                "PendingSignupBillingDetailsUpdated",
                "Rechnungsdetails wurden aktualisiert.",
                new { OldNextInvoiceDate = oldDate, NewNextInvoiceDate = entity.NextInvoiceDate, BillingNoteUpdated = dto.BillingNote is not null });
            return true;
        }

        entity.BillingNote = NormalizeOptional(dto.BillingNote);
        entity.NextInvoiceDate = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> MarkInvoiceSentAsync(Guid id) =>
        UpdateBillingStatusAsync(id, BillingStatuses.InvoiceSent, setInvoiceSentAt: true);

    public Task<bool> MarkInvoicePaidAsync(Guid id) =>
        UpdateBillingStatusAsync(id, BillingStatuses.Paid, setInvoicePaidAt: true);

    public Task<bool> MarkPaymentOverdueAsync(Guid id) =>
        UpdateBillingStatusAsync(id, BillingStatuses.PaymentOverdue);

    public Task<bool> MarkInvoicePendingAsync(Guid id) =>
        UpdateBillingStatusAsync(id, BillingStatuses.InvoicePending);

    private async Task<bool> UpdateBillingStatusAsync(
        Guid id,
        string billingStatus,
        bool setInvoiceSentAt = false,
        bool setInvoicePaidAt = false)
    {
        await EnsureSuperuserAsync();

        if (!BillingStatuses.IsValid(billingStatus))
        {
            throw new InvalidOperationException("Der Rechnungsstatus ist ungültig.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entity = await db.PendingSignups.FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null)
        {
            return false;
        }

        if (!IsPaidSignup(entity))
        {
            throw new InvalidOperationException("Für kostenlose Registrierungen sind keine Rechnungsaktionen verfügbar.");
        }

        var oldStatus = entity.BillingStatus;
        entity.BillingStatus = billingStatus;
        entity.UpdatedAt = DateTime.UtcNow;

        if (setInvoiceSentAt)
        {
            entity.InvoiceSentAt = DateTime.UtcNow;
        }

        if (setInvoicePaidAt)
        {
            entity.InvoicePaidAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        var action = billingStatus switch
        {
            BillingStatuses.InvoiceSent => "PendingSignupInvoiceMarkedSent",
            BillingStatuses.Paid => "PendingSignupInvoiceMarkedPaid",
            BillingStatuses.PaymentOverdue => "PendingSignupInvoiceMarkedOverdue",
            BillingStatuses.InvoicePending => "PendingSignupInvoiceMarkedPending",
            _ => "PendingSignupBillingStatusChanged"
        };

        var description = billingStatus switch
        {
            BillingStatuses.InvoiceSent => "Rechnung wurde als gesendet markiert.",
            BillingStatuses.Paid => "Registrierung wurde als bezahlt markiert.",
            BillingStatuses.PaymentOverdue => "Registrierung wurde als überfällig markiert.",
            BillingStatuses.InvoicePending => "Rechnungsstatus wurde auf offen gesetzt.",
            _ => "Rechnungsstatus wurde geändert."
        };

        await TryLogBillingAuditAsync(entity, action, description, new { OldStatus = oldStatus, NewStatus = billingStatus });
        return true;
    }

    private static bool IsPaidSignup(PendingSignup entity) =>
        !PendingSignupDisplayHelper.IsFreeSignup(entity.PaymentProvider, entity.Amount);

    private Task TryLogBillingAuditAsync(
        PendingSignup entity,
        string action,
        string description,
        object? metadata = null) =>
        TryLogAuditAsync(action, description, entity, metadata);

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
        PaymentProvider = p.PaymentProvider,
        MetadataJson = p.MetadataJson,
        BillingEmail = p.BillingEmail,
        BillingCompanyName = p.BillingCompanyName,
        BillingCycle = p.BillingCycle,
        BillingStatus = p.BillingStatus,
        NextInvoiceDate = p.NextInvoiceDate,
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
        BillingCycle = p.BillingCycle,
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
        MetadataJson = p.MetadataJson,
        BillingCompanyName = p.BillingCompanyName,
        BillingEmail = p.BillingEmail,
        BillingStreet = p.BillingStreet,
        BillingPostalCode = p.BillingPostalCode,
        BillingCity = p.BillingCity,
        BillingCountry = p.BillingCountry,
        BillingVatId = p.BillingVatId,
        BillingReference = p.BillingReference,
        BillingStatus = p.BillingStatus,
        InvoiceSentAt = p.InvoiceSentAt,
        InvoicePaidAt = p.InvoicePaidAt,
        NextInvoiceDate = p.NextInvoiceDate,
        BillingNote = p.BillingNote
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
