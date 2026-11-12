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

    /// <summary>Content-type ammessi per le foto.</summary>
    public string[] AllowedContentTypes { get; set; } = ["image/jpeg", "image/png", "image/webp"];
}
