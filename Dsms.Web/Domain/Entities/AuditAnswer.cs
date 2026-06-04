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

    public string? AnswerText { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; } = ComplianceLevel.Open;
    public string? Notes { get; set; }
    public DateTime? AnsweredAt { get; set; }

    /// <summary>Verarbeitungstätigkeiten, denen diese Audit-Antwort zugeordnet ist.</summary>
    public ICollection<ProcessingActivityAuditAnswer> ProcessingActivityLinks { get; set; } = [];
}
