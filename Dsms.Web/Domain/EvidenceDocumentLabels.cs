using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Nachweisdokumente.</summary>
public static class EvidenceDocumentLabels
{
    public static string GetTypeLabel(DocumentType type) => type switch
    {
        DocumentType.Evidence => "Nachweis",
        DocumentType.Policy => "Richtlinie",
        DocumentType.Form => "Formular",
        DocumentType.Contract => "Vertrag",
        DocumentType.CommunicationTemplate => "Kommunikationsvorlage",
        DocumentType.Other => "Sonstiges Dokument",
        _ => type.ToString()
    };

    public static string GetTypeBadgeVariant(DocumentType type) => type switch
    {
        DocumentType.Evidence => "doc-evidence",
        DocumentType.Policy => "doc-policy",
        DocumentType.Form => "doc-form",
        DocumentType.Contract => "doc-contract",
        DocumentType.CommunicationTemplate => "doc-template",
        DocumentType.Other => "doc-other",
        _ => "doc-other"
    };

    public static int GetTypeSortOrder(DocumentType type) => (int)type;

    public static IEnumerable<DocumentType> AllTypes { get; } = Enum.GetValues<DocumentType>();
}
