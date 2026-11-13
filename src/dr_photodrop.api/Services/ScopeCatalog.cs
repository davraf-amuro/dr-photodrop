using System.Text.Json;
using System.Text.RegularExpressions;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;
using Microsoft.Extensions.Options;

namespace dr_photodrop.api.Services;

/// <summary>
/// Legge <c>ScopesPath/*.json</c> con cache invalidata dall'elenco dei file (nome, timestamp, dimensione):
/// un ambito nuovo o modificato è visibile alla richiesta successiva. I file non validi sono saltati con un warning, mai un crash.
/// </summary>
public partial class ScopeCatalog(IOptions<InboxOptions> options, ILogger<ScopeCatalog> logger) : IScopeCatalog
{
    // Id e cartella finiscono in percorsi su disco: solo caratteri sicuri (anti path traversal)
    [GeneratedRegex(@"^[a-z0-9][a-z0-9_-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeNameRegex();

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyRegex();

    private static readonly string[] _fieldTypes = [InboxConstants.FieldTypeText, InboxConstants.FieldTypeNumber, InboxConstants.FieldTypeDate];

    private static readonly JsonSerializerOptions _readOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private string? _signature;
    private IReadOnlyList<ScopeDefinition> _scopes = [];

    public async Task<IReadOnlyList<ScopeDefinition>> GetScopesAsync(CancellationToken cancellationToken)
    {
        var folder = options.Value.ScopesPath;
        var files = Directory.Exists(folder)
            ? new DirectoryInfo(folder).EnumerateFiles("*" + InboxConstants.JsonExtension).OrderBy(f => f.Name, StringComparer.Ordinal).ToList()
            : [];
        var signature = string.Join('|', files.Select(f => $"{f.Name}:{f.LastWriteTimeUtc.Ticks}:{f.Length}"));

        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            if (_signature == signature)
            {
                return _scopes;
            }

            if (!Directory.Exists(folder))
            {
                logger.LogWarning("Cartella degli ambiti non trovata: {ScopesPath}", folder);
            }

            _scopes = await LoadAsync(files, cancellationToken);
            _signature = signature;
            logger.LogInformation("Ambiti caricati: {ScopeCount} da {FileCount} file", _scopes.Count, files.Count);
            return _scopes;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public async Task<ScopeDefinition?> FindAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id) || !SafeNameRegex().IsMatch(id))
        {
            return null;
        }

        var scopes = await GetScopesAsync(cancellationToken);
        return scopes.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<ScopeDefinition>> LoadAsync(List<FileInfo> files, CancellationToken cancellationToken)
    {
        var result = new List<ScopeDefinition>();
        foreach (var file in files)
        {
            try
            {
                await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
                var raw = await JsonSerializer.DeserializeAsync<ScopeFile>(stream, _readOptions, cancellationToken);
                var error = TryBuild(raw, result, out var scope);
                if (error is null)
                {
                    result.Add(scope!);
                }
                else
                {
                    logger.LogWarning("File di ambito {FileName} scartato: {Reason}", file.Name, error);
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "File di ambito {FileName} non leggibile, scartato", file.Name);
            }
        }

        return result;
    }

    /// <summary>Valida il file grezzo; restituisce il motivo dello scarto oppure <c>null</c> con l'ambito costruito.</summary>
    private static string? TryBuild(ScopeFile? raw, List<ScopeDefinition> existing, out ScopeDefinition? scope)
    {
        scope = null;
        if (raw is null)
        {
            return "contenuto vuoto";
        }

        if (raw.Id is null || !SafeNameRegex().IsMatch(raw.Id))
        {
            return "id mancante o con caratteri non ammessi";
        }

        if (existing.Any(s => s.Id == raw.Id))
        {
            return "id duplicato";
        }

        if (string.IsNullOrWhiteSpace(raw.Name))
        {
            return "name mancante";
        }

        if (raw.TargetFolder is null || !SafeNameRegex().IsMatch(raw.TargetFolder)
            || InboxConstants.ReservedFolders.Contains(raw.TargetFolder))
        {
            return "targetFolder mancante, non ammesso o riservato";
        }

        var fields = new List<ScopeFieldDefinition>();
        foreach (var field in raw.Fields ?? [])
        {
            if (field.Key is null || !FieldKeyRegex().IsMatch(field.Key) || fields.Any(f => f.Key == field.Key))
            {
                return "chiave di un campo mancante, non ammessa o duplicata";
            }

            if (field.Type is null || !_fieldTypes.Contains(field.Type))
            {
                return "tipo di un campo non ammesso";
            }

            if (string.IsNullOrWhiteSpace(field.Label))
            {
                return "label di un campo mancante";
            }

            fields.Add(new ScopeFieldDefinition(field.Key, field.Label, field.Type, field.Required, field.Default, field.Min, field.MaxLength));
        }

        scope = new ScopeDefinition(raw.Id, raw.Name, raw.Icon ?? string.Empty, raw.TargetFolder, raw.PhotoHints ?? [], fields);
        return null;
    }
}
