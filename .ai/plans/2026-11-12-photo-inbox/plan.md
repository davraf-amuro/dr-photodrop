# Piano: dr-photodrop — inbox di foto dal telefono al PC
Data: 2026-11-12
Stato: IN ATTESA DI APPROVAZIONE
Branch: plan/photo-inbox (proposto — la scelta finale spetta all'utente: branch dedicato oppure `main`)
Agenti paralleli: 2 (vedi "Parallelismo")

> Il piano è autosufficiente: una sessione nuova può eseguirlo leggendo solo questo file, `CLAUDE.md` e il codice del repository.
> Lo scheletro del progetto esiste già (Fase 0) ed è stato verificato: `dotnet build` e `dotnet test` verdi, `npm run build` della PWA verde. **Nessuna funzionalità è implementata.**

## Obiettivo

App generica "cattura foto dal telefono, parcheggio sul PC", indipendente dal dominio:

- una **PWA** (Android, Chrome, installabile) in cui si sceglie un **ambito**, si scattano una o più foto, si compilano i campi opzionali dell'ambito, si raggruppano le foto in un **lotto** e si invia; con **coda offline** (invio subito o più tardi);
- una **API di ricezione** su questo PC (Docker) che valida e salva ogni foto con un JSON affiancato in `E:\Davide\VM\docker\dr-photodrop\inbox\<ambito>\`;
- gli **ambiti sono configurazione** (un file JSON in `scopes/`), non codice;
- l'**interpretazione delle foto sta fuori** da questo progetto (una skill per ambito).

## Architettura

```
Telefono Android (Chrome, PWA installata)
  | IndexedDB: coda di lotti/foto  --(HTTPS, header X-Api-Key)-->
  v
nginx-proxy (C:\docker\nginx-proxy, rete Docker spectre-net)   <- HTTPS: da aggiungere (Fase 1)
  v
dr-photodrop-fe  (nginx + PWA statica; proxy /api/)
  v
dr-photodrop-api (.NET 10 Minimal API, porta interna 8080)
  |-- legge  /app/scopes/*.json            (bind mount ./scopes, sola lettura)
  '-- scrive /data/inbox/<ambito>/...      (bind mount E:\Davide\VM\docker\dr-photodrop\inbox)

Fuori dal progetto (lettura diretta della cartella, nessun MCP dedicato all'inizio):
  skill dr-partsbin-catalog-photos (repo dr-partsbin) --> MCP dr-partsbin (search_components / add_component / adjust_stock)
  sposta le foto elaborate in inbox\componenti\processed\, quelle incerte in inbox\componenti\review\
```

### Stack (stesso di dr-partsbin, versioni verificate sul PC)

| Parte | Scelta | Versione verificata |
|---|---|---|
| API | .NET 10 Minimal API, Serilog (console + file), `SimpleAuthenticationTools` (header `X-Api-Key`), validatori espliciti `IValidator<T>` | SDK 10.0.404, `global.json` rollForward latestMinor |
| PWA | Vue 3 + Vite 8 + TypeScript + `vite-plugin-pwa` 2.0.0 | Node v26.5.0 (engines `^22.18 \|\| >=24.12`) |
| Container | `dr-photodrop-api` e `dr-photodrop-fe` (nginx), rete esterna `spectre-net` | Docker 29.9.2 |
| Test | xUnit (`tests/dr_photodrop.tests`) | 2.9.3 |

Differenze rispetto a dr-partsbin: niente `core` project, niente EF/SQL Server, niente OpenAPI/Scalar/Asp.Versioning nello scheletro (l'API è piccola; il versionamento `api/v1` si decide in Fase 2), niente Pinia/router/Tailwind finché la PWA non ne ha bisogno, niente Graylog (aggiungibile come in dr-partsbin).

### Convenzioni di dominio (da rispettare nelle fasi)

- **Namespace** C# `dr_photodrop.api.*` (snake_case), classi PascalCase, primary constructors, `async`/`await` per ogni I/O, log strutturati con placeholder (mai interpolazione), ogni endpoint con body o upload usa un validatore esplicito (`Validators/`), nessun valore hardcoded (limiti in `InboxOptions`).
- **File di ambito** `scopes/<id>.json`: `id`, `name`, `icon`, `targetFolder`, `photoHints`, `fields[]` (`key`, `label`, `type` text|number|date, `required`, `default`, `min`, `maxLength`). Esempio già presente: `scopes/componenti.json` (quantità, ubicazione con default `Stanley-1`, nota). L'ambito `alimenti` (porzione, data) si aggiunge in Fase 2 come secondo file.
- **Layout inbox**: `inbox/<targetFolder>/<photoId>.<ext>` + `inbox/<targetFolder>/<photoId>.json`, più le sottocartelle `processed/` e `review/` gestite dalle skill. `photoId` è un GUID generato dal telefono: rende l'invio idempotente (un reinvio dalla coda non duplica).
- **JSON affiancato**: `photoId`, `batchId`, `scope`, `fields{}`, `capturedAt`, `receivedAt`, `originalFileName`, `contentType`, `sizeBytes`, `sha256`, `schemaVersion`. Scrittura atomica (file `.tmp` poi rename; il JSON per ultimo, così una skill che vede il JSON trova sempre la foto).
- **Endpoint** (versione da confermare in Fase 2): `GET /api/v1/scopes` (elenco ambiti con campi), `POST /api/v1/inbox/photos` (multipart: file + `scope`, `photoId`, `batchId`, `capturedAt`, `fields` JSON), `/health` anonimo.

## Disponibile sul PC vs alternativa

| Tema | Disponibile sul PC | Alternativa | Scelta e motivazione |
|---|---|---|---|
| Persistenza | SQL Server 2025 Enterprise Developer | File system + JSON affiancato | **File system.** Il dato è un parcheggio temporaneo che la skill consuma e sposta; non servono query né transazioni; la skill legge la cartella senza MCP né DB. Un DB (o SQLite) diventa utile solo se serviranno stato di elaborazione interrogabile, statistiche o più consumatori concorrenti. |
| Lettura delle foto | Ollama su RTX 4050 6 GB VRAM | Claude (la skill gira in Claude Code, che legge le immagini) | **Claude.** 6 GB di VRAM limitano a VLM piccoli, poco affidabili sulle etichette dei componenti (sigle, valori, OCR); l'errore costa catalogazioni sbagliate. Ollama resta un'opzione solo per un pre-filtro locale (es. scartare foto sfocate), non nel primo giro. |
| Raggiungibilità + HTTPS | `nginx-proxy` in `C:\docker\nginx-proxy` (oggi solo porta 80, HTTP) | Tailscale (`tailscale serve` + certificato reale) | **Decisione aperta, vedi Rischi.** Per la LAN serve aggiungere 443 + certificato a nginx-proxy; Tailscale dà HTTPS valido sul telefono senza installare una CA e funziona anche fuori casa, ma richiede l'app su telefono e PC. Da decidere con l'utente prima della Fase 1. |
| Client | PWA (Chrome Android) | App nativa (Kotlin/Flutter/MAUI) | **PWA.** Stesso stack di dr-partsbin, nessuno store né firma, installabile da Chrome, fotocamera via `<input capture>`, coda con IndexedDB. L'app nativa servirebbe solo per upload in background garantito a schermo spento: non necessario qui. |

## Perimetro negativo (vale per tutto il piano)

- Non modificare `E:\Davide\Progetti\dr-partsbin` (la skill `dr-partsbin-catalog-photos` vive lì ma si crea con un piano del *suo* repo, Fase 5).
- Non modificare `C:\docker\nginx-proxy` senza l'approvazione esplicita dell'utente alla Fase 1 (è un servizio condiviso con altri progetti: backup del `.conf` prima, `nginx -t` prima del reload).
- Nessun database, nessun MCP dedicato (evoluzioni future, vedi in fondo).
- Nessuna push e nessun remote finché l'utente non lo chiede.

## Parallelismo

Fase 2 (API) e Fase 3 (PWA) sono indipendenti dopo l'accordo sul contratto (sezione "Convenzioni di dominio"): file disgiunti (`src/`, `tests/` contro `fe/`). Fasi 1, 4, 5, 6 in serie. Solo l'orchestratore scrive questo file e committa.

## Fasi

### Fase 0: Scaffolding iniziale
- **Stato**: [x]
- **Dipende da**: —
- **Fatto**: repository git locale (`main`, nessun remote), `dr_photodrop.slnx`, `src/dr_photodrop.api` (health, auth con chiave, opzioni, interfaccia validatore), `tests/dr_photodrop.tests` (1 test), `fe/` (PWA Vue minimale + manifest + service worker generato), `Dockerfile`, `fe/Dockerfile` + `fe/nginx/default.conf.template`, `docker-compose.yml` (bozza), `.env.example`, `scopes/componenti.json`, `README.md`, `.gitignore`, `.dockerignore`.
- **Verifica passo**: `dotnet build dr_photodrop.slnx` 0 errori 0 avvisi; `dotnet test` 1/1; `cd fe && npm install && npm run build` verde.

### Fase 1: Infrastruttura locale, token e HTTPS
- **Stato**: [ ]
- **Dipende da**: decisione dell'utente su nginx-proxy vs Tailscale (Rischi 1-2)
- **Modello**: sessione
- **Precondizione**: piano approvato; stack non ancora avviato.
- **Operazioni**:
  1. Creare `E:\Davide\VM\docker\dr-photodrop\inbox\componenti\` (e `alimenti\` quando esiste l'ambito). Proporre il percorso all'utente prima di creare.
  2. Copiare `.env.example` in `.env` e generare il token (istruzione nel file); **mai** committarlo né riportarlo in chat/log.
  3. Percorso HTTPS scelto:
     - **nginx-proxy**: backup di `conf.d/spectre.conf`; aggiungere `server` su 443 con certificato (mkcert o CA locale, da installare sul telefono) e `server_name photodrop.spectre.local`, `proxy_pass` con variabile verso `dr-photodrop-fe:80` come negli altri blocchi; pubblicare 443 nel compose di nginx-proxy; far risolvere il nome dal telefono (DNS del router, non basta il file `hosts` del PC).
     - **Tailscale**: `tailscale serve` verso la porta del frontend, certificato `ts.net` valido; nessuna modifica a nginx-proxy.
- **Verifica passo**: la cartella esiste e il bind mount del compose punta lì; da Chrome Android, `https://<host>/healthz` risponde senza avvisi di certificato (con CA locale: dopo installazione della CA).
- **Su divergenza**: STOP e scrivere `Divergenza Fase 1: <cosa>`.

### Fase 2: API di ricezione
- **Stato**: [ ]
- **Dipende da**: Fase 0 (può girare in parallelo alla Fase 3)
- **Modello**: sessione
- **File**: `src/dr_photodrop.api/**`, `tests/dr_photodrop.tests/**`, `scopes/alimenti.json`
- **Azioni**:
  1. `ScopeCatalog` (servizio): legge `scopes/*.json` a ogni richiesta o con cache invalidata dal timestamp (nuovo ambito senza riavvio); valida il file di ambito all'avvio e logga quelli non validi senza fermare l'API.
  2. `GET /api/v1/scopes` (autenticato): elenco ambiti con campi, default e limiti.
  3. `UploadPhotoRequest` + `UploadPhotoRequestValidator` (`IValidator<T>`): ambito esistente, `photoId` GUID, `batchId` GUID, content-type in `AllowedContentTypes`, verifica dei magic bytes (non fidarsi del solo content-type), dimensione <= `MaxPhotoSizeMb`, campi conformi alla definizione dell'ambito (richiesti, tipo, `min`, `maxLength`), `capturedAt` plausibile. Errori in `ValidationProblem`.
  4. `POST /api/v1/inbox/photos`: multipart, `RequireAuthorization()`, limite di body esplicito (`DisableAntiforgery` motivato: API senza cookie), nome file mai preso dal client (si usa `photoId` + estensione dedotta dal tipo), scrittura atomica foto poi JSON, idempotenza su `photoId` (200 con stesso esito se già presente), path traversal impossibile (ambito e id validati, `Path.GetFullPath` verificato dentro `RootPath`).
  5. Log strutturati con placeholder (ambito, photoId, batchId, dimensione); nessun contenuto dei campi utente nei log oltre a quanto serve.
  6. Test xUnit: validatore (casi validi/non validi), scrittura su cartella temporanea, idempotenza, tentativo di path traversal, ambito sconosciuto; test di integrazione con `WebApplicationFactory<Program>` (401 senza chiave, 400 su file non immagine, 200 su foto valida).
- **Verifica passo**: `dotnet build` 0 avvisi; `dotnet test` verde; con `curl` locale (chiave di prova, cartella temporanea) il POST produce foto + JSON affiancato coerenti con la specifica.

### Fase 3: PWA
- **Stato**: [ ]
- **Dipende da**: Fase 0 (parallela alla Fase 2; il test end-to-end attende la Fase 2)
- **Modello**: sessione
- **File**: `fe/**`
- **Azioni**:
  1. Schermate: scelta ambito (da `GET /api/v1/scopes`, con cache locale per l'uso offline) -> scatto (`<input type="file" accept="image/*" capture="environment">`, più foto) -> campi dinamici generati dalla definizione dell'ambito (default `Stanley-1` per l'ubicazione dei componenti) -> raggruppa in lotto -> "Invia ora" / "Metti in coda".
  2. Ridimensionamento lato client (lato lungo max ~2000 px, JPEG qualità ~0.85) per tenere le foto sotto il limite e risparmiare dati; conservare l'originale solo se serve (decisione aperta, Rischi 4).
  3. Coda offline in IndexedDB (lotti, foto come Blob, stato per foto); invio sequenziale con retry con backoff esponenziale; `photoId` generato alla cattura; Background Sync dove disponibile + invio al ritorno online/apertura app; schermata "Coda" con stato e riprova.
  4. Token: richiesto una volta in una schermata impostazioni e salvato sul telefono (IndexedDB), inviato come `X-Api-Key`; mai nel bundle né nel repository. Lo schema esatto (token del telefono vs chiave iniettata dal proxy) è la decisione di Rischi 2.
  5. PWA: icone 192/512 + maskable, `theme_color`, `display: standalone`, service worker con precache dell'app shell e `registerType: autoUpdate`; nessuna cache delle risposte `/api/`.
  6. Lint e type-check (`npm run type-check`; aggiungere eslint/prettier allineati a dr-partsbin se si vuole il gate `npm run lint`).
- **Verifica passo**: `npm run build` verde; Lighthouse/Chrome DevTools "installabile"; in DevTools offline gli scatti restano in coda e partono al ritorno online (con API di prova).

### Fase 4: Deploy Docker e prova end-to-end
- **Stato**: [ ]
- **Dipende da**: Fasi 1, 2, 3
- **Modello**: sessione
- **File**: `docker-compose.yml`, `.env`, eventuale blocco nginx-proxy (Fase 1)
- **Azioni**: `docker compose build`, `docker compose up -d` (solo dopo approvazione esplicita dell'avvio dello stack); verificare `healthy` dei due container; prova reale da Chrome Android: installa la PWA, scatta foto di un componente con ambito `componenti`, invia un lotto con la rete attiva e uno in coda (rete spenta e poi riaccesa).
- **Verifica passo**: in `E:\Davide\VM\docker\dr-photodrop\inbox\componenti\` compaiono foto e JSON con tutti i campi (quantità, ubicazione `Stanley-1` di default, nota, `batchId` uguale tra le foto del lotto); reinvio dalla coda non crea duplicati; `docker compose ps` mostra entrambi i container sani.

### Fase 5: Skill di catalogazione `dr-partsbin-catalog-photos` (dipendenza esterna, fuori da questo repo)
- **Stato**: [ ]
- **Dipende da**: Fase 4 (formato inbox stabile)
- **Nota**: **non si crea qui.** Va in un piano del repository `E:\Davide\Progetti\dr-partsbin` (richiede le dr-guidelines di quel repo e il suo MCP). Contratto che questo progetto garantisce alla skill: layout e JSON affiancato descritti sopra. La skill legge `inbox\componenti\*.json`, usa l'MCP dr-partsbin (`search_components`, `add_component`, `adjust_stock`), sposta le foto elaborate in `processed\`, quelle incerte in `review\`. Per `alimenti` la skill e la destinazione dei dati sono ancora da decidere (Rischi 5).
- **Verifica passo**: una foto di prova nell'inbox viene catalogata (o finisce in `review\`) senza intervento manuale sul formato.

### Fase 6: Documentazione
- **Stato**: [ ]
- **Dipende da**: Fasi 2-4
- **File**: `README.md`, `docs/architettura.md` (nuovo), `docs/endpoint-*.md` (nuovi, uno per gruppo di endpoint)
- **Azioni**: documentare architettura, formato inbox e file di ambito, come aggiungere un ambito, deploy e HTTPS, installazione PWA; riallineare i documenti al codice come da `architecture-doc.instructions.md` (penultima fase, prima della verifica finale); revisione e footer come da `doc-versioning.instructions.md`. Su Claude Code si può usare `/dr-professor`.
- **Verifica passo**: ogni endpoint e campo documentato esiste nel codice; il README porta a tutti i documenti.

### Fase 7: Installazione dr-guidelines
- **Stato**: [x]
- **Fatto (2026-11-12)**: installati dal clone locale `E:\Davide\Progetti\dr-guidelines-workspace` (core, istruzioni, skill `.claude/skills` e `.agents/skills`, `CLAUDE.md`, `.editorconfig`, `.gitattributes`, `.gitignore`, catalogo, cartella `briefs/`): `dr-guidelines` (core), `dr-dotnet-backend` (dipendenza di `dr-minimalapi`; fornisce anche `Directory.Build.props`), `dr-minimalapi`, `dr-fe`. Il manifest è `.ai/dr-guidelines-packages.json`.
- **Non installati, e perché**: `dr-efdb` (nessun database), `dr-devops` (Docker Swarm/Portainer/GitLab CI: qui si usa solo docker compose), `dr-winsvc` (nessun Windows Service), `dr-ux-lcars` / `dr-ux-kitt` (temi grafici: nessun tema richiesto; si valutano con la PWA).
- **Modifiche locali dopo l'installazione**: `Directory.Build.props` (Authors/Company/Copyright = davraf, Product = dr-photodrop), `.gitignore` (aggiunte `.env` e `logs/`).
- **Verifica passo**: `.ai/dr-guidelines-packages.json` elenca i 4 pacchetti; `CLAUDE.md` contiene il marker `<!-- dr-guidelines -->`.

## Criteri di verifica finale

- [ ] `dotnet build dr_photodrop.slnx` senza avvisi e `dotnet test` verde
- [ ] `cd fe && npm run build` verde (type-check incluso)
- [ ] Gate di push (linee guida globali): `dotnet format dr_photodrop.slnx --verify-no-changes` exit 0 (`npm run lint` solo se aggiunto allo `package.json`)
- [ ] Un POST con token valido salva foto e JSON affiancato come da specifica; senza token 401; file non immagine o troppo grande 400; reinvio dello stesso `photoId` idempotente
- [ ] Nuovo ambito = nuovo file in `scopes/`, senza modifiche a codice (provato con `alimenti`)
- [ ] Dal telefono: PWA installata, lotto inviato subito e lotto inviato dalla coda dopo un periodo offline
- [ ] Nessun segreto nel repository (`git ls-files | grep -i env` mostra solo `.env.example`; nessun token in log o in `appsettings.json`)
- [ ] Documentazione allineata al codice (Fase 6)

## Rischi e punti aperti (richiedono decisione dell'utente)

1. **HTTPS e certificato in LAN.** Service worker, installazione PWA e coda offline richiedono un contesto sicuro (HTTPS, o `localhost`). Verificato in sola lettura: `nginx-proxy` ascolta **solo sulla porta 80** (`spectre.conf`, host `*.spectre.local`), quindi oggi non c'è HTTPS. Opzioni: (a) 443 su nginx-proxy con certificato di una CA locale (mkcert) da **installare sul telefono**; (b) Tailscale con certificato `ts.net` valido (nessuna CA da installare, funziona anche fuori casa). Va scelta prima della Fase 1; (a) modifica un servizio condiviso.
2. **Raggiungibilità dal telefono e autenticazione con token.** I nomi `*.spectre.local` oggi si risolvono dove c'è il `hosts` del PC: il telefono ha bisogno di DNS locale (router) o di Tailscale MagicDNS. Per il token ci sono due schemi: (A, usato da dr-partsbin) il proxy del frontend aggiunge `X-Api-Key`, quindi chiunque raggiunga il frontend può caricare foto; (B, scheletro attuale) il token vive sul telefono e viaggia nell'header: più sicuro, ma il token è in chiaro su IndexedDB del telefono e va inserito una volta. Proposta: B, con possibilità di revoca cambiando `.env`. Con HTTPS valido il token non è esposto in transito.
3. **Dimensione delle foto.** Le foto Android sono 3-10 MB; il limite iniziale (`MaxPhotoSizeMb` 10, `client_max_body_size` 25m) e il ridimensionamento lato client (Fase 3) vanno tarati sull'uso reale. Rischio disco: nessuna rotazione di `processed\`.
4. **Originale o ridotta?** Se le etichette hanno testo piccolo, ridurre a 2000 px può peggiorare la lettura da parte della skill: da verificare con foto vere prima di fissare il default.
5. **Ambito `alimenti`: destinazione dei dati ancora da decidere.** Il file di ambito e l'inbox funzionano comunque (campi: porzione, data); manca la skill/destinazione (un diario alimentare? un altro progetto?). Fino ad allora le foto restano in `inbox\alimenti\` senza consumatore.
6. **Coda offline e quota storage.** Molte foto in IndexedDB possono superare la quota di Chrome: mostrare l'occupazione, limitare i lotti in coda, chiedere `navigator.storage.persist()`.
7. **Privacy.** Le foto possono contenere altro oltre all'oggetto (sfondo, persone): l'inbox è locale al PC, nessun servizio esterno; Tailscale cifra il transito. Nessun upload verso servizi di terzi.
8. **Concorrenza tra API e skill.** Una skill che legge mentre l'API scrive: mitigata dalla scrittura atomica (JSON per ultimo); la skill sposta in `processed\` solo coppie complete.
9. **Il test `SkeletonTests`** nello scheletro serve solo a tenere verde la solution: sostituirlo in Fase 2.

## Evoluzioni future (non in questo piano)

- Database (SQLite o SQL Server) se servirà stato di elaborazione interrogabile.
- MCP dedicato `dr-photodrop` (elenco inbox, sposta in `processed\`/`review\`) al posto della lettura diretta della cartella.
- Pre-filtro locale con Ollama (qualità foto) se l'hardware lo permette.
- Altri ambiti (un file ciascuno in `scopes/`).
