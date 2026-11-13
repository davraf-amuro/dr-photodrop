using System.Text.Json;

namespace dr_photodrop.api.Models;

/// <summary>Upload già validato e tipizzato, pronto per la scrittura su disco.</summary>
public record SavePhotoCommand(
    ScopeDefinition Scope,
    Guid PhotoId,
    Guid BatchId,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, JsonElement> Fields,
    string? OriginalFileName,
    string ContentType,
    Stream Content);
