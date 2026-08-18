import { Link } from 'react-router-dom'
import {
  ArrowRight,
  Clock,
  Fingerprint,
  KeyRound,
  UserPlus,
  UserRound,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'

function formattaData(d: Date | null) {
  if (!d) return '—'
  return d.toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' })
}

interface Azione {
  to: string
  label: string
  descrizione: string
  icon: LucideIcon
  soloAdmin: boolean
}

const AZIONI: Azione[] = [
  {
    to: '/utenti/nuovo',
    label: 'Nuovo utente',
    descrizione: 'Crea un account e ottieni il suo identificativo',
    icon: UserPlus,
    soloAdmin: true,
  },
  {
    to: '/utenti',
    label: 'Gestione utenti',
    descrizione: 'Elenco, dettaglio, modifica, attivazione ed eliminazione',
    icon: Users,
    soloAdmin: true,
  },
  {
    to: '/password',
    label: 'Cambia password',
    descrizione: 'La tua password o quella di un altro utente',
    icon: KeyRound,
    soloAdmin: false,
  },
]

export function DashboardPage() {
  const { session, isAdmin, scadenza } = useAuth()
  const azioni = AZIONI.filter((a) => !a.soloAdmin || isAdmin)

  return (
    <>
      <PageHeader
        titolo="Riepilogo"
        descrizione="Stato della sessione corrente e scorciatoie alle operazioni disponibili."
      />

      <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
              <UserRound className="size-3.5" />
              Utente collegato
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="font-medium">{session?.fullName?.trim() || '—'}</p>
            <p className="text-muted-foreground text-sm">{session?.email}</p>
            <Badge variant={isAdmin ? 'default' : 'secondary'}>{session?.role}</Badge>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
              <Clock className="size-3.5" />
              Sessione
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="font-medium">{formattaData(scadenza)}</p>
            <p className="text-muted-foreground text-sm">
              Alla scadenza l'accesso viene chiuso automaticamente: il token non è rinnovabile.
            </p>
          </CardContent>
        </Card>

        <Card className="sm:col-span-2 lg:col-span-1">
          <CardHeader className="pb-3">
            <CardTitle className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
              <Fingerprint className="size-3.5" />
              Identificativo
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <code className="bg-muted block rounded px-2 py-1.5 font-mono text-xs break-all select-all">
              {session?.userId}
            </code>
            <p className="text-muted-foreground text-sm">
              Serve alle operazioni su utente, che identificano la risorsa per id.
            </p>
          </CardContent>
        </Card>
      </div>

      <h2 className="mb-3 text-lg font-semibold tracking-tight">Operazioni</h2>
      <div className="mb-8 grid gap-4 sm:grid-cols-2">
        {azioni.map((a) => (
          <Link key={a.to} to={a.to} className="group">
            <Card className="hover:border-primary/40 h-full transition-colors">
              <CardContent className="flex items-start gap-3">
                <div className="bg-muted text-foreground flex size-9 shrink-0 items-center justify-center rounded-md">
                  <a.icon className="size-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="flex items-center gap-1.5 font-medium">
                    {a.label}
                    <ArrowRight className="size-3.5 opacity-0 transition-opacity group-hover:opacity-100" />
                  </p>
                  <p className="text-muted-foreground text-sm">{a.descrizione}</p>
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      <Esito tono="attenzione" titolo="Nessun dato aggregato">
        Le informazioni qui sopra arrivano dalla risposta di login conservata in locale. Per mostrare
        contatori reali — utenti totali, attivi, per ruolo — serve un endpoint di statistiche: la
        lista è paginata, quindi ricavarli lato client richiederebbe di scaricare tutte le pagine.
        Manca anche un <code className="font-mono">GET /me</code> per rileggere il proprio profilo
        aggiornato.
      </Esito>
    </>
  )
}
