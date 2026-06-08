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

        if (value is not IEnumerable || value is string)
        {
            var type = value.GetType();
            if (type.IsPrimitive || value is decimal or DateTime or DateTimeOffset or Guid)
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
