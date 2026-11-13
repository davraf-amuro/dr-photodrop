namespace dr_photodrop.api.Helpers;

/// <summary>Sanifica il nome file originale: viene conservato solo come informazione nel JSON, mai usato come percorso.</summary>
public static class FileNameSanitizer
{
    private static readonly char[] _separators = ['/', '\\'];

    /// <summary>Nome senza percorso né caratteri di controllo o non validi, troncato; <c>null</c> se resta vuoto.</summary>
    public static string? Sanitize(string? fileName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Solo l'ultimo segmento, qualunque separatore usi il client
        var name = fileName[(fileName.LastIndexOfAny(_separators) + 1)..];
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !char.IsControl(c) && !invalid.Contains(c)).ToArray()).Trim();
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned[..maxLength];
        }

        return cleaned.Length == 0 ? null : cleaned;
    }
}
