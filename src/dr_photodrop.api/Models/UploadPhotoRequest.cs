namespace dr_photodrop.api.Models;

/// <summary>
/// Parti multipart grezze di un upload più l'ambito già risolto dal catalogo (<c>null</c> se sconosciuto).
/// Tutti i valori del client restano stringhe: li interpreta il validatore.
/// </summary>
public record UploadPhotoRequest(
    string? Scope,
    ScopeDefinition? ResolvedScope,
    string? PhotoId,
    string? BatchId,
    string? CapturedAt,
    string? Fields,
    bool HasFile,
    string? FileContentType,
    long FileLength,
    byte[] FileHeader);
