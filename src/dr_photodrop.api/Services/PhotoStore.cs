using System.Security.Cryptography;
using System.Text.Json;
using dr_photodrop.api.Helpers;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;
using Microsoft.Extensions.Options;

namespace dr_photodrop.api.Services;

/// <summary>
/// Layout: <c>&lt;RootPath&gt;/&lt;targetFolder&gt;/&lt;photoId&gt;.&lt;ext&gt;</c> + <c>&lt;photoId&gt;.json</c>.
/// Il nome file nasce da photoId (GUID) ed estensione dedotta dal tipo verificato: nulla di ciò che sceglie il client entra nel percorso.
/// </summary>
public class PhotoStore(IOptions<InboxOptions> options, TimeProvider timeProvider, ILogger<PhotoStore> logger) : IPhotoStore
{
    private static readonly JsonSerializerOptions _metadataOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // Lock a stripe: serializza due invii simultanei dello stesso photoId senza accumulare un lock per ogni id visto
    private readonly SemaphoreSlim[] _locks = Enumerable.Range(0, InboxConstants.LockStripes).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<SavePhotoResult> SaveAsync(SavePhotoCommand command, CancellationToken cancellationToken)
    {
        var extension = ImageSignature.ExtensionFor(command.ContentType)
            ?? throw new ArgumentException("Content-type non supportato.", nameof(command));

        var directory = ResolveDirectory(command.Scope);
        var baseName = command.PhotoId.ToString("D");
        var photoPath = Path.Combine(directory, baseName + extension);
        var jsonPath = Path.Combine(directory, baseName + InboxConstants.JsonExtension);

        var gate = _locks[(command.PhotoId.GetHashCode() & int.MaxValue) % _locks.Length];
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Il JSON è l'ultimo file scritto: se c'è, la foto è completa e l'invio è già stato ricevuto
            if (File.Exists(jsonPath))
            {
                var existing = new FileInfo(photoPath);
                logger.LogInformation("Foto duplicata ignorata: Scope={Scope} PhotoId={PhotoId}", command.Scope.Id, command.PhotoId);
                return new SavePhotoResult(true, existing.Exists ? existing.Length : 0);
            }

            Directory.CreateDirectory(directory);
            var photoTemp = photoPath + InboxConstants.TempExtension;
            var jsonTemp = jsonPath + InboxConstants.TempExtension;
            try
            {
                var (size, sha256) = await WritePhotoAsync(photoTemp, command.Content, cancellationToken);
                File.Move(photoTemp, photoPath, overwrite: true);

                var metadata = new PhotoMetadata(
                    command.PhotoId, command.BatchId, command.Scope.Id, command.Fields, command.CapturedAt,
                    timeProvider.GetUtcNow(), command.OriginalFileName, command.ContentType, size, sha256, InboxConstants.SchemaVersion);
                await using (var jsonStream = new FileStream(jsonTemp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(jsonStream, metadata, _metadataOptions, cancellationToken);
                }

                File.Move(jsonTemp, jsonPath, overwrite: false);
                logger.LogInformation(
                    "Foto salvata: Scope={Scope} PhotoId={PhotoId} BatchId={BatchId} SizeBytes={SizeBytes}",
                    command.Scope.Id, command.PhotoId, command.BatchId, size);
                return new SavePhotoResult(false, size);
            }
            catch
            {
                // Mai lasciare .tmp né una foto senza JSON dopo un errore o un annullamento
                TryDelete(photoTemp);
                TryDelete(jsonTemp);
                if (!File.Exists(jsonPath))
                {
                    TryDelete(photoPath);
                }

                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Cartella di destinazione, verificata dentro <c>RootPath</c> (difesa in profondità: il catalogo accetta già solo nomi sicuri).</summary>
    private string ResolveDirectory(ScopeDefinition scope)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        var directory = Path.GetFullPath(Path.Combine(root, scope.TargetFolder));
        var rootWithSeparator = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La cartella dell'ambito esce da RootPath.");
        }

        return directory;
    }

    private static async Task<(long Size, string Sha256)> WritePhotoAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, useAsync: true);
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
        }

        await target.FlushAsync(cancellationToken);
        return (total, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Pulizia best-effort: l'errore originale è quello da riportare
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
