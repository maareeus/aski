# AskiiPlatform.Web

Pannello di amministrazione di Askii Platform. React + Vite + TypeScript, con il
design system ufficiale di Designers Italia (`bootstrap-italia` +
`design-react-kit`).

## Avvio

L'API deve essere in ascolto su `http://localhost:5244`:

```bash
dotnet run --project ../AskiiPlatform.Api
```

Poi:

```bash
npm install
npm run dev
```

L'app risponde su `http://localhost:5173`.

### Perché serve il proxy

L'API **non ha CORS configurato**. Vite inoltra `/api` verso il backend
(`vite.config.ts`), quindi il browser dialoga sempre con l'origin di Vite e il
problema non si presenta in sviluppo. Per cambiare target:

```bash
API_URL=http://localhost:5000 npm run dev
```

In produzione serve l'hosting same-origin oppure `AddCors`/`UseCors` lato API.

## Struttura

```
src/
├── api/          client HTTP, tipi allineati ai record C#, un metodo per endpoint
├── auth/         sessione, lettura scadenza del JWT, guardie di rotta
├── layout/       header, sidebar, area contenuti
├── pages/        una pagina per operazione
└── ui/           header di pagina, hook per le chiamate, opzioni dei select
```

## Autenticazione

La sessione (token + identità dalla risposta di login) è in `localStorage`.
L'identità **non** viene letta dal JWT: `TokenService` mescola nomi brevi
(`sub`, `email`) e URI WS-* per nome e ruolo, quindi dipendere da quelle chiavi
renderebbe il client fragile. Del token si legge solo `exp`, per chiudere la
sessione alla scadenza senza attendere il primo 401.

Nota: `localStorage` è leggibile da JavaScript, quindi vulnerabile a XSS. È la
scelta obbligata finché l'API restituisce un bearer token; un cookie httpOnly
sarebbe più sicuro ma richiede una modifica lato backend.

## Cosa manca all'API perché la UI sia completa

| Serve | Perché |
|---|---|
| `GET /user/admin/list` | popolare l'elenco utenti, oggi una pagina che dichiara il vincolo |
| `GET /user/admin/{id}` | precompilare modifica ed eliminazione invece di far digitare il GUID |
| `GET /me` | rileggere il profilo aggiornato invece di fidarsi della risposta di login |
| lettura dei metodi 2FA | precompilare le caselle nel profilo: ora partono sempre vuote |
| password nella risposta di create, o invio email | un utente creato non può autenticarsi: la password generata è persa |
| `JsonStringEnumConverter` | gli enum viaggiano come numeri, quindi `TFA_Availables: [0]` invece di `["EMAIL_OTP"]` |
