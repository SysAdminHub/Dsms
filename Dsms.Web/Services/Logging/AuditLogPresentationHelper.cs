using System.Text.Json;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Hilfsfunktionen für Plattform-Protokoll: Modul-Zuordnung, Metadaten-Parsing und Redaktion sensibler Fachinhalte.
/// </summary>
public static class AuditLogPresentationHelper
{
    private static readonly HashSet<string> SensitiveBusinessEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProcessingActivity",
        "Dpia",
        "Tom",
        "Processor",
        "Measure",
        "Audit",
        "PrivacyIncident",
        "DataSubjectRequest",
        "Training",
        "TrainingTemplate",
        "TrainingParticipant",
        "EvidenceDocument",
        "DocumentCategory",
        "DataProtectionRole",
        "AuditTemplate",
        "AuditAnswer"
    };

    private static readonly IReadOnlyDictionary<string, string[]> ModuleEntityTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["VVT"] = ["ProcessingActivity"],
            ["DSFA"] = ["Dpia"],
            ["TOM"] = ["Tom"],
            ["Dienstleister"] = ["Processor"],
            ["Maßnahmen"] = ["Measure"],
            ["Audits"] = ["Audit", "AuditAnswer"],
            ["Auditvorlagen"] = ["AuditTemplate"],
            ["Datenschutzvorfälle"] = ["PrivacyIncident"],
            ["Betroffenenanfragen"] = ["DataSubjectRequest"],
            ["Dokumente"] = ["EvidenceDocument", "DocumentCategory"],
            ["Schulungen"] = ["Training", "TrainingTemplate", "TrainingParticipant"],
            ["Organisation"] = ["DataProtectionRole"],
            ["Support"] = ["SupportAccessGrant"],
            ["Plattform"] = ["License", "Tenant", "ApplicationUser", "SubscriptionPlan", "PendingSignup", "Route"]
        };

    public static string? ResolveModule(string? entityType, string? metadataJson)
    {
        var fromMetadata = TryReadMetadataString(metadataJson, "Module");
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        foreach (var (module, types) in ModuleEntityTypes)
        {
            if (types.Any(t => string.Equals(t, entityType, StringComparison.OrdinalIgnoreCase)))
            {
                return module;
            }
        }

        return null;
    }

    public static string? ResolveResult(string? metadataJson, string? description) =>
        TryReadMetadataString(metadataJson, "Result");

    public static bool ResolveIsSupportMode(string? metadataJson, string? description)
    {
        if (TryReadMetadataBool(metadataJson, "SupportMode") == true)
        {
            return true;
        }

        return description?.StartsWith("[Supportmodus]", StringComparison.Ordinal) == true;
    }

    public static int? ResolveSupportAccessGrantId(string? metadataJson)
    {
        if (TryReadMetadataInt(metadataJson, "SupportAccessGrantId") is int grantId)
        {
            return grantId;
        }

        return TryReadMetadataInt(metadataJson, "supportAccessGrantId");
    }

    public static string? RedactEntityNameForPlatform(string? entityType, string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return null;
        }

        if (entityType is not null && SensitiveBusinessEntityTypes.Contains(entityType))
        {
            return null;
        }

        return entityName;
    }

    public static IReadOnlyList<string> GetEntityTypesForModule(string? module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return [];
        }

        return ModuleEntityTypes.TryGetValue(module.Trim(), out var types)
            ? types
            : [];
    }

    public static IReadOnlyList<string> GetKnownModules() => ModuleEntityTypes.Keys.OrderBy(m => m).ToList();

    public static string ResolveDocumentLinkEntityLabel(DocumentLinkedEntityType entityType) =>
        DocumentLinkLabels.GetTypeLabelSingular(entityType);

    private static string? TryReadMetadataString(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.ToString()
                };
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool? TryReadMetadataBool(string? metadataJson, string propertyName)
    {
        var value = TryReadMetadataString(metadataJson, propertyName);
        if (value is null)
        {
            return null;
        }

        return bool.TryParse(value, out var parsed) ? parsed : null;
    }

    private static int? TryReadMetadataInt(string? metadataJson, string propertyName)
    {
        var value = TryReadMetadataString(metadataJson, propertyName);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
