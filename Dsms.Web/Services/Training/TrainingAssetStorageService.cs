namespace Dsms.Web.Services.Training;

/// <summary>
/// Speichert Schulungsassets im Dateisystem unter <c>Data/training-assets/{tenant-or-global}/{templateId}/</c>.
/// Getrennt vom Dokumentenmodul; Dateien werden nicht öffentlich unter wwwroot abgelegt.
/// </summary>
public class TrainingAssetStorageService(IWebHostEnvironment environment, IConfiguration configuration)
{
    private readonly string _storageRoot = configuration["Storage:TrainingAssetPath"] ?? "Data/training-assets";

    public string StorageRoot => _storageRoot;

    public string GetStorageRootPath() =>
        Path.Combine(environment.ContentRootPath, _storageRoot);

    /// <summary>
    /// Speichert eine Datei und liefert den relativen Pfad für die DB (Forward-Slashes).
    /// </summary>
    public async Task<(string StoragePath, string StoredFileName)> SaveAsync(
        int? tenantId,
        int templateId,
        string fileName,
        Stream content,
        CancellationToken ct = default)
    {
        var scopeFolder = tenantId?.ToString() ?? "global";
        var templateFolder = Path.Combine(GetStorageRootPath(), scopeFolder, templateId.ToString());
        Directory.CreateDirectory(templateFolder);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(templateFolder, safeName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        var relativePath = Path.Combine(_storageRoot, scopeFolder, templateId.ToString(), safeName)
            .Replace('\\', '/');

        return (relativePath, safeName);
    }

    public string GetFullPath(string storagePath) =>
        Path.Combine(environment.ContentRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Kopiert eine Asset-Datei in eine neue Vorlage (z. B. beim Template-Kopieren).</summary>
    public async Task<(string StoragePath, string StoredFileName)?> CopyAsync(
        string sourceStoragePath,
        int? targetTenantId,
        int targetTemplateId,
        string originalFileName,
        CancellationToken ct = default)
    {
        var sourceFullPath = GetFullPath(sourceStoragePath);
        if (!File.Exists(sourceFullPath))
            return null;

        await using var sourceStream = File.OpenRead(sourceFullPath);
        return await SaveAsync(targetTenantId, targetTemplateId, originalFileName, sourceStream, ct);
    }
}
