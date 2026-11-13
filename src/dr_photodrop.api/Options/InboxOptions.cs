namespace dr_photodrop.api.Options;

/// <summary>Posizione di inbox e configurazioni degli ambiti, e limiti di upload (sezione <c>Inbox</c>).</summary>
public class InboxOptions
{
    public const string SectionName = "Inbox";

    /// <summary>Cartella radice dell'inbox: in container è il volume montato (<c>/data/inbox</c>).</summary>
    public string RootPath { get; set; } = "inbox";

    /// <summary>Cartella con un file JSON per ambito.</summary>
    public string ScopesPath { get; set; } = "scopes";

    /// <summary>Dimensione massima di una foto in MB.</summary>
    public int MaxPhotoSizeMb { get; set; } = 10;

    /// <summary>Spazio in KB, oltre la foto, concesso alle altre parti multipart (limite di body = foto + questo margine).</summary>
    public int MultipartOverheadKb { get; set; } = 64;

    /// <summary>Content-type ammessi per le foto.</summary>
    public string[] AllowedContentTypes { get; set; } = ["image/jpeg", "image/png", "image/webp"];

    /// <summary>Minuti di anticipo massimi tollerati su <c>capturedAt</c> rispetto all'orologio del server (scarti di orologio del telefono).</summary>
    public int CapturedAtMaxFutureMinutes { get; set; } = 5;

    /// <summary>Giorni di anzianità massimi di <c>capturedAt</c>: una coda offline può restare ferma a lungo, ma non all'infinito.</summary>
    public int CapturedAtMaxAgeDays { get; set; } = 365;

    /// <summary>Lunghezza massima del nome file originale conservato (solo informativo) nel JSON affiancato.</summary>
    public int OriginalFileNameMaxLength { get; set; } = 128;
}
