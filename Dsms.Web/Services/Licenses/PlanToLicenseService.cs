using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Licenses;

public sealed class PlanToLicenseService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogService logService) : IPlanToLicenseService
{

    public async Task<LicenseFromPlanPreviewDto> PreviewLicenseFromPlanAsync(
        Guid planId,
        CreateLicenseFromPlanDto? input = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plan = await LoadPlanAsync(db, planId)
            ?? throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");

        var normalized = NormalizeInput(input);
        return BuildPreview(plan, normalized);
    }

    public async Task<Guid> CreateLicenseFromPlanAsync(CreateLicenseFromPlanDto dto)
    {
        PlanToLicenseValidator.ValidateForCreate(dto);

        await using var db = await dbFactory.CreateDbContextAsync();
        var plan = await LoadPlanAsync(db, dto.PlanId)
            ?? throw new InvalidOperationException("Bitte wählen Sie einen Tarif aus.");

        if (!plan.IsActive)
        {
            throw new InvalidOperationException("Der ausgewählte Tarif ist nicht aktiv.");
        }

        var normalized = NormalizeInput(dto);
        var licenseNumber = await ResolveLicenseNumberAsync(db, dto.LicenseNumber);

        var license = PlanToLicenseMapper.MapToLicense(plan, normalized, licenseNumber);
        license.Id = Guid.NewGuid();
        license.CreatedAt = DateTime.UtcNow;

        db.Licenses.Add(license);
        await db.SaveChangesAsync();

        await TryLogCreatedFromPlanAsync(plan, license);

        return license.Id;
    }

    private static async Task<SubscriptionPlan?> LoadPlanAsync(ApplicationDbContext db, Guid planId) =>
        await db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId);

    private static CreateLicenseFromPlanDto NormalizeInput(CreateLicenseFromPlanDto? input)
    {
        input ??= new CreateLicenseFromPlanDto();

        var status = string.IsNullOrWhiteSpace(input.Status) ? "Active" : input.Status.Trim();
        var validFrom = input.ValidFrom ?? DateTime.UtcNow.Date;

        return new CreateLicenseFromPlanDto
        {
            PlanId = input.PlanId,
            CustomerName = input.CustomerName?.Trim() ?? string.Empty,
            CustomerEmail = NormalizeOptional(input.CustomerEmail),
            Status = status,
            ValidFrom = validFrom,
            ValidUntil = input.ValidUntil,
            InternalNote = NormalizeOptional(input.InternalNote),
            LicenseNumber = NormalizeOptional(input.LicenseNumber),
            OverridePlanName = NormalizeOptional(input.OverridePlanName)
        };
    }

    private static LicenseFromPlanPreviewDto BuildPreview(
        SubscriptionPlan plan,
        CreateLicenseFromPlanDto input) => new()
    {
        PlanId = plan.Id,
        PlanName = plan.Name,
        PlanDisplayName = plan.DisplayName,
        PlanDescription = plan.Description,
        IsFree = plan.IsFree,
        PriceMonthly = plan.PriceMonthly,
        PriceYearly = plan.PriceYearly,
        Currency = plan.Currency,
        PlanNameForLicense = PlanToLicenseMapper.ResolvePlanNameForLicense(plan, input.OverridePlanName),
        Status = input.Status,
        ValidFrom = input.ValidFrom,
        ValidUntil = input.ValidUntil,
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

    private static async Task<string> ResolveLicenseNumberAsync(ApplicationDbContext db, string? requestedNumber)
    {
        if (string.IsNullOrWhiteSpace(requestedNumber))
        {
            return await LicenseNumberGenerator.GenerateAsync(db);
        }

        var number = requestedNumber.Trim();
        if (await db.Licenses.AnyAsync(l => l.LicenseNumber == number))
        {
            throw new InvalidOperationException($"Die Lizenznummer „{number}“ ist bereits vergeben.");
        }

        return number;
    }

    private async Task TryLogCreatedFromPlanAsync(SubscriptionPlan plan, License license)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "LicenseCreatedFromPlan",
                description: "Lizenz wurde aus Tarifvorlage erstellt.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                licenseId: license.Id,
                metadata: new
                {
                    PlanId = plan.Id,
                    PlanName = plan.Name,
                    PlanDisplayName = plan.DisplayName,
                    CreatedLicenseId = license.Id,
                    CreatedLicenseNumber = license.LicenseNumber,
                    CustomerName = license.CustomerName
                },
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
