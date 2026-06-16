# 01 — Architettura

## Visione d'insieme

Aski è una piattaforma SaaS B2B che vende software di ticketing in modalità
single-tenant: ogni cliente ottiene una propria istanza isolata (container app +
database dedicato). La fatturazione Stripe pilota direttamente il ciclo di vita
dell'infrastruttura, sul modello di provisioning di Supabase.

```mermaid
flowchart TB
    subgraph SA[Super Admin Control Plane]
        direction TB
        A1[Config Stripe Test/Live]
        A2[Piani d'abbonamento]
        A3[Server / Regioni]
    end

    subgraph TP[Tenant Control Plane - Customer Portal]
        direction TB
        B1[Registrazione azienda]
        B2[Acquisto piano - Stripe Checkout]
        B3[Gestione progetti]
        B4[Customer Portal Stripe]
    end

    subgraph INFRA[Infrastruttura provisionata]
        direction TB
        C1[Container App Ticketing]
        C2[(DB del progetto nel pool Postgres)]
    end

    STRIPE[(Stripe)]

    SA --> STRIPE
    TP --> STRIPE
    STRIPE -- webhook --> WH[Webhook Engine]
    WH --> PROV[Provisioning Coordinator]
    PROV --> INFRA
    B3 --> PROV
```

## Le tre macro-aree

### 1. Super Admin Control Plane
Pannello del proprietario del SaaS. Funzioni:
- inserimento chiavi Stripe (Secret, Publishable, Webhook Secret) con toggle **Test/Live**;
- creazione **piani** (prezzo, valuta, periodo) sincronizzati con i listini Stripe;
- gestione **server/regioni** e del limite *N* di progetti per container Postgres.

Progetto: `Aski.ControlPlane` (controller `AdminStripeController`, `AdminServersController`).

### 2. Tenant Control Plane (Customer Portal)
Portale dove le aziende:
- si registrano;
- scelgono un piano e pagano via **Stripe Checkout**;
- gestiscono i **progetti** (istanze ticketing) scegliendo solo il server/regione;
- gestiscono carta/disdetta via **Stripe Customer Portal**.

Progetto: `Aski.ControlPlane` (controller `TenantsController`, `BillingController`).

### 3. Ticketing Application (istanza del cliente)
Software di supporto isolato single-tenant. Backend API C# con tre ruoli
(Admin, Dev, Client). Database dedicato dentro il pool Postgres del server scelto.

Progetto: `Aski.Ticketing.Api`.

## Flusso di attivazione (happy path)

```mermaid
sequenceDiagram
    participant T as Tenant
    participant CP as Control Plane
    participant S as Stripe
    participant W as Webhook Engine
    participant I as Infrastruttura

    T->>CP: POST /api/billing/checkout
    CP->>S: Crea Checkout Session
    S-->>T: Redirect pagina pagamento
    T->>S: Paga (carta test 4242...)
    S-->>W: checkout.session.completed
    S-->>W: invoice.paid
    W->>W: Subscription -> Active
    W->>I: ProvisionAndStart(project)
    I->>I: Alloca DB nel pool (limite N)
    I->>I: Crea container app + label Traefik
    I-->>CP: ProvisioningStatus = Running
```

## Flusso di sospensione (insoluto / disdetta)

```mermaid
sequenceDiagram
    participant S as Stripe
    participant W as Webhook Engine
    participant I as Infrastruttura

    S-->>W: customer.subscription.updated (past_due)
    W->>W: Subscription -> PastDue
    W->>I: Suspend(project)
    I->>I: Stop SOLO container app (Postgres condiviso intatto)
    Note over I: I dati restano. Nessuna cancellazione.

    S-->>W: customer.subscription.deleted
    W->>W: Subscription -> Canceled
    W->>I: Stop(project) — dati conservati per retention
```

## Principi architetturali

1. **Billing come sorgente di verità**: lo stato dell'abbonamento determina lo stato
   dei container. Nessun avvio/spegnimento manuale fuori da questa logica.
2. **Configurazione Stripe a runtime**: chiavi e modalità Test/Live lette dal DB, non
   da file di configurazione → cambiabili senza redeploy.
3. **Idempotenza dei webhook**: ogni evento Stripe processato una sola volta.
4. **Isolamento dati per database fisico**: l'istanza ticketing non ha colonne di
   tenant-id; l'isolamento è il database dedicato nel pool.
5. **Astrazione infrastruttura**: `IInfrastructureProvider` disaccoppia la logica di
   business dal backend concreto (VPS Docker / AWS ECS).
6. **Sicurezza dei segreti**: chiavi Stripe cifrate a riposo; firma webhook verificata.

## Mappa dei progetti

```
Aski.slnx
└── src/
    ├── Aski.Shared/            # enum/contratti condivisi del Control Plane
    ├── Aski.ControlPlane/      # API Super Admin + Tenant Portal + billing + provisioning
    │   ├── Entities/           # StripeSettings, Plan, Server, DbContainer, Tenant, ...
    │   ├── Data/               # ControlPlaneDbContext + migrations
    │   ├── Services/Stripe/    # context provider, StripeService, webhook handler
    │   ├── Services/Infrastructure/  # provider, factory, modelli Docker/Traefik
    │   ├── Services/Provisioning/    # coordinatori (Docker / Logging)
    │   └── Controllers/        # admin, tenant, billing, webhook
    └── Aski.Ticketing.Api/     # API istanza single-tenant
        ├── Domain/             # entità + enum ticketing
        ├── Data/               # TicketingDbContext + migrations
        ├── Auth/               # JWT, claim, ruoli
        └── Controllers/        # auth, tickets, management
```
