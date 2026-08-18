import { Link } from 'react-router-dom'
import {
  ArrowRight,
  Clock,
  KeyRound,
  MailWarning,
  ShieldCheck,
  UserPlus,
  UserRound,
  Users,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { usersApi } from '@/api/endpoints'
import { ROLE_LIST } from '@/api/types'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useRisorsa } from '@/ui/useRisorsa'

function dataOra(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' })
}

function Contatore({
  etichetta,
  valore,
  icona,
  nota,
  href,
  inCorso,
}: {
  etichetta: string
  valore: number | undefined
  icona: LucideIcon
  nota?: string
  href?: string
  inCorso: boolean
}) {
  const Icona = icona

  const contenuto = (
    <Card className={href ? 'hover:border-primary/40 h-full transition-colors' : 'h-full'}>
      <CardHeader className="pb-2">
        <CardTitle className="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
          <Icona className="size-3.5" />
          {etichetta}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {inCorso ? (
          <Skeleton className="h-8 w-16" />
        ) : (
          <p className="text-3xl font-semibold tabular-nums">{valore ?? '—'}</p>
        )}
        {nota && <p className="text-muted-foreground mt-1 text-sm">{nota}</p>}
      </CardContent>
    </Card>
  )

  return href ? (
    <Link to={href} className="group">
      {contenuto}
    </Link>
  ) : (
    contenuto
  )
}

export function DashboardPage() {
  const { session, isAdmin, scadenza } = useAuth()

  // Le statistiche sono admin-only: per gli altri ruoli non si chiama l'endpoint.
  const stats = useRisorsa(
    () => (isAdmin ? usersApi.stats() : Promise.resolve(null)),
    [isAdmin],
  )
  const s = stats.dati

  return (
    <>
      <PageHeader
        titolo="Riepilogo"
        descrizione={
          isAdmin
            ? 'Stato degli utenti e della sessione corrente.'
            : 'Stato della sessione corrente.'
        }
      />

      {isAdmin && (
        <>
          {stats.errore && (
            <Esito tono="errore" className="mb-4">
              {stats.errore}
            </Esito>
          )}

          <div className="mb-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Contatore
              etichetta="Utenti"
              valore={s?.total}
              icona={Users}
              href="/users"
              inCorso={stats.inCorso}
              nota="Totale registrati"
            />
            <Contatore
              etichetta="Attivi"
              valore={s?.active}
              icona={UserRound}
              href="/users?isActive=true"
              inCorso={stats.inCorso}
              nota="Possono accedere"
            />
            <Contatore
              etichetta="Da attivare"
              valore={s?.pendingActivation}
              icona={MailWarning}
              href="/users?isActive=false"
              inCorso={stats.inCorso}
              nota="In attesa del codice"
            />
            <Contatore
              etichetta="Con 2FA"
              valore={s?.withTfa}
              icona={ShieldCheck}
              inCorso={stats.inCorso}
              nota={
                s && s.total > 0
                  ? `${Math.round((s.withTfa / s.total) * 100)}% del totale`
                  : undefined
              }
            />
          </div>

          <div className="mb-8 grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
                  Distribuzione per ruolo
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {stats.inCorso && <Skeleton className="h-20 w-full" />}
                {!stats.inCorso &&
                  s &&
                  ROLE_LIST.map((ruolo) => {
                    const quanti = s.byRole[ruolo] ?? 0
                    const percentuale = s.total > 0 ? (quanti / s.total) * 100 : 0

                    return (
                      <div key={ruolo} className="space-y-1">
                        <div className="flex items-center justify-between text-sm">
                          <span className="font-medium">{ruolo}</span>
                          <span className="text-muted-foreground tabular-nums">{quanti}</span>
                        </div>
                        <div className="bg-muted h-1.5 overflow-hidden rounded-full">
                          <div
                            className="bg-primary h-full rounded-full transition-all"
                            style={{ width: `${percentuale}%` }}
                            role="presentation"
                          />
                        </div>
                      </div>
                    )
                  })}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
                  Ultimo accesso registrato
                </CardTitle>
              </CardHeader>
              <CardContent>
                {stats.inCorso ? (
                  <Skeleton className="h-6 w-40" />
                ) : (
                  <p className="font-medium">{dataOra(s?.lastLoginUtc)}</p>
                )}
                <p className="text-muted-foreground mt-1 text-sm">
                  Fra tutti gli utenti, non solo il tuo.
                </p>
              </CardContent>
            </Card>
          </div>
        </>
      )}

      <h2 className="mb-3 text-lg font-semibold tracking-tight">La tua sessione</h2>
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
              Scadenza
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="font-medium">{scadenza ? dataOra(scadenza.toISOString()) : '—'}</p>
            <p className="text-muted-foreground text-sm">
              Il token non è rinnovabile: alla scadenza l'accesso viene chiuso.
            </p>
          </CardContent>
        </Card>

        <Card className="sm:col-span-2 lg:col-span-1">
          <CardHeader className="pb-3">
            <CardTitle className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
              Scorciatoie
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {isAdmin && (
              <Link
                to="/users/new"
                className="hover:text-primary flex items-center gap-2 text-sm font-medium"
              >
                <UserPlus className="size-4" />
                Nuovo utente
                <ArrowRight className="size-3.5 opacity-0 transition-opacity group-hover:opacity-100" />
              </Link>
            )}
            <Link
              to="/profile"
              className="hover:text-primary flex items-center gap-2 text-sm font-medium"
            >
              <ShieldCheck className="size-4" />
              Profilo e 2FA
            </Link>
            <Link
              to="/password"
              className="hover:text-primary flex items-center gap-2 text-sm font-medium"
            >
              <KeyRound className="size-4" />
              Cambia password
            </Link>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
