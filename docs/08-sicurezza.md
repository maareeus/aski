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

## Hardening — stato

- [x] Autenticazione Control Plane: cookie auth, registrazione self-service del Tenant
  (crea org + owner), policy `SuperAdmin`/`Tenant`, portale cliente scoped al `tenantId`.
- [x] **FallbackPolicy** `RequireAuthenticatedUser` su entrambe le app: default-deny,
  solo `[AllowAnonymous]` espliciti (login/registrazione, webhook firmato).
- [x] HSTS fuori da sviluppo + HTTPS redirection.
- [x] Rate limiting: login/registrazione 10/min per IP, webhook 120/min.
- [x] Credenziali Postgres **dedicate per progetto** (ruolo owner del solo DB, niente
  admin del pool nell'app; password generata e cifrata a riposo).
- [x] Guard di produzione (fail-fast all'avvio): vietate credenziali DB di default,
  password seed di default e chiave JWT di sviluppo.
- [ ] Audit log delle operazioni amministrative (modifica chiavi, piani, provisioning).
- [ ] CORS ristretto ai domini dei portali.
- [ ] Backup e retention dei database del pool (cancellazione post-Canceled).
- [ ] Key-ring DataProtection su store condiviso (volume/Redis/KMS) se multi-istanza.

## Variabili d'ambiente (produzione)

In produzione i segreti NON devono stare in `appsettings.json`. Override via env
(doppio underscore = nesting):

| Variabile | Uso |
|-----------|-----|
| `ConnectionStrings__ControlPlane` | DB del Control Plane (obbligatoria, no default) |
| `Seed__SuperAdminEmail` / `Seed__SuperAdminPassword` | primo Super Admin (password obbligatoria) |
| `ConnectionStrings__Tenant` | DB dell'istanza ticketing (iniettata al provisioning) |
| `Jwt__Key` | chiave firma JWT istanza, ≥ 32 caratteri (obbligatoria) |
| `Seed__AdminEmail` / `Seed__AdminPassword` | admin iniziale dell'istanza |

All'avvio in `Production` l'app si rifiuta di partire se una di queste è lasciata al
valore di default di sviluppo.

## Dati e privacy

- Suspend/Stop **non cancellano** mai i dati: i container vengono fermati.
- La cancellazione definitiva (`RemoveContainerAsync` con `removeVolumes:true`) va
  eseguita solo a fine retention, con processo tracciato.
