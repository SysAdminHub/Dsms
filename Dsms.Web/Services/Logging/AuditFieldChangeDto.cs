namespace Dsms.Web.Services.Logging;

/// <summary>Einzelne Feldänderung für Audit-Diffs.</summary>
public sealed class AuditFieldChangeDto
{
    public string FieldName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? FieldType { get; init; }
    public bool IsSensitive { get; init; }
}
