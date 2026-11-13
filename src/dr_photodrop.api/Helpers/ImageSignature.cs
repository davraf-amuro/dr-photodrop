namespace dr_photodrop.api.Helpers;

/// <summary>Riconoscimento del formato immagine dai magic bytes e estensione del file salvato: il content-type dichiarato dal client non basta.</summary>
public static class ImageSignature
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Webp = "image/webp";

    private static readonly byte[] _jpegmagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _pngmagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _riffmagic = "RIFF"u8.ToArray();
    private static readonly byte[] _webpmagic = "WEBP"u8.ToArray();

    // WEBP: "RIFF" + 4 byte di dimensione + "WEBP"
    private const int WebpTagOffset = 8;

    /// <summary>Content-type dedotto dai primi byte, oppure <c>null</c> se non è JPEG, PNG o WEBP.</summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(_jpegmagic))
        {
            return Jpeg;
        }

        if (header.StartsWith(_pngmagic))
        {
            return Png;
        }

        if (header.StartsWith(_riffmagic)
            && header.Length >= WebpTagOffset + _webpmagic.Length
            && header.Slice(WebpTagOffset, _webpmagic.Length).SequenceEqual(_webpmagic))
        {
            return Webp;
        }

        return null;
    }

    /// <summary>Estensione (con punto) per un content-type verificato, <c>null</c> se non supportato.</summary>
    public static string? ExtensionFor(string? contentType) => contentType switch
    {
        Jpeg => ".jpg",
        Png => ".png",
        Webp => ".webp",
        _ => null
    };
}
