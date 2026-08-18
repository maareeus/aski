# AskiiPlatform.Web

Pannello di amministrazione di Askii Platform. React + Vite + TypeScript con
**shadcn/ui** (Radix UI + Tailwind CSS v4).

I componenti di shadcn non sono una dipendenza npm: il CLI li copia in
`src/components/ui/`, quindi sono codice del progetto e si modificano
direttamente. Per aggiungerne altri:

```bash
npx shadcn@latest add <componente>
```

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

### Se Vite muore all'avvio con ENOSPC

Su Linux il watch dei file usa inotify, che ha un tetto di **istanze per utente**
(`fs.inotify.max_user_instances`, di default 128) facilmente saturato dai
language server degli editor: superato il tetto, Vite termina all'avvio con
`ENOSPC: System limit for number of file watchers reached`. Nota che il limite
in causa è quello delle *istanze*, non `max_user_watches`.

Il progetto usa il **polling**, che non passa da inotify e parte in ogni caso,
quindi `npm run dev` funziona senza interventi.

Per tornare al watch nativo, più leggero in CPU, va alzato il limite di sistema:

```bash
sudo sysctl -w fs.inotify.max_user_instances=512
```

Per renderlo permanente:

```bash
echo 'fs.inotify.max_user_instances=512' | sudo tee /etc/sysctl.d/99-inotify.conf
```

Poi si disattiva il polling:

```bash
VITE_POLLING=0 npm run dev
```

Per vedere quanto sei vicino al limite:

```bash
echo "limite: $(cat /proc/sys/fs/inotify/max_user_instances)"; for p in /proc/[0-9]*; do ls -l $p/fd 2>/dev/null | grep -c inotify; done | paste -sd+ | bc
```

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
├── components/ui/ componenti shadcn (codice nostro, non dipendenze)
├── layout/       header, sidebar collassabile, area contenuti
├── pages/        una pagina per operazione
└── ui/           header di pagina, riquadri di esito, hook per le chiamate
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
| `GET /me` | rileggere il profilo aggiornato invece di fidarsi della risposta di login |
| endpoint di statistiche | contatori nel riepilogo: la lista è paginata, sommarla lato client richiederebbe tutte le pagine |
| flusso di reset password | oggi l'admin imposta la password e la comunica, quindi la conosce; servirebbe un token monouso inviato all'utente |
| lettura dei metodi 2FA | precompilare le caselle nel profilo: ora partono sempre vuote |
| password nella risposta di create, o invio email | un utente creato non può autenticarsi: la password generata è persa |
| `JsonStringEnumConverter` | gli enum viaggiano come numeri, quindi `TFA_Availables: [0]` invece di `["EMAIL_OTP"]` |
