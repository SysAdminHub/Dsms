using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.Training;

/// <summary>Versand von Schulungseinladungen mit Zugangscode per E-Mail.</summary>
public class TrainingInvitationService(
    ApplicationDbContext db,
    ICurrentUserContext currentUser,
    TrainingAccessCodeService accessCodeService,
    TrainingAssignmentService assignmentService,
    IEmailService emailService,
    IApplicationInfoService appInfo,
    IOptions<TrainingAccessOptions> accessOptions,
    IComplianceAuditLogService complianceAuditLog)
{
    public async Task<TrainingAssignmentOperationResult> SendInvitationAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await assignmentService.CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var assignment = await db.TrainingAssignments
            .Include(a => a.Training)
            .ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId && !a.IsArchived, ct);

        if (assignment is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.AssignmentNotFound);

        if (assignment.Status == TrainingAssignmentStatus.Cancelled)
            return TrainingAssignmentOperationResult.Fail("Die Zuweisung wurde abgebrochen.");

        if (assignment.Status == TrainingAssignmentStatus.Completed)
            return TrainingAssignmentOperationResult.Fail("Die Schulung ist bereits abgeschlossen.");

        return await SendInvitationCoreAsync(assignment, isResend: assignment.InvitationSendCount > 0, ct);
    }

    public async Task<TrainingAssignmentOperationResult> ResendInvitationAsync(
        int assignmentId,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await assignmentService.CanManageAsync(ct))
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var assignment = await db.TrainingAssignments
            .Include(a => a.Training)
            .ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId && !a.IsArchived, ct);

        if (assignment is null)
            return TrainingAssignmentOperationResult.Fail(TrainingLabels.AssignmentNotFound);

        if (assignment.Status == TrainingAssignmentStatus.Cancelled)
            return TrainingAssignmentOperationResult.Fail("Die Zuweisung wurde abgebrochen.");

        accessCodeService.ResetFailedAccessAttempts(assignment);
        return await SendInvitationCoreAsync(assignment, isResend: true, ct);
    }

    public async Task<TrainingInvitationBatchResult> SendInvitationsForTrainingAsync(
        int trainingId,
        int tenantId,
        bool includeLocked,
        CancellationToken ct = default)
    {
        if (!await assignmentService.CanManageAsync(ct))
            return new TrainingInvitationBatchResult(0, 0, [TrainingLabels.TrainingAccessDenied]);

        var assignments = await db.TrainingAssignments
            .Include(a => a.Training)
            .ThenInclude(t => t.Tenant)
            .Where(a => a.TrainingId == trainingId
                && a.TenantId == tenantId
                && !a.IsArchived
                && (a.Status == TrainingAssignmentStatus.Assigned
                    || a.Status == TrainingAssignmentStatus.CodeExpired
                    || (includeLocked && a.Status == TrainingAssignmentStatus.Locked)))
            .ToListAsync(ct);

        var success = 0;
        var errors = new List<string>();

        foreach (var assignment in assignments)
        {
            if (assignment.Status == TrainingAssignmentStatus.Locked)
                accessCodeService.ResetFailedAccessAttempts(assignment);

            var result = await SendInvitationCoreAsync(
                assignment,
                isResend: assignment.InvitationSendCount > 0,
                ct);

            if (result.Success)
                success++;
            else
                errors.Add($"{assignment.ParticipantEmailSnapshot}: {result.Message}");
        }

        return new TrainingInvitationBatchResult(success, errors.Count, errors);
    }

    private async Task<TrainingAssignmentOperationResult> SendInvitationCoreAsync(
        Domain.Entities.TrainingAssignment assignment,
        bool isResend,
        CancellationToken ct)
    {
        var code = accessCodeService.GenerateCode();
        var hash = accessCodeService.HashCode(assignment, code);
        var now = DateTime.UtcNow;
        var validityDays = accessCodeService.ResolveValidityDays(assignment.Training);
        var expiresAt = accessCodeService.GetExpiryUtc(now, validityDays);
        var accessUrl = BuildAccessUrl();
        var displayName = assignment.ParticipantNameSnapshot ?? assignment.ParticipantEmailSnapshot;

        var variables = new Dictionary<string, string>
        {
            ["Name"] = displayName,
            ["TrainingTitle"] = assignment.Training.Title,
            ["AccessCode"] = code,
            ["AccessCodeExpiresAt"] = expiresAt.ToLocalTime().ToString("g"),
            ["TrainingAccessUrl"] = accessUrl,
            ["TenantName"] = assignment.Training.Tenant.Name,
            ["AppName"] = appInfo.ProductName
        };

        var emailResult = await emailService.SendTemplateEmailAsync(
            assignment.ParticipantEmailSnapshot,
            EmailTemplateKeys.TrainingInvitation,
            variables);

        if (!emailResult.Succeeded)
        {
            return TrainingAssignmentOperationResult.Fail(
                emailResult.Message ?? "E-Mail-Versand fehlgeschlagen.");
        }

        assignment.AccessCodeHash = hash;
        assignment.AccessCodeGeneratedAtUtc = now;
        assignment.AccessCodeExpiresAtUtc = expiresAt;
        assignment.AccessCodeSentAtUtc = now;
        assignment.InvitationSentAtUtc = now;
        assignment.InvitationSentByUserId = await currentUser.GetUserIdAsync();
        assignment.InvitationSendCount++;
        assignment.Status = TrainingAssignmentStatus.Invited;
        assignment.UpdatedAt = now;
        assignment.UpdatedByUserId = await currentUser.GetUserIdAsync();

        if (isResend)
            accessCodeService.ResetFailedAccessAttempts(assignment);

        await db.SaveChangesAsync(ct);

        if (isResend)
        {
            await complianceAuditLog.LogTrainingInvitationResentAsync(
                assignment.TrainingId,
                assignment.Training.Title,
                assignment.TenantId,
                assignment.Id,
                assignment.TrainingParticipantId);
        }
        else
        {
            await complianceAuditLog.LogTrainingInvitationSentAsync(
                assignment.TrainingId,
                assignment.Training.Title,
                assignment.TenantId,
                assignment.Id,
                assignment.TrainingParticipantId);
        }

        return TrainingAssignmentOperationResult.Ok(assignment.Id);
    }

    private string BuildAccessUrl()
    {
        var baseUrl = appInfo.AppUrl.TrimEnd('/');
        var path = accessOptions.Value.AccessPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        return $"{baseUrl}{path}";
    }
}
