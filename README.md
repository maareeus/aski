# Aski — SaaS B2B di Ticketing (Control Plane + Istanze)

Piattaforma SaaS multi-tenant ispirata al provisioning di Supabase, integrata con Stripe.
Tre macro-aree:

1. **Super Admin Control Plane** — gestione server, clienti, configurazione Stripe e piani.
2. **Tenant Control Plane (Customer Portal)** — registrazione aziende, acquisto piani via
   Stripe Checkout, gestione progetti (istanze di ticketing).
3. **Ticketing Application** — istanza single-tenant isolata (API C#), con ruoli Admin/Dev/Client.

## Struttura

```
Aski.slnx
└── src/
    ├── Aski.Shared/            # enum e contratti condivisi (Control Plane)
    ├── Aski.ControlPlane/      # API: Stripe, billing, webhook, provisioning infra
    └── Aski.Ticketing.Api/     # API isolata dell'istanza ticketing (ruoli + ticket)
```

## Componenti per fase

| Fase | Contenuto |
|------|-----------|
| 1 | Modelli core: `StripeSettings` (Test/Live + toggle), `Plan`, `Server` (limite N), `DbContainer` (xmin). Segreti cifrati a riposo (DataProtection). |
| 2 | `StripeService` (sync piani, Checkout, Customer Portal), webhook engine con verifica firma + idempotenza, macchina a stati abbonamento. |
| 3 | `IInfrastructureProvider` + factory, `VpsDockerProvider` (Docker.DotNet + label Traefik), pool Postgres N-per-container con concurrency token. |
| 4 | `TicketingDbContext` isolato, auth JWT, controller ticket/commenti con autorizzazione per ruolo. |

## Avvio rapido (script)

```powershell
# Avvia Postgres + migrazioni + Control Plane + istanza Ticketing
.\scripts\start-env.ps1            # -Fresh per ricreare il DB, -NoBrowser per non aprire il browser

# Ferma tutto
.\scripts\stop-env.ps1             # -RemoveDb per cancellare anche il container Postgres
```

- Control Plane → http://localhost:5080/Dashboard
- Ticketing API → http://localhost:5090 (login `admin@aski.local` / `ChangeMe123!`)

## Prerequisiti

- .NET 10 SDK
- PostgreSQL (es. via Docker)
- (Opzionale, per webhook reali) Stripe CLI
- (Opzionale, per provisioning reale) Docker + Traefik sul server VPS

```powershell
# Postgres locale per i test
docker run -d --name aski-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine
dotnet tool install --global dotnet-ef
```

## Avvio Control Plane

```powershell
# applica le migration (crea il DB aski_controlplane)
dotnet ef database update --project src/Aski.ControlPlane

dotnet run --project src/Aski.ControlPlane
```

`Provisioning:Mode` (appsettings) = `Logging` (default, niente Docker) oppure `Docker`
(richiede demone Docker raggiungibile via `Server.ConfigJson`).

## Avvio istanza Ticketing

```powershell
# crea il DB e l'admin iniziale automaticamente all'avvio (Seed)
dotnet run --project src/Aski.Ticketing.Api
# login: admin@aski.local / ChangeMe123!  (override in Seed:*)
```

## Test end-to-end del billing (sandbox Stripe)

1. Configura le chiavi Test nel Super Admin:
   ```http
   PUT /api/admin/stripe-settings
   { "isTestMode": true, "testSecretKey": "sk_test_...", "testWebhookSecret": "whsec_...", "testPublishableKey": "pk_test_..." }
   ```
2. Crea un piano (sincronizza Product+Price su Stripe):
   ```http
   POST /api/admin/plans
   { "name": "Pro", "amount": 1999, "currency": "eur", "period": 0 }
   ```
3. Crea un server abilitato:
   ```http
   POST /api/admin/servers
   { "name": "EU Milano", "region": "it-mil-1", "type": 0, "maxProjectsPerDbContainer": 10, "isEnabled": true }
   ```
4. Registra un tenant e un progetto, poi avvia il Checkout:
   ```http
   POST /api/tenants                      { "companyName": "Acme", "billingEmail": "a@acme.it" }
   POST /api/tenants/{id}/projects        { "name": "Support", "serverId": 1, "subdomain": "acme" }
   POST /api/billing/checkout             { "tenantId": 1, "planId": 1 }
   ```
5. Inoltra i webhook a localhost con la Stripe CLI:
   ```powershell
   stripe listen --forward-to https://localhost:5001/api/stripe/webhook
   ```
   Pagando con carta di test `4242 4242 4242 4242`, gli eventi `checkout.session.completed`,
   `invoice.paid`, `customer.subscription.*` pilotano lo stato dell'abbonamento e, di
   conseguenza, il provisioning/sospensione dei container del progetto.

## Note di sicurezza

- I segreti Stripe sono cifrati a riposo (DataProtection); il key-ring (`keys/`) è escluso da git.
- Endpoint Super Admin/Tenant NON ancora protetti da auth: aggiungere prima del deploy.
- L'istanza Ticketing usa JWT con ruoli; cambiare `Jwt:Key` e le credenziali `Seed:*` in produzione.

## Estensioni previste

- `AwsEcsProvider` (attualmente stub) per server `ServerType.AwsEcs`.
- Frontend Razor + shadcn per Customer Portal e istanza Ticketing.
- Provisioning asincrono via background worker (il webhook deve rispondere 200 < 10s).
