using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

/// <summary>Öffentliches Teilnehmerportal: Zugang, Karten, Fortschritt, Quiz, Abschluss.</summary>
public class TrainingParticipantPortalService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    TrainingAccessCodeService accessCodeService,
    TrainingParticipantSessionService sessionService)
{
    private const string InvalidCredentialsMessage =
        "Die eingegebenen Zugangsdaten sind ungültig oder abgelaufen.";

    public async Task<TrainingParticipantLoginResult> LoginAsync(string email, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !TrainingParticipantService.IsValidEmail(email))
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.InvalidCredentials, InvalidCredentialsMessage);

        if (!IsValidCodeFormat(code))
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.InvalidCredentials, InvalidCredentialsMessage);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var normalizedEmail = TrainingParticipantService.NormalizeEmail(email);
        var utcNow = DateTime.UtcNow;

        var assignments = await db.TrainingAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Training)
            .Where(a => a.ParticipantEmailSnapshot == normalizedEmail
                && !a.IsArchived
                && a.Status != TrainingAssignmentStatus.Cancelled)
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.InvalidCredentials, InvalidCredentialsMessage);

        TrainingAssignment? matched = null;
        TrainingAccessCodeValidationResult? lastResult = null;
        TrainingAssignment? statusIssueAssignment = null;
        TrainingAccessCodeValidationResult? statusIssueResult = null;

        foreach (var assignment in assignments)
        {
            RefreshExpiredStatus(assignment, utcNow);
            var result = accessCodeService.ValidateAccessCode(assignment, code.Trim(), utcNow);

            if (result == TrainingAccessCodeValidationResult.Valid)
            {
                matched = assignment;
                lastResult = result;
                break;
            }

            if (statusIssueAssignment is null
                && result is TrainingAccessCodeValidationResult.Expired
                    or TrainingAccessCodeValidationResult.Locked
                    or TrainingAccessCodeValidationResult.Cancelled)
            {
                statusIssueAssignment = assignment;
                statusIssueResult = result;
            }
        }

        if (matched is null)
        {
            if (statusIssueAssignment is not null)
            {
                matched = statusIssueAssignment;
                lastResult = statusIssueResult;
            }
            else
            {
                var lockTarget = assignments.FirstOrDefault(a =>
                    !string.IsNullOrEmpty(a.AccessCodeHash)
                    && a.Status != TrainingAssignmentStatus.Completed);
                if (lockTarget is not null)
                {
                    accessCodeService.ApplyFailedAccessAttempt(lockTarget, utcNow);
                    await db.SaveChangesAsync(ct);
                }

                return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.InvalidCredentials, InvalidCredentialsMessage);
            }
        }

        var trainingStatus = TrainingStatusMapper.Normalize(matched.Training.Status);
        if (trainingStatus == TrainingStatus.Archived)
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.TrainingArchived,
                "Diese Schulung ist nicht mehr verfügbar.");

        if (trainingStatus != TrainingStatus.Active)
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.TrainingInactive,
                "Diese Schulung ist derzeit nicht aktiv.");

        if (matched.Status == TrainingAssignmentStatus.Cancelled)
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.Cancelled,
                "Diese Schulungszuweisung ist nicht mehr gültig.");

        if (accessCodeService.IsLocked(matched, utcNow) || matched.Status == TrainingAssignmentStatus.Locked)
        {
            var lockedUntil = matched.LockedUntilUtc?.ToLocalTime().ToString("g");
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.Locked,
                lockedUntil is not null
                    ? $"Der Zugang ist vorübergehend gesperrt. Gesperrt bis: {lockedUntil}"
                    : "Der Zugang ist vorübergehend gesperrt. Bitte versuchen Sie es später erneut.");
        }

        if (accessCodeService.IsCodeExpired(matched, utcNow) || matched.Status == TrainingAssignmentStatus.CodeExpired)
        {
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.Expired,
                "Der Zugangscode ist abgelaufen. Bitte wenden Sie sich an die verantwortliche Person oder fordern Sie eine neue Einladung an.");
        }

        if (lastResult != TrainingAccessCodeValidationResult.Valid)
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.InvalidCredentials, InvalidCredentialsMessage);

        if (matched.Status == TrainingAssignmentStatus.Completed)
        {
            sessionService.SetSession(new TrainingParticipantSessionData(matched.Id, matched.TenantId, utcNow));
            return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.AlreadyCompleted);
        }

        accessCodeService.ResetFailedAccessAttempts(matched);
        matched.LastAccessAtUtc = utcNow;
        matched.LastAccessAttemptAtUtc = utcNow;

        if (matched.StartedAtUtc is null)
        {
            matched.StartedAtUtc = utcNow;
            if (matched.Status is TrainingAssignmentStatus.Assigned or TrainingAssignmentStatus.Invited or TrainingAssignmentStatus.CodeExpired)
                matched.Status = TrainingAssignmentStatus.Started;
        }

        await db.SaveChangesAsync(ct);
        sessionService.SetSession(new TrainingParticipantSessionData(matched.Id, matched.TenantId, utcNow));

        return new TrainingParticipantLoginResult(TrainingParticipantLoginStatus.Success);
    }

    public async Task<TrainingParticipantContext?> GetContextAsync(CancellationToken ct = default)
    {
        var session = sessionService.GetSession();
        if (session is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await LoadAssignmentAsync(db, session.AssignmentId, session.TenantId, ct);
        if (assignment is null)
            return null;

        if (assignment.Status == TrainingAssignmentStatus.Cancelled)
            return null;

        var trainingStatus = TrainingStatusMapper.Normalize(assignment.Training.Status);
        if (trainingStatus != TrainingStatus.Active && assignment.Status != TrainingAssignmentStatus.Completed)
            return null;

        TrainingTemplate? template = null;
        IReadOnlyList<TrainingTemplateSection> sections = [];
        IReadOnlyList<TrainingQuestion> questions = [];

        if (assignment.Training.TrainingTemplateId is int templateId)
        {
            template = await db.TrainingTemplates
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId, ct);

            sections = await db.TrainingTemplateSections
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.TrainingTemplateId == templateId && s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);

            questions = await db.TrainingQuestions
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(q => q.Options.Where(o => o.IsActive))
                .Where(q => q.TrainingTemplateId == templateId && q.IsActive)
                .OrderBy(q => q.SortOrder)
                .ToListAsync(ct);
        }

        var viewedCount = await db.TrainingAssignmentSectionProgress
            .IgnoreQueryFilters()
            .CountAsync(p => p.TrainingAssignmentId == assignment.Id, ct);

        var latestAttempt = await db.TrainingQuizAttempts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TrainingAssignmentId == assignment.Id && a.SubmittedAtUtc != null)
            .OrderByDescending(a => a.AttemptNumber)
            .FirstOrDefaultAsync(ct);

        return new TrainingParticipantContext(
            assignment,
            assignment.Training,
            template,
            sections,
            questions,
            viewedCount,
            latestAttempt,
            assignment.Status == TrainingAssignmentStatus.Completed);
    }

    public async Task<string> PrepareMarkdownHtmlAsync(string? markdown, int templateId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assets = await db.TrainingTemplateAssets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .ToListAsync(ct);

        var availableKeys = assets.Select(a => a.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var altTexts = assets.ToDictionary(a => a.AssetKey, a => a.AltText ?? a.AssetKey, StringComparer.OrdinalIgnoreCase);

        var resolved = TrainingMarkdownAssetResolver.ResolvePortalAssetPlaceholders(markdown, availableKeys, altTexts);
        return TrainingMarkdownRenderer.ToHtml(resolved);
    }

    public async Task RecordSectionViewAsync(int sectionId, CancellationToken ct = default)
    {
        var session = sessionService.GetSession()
            ?? throw new InvalidOperationException("Keine Teilnehmer-Session.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await LoadAssignmentAsync(db, session.AssignmentId, session.TenantId, ct);
        if (assignment is null || assignment.Status == TrainingAssignmentStatus.Completed)
            return;

        var templateId = assignment.Training.TrainingTemplateId
            ?? throw new InvalidOperationException("Schulung ohne Vorlage.");

        var sectionExists = await db.TrainingTemplateSections
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Id == sectionId && s.TrainingTemplateId == templateId && s.IsActive, ct);

        if (!sectionExists)
            return;

        var now = DateTime.UtcNow;
        var progress = await db.TrainingAssignmentSectionProgress
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TrainingAssignmentId == assignment.Id && p.TrainingTemplateSectionId == sectionId, ct);

        if (progress is null)
        {
            db.TrainingAssignmentSectionProgress.Add(new TrainingAssignmentSectionProgress
            {
                TenantId = assignment.TenantId,
                TrainingAssignmentId = assignment.Id,
                TrainingTemplateSectionId = sectionId,
                ViewedAtUtc = now,
                LastViewedAtUtc = now,
                ViewCount = 1,
                CreatedAt = now
            });
        }
        else
        {
            progress.LastViewedAtUtc = now;
            progress.ViewCount++;
            progress.UpdatedAt = now;
        }

        assignment.LastAccessAtUtc = now;
        await db.SaveChangesAsync(ct);
    }

    public async Task<TrainingParticipantProgressInfo> GetProgressAsync(CancellationToken ct = default)
    {
        var context = await GetContextAsync(ct);
        if (context is null)
            return new TrainingParticipantProgressInfo(0, 0);

        return new TrainingParticipantProgressInfo(context.Sections.Count, context.ViewedSectionCount);
    }

    public async Task<bool> AllSectionsViewedAsync(CancellationToken ct = default)
    {
        var context = await GetContextAsync(ct);
        if (context is null || context.Sections.Count == 0)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var sectionIds = context.Sections.Select(s => s.Id).ToHashSet();
        var viewedIds = await db.TrainingAssignmentSectionProgress
            .IgnoreQueryFilters()
            .Where(p => p.TrainingAssignmentId == context.Assignment.Id && sectionIds.Contains(p.TrainingTemplateSectionId))
            .Select(p => p.TrainingTemplateSectionId)
            .Distinct()
            .CountAsync(ct);

        return viewedIds >= context.Sections.Count;
    }

    public bool QuizRequired(TrainingParticipantContext context) =>
        context.Template?.IsQuizRequired == true || context.Questions.Count > 0;

    public bool QuizMandatory(TrainingParticipantContext context) =>
        context.Template?.IsQuizRequired == true;

    public async Task<TrainingParticipantQuizSubmitResult> SubmitQuizAsync(
        IReadOnlyDictionary<int, IReadOnlyList<int>> selectedOptionsByQuestion,
        CancellationToken ct = default)
    {
        var context = await GetContextAsync(ct);
        if (context is null)
            return new TrainingParticipantQuizSubmitResult(false, "Sitzung abgelaufen.", 0, 0, 0, false, 0);

        if (!await AllSectionsViewedAsync(ct))
            return new TrainingParticipantQuizSubmitResult(false, "Bitte sehen Sie sich zuerst alle Schulungskarten an.", 0, 0, 0, false, 0);

        if (context.Template?.IsQuizRequired == true && context.Questions.Count == 0)
        {
            return new TrainingParticipantQuizSubmitResult(false,
                "Für diese Schulung ist ein Quiz erforderlich, es sind jedoch keine gültigen Fragen hinterlegt. Bitte wenden Sie sich an die verantwortliche Person.",
                0, 0, 0, false, 0);
        }

        if (context.Questions.Count == 0)
            return new TrainingParticipantQuizSubmitResult(false, "Kein Quiz vorhanden.", 0, 0, 0, false, 0);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var now = DateTime.UtcNow;
        var attemptNumber = await db.TrainingQuizAttempts
            .IgnoreQueryFilters()
            .CountAsync(a => a.TrainingAssignmentId == context.Assignment.Id, ct) + 1;

        var totalPoints = context.Questions.Sum(q => q.Points);
        var achievedPoints = 0;
        var attempt = new TrainingQuizAttempt
        {
            TenantId = context.Assignment.TenantId,
            TrainingAssignmentId = context.Assignment.Id,
            StartedAtUtc = now,
            SubmittedAtUtc = now,
            AttemptNumber = attemptNumber,
            TotalPoints = totalPoints,
            CreatedAt = now
        };

        foreach (var question in context.Questions)
        {
            var selectedIds = selectedOptionsByQuestion.TryGetValue(question.Id, out var ids)
                ? ids.ToHashSet()
                : [];

            var activeOptions = question.Options.Where(o => o.IsActive).ToList();
            var correctIds = activeOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();

            var questionCorrect = question.QuestionType switch
            {
                TrainingQuestionType.SingleChoice =>
                    selectedIds.Count == 1 && correctIds.Count == 1 && correctIds.SetEquals(selectedIds),
                TrainingQuestionType.MultipleChoice =>
                    selectedIds.Count > 0 && correctIds.SetEquals(selectedIds),
                _ => false
            };

            var pointsAwarded = questionCorrect ? question.Points : 0;
            achievedPoints += pointsAwarded;

            var firstOption = true;
            foreach (var option in activeOptions)
            {
                var isSelected = selectedIds.Contains(option.Id);
                attempt.Answers.Add(new TrainingQuizAnswer
                {
                    TenantId = context.Assignment.TenantId,
                    TrainingQuestionId = question.Id,
                    TrainingQuestionOptionId = option.Id,
                    AnswerTextSnapshot = option.AnswerText,
                    IsSelected = isSelected,
                    IsCorrect = option.IsCorrect,
                    PointsAwarded = questionCorrect && firstOption ? pointsAwarded : 0,
                    CreatedAt = now
                });
                firstOption = false;
            }
        }

        attempt.AchievedPoints = achievedPoints;
        attempt.ScorePercent = totalPoints > 0 ? (int)Math.Round(achievedPoints * 100.0 / totalPoints) : 0;
        var passingScore = context.Template?.PassingScorePercent ?? 80;
        attempt.Passed = attempt.ScorePercent >= passingScore;

        db.TrainingQuizAttempts.Add(attempt);

        var assignment = await db.TrainingAssignments
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == context.Assignment.Id, ct);
        assignment.LastAccessAtUtc = now;

        await db.SaveChangesAsync(ct);

        return new TrainingParticipantQuizSubmitResult(
            true,
            null,
            attempt.ScorePercent,
            achievedPoints,
            totalPoints,
            attempt.Passed,
            attemptNumber);
    }

    public async Task<TrainingParticipantCompletionResult> CompleteAsync(bool confirmed, CancellationToken ct = default)
    {
        if (!confirmed)
            return new TrainingParticipantCompletionResult(false, "Bitte bestätigen Sie die Teilnahme.");

        var context = await GetContextAsync(ct);
        if (context is null)
            return new TrainingParticipantCompletionResult(false, "Sitzung abgelaufen.");

        if (context.IsCompleted)
            return new TrainingParticipantCompletionResult(true, null);

        if (!await AllSectionsViewedAsync(ct))
            return new TrainingParticipantCompletionResult(false, "Bitte sehen Sie sich zuerst alle Schulungskarten an.");

        if (QuizRequired(context))
        {
            if (context.Template?.IsQuizRequired == true && context.Questions.Count == 0)
            {
                return new TrainingParticipantCompletionResult(false,
                    "Für diese Schulung ist ein Quiz erforderlich, es sind jedoch keine gültigen Fragen hinterlegt.");
            }

            if (context.Questions.Count > 0)
            {
                var passed = context.LatestQuizAttempt?.Passed == true;
                if (!passed)
                    return new TrainingParticipantCompletionResult(false, "Bitte bestehen Sie zuerst das Quiz.");
            }
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var now = DateTime.UtcNow;
        var assignment = await db.TrainingAssignments
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == context.Assignment.Id, ct);

        assignment.Status = TrainingAssignmentStatus.Completed;
        assignment.CompletedAtUtc = now;
        assignment.ParticipationConfirmedAtUtc = now;
        assignment.LastAccessAtUtc = now;
        assignment.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return new TrainingParticipantCompletionResult(true, null);
    }

    public async Task<bool> ValidatePortalAssetAccessAsync(string assetKey, CancellationToken ct = default)
    {
        var session = sessionService.GetSession();
        if (session is null || !TrainingAssetUploadValidation.IsValidAssetKey(assetKey))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await LoadAssignmentAsync(db, session.AssignmentId, session.TenantId, ct);
        if (assignment is null || assignment.Status == TrainingAssignmentStatus.Cancelled)
            return false;

        if (TrainingStatusMapper.Normalize(assignment.Training.Status) != TrainingStatus.Active
            && assignment.Status != TrainingAssignmentStatus.Completed)
            return false;

        if (accessCodeService.IsLocked(assignment, DateTime.UtcNow))
            return false;

        var templateId = assignment.Training.TrainingTemplateId;
        if (templateId is null)
            return false;

        var normalizedKey = TrainingAssetUploadValidation.NormalizeAssetKey(assetKey);
        return await db.TrainingTemplateAssets
            .IgnoreQueryFilters()
            .AnyAsync(a => a.TrainingTemplateId == templateId && a.AssetKey == normalizedKey && a.IsActive, ct);
    }

    public async Task<TrainingTemplateAsset?> GetPortalAssetAsync(string assetKey, CancellationToken ct = default)
    {
        if (!await ValidatePortalAssetAccessAsync(assetKey, ct))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var session = sessionService.GetSession()!;
        var assignment = await LoadAssignmentAsync(db, session.AssignmentId, session.TenantId, ct);
        var templateId = assignment!.Training.TrainingTemplateId!.Value;
        var normalizedKey = TrainingAssetUploadValidation.NormalizeAssetKey(assetKey);

        return await db.TrainingTemplateAssets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TrainingTemplateId == templateId && a.AssetKey == normalizedKey && a.IsActive, ct);
    }

    public void Logout() => sessionService.ClearSession();

    private static async Task<TrainingAssignment?> LoadAssignmentAsync(
        ApplicationDbContext db,
        int assignmentId,
        int tenantId,
        CancellationToken ct) =>
        await db.TrainingAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Training)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId && !a.IsArchived, ct);

    private static bool IsValidCodeFormat(string code) =>
        !string.IsNullOrWhiteSpace(code) && code.Trim().Length == 6 && code.Trim().All(char.IsDigit);

    private void RefreshExpiredStatus(TrainingAssignment assignment, DateTime utcNow)
    {
        if (assignment.Status == TrainingAssignmentStatus.Invited
            && accessCodeService.IsCodeExpired(assignment, utcNow))
        {
            assignment.Status = TrainingAssignmentStatus.CodeExpired;
        }
    }
}
