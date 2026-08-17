# AskiiPlatform

API .NET 10 (minimal API, versionata) per la gestione utenti e autenticazione JWT.

```
AskiiPlatform.Api/         API
├── Auth/                  configurazione autenticazione e policy JWT
├── Common/                helper, extension, eccezioni di dominio
├── Database/              AppDbContext, entità, configurazioni EF, migration
└── Features/              un file per endpoint, con i suoi model
    ├── Auth/              /auth/login + TokenService
    └── Users/             /user/create|activate|update|delete|changepassword
AskiiPlatform.Api.Tests/   suite xUnit su SQLite in-memory
```

## Configurazione dei segreti

I file di configurazione nel repo contengono **placeholder**, non valori reali.
Prima di avviare il progetto vanno impostati i segreti in locale, fuori dal
controllo di versione.

### Chiave di firma JWT

Deve essere lunga almeno 32 byte (HMAC-SHA256).

```bash
cd AskiiPlatform.Api && dotnet user-secrets init && dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

### Amministratore iniziale

Al primo avvio su un database vuoto, `DbInitializer` crea il super admin
leggendo la sezione `InitialAdmin`. Senza questi valori il database resta senza
utenti e l'avvio logga solo un warning.

```bash
cd AskiiPlatform.Api && dotnet user-secrets set "InitialAdmin:Email" "admin@example.com" && dotnet user-secrets set "InitialAdmin:Password" "una-password-robusta"
```

In produzione usare variabili d'ambiente (`Jwt__Key`, `InitialAdmin__Password`)
o un secret manager, mai i file `appsettings*.json`.

## Avvio

```bash
dotnet run --project AskiiPlatform.Api
```

Le migration vengono applicate automaticamente all'avvio. Documentazione
interattiva su `/scalar/v1`.

## Test

```bash
dotnet test AskiiPlatform.slnx
```

I test girano su SQLite in-memory e invocano direttamente gli `Impl` degli
endpoint. `KnownIssuesTests.cs` contiene test di *caratterizzazione*: descrivono
il comportamento attuale dei difetti ancora aperti, quindi sono verdi finché il
difetto è presente. Quando ne correggi uno, il test relativo diventa rosso: va
riscritto come verifica del comportamento corretto.
