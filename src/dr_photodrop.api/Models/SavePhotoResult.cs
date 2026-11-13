namespace dr_photodrop.api.Models;

/// <summary>Esito della scrittura: <see cref="Duplicate"/> è <c>true</c> se il photoId era già presente.</summary>
public record SavePhotoResult(bool Duplicate, long SizeBytes);
