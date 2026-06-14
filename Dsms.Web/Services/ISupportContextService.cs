namespace Dsms.Web.Services;

/// <summary>Session-Persistenz für den Supportmodus (Superuser + gültige Freigabe).</summary>
public interface ISupportContextService
{
    Task<int?> GetActiveGrantIdAsync();
    Task SetActiveGrantIdAsync(int grantId);
    Task ClearAsync();
    Task LoadFromSessionAsync();
}
