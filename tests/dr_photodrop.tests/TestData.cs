using dr_photodrop.api.Models;
using dr_photodrop.api.Options;

namespace dr_photodrop.tests;

/// <summary>Dati di prova condivisi: ambito, immagini minime con magic bytes corretti, orologio fisso.</summary>
public static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05];

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    public static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8, 0x56, 0x50, 0x38, 0x20];

    public static readonly byte[] NotAnImage = "questo non e' un'immagine, solo testo"u8.ToArray();

    public static ScopeDefinition Scope(string id = "componenti", string folder = "componenti") => new(
        id, "Componenti", "cpu", folder, ["etichetta"],
        [
            new ScopeFieldDefinition("quantity", "Quantità", InboxConstants.FieldTypeNumber, false, null, 1, null),
            new ScopeFieldDefinition("location", "Ubicazione", InboxConstants.FieldTypeText, false, "Stanley-1", null, 8),
            new ScopeFieldDefinition("when", "Quando", InboxConstants.FieldTypeDate, false, null, null, null),
            new ScopeFieldDefinition("must", "Obbligatorio", InboxConstants.FieldTypeText, true, null, null, null)
        ]);

    public static UploadPhotoRequest ValidRequest(ScopeDefinition? scope = null, Action<UploadPhotoRequestBuilder>? tweak = null)
    {
        var builder = new UploadPhotoRequestBuilder(scope ?? Scope());
        tweak?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>Costruttore mutabile di richieste valide, per ritoccare una sola parte per test.</summary>
    public class UploadPhotoRequestBuilder(ScopeDefinition scope)
    {
        public ScopeDefinition? ResolvedScope { get; set; } = scope;
        public string? Scope { get; set; } = scope.Id;
        public string? PhotoId { get; set; } = Guid.NewGuid().ToString();
        public string? BatchId { get; set; } = Guid.NewGuid().ToString();
        public string? CapturedAt { get; set; } = "2026-11-13T12:30:00+01:00";
        public string? Fields { get; set; } = """{"must":"si","quantity":3}""";
        public bool HasFile { get; set; } = true;
        public string? ContentType { get; set; } = "image/jpeg";
        public long Length { get; set; } = Jpeg.Length;
        public byte[] Header { get; set; } = Jpeg;

        public UploadPhotoRequest Build() =>
            new(Scope, ResolvedScope, PhotoId, BatchId, CapturedAt, Fields, HasFile, ContentType, Length, Header);
    }
}
