using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Services.Reminders;

public sealed class ReminderService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    NavigationManager navigationManager,
    ILogger<ReminderService> logger,
    ILogService logService) : IReminderService
{
    private const int DsfaTomAvvDaysAhead = 14;
    private const int MeasureDaysAhead = 7;
    private const int AuditInactivityDays = 14;

    public async Task<ReminderPreviewResult> GetReminderPreviewAsync()
    {
        await EnsureCanAccessRemindersAsync();
        var tenantIds = await GetAccessibleTenantIdsAsync();
        return await BuildPreviewAsync(tenantIds);
    }

    public async Task<ReminderSendResult> SendRemindersAsync()
    {
        await EnsureCanAccessRemindersAsync();
        var tenantIds = await GetAccessibleTenantIdsAsync();
        var preview = await BuildPreviewAsync(tenantIds);

        var tenantResults = new List<ReminderTenantSendResultDto>();
        var anySuccess = false;
        var anyFailure = false;

        foreach (var tenant in preview.Tenants.Where(t => t.Items.Count > 0))
        {
            if (tenant.AdminRecipients.Count == 0)
            {
                tenantResults.Add(new ReminderTenantSendResultDto
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.TenantName,
                    ItemCount = tenant.Items.Count,
                    Skipped = true,
                    Success = false,
                    Message = "Keine Admin-Empfänger gefunden, nicht versendet."
                });
                continue;
            }

            var reminderText = BuildReminderEmailText(tenant.Items);
            var supportEmail = await GetSupportEmailAsync();
            var actionLink = navigationManager.ToAbsoluteUri("/").AbsoluteUri;
            var sentCount = 0;
            var failedCount = 0;

            foreach (var recipient in tenant.AdminRecipients)
            {
                var result = await emailService.SendReminderEmailAsync(
                    recipient.Email,
                    recipient.DisplayName,
                    "Fällige Datenschutz-Erinnerungen",
                    reminderText,
                    "siehe Übersicht",
                    actionLink,
                    tenant.TenantId,
                    supportEmail);

                if (result.Succeeded)
                {
                    sentCount++;
                }
                else
                {
                    failedCount++;
                    logger.LogWarning(
                        "Erinnerungsmail für Mandant {TenantId} an {Email} fehlgeschlagen: {Detail}",
                        tenant.TenantId,
                        recipient.Email,
                        result.DetailMessage ?? result.Message);
                }
            }

            if (sentCount > 0)
            {
                anySuccess = true;
            }

            if (failedCount > 0)
            {
                anyFailure = true;
            }

            tenantResults.Add(new ReminderTenantSendResultDto
            {
                TenantId = tenant.TenantId,
                TenantName = tenant.TenantName,
                RecipientCount = sentCount,
                ItemCount = tenant.Items.Count,
                Success = sentCount > 0 && failedCount == 0,
                Message = failedCount > 0
                    ? $"{sentCount} von {tenant.AdminRecipients.Count} Emails versendet."
                    : $"{sentCount} Empfänger, {tenant.Items.Count} Erinnerungen."
            });
        }

        if (anyFailure)
        {
            await logService.LogSystemErrorAsync(
                action: "ReminderJobFailed",
                description: "Reminder-Job konnte nicht vollständig ausgeführt werden.",
                exception: new InvalidOperationException("Einige Erinnerungs-E-Mails konnten nicht versendet werden."),
                metadata: new
                {
                    TenantCount = tenantResults.Count,
                    FailedTenants = tenantResults.Count(r => !r.Success && !r.Skipped)
                });
        }

        return new ReminderSendResult
        {
            Succeeded = anySuccess && !anyFailure,
            Message = anyFailure
                ? "Einige Erinnerungen konnten nicht versendet werden. Bitte prüfen Sie die Ergebnisse."
                : anySuccess
                    ? "Erinnerungen wurden versendet."
                    : "Es wurden keine Erinnerungen versendet.",
            TenantResults = tenantResults
        };
    }

    private async Task<ReminderPreviewResult> BuildPreviewAsync(IReadOnlyList<int> tenantIds)
    {
        if (tenantIds.Count == 0)
        {
            return new ReminderPreviewResult();
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id) && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var previews = new List<ReminderTenantPreviewDto>();

        foreach (var tenant in tenants)
        {
            var items = new List<ReminderItemDto>();
            items.AddRange(await CollectMeasureRemindersAsync(db, tenant, today));
            items.AddRange(await CollectDsfaRemindersAsync(db, tenant, today));
            items.AddRange(await CollectTomRemindersAsync(db, tenant, today));
            items.AddRange(await CollectAvvRemindersAsync(db, tenant, today));
            items.AddRange(await CollectAuditRemindersAsync(db, tenant));

            if (items.Count == 0)
            {
                continue;
            }

            var recipients = await GetTenantAdminRecipientsAsync(db, tenant.Id);
            string? warning = null;
            if (recipients.Count == 0)
            {
                warning = "Für diesen Mandanten wurden keine Admin-Empfänger gefunden. Es wird keine Email versendet.";
            }

            previews.Add(new ReminderTenantPreviewDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                AdminRecipients = recipients,
                Items = items.OrderBy(i => i.ReminderType).ThenBy(i => i.DueDate).ToList(),
                WarningMessage = warning
            });
        }

        return new ReminderPreviewResult { Tenants = previews };
    }

    private static async Task<List<ReminderItemDto>> CollectMeasureRemindersAsync(
        ApplicationDbContext db, Tenant tenant, DateOnly today)
    {
        var threshold = today.AddDays(MeasureDaysAhead);
        var measures = await db.Measures
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == tenant.Id
                && !m.IsArchived
                && m.DueDate != null
                && m.DueDate <= threshold
                && m.Status != MeasureStatus.Done
                && m.Status != MeasureStatus.Cancelled)
            .ToListAsync();

        return measures.Select(m => CreateDateItem(
            tenant, ReminderType.Measure, m.Id, m.Title,
            FormatMeasureDescription(m.Title, m.DueDate!.Value, today),
            m.DueDate, today, $"measures/edit/{m.Id}")).ToList();
    }

    private static async Task<List<ReminderItemDto>> CollectDsfaRemindersAsync(
        ApplicationDbContext db, Tenant tenant, DateOnly today)
    {
        var threshold = today.AddDays(DsfaTomAvvDaysAhead);
        var items = await db.DataProtectionImpactAssessments
            .IgnoreQueryFilters()
            .Where(d => d.TenantId == tenant.Id
                && !d.IsArchived
                && d.Status != DpiaStatus.Archived
                && d.NextReviewAt != null
                && d.NextReviewAt <= threshold)
            .ToListAsync();

        return items.Select(d => CreateDateItem(
            tenant, ReminderType.Dsfa, d.Id, d.Title,
            FormatReviewDescription("DSFA", d.Title, d.NextReviewAt!.Value, today),
            d.NextReviewAt, today, $"dsfa/{d.Id}")).ToList();
    }

    private static async Task<List<ReminderItemDto>> CollectTomRemindersAsync(
        ApplicationDbContext db, Tenant tenant, DateOnly today)
    {
        var threshold = today.AddDays(DsfaTomAvvDaysAhead);
        var items = await db.Toms
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenant.Id
                && !t.IsArchived
                && t.NextReviewAt != null
                && t.NextReviewAt <= threshold)
            .ToListAsync();

        return items.Select(t => CreateDateItem(
            tenant, ReminderType.Tom, t.Id, t.Title,
            FormatReviewDescription("TOM", t.Title, t.NextReviewAt!.Value, today),
            t.NextReviewAt, today, $"toms/{t.Id}")).ToList();
    }

    private static async Task<List<ReminderItemDto>> CollectAvvRemindersAsync(
        ApplicationDbContext db, Tenant tenant, DateOnly today)
    {
        var threshold = today.AddDays(DsfaTomAvvDaysAhead);
        var items = await db.ServiceProviders
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenant.Id
                && !s.IsArchived
                && s.Status != ServiceProviderStatus.Terminated
                && s.Status != ServiceProviderStatus.Blocked
                && s.Status != ServiceProviderStatus.Archived
                && s.DataProcessingAgreementReviewedAt != null
                && s.DataProcessingAgreementReviewedAt <= threshold)
            .ToListAsync();

        return items.Select(s => CreateDateItem(
            tenant, ReminderType.ServiceProviderAvv, s.Id, s.Name,
            FormatAvvDescription(s.Name, s.DataProcessingAgreementReviewedAt!.Value, today),
            s.DataProcessingAgreementReviewedAt, today, $"service-providers/{s.Id}")).ToList();
    }

    private static async Task<List<ReminderItemDto>> CollectAuditRemindersAsync(
        ApplicationDbContext db, Tenant tenant)
    {
        var cutoff = DateTime.UtcNow.AddDays(-AuditInactivityDays);
        var runs = await db.AuditRuns
            .IgnoreQueryFilters()
            .Include(r => r.Answers)
            .Where(r => r.TenantId == tenant.Id
                && !r.IsArchived
                && (r.Status == AuditRunStatus.Draft || r.Status == AuditRunStatus.InProgress))
            .ToListAsync();

        var items = new List<ReminderItemDto>();
        foreach (var run in runs)
        {
            var lastActivity = GetLastAuditActivity(run);
            if (lastActivity >= cutoff)
            {
                continue;
            }

            var inactiveDays = (int)Math.Floor((DateTime.UtcNow - lastActivity).TotalDays);
            items.Add(new ReminderItemDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                ReminderType = ReminderType.AuditRun,
                EntityId = run.Id,
                Title = run.Title,
                Description = $"Audit „{run.Title}“ wurde seit {inactiveDays} Tagen nicht bearbeitet.",
                IsOverdue = true,
                DetailUrl = $"audit-runs/answers/{run.Id}"
            });
        }

        return items;
    }

    private static DateTime GetLastAuditActivity(AuditRun run)
    {
        var answerTimestamps = run.Answers
            .Select(a => a.AnsweredAt ?? a.UpdatedAt ?? (DateTime?)a.CreatedAt)
            .Where(d => d.HasValue)
            .Select(d => d!.Value);

        if (answerTimestamps.Any())
        {
            return answerTimestamps.Max();
        }

        return run.UpdatedAt ?? run.StartedAt ?? run.CreatedAt;
    }

    private async Task<IReadOnlyList<ReminderRecipientDto>> GetTenantAdminRecipientsAsync(
        ApplicationDbContext db, int tenantId)
    {
        var userIdsInTenant = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .ToListAsync();

        var legacyUsers = await db.Users
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync();

        var allUserIds = userIdsInTenant.Concat(legacyUsers).Distinct().ToList();
        var recipients = new List<ReminderRecipientDto>();

        foreach (var userId in allUserIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            if (await userManager.IsInRoleAsync(user, DsmsRoles.Superuser))
            {
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, DsmsRoles.Admin))
            {
                continue;
            }

            recipients.Add(new ReminderRecipientDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName
            });
        }

        return recipients
            .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.DisplayName)
            .ToList();
    }

    internal static string BuildReminderEmailText(IReadOnlyList<ReminderItemDto> items)
    {
        if (items.Count == 0)
        {
            return "Es liegen keine fälligen Erinnerungen vor.";
        }

        var tenantName = items[0].TenantName;
        var lines = new List<string>
        {
            $"Für den Mandanten {tenantName} gibt es fällige oder bald fällige Datenschutz-Themen:",
            ""
        };

        AppendSection(lines, "Maßnahmen", items.Where(i => i.ReminderType == ReminderType.Measure));
        AppendSection(lines, "DSFA", items.Where(i => i.ReminderType == ReminderType.Dsfa));
        AppendSection(lines, "TOM", items.Where(i => i.ReminderType == ReminderType.Tom));
        AppendSection(lines, "Dienstleister / AVV", items.Where(i => i.ReminderType == ReminderType.ServiceProviderAvv));
        AppendSection(lines, "Audit-Durchläufe", items.Where(i => i.ReminderType == ReminderType.AuditRun));

        lines.Add("");
        lines.Add("Bitte prüfen Sie die genannten Punkte im Datenschutzmanagementsystem.");

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendSection(List<string> lines, string sectionTitle, IEnumerable<ReminderItemDto> sectionItems)
    {
        var list = sectionItems.ToList();
        if (list.Count == 0)
        {
            return;
        }

        lines.Add($"{sectionTitle}:");
        foreach (var item in list)
        {
            lines.Add($"- {item.Description}");
        }

        lines.Add("");
    }

    private static ReminderItemDto CreateDateItem(
        Tenant tenant,
        ReminderType type,
        int entityId,
        string title,
        string description,
        DateOnly? dueDate,
        DateOnly today,
        string detailUrl)
    {
        var daysUntil = dueDate.HasValue ? dueDate.Value.DayNumber - today.DayNumber : (int?)null;
        return new ReminderItemDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            ReminderType = type,
            EntityId = entityId,
            Title = title,
            Description = description,
            DueDate = dueDate,
            DaysUntilDue = daysUntil,
            IsOverdue = daysUntil < 0,
            DetailUrl = detailUrl
        };
    }

    private static string FormatMeasureDescription(string title, DateOnly dueDate, DateOnly today)
    {
        if (dueDate < today)
        {
            var days = today.DayNumber - dueDate.DayNumber;
            return $"Maßnahme „{title}“ ist seit {days} Tag(en) überfällig.";
        }

        if (dueDate == today)
        {
            return $"Maßnahme „{title}“ ist heute fällig.";
        }

        return $"Maßnahme „{title}“ ist am {dueDate:d} fällig.";
    }

    private static string FormatReviewDescription(string typeLabel, string title, DateOnly reviewDate, DateOnly today)
    {
        if (reviewDate < today)
        {
            var days = today.DayNumber - reviewDate.DayNumber;
            return $"{typeLabel} „{title}“ ist seit {days} Tag(en) überfällig (Prüfung am {reviewDate:d}).";
        }

        if (reviewDate == today)
        {
            return $"{typeLabel} „{title}“ muss heute erneut geprüft werden.";
        }

        return $"{typeLabel} „{title}“ muss am {reviewDate:d} erneut geprüft werden.";
    }

    private static string FormatAvvDescription(string name, DateOnly reviewDate, DateOnly today)
    {
        if (reviewDate < today)
        {
            var days = today.DayNumber - reviewDate.DayNumber;
            return $"AVV-Prüfung für „{name}“ ist seit {days} Tag(en) überfällig (am {reviewDate:d}).";
        }

        if (reviewDate == today)
        {
            return $"AVV-Prüfung für „{name}“ ist heute fällig.";
        }

        return $"AVV-Prüfung für „{name}“ ist am {reviewDate:d} fällig.";
    }

    private async Task<IReadOnlyList<int>> GetAccessibleTenantIdsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (await access.IsSuperuserAsync())
        {
            return await db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.IsActive)
                .Select(t => t.Id)
                .ToListAsync();
        }

        var tenantId = await access.GetCurrentTenantIdAsync();
        return tenantId.HasValue ? [tenantId.Value] : [];
    }

    private async Task EnsureCanAccessRemindersAsync()
    {
        if (await access.IsSuperuserAsync())
        {
            return;
        }

        throw new UnauthorizedAccessException("Keine Berechtigung für Erinnerungen.");
    }

    private async Task<string> GetSupportEmailAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.EmailSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        return settings?.SenderEmail ?? string.Empty;
    }
}
