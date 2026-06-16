# 07 — API Ticketing (istanza)

Base URL di sviluppo: `https://localhost:5xxx` (porta dell'istanza). Tutte le rotte
tranne il login richiedono header `Authorization: Bearer <jwt>`.

## Autenticazione

### `POST /api/auth/login`
```json
{ "email": "admin@aski.local", "password": "ChangeMe123!" }
```
→ `{ "token": "<jwt>", "role": "Admin" }`

Il JWT contiene i claim: `NameIdentifier` (userId), `Email`, `Role`, e per i Client
`companyId`. Firma HMAC-SHA256 con `Jwt:Key`.

## Ruoli e permessi

| Azione | Admin | Dev | Client |
|--------|:-----:|:---:|:------:|
| Vedere tutti i ticket | ✅ | ambito assegnato | solo propria azienda |
| Aprire ticket | ✅ (per qualsiasi azienda) | ❌ | ✅ (propria azienda) |
| Cambiare stato / assegnare | ✅ | ✅ (ambito) | ❌ |
| Chiudere ticket | ✅ | ✅ (ambito) | ✅ (propria azienda) |
| Commenti interni | ✅ | ✅ | ❌ (nascosti) |
| Gestione utenti/aziende/software | ✅ | ❌ | ❌ |

**Ambito del Dev**: definito da `DevAssignment`. Un Dev vede i ticket assegnati a lui,
oppure delle aziende/software a lui assegnati. Un'assegnazione senza vincoli = accesso totale.

## Ticket

### `GET /api/tickets`
Lista filtrata per ruolo (Admin tutto, Dev ambito, Client propria azienda).

### `GET /api/tickets/{id}`
Dettaglio + commenti. I Client non ricevono i commenti `IsInternal`. `403` se fuori ambito.

### `POST /api/tickets`
```json
{ "title": "Errore login", "description": "...", "softwareId": 2, "priority": 1, "companyId": null }
```
- **Client**: `companyId` ignorato, forzato alla propria azienda.
- **Admin**: `companyId` obbligatorio.
- **Dev**: `403` (i Dev non aprono ticket per i clienti).

→ `201 { "id": 10 }`

### `PATCH /api/tickets/{id}/status` — *(Admin, Dev)*
```json
{ "status": 1, "assignedDevUserId": 3 }
```
`status`: `0 Open, 1 InProgress, 2 Waiting, 3 Resolved, 4 Closed`. Impostando `Closed`
viene valorizzato `ClosedAtUtc`.

### `POST /api/tickets/{id}/close`
Chiusura. I **Client** possono chiudere solo ticket della propria azienda (chiusura
autonoma); Admin/Dev nel proprio ambito.

### `POST /api/tickets/{id}/comments`
```json
{ "body": "Stiamo verificando", "isInternal": false }
```
- `isInternal` viene forzato a `false` per i Client.
- Un commento del Client su un ticket `Resolved` lo **riapre** (torna `Open`).

→ `201 { "id": 55 }`

## Gestione (solo Admin) — `/api/manage`

### Aziende
- `GET /api/manage/companies`
- `POST /api/manage/companies` → `{ "name": "Cliente X" }`

### Software
- `GET /api/manage/software`
- `POST /api/manage/software` → `{ "name": "App Mobile", "description": "..." }`

### Utenti
- `GET /api/manage/users`
- `POST /api/manage/users`
  ```json
  { "email": "dev@aski.local", "password": "Secret123!", "role": 1, "fullName": "Mario", "companyId": null }
  ```
  `role`: `0 Admin, 1 Dev, 2 Client`. I Client richiedono `companyId`.

### Assegnazioni Dev
- `POST /api/manage/assignments`
  ```json
  { "userId": 3, "companyId": 1, "softwareId": null }
  ```
  Collega un Dev a un'azienda e/o software. `null/null` = accesso totale.

## Esempio di flusso

```http
POST /api/auth/login                 admin -> token
POST /api/manage/companies           { "name":"Cliente X" }            -> companyId 1
POST /api/manage/software            { "name":"App" }                  -> softwareId 1
POST /api/manage/users               { role:2, companyId:1, ... }      -> client
POST /api/manage/users               { role:1, ... }                   -> dev
POST /api/manage/assignments         { userId:dev, companyId:1 }
# login come client:
POST /api/tickets                    { "title":"Bug", "priority":2 }   -> ticket
# login come dev:
PATCH /api/tickets/1/status          { "status":1, "assignedDevUserId":dev }
# client chiude:
POST /api/tickets/1/close
```
