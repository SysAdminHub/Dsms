using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>Verwaltung von Quiz-Fragen und Antwortoptionen einer Schulungsvorlage.</summary>
public class TrainingQuestionService(
    ApplicationDbContext db,
    ICurrentUserContext currentUser,
    TrainingTemplateAccessService templateAccess,
    IComplianceAuditLogService complianceAuditLog)
{
    public async Task<IReadOnlyList<TrainingQuestion>> GetQuestionsAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return [];

        var query = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestions.AsQueryable(), ct);
        return await query
            .Include(q => q.Options.Where(o => o.IsActive))
            .Where(q => q.TrainingTemplateId == templateId && q.IsActive)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<TrainingQuestionOperationResult> SaveQuestionAsync(
        TrainingQuestion question, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateForMutationAsync(question.TrainingTemplateId, ct);
        if (template is null)
            return TrainingQuestionOperationResult.Fail(TrainingLabels.AccessDenied);

        if (string.IsNullOrWhiteSpace(question.QuestionText))
            return TrainingQuestionOperationResult.Fail("Fragetext ist erforderlich.");

        var userId = await currentUser.GetUserIdAsync();
        if (question.Id == 0)
        {
            question.TenantId = template.TenantId;
            question.CreatedByUserId = userId;
            question.CreatedAt = DateTime.UtcNow;
            db.TrainingQuestions.Add(question);
        }
        else
        {
            var query = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestions.AsQueryable(), ct);
            var existing = await query
                .FirstOrDefaultAsync(q => q.Id == question.Id && q.TrainingTemplateId == question.TrainingTemplateId, ct);
            if (existing is null)
                return TrainingQuestionOperationResult.Fail("Frage wurde nicht gefunden.");

            existing.SortOrder = question.SortOrder;
            existing.QuestionText = question.QuestionText.Trim();
            existing.QuestionType = question.QuestionType;
            existing.Explanation = question.Explanation?.Trim();
            existing.Points = question.Points;
            existing.IsRequired = question.IsRequired;
            existing.IsActive = question.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingQuestionChangedAsync(
            template.Id, template.Title, template.TenantId, question.Id);
        return TrainingQuestionOperationResult.Ok(question.Id);
    }

    public async Task<TrainingQuestionOperationResult> SaveOptionAsync(
        TrainingQuestionOption option, CancellationToken ct = default)
    {
        var questionsQuery = await templateAccess.ApplyQueryScopeAsync(
            db.TrainingQuestions.Include(q => q.TrainingTemplate), ct);
        var question = await questionsQuery
            .FirstOrDefaultAsync(q => q.Id == option.TrainingQuestionId, ct);
        if (question is null || !await templateAccess.CanEditAsync(question.TrainingTemplate, ct))
            return TrainingQuestionOperationResult.Fail(TrainingLabels.AccessDenied);

        if (string.IsNullOrWhiteSpace(option.AnswerText))
            return TrainingQuestionOperationResult.Fail("Antworttext ist erforderlich.");

        var userId = await currentUser.GetUserIdAsync();
        if (option.Id == 0)
        {
            option.TenantId = question.TenantId;
            option.CreatedByUserId = userId;
            option.CreatedAt = DateTime.UtcNow;
            db.TrainingQuestionOptions.Add(option);
        }
        else
        {
            var optionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestionOptions.AsQueryable(), ct);
            var existing = await optionsQuery
                .FirstOrDefaultAsync(o => o.Id == option.Id && o.TrainingQuestionId == option.TrainingQuestionId, ct);
            if (existing is null)
                return TrainingQuestionOperationResult.Fail("Antwortoption wurde nicht gefunden.");

            existing.SortOrder = option.SortOrder;
            existing.AnswerText = option.AnswerText.Trim();
            existing.IsCorrect = option.IsCorrect;
            existing.Explanation = option.Explanation?.Trim();
            existing.IsActive = option.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }

        question.TrainingTemplate.UpdatedAt = DateTime.UtcNow;
        question.TrainingTemplate.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);
        return TrainingQuestionOperationResult.Ok(option.Id);
    }

    public async Task<TrainingQuestionOperationResult> ReorderQuestionsAsync(
        int templateId, IReadOnlyList<int> questionIdsInOrder, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateForMutationAsync(templateId, ct);
        if (template is null)
            return TrainingQuestionOperationResult.Fail(TrainingLabels.AccessDenied);

        var query = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestions.AsQueryable(), ct);
        var questions = await query
            .Where(q => q.TrainingTemplateId == templateId && q.IsActive)
            .ToListAsync(ct);

        for (var i = 0; i < questionIdsInOrder.Count; i++)
        {
            var question = questions.FirstOrDefault(q => q.Id == questionIdsInOrder[i]);
            if (question is not null)
                question.SortOrder = i + 1;
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);
        return TrainingQuestionOperationResult.Ok(templateId);
    }

    public async Task<TrainingQuestionOperationResult> DeactivateQuestionAsync(
        int templateId, int questionId, CancellationToken ct = default)
    {
        var query = await templateAccess.ApplyQueryScopeAsync(
            db.TrainingQuestions
                .Include(q => q.TrainingTemplate)
                .Include(q => q.Options), ct);
        var question = await query
            .FirstOrDefaultAsync(q => q.Id == questionId && q.TrainingTemplateId == templateId && q.IsActive, ct);

        if (question is null || !await templateAccess.CanEditAsync(question.TrainingTemplate, ct))
            return TrainingQuestionOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        question.IsActive = false;
        question.UpdatedAt = DateTime.UtcNow;
        question.UpdatedByUserId = userId;

        foreach (var option in question.Options.Where(o => o.IsActive))
        {
            option.IsActive = false;
            option.UpdatedAt = DateTime.UtcNow;
            option.UpdatedByUserId = userId;
        }

        question.TrainingTemplate.UpdatedAt = DateTime.UtcNow;
        question.TrainingTemplate.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingQuestionChangedAsync(
            templateId, question.TrainingTemplate.Title, question.TrainingTemplate.TenantId, questionId);
        return TrainingQuestionOperationResult.Ok(questionId);
    }

    public async Task<TrainingTemplateValidationResult> ValidateQuizAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return TrainingTemplateValidationResult.Fail(["Schulungsvorlage wurde nicht gefunden."]);

        var errors = new List<string>();
        var questions = await GetQuestionsAsync(templateId, tenantId, ct);

        if (questions.Count == 0)
        {
            errors.Add("Mindestens eine aktive Quizfrage ist erforderlich.");
            return TrainingTemplateValidationResult.Fail(errors);
        }

        foreach (var question in questions)
        {
            var activeOptions = question.Options.Where(o => o.IsActive).ToList();
            if (activeOptions.Count < 2)
            {
                errors.Add($"Frage „{question.QuestionText}“ benötigt mindestens zwei aktive Antwortoptionen.");
                continue;
            }

            var correctCount = activeOptions.Count(o => o.IsCorrect);
            switch (question.QuestionType)
            {
                case TrainingQuestionType.SingleChoice when correctCount != 1:
                    errors.Add($"Single-Choice-Frage „{question.QuestionText}“ benötigt genau eine korrekte Antwort.");
                    break;
                case TrainingQuestionType.MultipleChoice when correctCount < 1:
                    errors.Add($"Multiple-Choice-Frage „{question.QuestionText}“ benötigt mindestens eine korrekte Antwort.");
                    break;
            }
        }

        return errors.Count == 0
            ? TrainingTemplateValidationResult.Ok()
            : TrainingTemplateValidationResult.Fail(errors);
    }
}

public readonly record struct TrainingQuestionOperationResult(bool Success, string? Message = null, int? Id = null)
{
    public static TrainingQuestionOperationResult Ok(int id) => new(true, null, id);
    public static TrainingQuestionOperationResult Fail(string message) => new(false, message);
}
