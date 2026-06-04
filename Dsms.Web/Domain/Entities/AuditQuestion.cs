namespace Dsms.Web.Domain.Entities;

/// <summary>Einzelne Prüffrage innerhalb einer <see cref="AuditTemplate"/>.</summary>
public class AuditQuestion : EntityBase
{
    public int AuditTemplateId { get; set; }
    public AuditTemplate AuditTemplate { get; set; } = null!;

    public int SortOrder { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsRequired { get; set; } = true;

    public ICollection<AuditAnswer> Answers { get; set; } = [];
}
