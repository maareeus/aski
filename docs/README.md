# Documentazione Aski

Indice della documentazione della piattaforma SaaS B2B di ticketing **Aski**.

| Doc | Contenuto |
|-----|-----------|
| [01 — Architettura](01-architettura.md) | Visione d'insieme, macro-aree, diagrammi di flusso |
| [02 — Setup e sviluppo](02-setup.md) | Prerequisiti, avvio locale, migration, configurazione |
| [03 — Stripe & Billing](03-stripe-billing.md) | Chiavi, piani, Checkout, Customer Portal, webhook, stati |
| [04 — Infrastruttura & Provisioning](04-infrastruttura-provisioning.md) | Provider, factory, Traefik, pool Postgres N-per-container |
| [05 — Modello dati](05-modello-dati.md) | Entità, relazioni, ERD dei due database |
| [06 — API Control Plane](06-api-control-plane.md) | Endpoint super-admin, tenant, billing, webhook |
| [07 — API Ticketing](07-api-ticketing.md) | Auth, ruoli, ticket, commenti |
| [08 — Sicurezza](08-sicurezza.md) | Cifratura segreti, firma webhook, JWT, hardening |
| [09 — Deployment](09-deployment.md) | Container, variabili d'ambiente, Traefik, scaling |

## Glossario rapido

- **Control Plane** — applicazione che gestisce clienti, billing e infrastruttura.
- **Super Admin** — proprietario del SaaS; configura Stripe, piani e server.
- **Tenant** — azienda cliente registrata sul Customer Portal.
- **Progetto** — singola istanza di ticketing isolata di un tenant.
- **Pool Postgres** — container Postgres condiviso da più progetti fino al limite *N*.
- **Provider** — implementazione di `IInfrastructureProvider` (VPS Docker o AWS ECS).
