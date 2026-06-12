using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services;

public readonly record struct DocumentLinkTarget(DocumentLinkedEntityType EntityType, int EntityId);

public sealed class DocumentLinkSelectionViewModel
{
    public DocumentLinkedEntityType EntityType { get; init; }
    public int EntityId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSelected { get; set; }
    public bool IsArchived { get; init; }
}

public sealed class DocumentLinkSummaryViewModel
{
    public int TotalCount { get; init; }
    public IReadOnlyDictionary<DocumentLinkedEntityType, int> CountsByType { get; init; }
        = new Dictionary<DocumentLinkedEntityType, int>();
    public string? SingleDisplayText { get; init; }
    public string CompactDisplayText { get; init; } = "—";
    public IReadOnlyList<string> DetailLines { get; init; } = [];
}

public static class DocumentLinkLabels
{
    public static string GetTypeLabelPlural(DocumentLinkedEntityType type) => type switch
    {
        DocumentLinkedEntityType.AuditRun => "Audit-Durchläufe",
        DocumentLinkedEntityType.Measure => "Maßnahmen",
        DocumentLinkedEntityType.ServiceProvider => "Dienstleister",
        DocumentLinkedEntityType.ProcessingActivity => "Verarbeitungstätigkeiten",
        DocumentLinkedEntityType.Dsfa => "DSFA",
        DocumentLinkedEntityType.PrivacyIncident => "Datenschutzvorfälle",
        DocumentLinkedEntityType.Tom => "TOMs",
        DocumentLinkedEntityType.DataSubjectRequest => "Betroffenenanfragen",
        _ => type.ToString()
    };

    public static string GetTypeLabelSingular(DocumentLinkedEntityType type) => type switch
    {
        DocumentLinkedEntityType.AuditRun => "Audit-Durchlauf",
        DocumentLinkedEntityType.Measure => "Maßnahme",
        DocumentLinkedEntityType.ServiceProvider => "Dienstleister",
        DocumentLinkedEntityType.ProcessingActivity => "Verarbeitungstätigkeit",
        DocumentLinkedEntityType.Dsfa => "DSFA",
        DocumentLinkedEntityType.PrivacyIncident => "Datenschutzvorfall",
        DocumentLinkedEntityType.Tom => "TOM",
        DocumentLinkedEntityType.DataSubjectRequest => "Betroffenenanfrage",
        _ => type.ToString()
    };

    public static string GetSelectButtonLabel(DocumentLinkedEntityType type) =>
        $"{GetTypeLabelPlural(type)} auswählen";

    public static string GetModalTitle(DocumentLinkedEntityType type) =>
        $"{GetTypeLabelPlural(type)} auswählen";

    public static string GetSearchPlaceholder(DocumentLinkedEntityType type) =>
        $"{GetTypeLabelPlural(type)} suchen…";

    /// <summary>Reihenfolge in Upload- und Bearbeiten-UI.</summary>
    public static IReadOnlyList<DocumentLinkedEntityType> UiTypes { get; } =
    [
        DocumentLinkedEntityType.ProcessingActivity,
        DocumentLinkedEntityType.Tom,
        DocumentLinkedEntityType.Measure,
        DocumentLinkedEntityType.AuditRun,
        DocumentLinkedEntityType.ServiceProvider,
        DocumentLinkedEntityType.Dsfa,
        DocumentLinkedEntityType.PrivacyIncident,
        DocumentLinkedEntityType.DataSubjectRequest,
        DocumentLinkedEntityType.Tom
    ];

    public static int GetTypeSortOrder(DocumentLinkedEntityType type)
    {
        for (var i = 0; i < UiTypes.Count; i++)
        {
            if (UiTypes[i] == type)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    public static IReadOnlyList<DocumentLinkedEntityType> AllTypes => UiTypes;

    public static string FormatTypeSelectionStatus(
        DocumentLinkedEntityType type,
        IEnumerable<DocumentLinkSelectionViewModel> selectedItems)
    {
        var items = selectedItems.Where(i => i.EntityType == type).ToList();
        if (items.Count == 0)
        {
            return "Keine ausgewählt";
        }

        if (items.Count == 1)
        {
            return items[0].DisplayName;
        }

        return $"{items.Count} ausgewählt";
    }
}
