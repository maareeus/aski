# 10 — Guida all'uso (Super Admin)

Guida operativa passo-passo dall'avvio del Control Plane alla prima istanza attiva.

## Accesso

Il Control Plane richiede login. Super Admin iniziale (seed allo startup):

- **Email:** `admin@aski.local`
- **Password:** `ChangeMe123!`
- Override via `Seed:SuperAdminEmail` / `Seed:SuperAdminPassword` (obbligatorio in Production).

Login su `http://localhost:5080/Account/Login`. I **clienti non li crei tu**: si registrano
da soli su `/Account/Register` (crea automaticamente la loro org).

## Due impostazioni indipendenti

| Impostazione | Dove | Cosa controlla |
|--------------|------|----------------|
| **Modalità Stripe** (`Simulato`/`Test`/`Live`) | UI → **Stripe** | se l'acquisto del cliente passa da Stripe o è simulato |
| **Provisioning:Mode** (`Logging`/`Docker`) | `appsettings.json` | se i container sono reali (Docker) o simulati (Logging) |

Per provare tutto in locale senza dipendenze: **Stripe = Simulato** + **Provisioning = Logging** (default).

---

## Percorso A — Tutto simulato (nessuna Stripe, nessun Docker)

1. **Avvia**: `./scripts/start-env.ps1` → login su http://localhost:5080.
2. **Stripe** (`/StripeAdmin`): seleziona modalità **Simulato** → Salva (è già il default).
3. **Piani** (`/PlanAdmin`): crea `Pro`, `19.99`, `eur`, Mensile. In Simulato non serve sincronizzare con Stripe.
4. **Server** (`/ServerAdmin`): `EU Milano` / `it-mil-1`, Tipo VPS Docker, Config JSON **vuoto**, N `10`, Abilitato.
5. **Cliente** (browser **incognito**): `/Account/Register` → azienda + email + password → atterra su `/Portal`.
6. Nel portale: **Nuovo progetto** (scegli il server) → poi sulla riga del progetto scegli il piano e premi **Attiva**.
7. Il progetto passa a **Running** e l'abbonamento a **Attivo**. Nessuna Stripe coinvolta.

---

## Percorso B — Billing reale con Stripe (sandbox)

### B.1 Chiavi Stripe (test)
Da https://dashboard.stripe.com (Test mode) → **Developers → API keys**: copia
`pk_test_...` e `sk_test_...`.

### B.2 Webhook in locale
```powershell
stripe login
stripe listen --forward-to http://localhost:5080/api/stripe/webhook
```
Copia il `whsec_...` mostrato.

### B.3 Configura la modalità
Menu **Stripe**: seleziona **Test (sandbox)**, incolla Publishable/Secret/Webhook secret → Salva.

### B.4 Piani
Menu **Piani**: crea `Pro` → in Test/Live viene **sincronizzato** con Stripe (compare un `price_...`).

### B.5 Server
Come nel Percorso A (passo 4).

### B.6 Lato cliente
In incognito: registrazione → crea progetto → sceglie il piano e preme **Attiva** →
si apre **Stripe Checkout** → paga con `4242 4242 4242 4242` (data futura, CVC qualsiasi).
Il webhook attiva l'abbonamento e provisiona il progetto.

### B.7 Fatturazione
Nel portale, **Gestisci fatturazione** apre lo Stripe Customer Portal (cambio carta,
disdetta). La disdetta porta l'abbonamento a **Cancellato** e ferma i container (dati conservati).

---

## Mappa dei menu (Super Admin)

| Menu | URL | A cosa serve |
|------|-----|--------------|
| Dashboard | `/Dashboard` | KPI + modalità billing attiva |
| Stripe | `/StripeAdmin` | Modalità Simulato/Test/Live + chiavi |
| Piani | `/PlanAdmin` | Crea/sincronizza i listini |
| Server | `/ServerAdmin` | Regioni, limite N, abilita/disabilita |
| Clienti | `/AdminCustomers` | Panoramica org registrate (sola lettura) |
| Audit | `/AdminAudit` | Registro operazioni |

Il cliente usa solo `/Portal` (la sua area, isolata).

---

## Config JSON del server (solo `Provisioning:Mode=Docker`)

```json
{
  "dockerHost": "tcp://IP_VPS:2376",
  "network": "traefik",
  "appImage": "registry.example.com/aski-ticketing:latest",
  "postgresImage": "postgres:16-alpine",
  "certResolver": "le",
  "entrypoint": "websecure",
  "pgAdminUser": "postgres",
  "pgAdminPassword": "***",
  "domainSuffix": "aski.app"
}
```
Prerequisiti VPS: Docker raggiungibile + Traefik. Dettagli in [09 — Deployment](09-deployment.md).

---

## Problemi comuni

| Sintomo | Causa / rimedio |
|---------|------------------|
| Banner "modalità Test senza chiavi" | Inserisci le chiavi in **Stripe** o passa a Simulato. |
| Piano senza `price_...` in Test/Live | Secret key Stripe errata/assente. |
| In Simulato i piani non hanno price | Normale: in Simulato non si sincronizza con Stripe. |
| "Attiva" non compare sul progetto | Nessun piano disponibile, o progetto già attivo. |
| Pagato (Test) ma progetto non Running | `stripe listen` non attivo. |
| Provisioning fallisce in `Docker` | `dockerHost`/Traefik non raggiungibili: verifica `ConfigJson`. |
