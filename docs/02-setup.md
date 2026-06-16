# 02 — Setup e sviluppo

## Prerequisiti

| Strumento | Versione | Note |
|-----------|----------|------|
| .NET SDK | 10.0+ | `dotnet --version` |
| PostgreSQL | 14+ | locale o via Docker |
| dotnet-ef | 10.0+ | `dotnet tool install --global dotnet-ef` |
| Docker | recente | solo per provisioning reale (`Provisioning:Mode=Docker`) |
| Stripe CLI | recente | solo per testare i webhook in locale |

## 1. Database PostgreSQL

```powershell
docker run -d --name aski-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine
```

Due database vengono usati:
- `aski_controlplane` — Control Plane (creato dalle migration).
- `aski_ticketing_dev` — istanza ticketing di sviluppo (creato all'avvio).

## 2. Control Plane

```powershell
dotnet ef database update --project src/Aski.ControlPlane
dotnet run --project src/Aski.ControlPlane
```

Configurazione (`src/Aski.ControlPlane/appsettings.json`):

```json
{
  "ConnectionStrings": { "ControlPlane": "Host=localhost;Port=5432;Database=aski_controlplane;Username=postgres;Password=postgres" },
  "DataProtection": { "KeyRingPath": "keys" },
  "Portal": { "BaseUrl": "https://localhost:5001" },
  "Provisioning": { "Mode": "Logging" }
}
```

- `Provisioning:Mode`
  - `Logging` (default) — nessun Docker; aggiorna solo lo stato. Ideale per testare il billing.
  - `Docker` — provisioning reale via `VpsDockerProvider`.
- `DataProtection:KeyRingPath` — cartella del key-ring che cifra i segreti Stripe. **Mai committare.**

## 3. Istanza Ticketing

```powershell
dotnet run --project src/Aski.Ticketing.Api
```

All'avvio applica le migration e crea l'admin iniziale se non esistono utenti.

Configurazione (`src/Aski.Ticketing.Api/appsettings.json`):

```json
{
  "ConnectionStrings": { "Tenant": "Host=localhost;Port=5432;Database=aski_ticketing_dev;Username=postgres;Password=postgres" },
  "Jwt": { "Key": "...32+ char...", "Issuer": "aski-ticketing", "Audience": "aski-ticketing", "ExpiryMinutes": 480 },
  "Seed": { "ApplyMigrations": true, "AdminEmail": "admin@aski.local", "AdminPassword": "ChangeMe123!" }
}
```

> In produzione la connection string `Tenant` viene iniettata dal Control Plane in fase
> di provisioning (variabile d'ambiente `ConnectionStrings__Tenant`), insieme a `Jwt__Key`.

## Build e verifica

```powershell
dotnet build Aski.slnx           # intera solution
dotnet build src/Aski.ControlPlane
dotnet build src/Aski.Ticketing.Api
```

## Migration EF Core

```powershell
# Control Plane
dotnet ef migrations add <Nome> --project src/Aski.ControlPlane --output-dir Data/Migrations
dotnet ef database update --project src/Aski.ControlPlane

# Ticketing
dotnet ef migrations add <Nome> --project src/Aski.Ticketing.Api --output-dir Data/Migrations
```

Entrambi i progetti hanno una `IDesignTimeDbContextFactory` per `dotnet ef`. La
connection string usata a design-time è configurabile via env:
- `ASKI_DB` (Control Plane)
- `ASKI_TENANT_DB` (Ticketing)

## Struttura cartelle generate (ignorate da git)

- `bin/`, `obj/` — output di build.
- `**/keys/` — key-ring DataProtection (segreti!).
