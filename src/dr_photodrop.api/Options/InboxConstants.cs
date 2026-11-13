namespace dr_photodrop.api.Options;

/// <summary>Valori fissi del formato inbox e del contratto API: cambiarli significa cambiare il contratto, non la configurazione.</summary>
public static class InboxConstants
{
    /// <summary>Versione dello schema del JSON affiancato.</summary>
    public const int SchemaVersion = 1;

    public const string ApiGroupPrefix = "/api/v1";

    // Nomi delle parti multipart
    public const string FilePart = "file";
    public const string ScopePart = "scope";
    public const string PhotoIdPart = "photoId";
    public const string BatchIdPart = "batchId";
    public const string CapturedAtPart = "capturedAt";
    public const string FieldsPart = "fields";

    // Tipi di campo ammessi nei file di ambito
    public const string FieldTypeText = "text";
    public const string FieldTypeNumber = "number";
    public const string FieldTypeDate = "date";

    /// <summary>Formato di un campo <c>date</c>.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    public const string JsonExtension = ".json";
    public const string TempExtension = ".tmp";

    /// <summary>Sottocartelle gestite dalle skill consumatrici: non possono essere <c>targetFolder</c> di un ambito.</summary>
    public static readonly string[] ReservedFolders = ["processed", "review"];

    /// <summary>Quanti byte iniziali servono per riconoscere il formato dell'immagine.</summary>
    public const int HeaderLength = 16;

    /// <summary>Numero di lock a stripe per serializzare le scritture sullo stesso photoId.</summary>
    public const int LockStripes = 64;
}
