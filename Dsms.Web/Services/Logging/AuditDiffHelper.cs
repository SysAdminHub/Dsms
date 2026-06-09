using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Erzeugt lesbare Audit-Diffs für OldValuesJson/NewValuesJson.
/// Enums werden als String formatiert, nicht als leeres Objekt {}.
/// </summary>
public static class AuditDiffHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public const string ChangeListMetadataKey = "ChangeList";

    public static bool AddIfChanged(
        List<AuditFieldChangeDto> changes,
        string fieldName,
        string displayName,
        object? oldValue,
        object? newValue,
        Func<object?, string?>? format = null)
    {
        var oldFormatted = format is not null ? format(oldValue) : FormatAuditValue(oldValue);
        var newFormatted = format is not null ? format(newValue) : FormatAuditValue(newValue);

        if (string.Equals(oldFormatted, newFormatted, StringComparison.Ordinal))
        {
            return false;
        }

        changes.Add(new AuditFieldChangeDto
        {
            FieldName = fieldName,
            DisplayName = displayName,
            OldValue = oldFormatted,
            NewValue = newFormatted,
            FieldType = InferFieldType(oldValue ?? newValue)
        });

        return true;
    }

    public static bool AddLongTextChanged(
        List<AuditFieldChangeDto> changes,
        string fieldName,
        string displayName,
        string? oldValue,
        string? newValue)
    {
        var oldNorm = NormalizeText(oldValue);
        var newNorm = NormalizeText(newValue);

        if (string.Equals(oldNorm, newNorm, StringComparison.Ordinal))
        {
            return false;
        }

        changes.Add(new AuditFieldChangeDto
        {
            FieldName = fieldName,
            DisplayName = displayName,
            OldValue = "Text vorhanden",
            NewValue = "Text wurde geändert",
            FieldType = "LongText",
            IsSensitive = true
        });

        return true;
    }

    public static AuditDiffPayload? BuildPayload(IReadOnlyList<AuditFieldChangeDto> changes)
    {
        if (changes.Count == 0)
        {
            return null;
        }

        var oldDict = new Dictionary<string, string?>();
        var newDict = new Dictionary<string, string?>();

        foreach (var change in changes)
        {
            oldDict[change.FieldName] = change.OldValue;
            newDict[change.FieldName] = change.NewValue;
        }

        return new AuditDiffPayload(oldDict, newDict, new Dictionary<string, object?>
        {
            [ChangeListMetadataKey] = changes
        });
    }

    public static string? FormatAuditValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
            bool b => b ? "Ja" : "Nein",
            DateOnly d => d.ToString("dd.MM.yyyy"),
            DateTime dt => dt.ToString("dd.MM.yyyy HH:mm"),
            DateTimeOffset dto => dto.ToString("dd.MM.yyyy HH:mm"),
            Enum e => e.ToString(),
            Guid g => g.ToString(),
            int or long or short or byte or uint or ulong or ushort or sbyte or decimal or double or float
                => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            _ when value.GetType().IsEnum => value.ToString()!,
            _ => Convert.ToString(value)?.Trim()
        };
    }

    public static string SerializeDictionary(Dictionary<string, string?> dict) =>
        JsonSerializer.Serialize(dict, SerializerOptions);

    public static string SerializeMetadata(object metadata) =>
        LogJsonHelper.SerializeSafe(metadata) ?? "{}";

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? InferFieldType(object? value) => value switch
    {
        null => null,
        bool => "Boolean",
        DateOnly => "Date",
        DateTime => "DateTime",
        Enum => "Enum",
        string => "String",
        _ when value.GetType().IsEnum => "Enum",
        _ => value.GetType().Name
    };
}

public sealed record AuditDiffPayload(
    Dictionary<string, string?> OldValues,
    Dictionary<string, string?> NewValues,
    Dictionary<string, object?> Metadata);
