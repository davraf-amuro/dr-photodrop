# dr-photodrop

App generica "cattura foto dal telefono, parcheggio sul PC": una PWA sul telefono scatta foto per **ambito** (componenti, alimenti, ...) e le invia a un'API sul PC, che le salva in una inbox su file system con un JSON affiancato. L'interpretazione delle foto è fuori da questo progetto (una skill per ambito).

**Stato:** scheletro. Nessuna funzionalità implementata; il piano è in [.ai/plans/2026-11-12-photo-inbox/plan.md](.ai/plans/2026-11-12-photo-inbox/plan.md) (in attesa di approvazione).

## Parti

- `src/dr_photodrop.api` — API di ricezione (.NET 10 Minimal API, chiave in header `X-Api-Key`).
- `fe/` — PWA (Vue 3 + Vite + `vite-plugin-pwa`).
- `scopes/` — un file JSON per ambito (nome, icona, campi, cartella): nuovo ambito = nuovo file, senza toccare il codice.
- `tests/dr_photodrop.tests` — test xUnit.

## Sviluppo

```
dotnet build dr_photodrop.slnx
dotnet test dr_photodrop.slnx
cd fe && npm install && npm run build
```

Per `dotnet run` crea `src/dr_photodrop.api/appsettings.local.json` (non committato) con `Authentication:ApiKey:ApiKeyValue`. Per docker compose copia `.env.example` in `.env`. I dati stanno fuori dal repository: `E:\Davide\VM\docker\dr-photodrop\inbox\`.

La PWA richiede HTTPS (service worker, installazione, coda offline): vedi i rischi nel piano.
