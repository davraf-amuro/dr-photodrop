using Microsoft.AspNetCore.Mvc.Testing;

namespace dr_photodrop.tests;

/// <summary>
/// Avvia l'API in memoria con chiave di prova e cartelle temporanee. Le impostazioni passano da variabili d'ambiente:
/// Program.cs legge la chiave subito all'avvio, prima che la configurazione della factory sia applicata.
/// </summary>
public sealed class InboxApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "chiave-di-prova-solo-per-i-test";

    private static readonly string[] _environmentKeys =
        ["Authentication__ApiKey__ApiKeyValue", "Inbox__RootPath", "Inbox__ScopesPath"];

    private readonly string _workDir = Path.Combine(Path.GetTempPath(), "photodrop-api-" + Guid.NewGuid().ToString("N"));

    public string InboxPath => Path.Combine(_workDir, "inbox");

    public InboxApiFactory()
    {
        var scopes = Path.Combine(_workDir, "scopes");
        Directory.CreateDirectory(scopes);
        File.WriteAllText(Path.Combine(scopes, "componenti.json"), """
            { "id": "componenti", "name": "Componenti", "icon": "cpu", "targetFolder": "componenti", "photoHints": ["etichetta"],
              "fields": [ { "key": "quantity", "label": "Quantità", "type": "number", "min": 1 },
                          { "key": "location", "label": "Ubicazione", "type": "text", "default": "Stanley-1", "maxLength": 64 } ] }
            """);

        Environment.SetEnvironmentVariable("Authentication__ApiKey__ApiKeyValue", ApiKey);
        Environment.SetEnvironmentVariable("Inbox__RootPath", InboxPath);
        Environment.SetEnvironmentVariable("Inbox__ScopesPath", scopes);
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var key in _environmentKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, recursive: true);
        }
    }
}
