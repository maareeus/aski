# 06 — Control Plane: interfacce

## Importante: niente più API REST amministrative

Le precedenti API REST (`/api/admin/*`, `/api/tenants`, `/api/billing`) sono state
**rimosse** per sicurezza: prendevano id dall'input (rischio cross-tenant) e duplicavano
l'UI. Tutte le funzioni sono ora nell'**UI MVC autenticata** del Control Plane.

L'unico endpoint HTTP pubblico rimasto è il **webhook Stripe**.

## Autenticazione

Cookie auth con due ruoli (`SuperAdmin`, `TenantOwner`). Policy di fallback
`RequireAuthenticatedUser`: ogni rotta richiede login, salvo `[AllowAnonymous]`
(login, registrazione, webhook).

- Super Admin seed: `admin@aski.local` / `ChangeMe123!` (override via `Seed:SuperAdminEmail` / `Seed:SuperAdminPassword`; obbligatorio in Production).
- Clienti: **registrazione self-service** su `/Account/Register` (crea org + owner).

## Pagine UI

| Area | Rotta | Ruolo | Funzione |
|------|-------|-------|----------|
| Login | `/Account/Login` | anonimo | accesso |
| Registrazione | `/Account/Register` | anonimo | nuovo cliente (org + owner) |
| Dashboard | `/Dashboard` | SuperAdmin | KPI piattaforma |
| Stripe | `/StripeAdmin` | SuperAdmin | modalità (Simulato/Test/Live) + chiavi |
| Piani | `/PlanAdmin` | SuperAdmin | crea/sincronizza piani |
| Server | `/ServerAdmin` | SuperAdmin | regioni, limite N, abilita |
| Clienti | `/AdminCustomers` | SuperAdmin | panoramica org (sola lettura) |
| Audit | `/AdminAudit` | SuperAdmin | registro operazioni |
| Portale cliente | `/Portal` | TenantOwner | progetti, attivazione piano, fatturazione |

Le mutazioni passano da form con antiforgery; login/registrazione hanno rate limiting.

## Webhook Stripe (unico endpoint API)

### `POST /api/stripe/webhook`
Pubblico ma autenticato dalla **firma** `Stripe-Signature` + idempotenza
(`ProcessedStripeEvents`). Usato solo in modalità `Test`/`Live`. Dettagli in
[03 — Stripe & Billing](03-stripe-billing.md).

## Flusso operativo (UI)

Vedi la guida passo-passo: [10 — Guida Super Admin](10-guida-super-admin.md) e
`docs/guida-admin.html`. In sintesi:

1. Super Admin: login → imposta **modalità Stripe** → (se Test/Live) chiavi + piani → server.
2. Cliente: registrazione → crea progetto → **Attiva** un piano.
3. Modalità `Simulated` → attivazione immediata; `Test`/`Live` → Stripe Checkout.
