using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Idempotente Demo-/Globale Schulungsvorlagen für Entwicklung.</summary>
public static class TrainingTemplateSeeder
{
    private const string GlobalTemplateTitle = "Grundlagenschulung Datenschutz";

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await SeedGlobalTemplateAsync(db, ct);
    }

    private static async Task SeedGlobalTemplateAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.TrainingTemplates
                .IgnoreQueryFilters()
                .AnyAsync(t => t.IsGlobal && t.Title == GlobalTemplateTitle, ct))
        {
            return;
        }

        var template = new TrainingTemplate
        {
            TenantId = null,
            Title = GlobalTemplateTitle,
            Description = "Demo-Schulungsvorlage zu Datenschutzgrundlagen für alle Mandanten.",
            TrainingType = TrainingType.PrivacyBasics,
            TargetAudience = "Alle Mitarbeitenden",
            EstimatedDurationMinutes = 30,
            RecommendedRepeatAfterMonths = 12,
            PassingScorePercent = 80,
            IsQuizRequired = true,
            IsGlobal = true,
            IsCommunityTemplate = false,
            CommunityStatus = CommunityTemplateStatus.None,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        template.Sections.Add(new TrainingTemplateSection
        {
            SortOrder = 1,
            Title = "Was sind personenbezogene Daten?",
            ContentMarkdown = """
                # Was sind personenbezogene Daten?

                Personenbezogene Daten sind alle Informationen, die sich auf eine identifizierte oder identifizierbare natürliche Person beziehen.

                Beispiele:
                - Name
                - E-Mail-Adresse
                - Kundennummer
                """,
            IsActive = true
        });

        template.Sections.Add(new TrainingTemplateSection
        {
            SortOrder = 2,
            Title = "Datenschutz im Arbeitsalltag",
            ContentMarkdown = """
                # Datenschutz im Arbeitsalltag

                - Nur auf Daten zugreifen, die für die Aufgabe nötig sind.
                - Keine personenbezogenen Daten ungeschützt weitergeben.
                - Bildschirm sperren, wenn Sie den Arbeitsplatz verlassen.
                """,
            IsActive = true
        });

        template.Sections.Add(new TrainingTemplateSection
        {
            SortOrder = 3,
            Title = "Datenschutzvorfälle erkennen",
            ContentMarkdown = """
                # Datenschutzvorfälle erkennen

                Ein Datenschutzvorfall liegt vor, wenn personenbezogene Daten unbefugt offengelegt, verloren oder beschädigt wurden.

                Typische Anzeichen:
                - Verdächtige E-Mails mit Anhängen
                - Versehentlicher Versand an falsche Empfänger
                """,
            IsActive = true
        });

        template.Sections.Add(new TrainingTemplateSection
        {
            SortOrder = 4,
            Title = "Betroffenenrechte",
            ContentMarkdown = """
                # Betroffenenrechte

                Betroffene Personen haben Rechte nach der DSGVO, z. B. Auskunft, Berichtigung oder Löschung.

                Anfragen sollten strukturiert erfasst und fristgerecht bearbeitet werden.
                """,
            IsActive = true
        });

        template.Sections.Add(new TrainingTemplateSection
        {
            SortOrder = 5,
            Title = "Zusammenfassung",
            ContentMarkdown = """
                # Zusammenfassung

                Datenschutz ist Teil des täglichen Handelns. Bei Unsicherheit oder Vorfällen intern melden.
                """,
            IsActive = true
        });

        var question1 = new TrainingQuestion
        {
            SortOrder = 1,
            QuestionText = "Welche Angaben können personenbezogene Daten sein?",
            QuestionType = TrainingQuestionType.MultipleChoice,
            Explanation = "Personenbezogene Daten sind alle Informationen, die sich auf identifizierbare Personen beziehen.",
            Points = 1,
            IsRequired = true,
            IsActive = true
        };
        question1.Options.Add(new TrainingQuestionOption { SortOrder = 1, AnswerText = "Name", IsCorrect = true, IsActive = true });
        question1.Options.Add(new TrainingQuestionOption { SortOrder = 2, AnswerText = "E-Mail-Adresse", IsCorrect = true, IsActive = true });
        question1.Options.Add(new TrainingQuestionOption { SortOrder = 3, AnswerText = "Kundennummer", IsCorrect = true, IsActive = true });
        question1.Options.Add(new TrainingQuestionOption { SortOrder = 4, AnswerText = "Wetterbericht ohne Personenbezug", IsCorrect = false, IsActive = true });
        template.Questions.Add(question1);

        var question2 = new TrainingQuestion
        {
            SortOrder = 2,
            QuestionText = "Was sollte bei einem möglichen Datenschutzvorfall getan werden?",
            QuestionType = TrainingQuestionType.SingleChoice,
            Explanation = "Vorfälle sollten unverzüglich intern gemeldet werden.",
            Points = 1,
            IsRequired = true,
            IsActive = true
        };
        question2.Options.Add(new TrainingQuestionOption { SortOrder = 1, AnswerText = "Intern melden", IsCorrect = true, IsActive = true });
        question2.Options.Add(new TrainingQuestionOption { SortOrder = 2, AnswerText = "Ignorieren", IsCorrect = false, IsActive = true });
        question2.Options.Add(new TrainingQuestionOption { SortOrder = 3, AnswerText = "Öffentlich posten", IsCorrect = false, IsActive = true });
        template.Questions.Add(question2);

        db.TrainingTemplates.Add(template);
        await db.SaveChangesAsync(ct);
    }
}
