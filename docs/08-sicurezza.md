# 08 — Sicurezza

## Stato attuale

| Area | Stato | Note |
|------|-------|------|
| Segreti Stripe a riposo | ✅ cifrati | DataProtection ValueConverter |
| Firma webhook Stripe | ✅ verificata | `EventUtility.ConstructEvent` |
| Idempotenza webhook | ✅ | `ProcessedStripeEvents` |
| Auth istanza Ticketing | ✅ JWT + ruoli | BCrypt per le password |
| Auth Control Plane (admin/tenant) | ❌ **mancante** | da aggiungere prima del deploy |
| Iniezione SQL su nome DB | ✅ mitigata | identificatore validato/quotato |
| Segreti in git | ✅ esclusi | key-ring e appsettings.*.local ignorati |

## Cifratura dei segreti Stripe

`EncryptedConverter` (purpose `Aski.StripeSecrets.v1`) cifra `*SecretKey` e
`*WebhookSecret` a riposo. Il key-ring DataProtection è persistito su filesystem
(`keys/`, escluso da git).

- **Non cambiare il purpose**: invaliderebbe i dati cifrati esistenti.
- **Multi-istanza**: con più repliche del Control Plane il key-ring deve essere
  condiviso (volume comune, Redis, o un KMS). Altrimenti un'istanza non decifra ciò
  che ha cifrato un'altra.
- **Rotazione**: la rotazione delle chiavi DataProtection è gestita dal framework;
  i vecchi valori restano decifrabili finché la vecchia chiave è nel ring.

## Webhook

- La firma è verificata con il **webhook secret attivo** (Test o Live secondo `IsTestMode`).
- Il body viene letto raw (non deserializzato prima della verifica).
- In caso di errore di elaborazione l'evento non è marcato come processato → Stripe ritrasmette.
- **Da fare**: rendere il provisioning asincrono per garantire risposta `200` < 10s.

## JWT (istanza Ticketing)

- HMAC-SHA256 con `Jwt:Key` (≥ 32 caratteri). **Cambiare in produzione.**
- Claim: userId, email, ruolo, companyId. Scadenza configurabile (`ExpiryMinutes`).
- Validazione: issuer, audience, lifetime, signing key.
- Password con **BCrypt** (`BCrypt.Net.BCrypt.HashPassword/Verify`).
- L'admin iniziale è seedato da `Seed:AdminEmail/AdminPassword`: cambiarli al primo accesso.

## Autorizzazione per ruolo

- `[Authorize(Roles = "...")]` su rotte sensibili (status, management).
- Controlli di **ambito** programmaticisul singolo ticket (`CanAccessAsync`,
  `ApplyScopeAsync`) per Dev e Client, perché i ruoli non bastano (serve filtrare per
  azienda/software/assegnazione).

## Hardening prima del deploy (checklist)

- [ ] Aggiungere autenticazione al Control Plane:
  - Super Admin: account dedicati (es. Identity + policy `SuperAdmin`).
  - Tenant: login del portale + autorizzazione per `tenantId` (un tenant non deve
    vedere/operare su risorse di un altro).
- [ ] Forzare HTTPS e HSTS ovunque.
- [ ] Rate limiting su login e webhook.
- [ ] Segregare le credenziali Postgres per progetto (oggi si usa l'admin del pool):
  creare un utente DB dedicato con privilegi solo sul proprio database.
- [ ] `Jwt:Key` e segreti via variabili d'ambiente / secret manager, non in appsettings.
- [ ] Audit log delle operazioni amministrative (modifica chiavi, creazione piani, provisioning).
- [ ] CORS ristretto ai domini dei portali.
- [ ] Backup e retention dei database del pool (politica di cancellazione post-Canceled).

## Dati e privacy

- Suspend/Stop **non cancellano** mai i dati: i container vengono fermati.
- La cancellazione definitiva (`RemoveContainerAsync` con `removeVolumes:true`) va
  eseguita solo a fine retention, con processo tracciato.
