using dr_photodrop.api.Models;

namespace dr_photodrop.api.Services;

/// <summary>Catalogo degli ambiti letti da <c>scopes/*.json</c>.</summary>
public interface IScopeCatalog
{
    /// <summary>Ambiti validi correnti; un nuovo file in cartella compare senza riavvio.</summary>
    Task<IReadOnlyList<ScopeDefinition>> GetScopesAsync(CancellationToken cancellationToken);

    /// <summary>Ambito per id, <c>null</c> se non esiste (o l'id non ha la forma ammessa).</summary>
    Task<ScopeDefinition?> FindAsync(string? id, CancellationToken cancellationToken);
}
