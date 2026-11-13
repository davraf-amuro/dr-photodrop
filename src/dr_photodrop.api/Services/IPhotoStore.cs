using dr_photodrop.api.Models;

namespace dr_photodrop.api.Services;

/// <summary>Scrittura delle foto ricevute nell'inbox.</summary>
public interface IPhotoStore
{
    /// <summary>
    /// Salva foto e JSON affiancato (JSON per ultimo, entrambi con scrittura atomica). Se il photoId è già presente nell'ambito
    /// non riscrive nulla e restituisce <see cref="SavePhotoResult.Duplicate"/> a <c>true</c>.
    /// </summary>
    Task<SavePhotoResult> SaveAsync(SavePhotoCommand command, CancellationToken cancellationToken);
}
