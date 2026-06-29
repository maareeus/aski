# Aski Tickets — Backend API

Backend **solo API** per un sistema di assistenza/ticketing orientato al supporto software.
Tutta la gestione (utenti, software, clienti, ticket) vive qui; nessun pannello separato.

Stack: **.NET 10**, **ASP.NET Core Identity**, **EF Core + SQLite**, **JWT** (access + refresh).
Documentazione interattiva via **Swagger** in sviluppo.

## Stato

| Fase | Contenuto | Stato |
|------|-----------|-------|
| 1 | Autenticazione: Identity + JWT, ruoli, login/refresh/me, seed admin | ✅ |
| 2 | Gestione utenti, clienti (Company), software | ✅ |
| 3 | Ticket: apertura, assegnazione, stati, commenti | ✅ |

## Avvio

```powershell
cd backend
dotnet run
```

- API: `http://localhost:<porta>` (vedi `Properties/launchSettings.json`)
- Swagger UI (solo Development): `/swagger`
- Migrazioni e seed (ruoli + admin) vengono applicati all'avvio.
- DB SQLite: file `aski-tickets.db` (creato automaticamente, escluso da git).

### Migrazioni EF Core

```powershell
dotnet ef migrations add <Nome> --project backend/Aski.Tickets.Api.csproj --output-dir Data/Migrations
dotnet ef database update --project backend/Aski.Tickets.Api.csproj
```

## Ruoli

| Ruolo | Permessi |
|-------|----------|
| `Admin` | gestione totale: utenti, software, clienti, ticket |
| `Agent` | operatore: lavora i ticket di competenza |
| `Client` | cliente: apre e segue i ticket della propria azienda |

Admin iniziale (seed): **`admin@aski.local` / `ChangeMe123!`** (modificabili via
`Seed:AdminEmail` / `Seed:AdminPassword`; in Production cambiare e usare una `Jwt:Key` robusta).

## Autenticazione (JWT)

Login restituisce un **access token** (JWT, default 30 min) e un **refresh token**
(opaco, default 14 giorni). Le rotte protette richiedono header
`Authorization: Bearer <accessToken>`. Il refresh **ruota** il token: quello usato
viene revocato.

### Endpoint

| Metodo | Rotta | Auth | Descrizione |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | — | Login con email/password |
| POST | `/api/auth/refresh` | — | Nuovo access token da refresh (rotazione) |
| POST | `/api/auth/logout` | Bearer | Revoca un refresh token |
| GET | `/api/auth/me` | Bearer | Profilo dell'utente corrente |
| POST | `/api/auth/change-password` | Bearer | Cambio password |

### Esempi

```http
POST /api/auth/login
{ "email": "admin@aski.local", "password": "ChangeMe123!" }
```
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "x9f...",
  "expiresInSeconds": 1800,
  "user": { "id": "...", "email": "admin@aski.local", "fullName": "Administrator", "companyId": null, "roles": ["Admin"] }
}
```

```http
POST /api/auth/refresh
{ "refreshToken": "x9f..." }
```

```http
GET /api/auth/me
Authorization: Bearer eyJ...
```

## Gestione (Admin)

| Metodo | Rotta | Auth | Descrizione |
|--------|-------|------|-------------|
| GET/POST | `/api/companies` | Admin | Lista / crea azienda cliente |
| PUT | `/api/companies/{id}` | Admin | Aggiorna azienda |
| POST | `/api/companies/{id}/active/{bool}` | Admin | Attiva/disattiva |
| PUT | `/api/companies/{id}/software` | Admin | Imposta i software associati all'azienda |
| GET | `/api/software` | Bearer | Software attivi (Admin: tutti; Agent: assegnati; Client: della sua azienda) |
| POST/PUT | `/api/software[/{id}]` | Admin | Crea/aggiorna software (versione obbligatoria) |
| GET | `/api/users` | Admin | Lista utenti (anagrafica, ruoli, software) |
| POST | `/api/users` | Admin | Crea utente (anagrafica, `role`, `companyId` per Client, `softwareIds`) |
| PUT | `/api/users/{id}` | Admin | Aggiorna anagrafica |
| PUT | `/api/users/{id}/software` | Admin | Imposta i software di competenza (Agent) |
| POST | `/api/users/{id}/active/{bool}` | Admin | Attiva/disattiva utente |
| PUT | `/api/users/{id}/role` | Admin | Cambia ruolo |

**Modello:** anagrafica su utenti (nome/cognome/telefono) e aziende (P.IVA/telefono/indirizzo);
software **versionati**; Azienda↔Software e Utente↔Software molti-a-molti. L'**Agent** vede e
lavora solo i ticket dei software che gli sono assegnati (oltre a quelli assegnati direttamente a lui).

## Ticket

| Metodo | Rotta | Auth | Descrizione |
|--------|-------|------|-------------|
| GET | `/api/tickets?status=` | Bearer | Lista (staff: tutti; client: propria azienda) |
| GET | `/api/tickets/{id}` | Bearer | Dettaglio + commenti (client non vede note interne) |
| POST | `/api/tickets` | Client/Admin | Apre un ticket |
| PATCH | `/api/tickets/{id}/status` | Admin/Agent | Cambia stato |
| POST | `/api/tickets/{id}/assign` | Admin/Agent | Assegna a un agent |
| POST | `/api/tickets/{id}/close` | Bearer (proprio/staff) | Chiude |
| POST | `/api/tickets/{id}/comments` | Bearer | Commento (client: niente note interne; commento client su Resolved riapre) |

Stati ticket: `Open(0) InProgress(1) Waiting(2) Resolved(3) Closed(4)` · Priorità: `Low(0) Normal(1) High(2) Urgent(3)`.

## Configurazione

`appsettings.json`:

```json
{
  "ConnectionStrings": { "Default": "Data Source=aski-tickets.db" },
  "Jwt": { "Key": "...32+ caratteri...", "Issuer": "aski-tickets", "Audience": "aski-tickets",
           "AccessTokenMinutes": 30, "RefreshTokenDays": 14 },
  "Seed": { "AdminEmail": "admin@aski.local", "AdminPassword": "ChangeMe123!" }
}
```

In produzione fornire i segreti via variabili d'ambiente
(`Jwt__Key`, `Seed__AdminPassword`, `ConnectionStrings__Default`).

## Struttura

```
backend/
├── Domain/        # AppUser (Identity), Company, RefreshToken, Roles
├── Data/          # AppDbContext (IdentityDbContext), DbSeeder, migrations, design-time factory
├── Auth/          # JwtOptions, JwtTokenService, DTO
├── Controllers/   # AuthController
└── Program.cs     # Identity + JWT + Swagger + pipeline
```
