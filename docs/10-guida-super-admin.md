# 10 — Guida all'uso (Super Admin)

Guida operativa passo-passo dall'avvio del Control Plane alla prima istanza attiva.

## Premessa: due modalità di provisioning

Impostazione `Provisioning:Mode` in `src/Aski.ControlPlane/appsettings.json`:

| Modalità | Cosa serve | Cosa succede al provisioning |
|----------|-----------|-------------------------------|
| `Logging` (default) | niente | **Simulato**: il progetto passa a `Running` senza container reali. Ideale per provare i flussi. |
| `Docker` | VPS con Docker + Traefik raggiungibili | **Reale**: crea container Postgres + app, configura Traefik. |

Cosa serve **comunque** per il billing reale: un account **Stripe in modalità Test** (chiavi `sk_test`/`pk_test`) e, per i webhook in locale, la **Stripe CLI**.

> Puoi provare **provisioning** senza Stripe usando il pulsante **Provisiona** sul progetto.
> Puoi provare **billing** completo solo con chiavi Stripe valide + webhook.

---

## Percorso A — Prova rapida senza Stripe (modalità simulazione)

Obiettivo: vedere un progetto diventare `Running` senza account Stripe né VPS.

1. **Avvia l'ambiente**: `./scripts/start-env.ps1` → apri http://localhost:5080/Dashboard
2. **Aggiungi un server** (menu **Server**):
   - Nome: `EU Milano` · Regione: `it-mil-1` · Tipo: `VPS Docker`
   - Config JSON: lascia vuoto (in `Logging` non viene usato)
   - Limite N: `10` · **Abilitato**: sì → **Crea server**
3. **Registra un tenant** (menu **Tenant**):
   - Ragione sociale: `Acme Srl` · Email: `billing@acme.it` → **Registra tenant**
4. **Apri il tenant** → **Nuovo progetto**:
   - Nome: `Supporto` · Server: `EU Milano` · Sottodominio: `acme` → **Crea progetto**
5. Nella riga del progetto premi **Provisiona** → lo stato passa a **Running**.

Fatto: hai simulato l'intero ciclo senza dipendenze esterne.

---

## Percorso B — Billing completo con Stripe (sandbox)

### B.1 Procurati le chiavi Stripe (test)
1. Crea/usa un account su https://dashboard.stripe.com (resta in **Test mode**).
2. Vai su **Developers → API keys**: copia **Publishable key** (`pk_test_...`) e **Secret key** (`sk_test_...`).

### B.2 Avvia l'inoltro dei webhook
In un terminale separato (richiede [Stripe CLI](https://stripe.com/docs/stripe-cli)):

```powershell
stripe login
stripe listen --forward-to http://localhost:5080/api/stripe/webhook
```

Il comando stampa un **webhook signing secret** `whsec_...`: copialo.

### B.3 Configura Stripe nel Super Admin
Menu **Stripe** (http://localhost:5080/StripeAdmin):
- Modalità: lascia su **TEST** (toggle).
- Ambiente TEST:
  - Publishable key → `pk_test_...`
  - Secret key → `sk_test_...`
  - Webhook secret → `whsec_...` (dalla Stripe CLI)
- **Salva impostazioni**.

> I segreti sono cifrati a riposo. I campi password vuoti non sovrascrivono i valori già salvati.

### B.4 Crea un piano
Menu **Piani** (http://localhost:5080/PlanAdmin):
- Nome: `Pro` · Prezzo: `19.99` · Valuta: `eur` · Periodo: `Mensile` → **Crea e sincronizza**.

Se la sincronizzazione riesce vedrai un `price_...` accanto al piano (creato su Stripe).
Se fallisce, le chiavi Stripe non sono valide: ricontrolla la Secret key.

### B.5 Aggiungi un server e un tenant
Come nel Percorso A (passi 2–3).

### B.6 Crea progetto e acquista
1. Apri il tenant → **Nuovo progetto** (scegli il server, dai un sottodominio).
2. Nella sezione **Acquista un piano** premi **Checkout →**.
3. Si apre Stripe Checkout: paga con la carta di test **`4242 4242 4242 4242`**, data futura, CVC qualsiasi.
4. Stripe invia gli eventi al webhook → l'abbonamento diventa **Attivo** e parte il provisioning
   del progetto collegato (in `Logging` lo stato passa a `Running`).

### B.7 Gestione fatturazione
Nel dettaglio tenant, con un cliente Stripe esistente, **Gestisci fatturazione** apre lo
Stripe Customer Portal (cambio carta, disdetta). La disdetta genera
`customer.subscription.deleted` → l'abbonamento passa a **Cancellato** e i container
vengono fermati (dati conservati).

---

## Mappa dei menu (Super Admin)

| Menu | URL | A cosa serve |
|------|-----|--------------|
| Dashboard | `/Dashboard` | KPI: piani, server, tenant, progetti, abbonamenti attivi, modalità |
| Stripe | `/StripeAdmin` | Chiavi Test/Live + toggle sandbox |
| Piani | `/PlanAdmin` | Crea/sincronizza i listini con Stripe |
| Server | `/ServerAdmin` | Regioni, tipo provider, limite N, abilita/disabilita |
| Tenant | `/Portal` | Registra aziende; apri il dettaglio per progetti e billing |

---

## Config JSON del server (solo modalità `Docker`)

Quando `Provisioning:Mode=Docker`, il campo **Config JSON** del server deve descrivere
come raggiungere la VPS:

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

Prerequisiti sulla VPS: Docker con API esposta al Control Plane, Traefik in ascolto con
entrypoint `websecure` e certresolver. Dettagli in [09 — Deployment](09-deployment.md).

---

## Ordine consigliato (riepilogo)

```
1. Stripe (chiavi test)      ─┐
2. Piano (sync)               │  billing
3. Server (abilitato)        ─┼─ infrastruttura
4. Tenant (registra)         ─┐
5. Progetto (scegli server)   │  cliente
6. Checkout / Provisiona     ─┘
```

## Problemi comuni

| Sintomo | Causa / rimedio |
|---------|------------------|
| "Stripe non configurato" in dashboard | Vai su **Stripe** e salva le chiavi. |
| Piano creato ma senza `price_...` | Secret key Stripe errata/assente. |
| Checkout dà errore | Piano non sincronizzato o chiavi mancanti. |
| Pagato ma progetto non `Running` | `stripe listen` non attivo, o nessun progetto collegato all'abbonamento (collega/usa **Provisiona**). |
| Provisioning fallisce in `Docker` | `dockerHost`/Traefik non raggiungibili: verifica `ConfigJson`. |
