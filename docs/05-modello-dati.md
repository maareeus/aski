# 05 — Modello dati

Due database distinti: **Control Plane** e **istanza Ticketing**.

## Database Control Plane

```mermaid
erDiagram
    StripeSettings ||..|| StripeSettings : "riga singola Id=1"
    Tenant ||--o{ Subscription : ha
    Tenant ||--o{ Project : ha
    Plan ||--o{ Subscription : tariffa
    Subscription |o--|| Project : finanzia
    Server ||--o{ DbContainer : ospita
    Server ||--o{ Project : regione
    DbContainer ||--o{ Project : assegna

    Tenant {
        int Id PK
        string CompanyName
        string BillingEmail
        string StripeCustomerId
    }
    Plan {
        int Id PK
        string Name
        string StripeProductId
        string StripePriceId
        long Amount
        string Currency
        int Period
        bool IsActive
    }
    Subscription {
        int Id PK
        int TenantId FK
        int PlanId FK
        string StripeSubscriptionId
        int Status
        datetime CurrentPeriodEndUtc
        bool CancelAtPeriodEnd
    }
    Project {
        int Id PK
        int TenantId FK
        int SubscriptionId FK
        int ServerId FK
        int DbContainerId FK
        string Name
        string Subdomain
        string CustomDomain
        string DatabaseName
        string AppContainerId
        int ProvisioningStatus
    }
    Server {
        int Id PK
        string Name
        string Region
        int Type
        string ConfigJson
        int MaxProjectsPerDbContainer
        bool IsEnabled
    }
    DbContainer {
        int Id PK
        int ServerId FK
        string ContainerName
        string Host
        int Port
        int CurrentProjectCount
        bool IsFull
        uint Version "xmin concurrency"
    }
    StripeSettings {
        int Id PK "=1"
        int Mode "Simulated/Test/Live"
        string TestSecretKey "cifrato"
        string LiveSecretKey "cifrato"
    }
    ProcessedStripeEvent {
        string EventId PK
        string EventType
        datetime ProcessedAtUtc
    }
```

### Enum (Aski.Shared)

| Enum | Valori |
|------|--------|
| `BillingPeriod` | `Monthly=0`, `Annual=1` |
| `SubscriptionStatus` | `Pending=0`, `Active=1`, `PastDue=2`, `Suspended=3`, `Canceled=4` |
| `ServerType` | `VpsDocker=0`, `AwsEcs=1` |
| `ProvisioningStatus` | `NotProvisioned=0`, `Provisioning=1`, `Running=2`, `Stopped=3`, `Failed=4` |
| `StripeMode` | `Simulated=0`, `Test=1`, `Live=2` |
| `PortalUserRole` | `SuperAdmin=0`, `TenantOwner=1` |

Altre tabelle del Control Plane: `PortalUser` (login: SuperAdmin/TenantOwner, password BCrypt),
`AuditLog` (operazioni sensibili), `Project.DbUser`/`DbPassword` (credenziali DB dedicate, password cifrata).

### Note di mapping

- `StripeSettings.Id` non auto-incrementa (riga singola, Id=1).
- Segreti Stripe cifrati via `EncryptedConverter` (DataProtection).
- `Plan.StripePriceId` indice unico (filtrato).
- `DbContainer.Version` = `xmin` (concurrency token Npgsql).
- `Subscription.StripeSubscriptionId` indice unico.
- `Project.Subdomain` indice unico; cascate: Tenant→(Subscription,Project) cascade,
  riferimenti a Server/DbContainer su delete `SetNull`.

## Database istanza Ticketing

```mermaid
erDiagram
    Company ||--o{ AppUser : appartiene
    Company ||--o{ Ticket : ha
    SoftwareProduct ||--o{ Ticket : riguarda
    AppUser ||--o{ DevAssignment : assegnato
    Company ||--o{ DevAssignment : ambito
    SoftwareProduct ||--o{ DevAssignment : ambito
    AppUser ||--o{ Ticket : "apre / lavora"
    Ticket ||--o{ TicketComment : commenti

    Company {
        int Id PK
        string Name
    }
    SoftwareProduct {
        int Id PK
        string Name
        string Description
    }
    AppUser {
        int Id PK
        string Email
        string PasswordHash
        int Role "Admin/Dev/Client"
        int CompanyId FK
        bool IsActive
    }
    DevAssignment {
        int Id PK
        int UserId FK
        int CompanyId FK
        int SoftwareId FK
    }
    Ticket {
        int Id PK
        string Title
        string Description
        int Status
        int Priority
        int CompanyId FK
        int SoftwareId FK
        int CreatedByUserId FK
        int AssignedDevUserId FK
        datetime ClosedAtUtc
    }
    TicketComment {
        int Id PK
        int TicketId FK
        int AuthorUserId FK
        string Body
        bool IsInternal
    }
```

### Enum (Aski.Ticketing.Api.Domain)

| Enum | Valori |
|------|--------|
| `TicketRole` | `Admin=0`, `Dev=1`, `Client=2` |
| `TicketStatus` | `Open=0`, `InProgress=1`, `Waiting=2`, `Resolved=3`, `Closed=4` |
| `TicketPriority` | `Low=0`, `Normal=1`, `High=2`, `Urgent=3` |

### Regole

- `AppUser.Email` indice unico; i Client devono avere `CompanyId`.
- `DevAssignment` con `CompanyId` e `SoftwareId` entrambi nulli = accesso totale (Dev senza vincoli).
- Indice unico `(UserId, CompanyId, SoftwareId)` per evitare assegnazioni duplicate.
- `Ticket` indice `(CompanyId, Status)` per le query filtrate per azienda.
- `TicketComment.IsInternal` = nota visibile solo a Dev/Admin.
