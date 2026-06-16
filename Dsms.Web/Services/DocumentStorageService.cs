namespace Dsms.Web.Services;

/// <summary>
/// Speichert Nachweis-Dateien im Dateisystem unter <c>Data/Uploads/{tenantId}/</c>.
/// In der Datenbank wird nur der relative Pfad (<see cref="Domain.Entities.EvidenceDocument.StoragePath"/>) abgelegt.
/// </summary>
public class DocumentStorageService(IWebHostEnvironment environment, IConfiguration configuration)
{
    private readonly string _uploadRoot = configuration["Storage:UploadPath"] ?? "Data/Uploads";

    /// <summary>Relativer Upload-Pfad (für DB-Einträge und Konfiguration).</summary>
    public string UploadRoot => _uploadRoot;

    /// <summary>Absoluter Pfad zum Upload-Stammverzeichnis.</summary>
    public string GetUploadRootPath() =>
        Path.Combine(environment.ContentRootPath, _uploadRoot);

    /// <summary>
    /// Schreibt die Datei mandantenspezifisch und liefert den relativen Pfad für die DB (Forward-Slashes).
    /// GUID-Präfix verhindert Namenskollisionen und Path-Traversal im Originaldateinamen.
    /// </summary>
    public async Task<string> SaveAsync(int tenantId, string fileName, Stream content, CancellationToken ct = default)
    {
        var tenantFolder = Path.Combine(GetUploadRootPath(), tenantId.ToString());
        Directory.CreateDirectory(tenantFolder);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(tenantFolder, safeName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return Path.Combine(_uploadRoot, tenantId.ToString(), safeName).Replace('\\', '/');
    }

    /// <summary>Mappt einen in der DB gespeicherten relativen Pfad auf den absoluten Dateipfad.</summary>
    public string GetFullPath(string storagePath) =>
        Path.Combine(environment.ContentRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
}
