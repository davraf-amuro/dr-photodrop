using System.Text.Json;

namespace dr_photodrop.api.Models;

/// <summary>JSON affiancato alla foto (<c>&lt;photoId&gt;.json</c>): è il contratto verso le skill consumatrici.</summary>
public record PhotoMetadata(
    Guid PhotoId,
    Guid BatchId,
    string Scope,
    IReadOnlyDictionary<string, JsonElement> Fields,
    DateTimeOffset CapturedAt,
    DateTimeOffset ReceivedAt,
    string? OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    int SchemaVersion);
