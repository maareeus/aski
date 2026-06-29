# Aski Tickets — Frontend (Blazor WebAssembly)

SPA in **C# / Blazor WebAssembly** che consuma le API del backend. Stile moderno
(tipo Cloudflare/RevenueCat) con **Tailwind CSS** (via CDN), sidebar, card e tabelle.

## Stack
- Blazor WebAssembly (.NET 10)
- Auth JWT: token in `localStorage` (Blazored.LocalStorage), `AuthenticationStateProvider`
  custom che decodifica i claim del JWT, `BearerHandler` che allega il token alle chiamate.
- Tailwind CDN per lo stile.

## Avvio

```powershell
# backend + frontend insieme
.\scripts\start-dev.ps1

# oppure solo frontend (richiede il backend attivo su http://localhost:5095)
cd frontend
dotnet run
```

- Frontend: `http://localhost:5200`
- Login: `admin@aski.local` / `ChangeMe123!`
- L'URL dell'API è in `wwwroot/appsettings.json` (`ApiBaseUrl`, default `http://localhost:5095`).
  Il backend deve avere il CORS abilitato per l'origine del frontend (già configurato in Development).

## Pagine

| Rotta | Ruolo | Contenuto |
|-------|-------|-----------|
| `/login` | anonimo | accesso |
| `/` | autenticato | dashboard con KPI + ticket recenti |
| `/tickets` | autenticato | lista ticket + filtro + creazione (Client/Admin) |
| `/tickets/{id}` | autenticato | dettaglio, commenti, azioni (staff: stato/assegna/chiudi) |
| `/clients` | Admin | aziende clienti |
| `/software` | Admin | software assistiti |
| `/users` | Admin | utenti (crea Agent/Client/Admin) |

La navigazione mostra le voci Admin solo agli Admin; le pagine sono protette per ruolo.

## Struttura
```
frontend/
├── Auth/        # AuthService, AuthenticationStateProvider, BearerHandler, RedirectToLogin
├── Services/    # ApiClient (chiamate tipizzate)
├── Models/      # DTO (record)
├── Layout/      # MainLayout (sidebar+topbar), EmptyLayout, NavItem
├── Components/  # Stat, StatusBadge, PriorityBadge
├── Pages/       # Login, Home, Tickets, TicketDetailPage, Clients, SoftwarePage, Users
└── wwwroot/     # index.html (Tailwind), appsettings.json (ApiBaseUrl)
```
