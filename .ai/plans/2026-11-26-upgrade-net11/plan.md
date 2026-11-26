# Piano: aggiornamento del progetto a .NET 11 (codice, test, Docker)
Data: 2026-11-26
Stato: COMPLETATO
Branch: main (nessuna scelta esplicita all'approvazione: nessun branch creato)

## Obiettivo
Portare API, test, SDK pin e immagini Docker da .NET 10 a .NET 11, con build, test, format e build Docker puliti.

## Scope
### File da modificare
- [x] `global.json` — SDK `11.0.100`, `rollForward: latestMinor` (pin stretto sulla major 11)
- [x] `Directory.Build.props` — `TargetFramework` `net11.0`, `LangVersion` `15.0`
- [x] `src/dr_photodrop.api/dr_photodrop.api.csproj` — `Microsoft.AspNetCore.OpenApi` 11.0.x; Serilog.AspNetCore a 11.x solo se pubblicato
- [x] `tests/dr_photodrop.tests/dr_photodrop.tests.csproj` — `Microsoft.AspNetCore.Mvc.Testing` 11.0.x
- [x] `Dockerfile` — `sdk:11.0` e `aspnet:11.0`
- [x] `README.md` — riga 11: ".NET 10 Minimal API" → ".NET 11 Minimal API"

### Perimetro negativo
- Non toccherò: file portati dai pacchetti dr-* (`.github/instructions/`, `.github/prompts/`, `.claude/skills/`, `.agents/skills/`), che parlano di .NET 10 e vengono sovrascritti da `/dr-get-latest`
- Non toccherò: codice applicativo in `src/` e `tests/` (salvo divergenza segnalata), `docker-compose.yml`, piano `2026-11-12-photo-inbox`
- Non disinstallerò l'SDK 10.0.404

## Fasi

### Fase 1: SDK pin
- **Stato**: [x]
- **Dipende da**: —
- **Modello**: sessione
- **Precondizione**: `dotnet --list-sdks` contiene `11.0.100`
- **File**: `global.json`
- **Operazione**: EDIT
- **Azione**: `version` → `11.0.100`, `rollForward` → `latestMinor`
- **Tool ammessi**: nessuno
- **Verifica passo**: `dotnet --version` nella radice restituisce `11.0.x`
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 1: <cosa>` in plan.md, non procedere

### Fase 2: Target framework e linguaggio
- **Stato**: [x]
- **Dipende da**: 1
- **Modello**: sessione
- **Precondizione**: Fase 1 [x]
- **File**: `Directory.Build.props`
- **Operazione**: EDIT
- **Azione**: `net10.0` → `net11.0`; `LangVersion` `14.0` → `15.0`
- **Tool ammessi**: nessuno
- **Verifica passo**: il file contiene `net11.0` e `15.0`; nessun `net10.0`
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 2: <cosa>` in plan.md, non procedere

### Fase 3: Pacchetti API
- **Stato**: [x] — OpenApi 11.0.0, Serilog.AspNetCore 11.0.0 (pubblicato)
- **Dipende da**: 2
- **Modello**: sessione
- **Precondizione**: Fase 2 [x]
- **File**: `src/dr_photodrop.api/dr_photodrop.api.csproj`
- **Operazione**: EDIT
- **Azione**: `Microsoft.AspNetCore.OpenApi` all'ultima 11.0.x stabile; `Serilog.AspNetCore` all'ultima 11.x stabile se esiste, altrimenti resta 10.0.0 (dichiararlo)
- **Tool ammessi**: `dotnet package search` (sola lettura)
- **Verifica passo**: `dotnet restore` ok, nessun warning NU1xxx
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 3: <cosa>` in plan.md, non procedere

### Fase 4: Pacchetti test
- **Stato**: [x]
- **Dipende da**: 2
- **Modello**: sessione
- **Precondizione**: Fase 2 [x]
- **File**: `tests/dr_photodrop.tests/dr_photodrop.tests.csproj`
- **Operazione**: EDIT
- **Azione**: `Microsoft.AspNetCore.Mvc.Testing` all'ultima 11.0.x stabile
- **Tool ammessi**: `dotnet package search` (sola lettura)
- **Verifica passo**: `dotnet restore` ok
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 4: <cosa>` in plan.md, non procedere

### Fase 5: Immagini Docker
- **Stato**: [x]
- **Dipende da**: 1
- **Modello**: sessione
- **Precondizione**: `docker manifest inspect mcr.microsoft.com/dotnet/sdk:11.0` e `.../aspnet:11.0` rispondono
- **File**: `Dockerfile`
- **Operazione**: EDIT
- **Azione**: `sdk:10.0` → `sdk:11.0`, `aspnet:10.0` → `aspnet:11.0`
- **Tool ammessi**: docker (sola lettura del registry)
- **Verifica passo**: nessun `10.0` nel Dockerfile
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 5: <cosa>` in plan.md, non procedere

### Fase 6: README
- **Stato**: [x]
- **Dipende da**: —
- **Modello**: sessione
- **Precondizione**: —
- **File**: `README.md`
- **Operazione**: EDIT
- **Azione**: riga 11 ".NET 10" → ".NET 11"
- **Tool ammessi**: nessuno
- **Verifica passo**: `grep -n "NET 10" README.md` vuoto
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 6: <cosa>` in plan.md, non procedere

### Fase 7: Verifica complessiva
- **Stato**: [x] — build 0 avvisi/0 errori, test 62/62, format exit 0, `docker build` exit 0; smoke run container: runtime 11.0.0, "Application started", GET / = 404 (nessun endpoint root)
- **Dipende da**: 1–6
- **Modello**: sessione
- **Precondizione**: fasi 1–6 [x]
- **File**: nessuno
- **Operazione**: —
- **Azione**: `dotnet build dr_photodrop.slnx`, `dotnet test dr_photodrop.slnx`, `dotnet format dr_photodrop.slnx --verify-no-changes`, `docker build .`
- **Tool ammessi**: dotnet, docker
- **Verifica passo**: tutti exit 0; build 0 errori; test tutti verdi (stesso numero di prima)
- **Su divergenza**: STOP — scrivi `⚠️ Divergenza Fase 7: <cosa>` in plan.md, non procedere

## Criteri di verifica finale
- [x] Nessun riferimento a `net10.0`, `sdk:10.0`, `aspnet:10.0` nei file di Scope
- [x] `dotnet build` 0 errori, nuovi warning dichiarati
- [x] `dotnet test` tutti verdi
- [x] `dotnet format --verify-no-changes` exit 0
- [x] `docker build .` exit 0

## Note
- .NET 11 è STS (24 mesi, fine supporto ~nov 2028), .NET 10 è LTS (fine supporto nov 2028): stessa scadenza, nessuna perdita di supporto.
- Rischio: `dr-dotnet-backend` porta un `global.json` con pin `10.0.100`; un `/dr-get-latest` potrebbe riportarlo indietro. Da verificare dopo il primo aggiornamento ed eventualmente segnalare con `/dr-file-feedback`.
