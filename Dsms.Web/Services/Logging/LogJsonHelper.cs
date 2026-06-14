using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Serialisiert Metadaten für Logeinträge und filtert sensible Eigenschaftsnamen.
/// </summary>
public static class LogJsonHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "Token",
        "AccessToken",
        "RefreshToken",
        "ApiKey",
        "ApiSecret",
        "Secret",
        "Cookie",
        "Session",
        "SecurityStamp",
        "ResetLink",
        "InvitationToken",
        "TwoFactorCode",
        "EncryptedSmtpPassword",
        "SmtpPassword"
    };

    public static string? SerializeSafe(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            var sanitized = SanitizeValue(value);
            return JsonSerializer.Serialize(sanitized, SerializerOptions);
        }
        catch
        {
            return "{\"serializationError\":\"Wert konnte nicht serialisiert werden.\"}";
        }
    }

    public static object? MergeMetadata(object? existing, object additional)
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (existing is not null)
        {
            foreach (var pair in ToDictionary(existing))
            {
                merged[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in ToDictionary(additional))
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }

    private static Dictionary<string, object?> ToDictionary(object value)
    {
        if (value is IDictionary dict)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = entry.Value;
                }
            }

            return result;
        }

        var sanitized = SanitizeValue(value);
        if (sanitized is Dictionary<string, object?> sanitizedDict)
        {
            return sanitizedDict;
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Value"] = sanitized
        };
    }

    private static object? SanitizeValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string s)
        {
            return s;
        }

        if (value is Enum)
        {
            return value.ToString()!;
        }

        var valueType = value.GetType();
        if (valueType.IsEnum)
        {
            return value.ToString()!;
        }

        var underlyingEnumType = Nullable.GetUnderlyingType(valueType);
        if (underlyingEnumType?.IsEnum == true)
        {
            return value.ToString()!;
        }

        if (value is bool b)
        {
            return b ? "Ja" : "Nein";
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToString("dd.MM.yyyy");
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("dd.MM.yyyy HH:mm");
        }

        if (value is not IEnumerable || value is string)
        {
            if (valueType.IsPrimitive || value is decimal or DateTimeOffset or Guid)
            {
                return value;
            }
        }

        if (value is IDictionary dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                result[key] = IsSensitiveName(key)
                    ? "[REDACTED]"
                    : SanitizeValue(entry.Value);
            }

            return result;
        }

        if (value is IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object?>().Select(SanitizeValue).ToList();
        }

        var props = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        var obj = new Dictionary<string, object?>();
        foreach (var prop in props)
        {
            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                propValue = "[UNREADABLE]";
            }

            obj[prop.Name] = IsSensitiveName(prop.Name)
                ? "[REDACTED]"
                : SanitizeValue(propValue);
        }

        return obj;
    }

    private static bool IsSensitiveName(string name) =>
        SensitivePropertyNames.Contains(name)
        || name.Contains("password", StringComparison.OrdinalIgnoreCase)
        || name.Contains("token", StringComparison.OrdinalIgnoreCase)
        || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || name.Contains("cookie", StringComparison.OrdinalIgnoreCase);
}
