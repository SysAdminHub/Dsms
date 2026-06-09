using System.Text.Json;

namespace Dsms.Web.Services.Logging;

/// <summary>Parst Änderungsdetails aus Logeinträgen (neues und altes Format).</summary>
public static class AuditLogChangeParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AuditLogChangesViewModel Parse(string? oldValuesJson, string? newValuesJson, string? metadataJson)
    {
        var fromMetadata = TryParseChangeListFromMetadata(metadataJson);
        if (fromMetadata.Count > 0)
        {
            return new AuditLogChangesViewModel(fromMetadata, isLegacyFormat: false);
        }

        var fromDicts = TryParseFromDictionaries(oldValuesJson, newValuesJson);
        if (fromDicts.Count > 0)
        {
            return new AuditLogChangesViewModel(fromDicts, isLegacyFormat: ContainsEmptyObjectValues(oldValuesJson, newValuesJson));
        }

        if (!string.IsNullOrWhiteSpace(oldValuesJson) || !string.IsNullOrWhiteSpace(newValuesJson))
        {
            return new AuditLogChangesViewModel([], isLegacyFormat: true, oldValuesJson, newValuesJson);
        }

        return new AuditLogChangesViewModel([], isLegacyFormat: false);
    }

    private static List<AuditFieldChangeDto> TryParseChangeListFromMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty(AuditDiffHelper.ChangeListMetadataKey, out var list)
                && !doc.RootElement.TryGetProperty("changeList", out list))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<AuditFieldChangeDto>>(list.GetRawText(), Options) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<AuditFieldChangeDto> TryParseFromDictionaries(string? oldJson, string? newJson)
    {
        var oldDict = ParseStringDictionary(oldJson);
        var newDict = ParseStringDictionary(newJson);
        if (oldDict.Count == 0 && newDict.Count == 0)
        {
            return [];
        }

        var keys = oldDict.Keys.Union(newDict.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        var changes = new List<AuditFieldChangeDto>();

        foreach (var key in keys)
        {
            oldDict.TryGetValue(key, out var oldVal);
            newDict.TryGetValue(key, out var newVal);

            if (IsEmptyObjectToken(oldVal) || IsEmptyObjectToken(newVal))
            {
                continue;
            }

            if (string.Equals(oldVal, newVal, StringComparison.Ordinal))
            {
                continue;
            }

            changes.Add(new AuditFieldChangeDto
            {
                FieldName = key,
                DisplayName = key,
                OldValue = oldVal,
                NewValue = newVal
            });
        }

        return changes;
    }

    private static Dictionary<string, string?> ParseStringDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = FormatJsonElement(prop.Value);
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? FormatJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => "Ja",
        JsonValueKind.False => "Nein",
        JsonValueKind.Object when element.EnumerateObject().Any() == false => null,
        JsonValueKind.Object => "(komplexer Wert)",
        _ => element.ToString()
    };

    private static bool IsEmptyObjectToken(string? value) =>
        string.Equals(value, "{}", StringComparison.Ordinal) || string.Equals(value, "(komplexer Wert)", StringComparison.Ordinal);

    private static bool ContainsEmptyObjectValues(string? oldJson, string? newJson) =>
        (oldJson?.Contains("{}", StringComparison.Ordinal) ?? false)
        || (newJson?.Contains("{}", StringComparison.Ordinal) ?? false);

    public static bool HasDisplayableMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return true;
            }

            return doc.RootElement.EnumerateObject().Any(p =>
                !string.Equals(p.Name, AuditDiffHelper.ChangeListMetadataKey, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(p.Name, "changeList", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return true;
        }
    }
}

public sealed class AuditLogChangesViewModel
{
    public AuditLogChangesViewModel(
        IReadOnlyList<AuditFieldChangeDto> changes,
        bool isLegacyFormat,
        string? legacyOldJson = null,
        string? legacyNewJson = null)
    {
        Changes = changes;
        IsLegacyFormat = isLegacyFormat;
        LegacyOldJson = legacyOldJson;
        LegacyNewJson = legacyNewJson;
    }

    public IReadOnlyList<AuditFieldChangeDto> Changes { get; }
    public bool IsLegacyFormat { get; }
    public bool HasStructuredChanges => Changes.Count > 0;
    public string? LegacyOldJson { get; }
    public string? LegacyNewJson { get; }
}
