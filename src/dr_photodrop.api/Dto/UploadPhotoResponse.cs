namespace dr_photodrop.api.Dto;

/// <summary>Risposta dell'upload: <see cref="Duplicate"/> è <c>true</c> se la foto era già stata ricevuta.</summary>
public record UploadPhotoResponse(Guid PhotoId, Guid BatchId, string Scope, bool Duplicate);
