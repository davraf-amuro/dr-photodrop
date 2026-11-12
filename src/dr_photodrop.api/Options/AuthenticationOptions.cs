namespace dr_photodrop.api.Options;

/// <summary>
/// Chiave API letta da <c>SimpleAuthenticationTools</c> nella sezione <c>Authentication:ApiKey</c>.
/// Il valore non sta mai in un file committato: arriva da <c>appsettings.local.json</c> o dalla variabile
/// d'ambiente <c>Authentication__ApiKey__ApiKeyValue</c> (<c>.env</c> per docker compose).
/// </summary>
public static class AuthenticationOptions
{
    public const string SectionName = "Authentication:ApiKey";

    /// <summary>Chiave di configurazione del valore della chiave API.</summary>
    public const string ApiKeyValueKey = $"{SectionName}:ApiKeyValue";

    /// <summary>Header HTTP che porta la chiave.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>Ferma l'avvio se la chiave manca: senza, l'API sarebbe inutilizzabile o, peggio, aperta per sbaglio.</summary>
    public static void EnsureApiKey(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration[ApiKeyValueKey]))
        {
            throw new InvalidOperationException(
                $"{ApiKeyValueKey} è obbligatorio: impostalo in appsettings.local.json o nella variabile d'ambiente Authentication__ApiKey__ApiKeyValue.");
        }
    }
}
