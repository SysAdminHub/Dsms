namespace Dsms.Web.Services.Email;

/// <summary>Ergebnis eines Email-Vorgangs (Versand, Speichern, Validierung).</summary>
public sealed class EmailOperationResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public string? DetailMessage { get; init; }
    public bool IsWarning { get; init; }

    public static EmailOperationResult Ok(string? message = null) =>
        new() { Succeeded = true, Message = message };

    public static EmailOperationResult Fail(string message, string? detail = null) =>
        new() { Succeeded = false, Message = message, DetailMessage = detail };

    public static EmailOperationResult Warn(string message, string? detail = null) =>
        new() { Succeeded = true, IsWarning = true, Message = message, DetailMessage = detail };
}
