using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Antwort zu einer Vorlagenfrage innerhalb eines <see cref="AuditRun"/>.
/// Pro Durchlauf und Frage existiert höchstens ein Datensatz (Unique-Index im DbContext).
/// </summary>
public class AuditAnswer : EntityBase
{
    public int AuditRunId { get; set; }
    public AuditRun AuditRun { get; set; } = null!;

    public int AuditQuestionId { get; set; }
    public AuditQuestion AuditQuestion { get; set; } = null!;

    /// <summary>Snapshot des Fragentexts beim Auditstart – unabhängig von späteren Vorlagenänderungen.</summary>
    public string? QuestionText { get; set; }

    public int QuestionSortOrder { get; set; }
    public string? QuestionCategory { get; set; }
    public bool QuestionIsRequired { get; set; } = true;

    public string? AnswerText { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; } = ComplianceLevel.Open;
    public string? Notes { get; set; }
    public DateTime? AnsweredAt { get; set; }

    /// <summary>Verarbeitungstätigkeiten, denen diese Audit-Antwort zugeordnet ist.</summary>
    public ICollection<ProcessingActivityAuditAnswer> ProcessingActivityLinks { get; set; } = [];

    /// <summary>Maßnahmen, die aus dieser Audit-Antwort entstanden sind.</summary>
    public ICollection<Measure> Measures { get; set; } = [];
}
