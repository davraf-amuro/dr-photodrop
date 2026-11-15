# dr-photodrop

App casalinga "cattura foto dal telefono, parcheggio sul PC": un'app Android scatta foto per **ambito** (componenti, alimenti, ...) e le invia a un'API sul PC di casa, che le salva in una inbox su file system con un JSON affiancato. L'interpretazione delle foto è fuori da questo progetto (una skill per ambito).

Uso solo nella rete di casa: HTTP senza HTTPS, riconoscimento app↔API con un token (`X-Api-Key`). Il PC può essere spento: l'app conserva le foto e le invia da sola quando il PC torna raggiungibile.

**Stato:** in sviluppo; piano in [.ai/plans/2026-11-12-photo-inbox/plan.md](.ai/plans/2026-11-12-photo-inbox/plan.md).

## Parti

- `src/dr_photodrop.api` — API di ricezione (.NET 10 Minimal API, chiave in header `X-Api-Key`).
- `src/dr-photodrop-android` — app Android (Kotlin), in arrivo.
- `scopes/` — un file JSON per ambito (nome, icona, campi, cartella): nuovo ambito = nuovo file, senza toccare il codice.
- `tests/dr_photodrop.tests` — test xUnit.

## Sviluppo

```
dotnet build dr_photodrop.slnx
dotnet test dr_photodrop.slnx
```

Per `dotnet run` crea `src/dr_photodrop.api/appsettings.local.json` (non committato) con `Authentication:ApiKey:ApiKeyValue`. Per docker compose copia `.env.example` in `.env`. I dati stanno fuori dal repository: `E:\Davide\VM\docker\dr-photodrop\inbox\`.
