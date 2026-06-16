# 06 — API Control Plane

Base URL di sviluppo: `https://localhost:5001`.

> ⚠️ Questi endpoint **non sono ancora protetti** da autenticazione. Aggiungere auth
> Super Admin / Tenant prima del deploy (vedi [08 — Sicurezza](08-sicurezza.md)).

## Super Admin — Impostazioni Stripe

### `GET /api/admin/stripe-settings`
Vista mascherata (i segreti non vengono mai restituiti in chiaro).

```json
{ "configured": true, "isTestMode": true, "testSecretKeySet": true, "testWebhookSecretSet": true, "testPublishableKey": "pk_test_..." }
```

### `PUT /api/admin/stripe-settings`
Crea/aggiorna le chiavi. I campi null lasciano invariato il valore esistente.

```json
{
  "isTestMode": true,
  "testPublishableKey": "pk_test_...",
  "testSecretKey": "sk_test_...",
  "testWebhookSecret": "whsec_..."
}
```
→ `204 No Content`

### `POST /api/admin/stripe-settings/test-mode/{enabled}`
Toggle rapido Test/Live. Es. `.../test-mode/false` → `{ "isTestMode": false }`.

## Super Admin — Piani

### `POST /api/admin/plans`
Crea un piano e lo sincronizza con Stripe (Product + Price).

```json
{ "name": "Pro", "description": "Piano professionale", "amount": 1999, "currency": "eur", "period": 0 }
```
→ `{ "id": 1, "stripeProductId": "prod_...", "stripePriceId": "price_..." }`

`period`: `0` mensile, `1` annuale. `amount` in centesimi.

### `POST /api/admin/plans/{id}/sync`
Ri-sincronizza un piano esistente con Stripe.

## Super Admin — Server

### `GET /api/admin/servers`
Lista server con `Id, Name, Region, Type, MaxProjectsPerDbContainer, IsEnabled`.

### `POST /api/admin/servers`
```json
{
  "name": "EU Milano", "region": "it-mil-1", "type": 0,
  "hostname": "10.0.0.5",
  "configJson": "{\"dockerHost\":\"tcp://10.0.0.5:2376\",\"network\":\"traefik\",\"domainSuffix\":\"aski.app\"}",
  "maxProjectsPerDbContainer": 10, "isEnabled": true
}
```
`type`: `0` VpsDocker, `1` AwsEcs.

## Tenant — Customer Portal

### `POST /api/tenants`
```json
{ "companyName": "Acme Srl", "billingEmail": "billing@acme.it" }
```
→ `{ "id": 1 }`

### `GET /api/tenants/{id}`
Tenant con progetti e abbonamenti.

### `POST /api/tenants/{id}/projects`
```json
{ "name": "Supporto", "serverId": 1, "subdomain": "acme", "customDomain": "support.acme.com", "subscriptionId": null }
```
→ `{ "id": 1 }`

### `POST /api/tenants/projects/{projectId}/provision`
Trigger manuale di provisioning (per test, in produzione lo pilota il webhook). → `202 Accepted`

## Billing

### `POST /api/billing/checkout`
```json
{ "tenantId": 1, "planId": 1 }
```
→ `{ "url": "https://checkout.stripe.com/..." }` — reindirizzare il browser.

### `POST /api/billing/portal`
```json
{ "tenantId": 1 }
```
→ `{ "url": "https://billing.stripe.com/..." }`

## Webhook

### `POST /api/stripe/webhook`
Riceve gli eventi Stripe. Verifica firma `Stripe-Signature` + idempotenza. Vedi
[03 — Stripe & Billing](03-stripe-billing.md).

## Sequenza di test end-to-end

```http
PUT  /api/admin/stripe-settings      {... testSecretKey, testWebhookSecret ...}
POST /api/admin/plans                { "name":"Pro","amount":1999,"currency":"eur","period":0 }
POST /api/admin/servers              { "name":"EU","region":"it","type":0,"maxProjectsPerDbContainer":10,"isEnabled":true }
POST /api/tenants                    { "companyName":"Acme","billingEmail":"a@acme.it" }
POST /api/tenants/1/projects         { "name":"Supporto","serverId":1,"subdomain":"acme" }
POST /api/billing/checkout           { "tenantId":1,"planId":1 }   --> apri url, paga 4242...
```
Con `stripe listen` attivo, gli eventi attivano il provisioning → `ProvisioningStatus = Running`.
