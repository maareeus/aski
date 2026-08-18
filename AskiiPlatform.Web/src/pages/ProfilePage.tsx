import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { usersApi } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useRisorsa } from '@/ui/useRisorsa'
import { TfaCard } from './profile/TfaCard'

function Voce({ etichetta, children }: { etichetta: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <dt className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
        {etichetta}
      </dt>
      <dd className="text-sm">{children}</dd>
    </div>
  )
}

export function ProfilePage() {
  const { session, scadenza } = useAuth()

  // Dal database e non dalla sessione in locale: un cambio di ruolo o di
  // anagrafica fatto da un amministratore si vede subito.
  const profilo = useRisorsa(() => usersApi.me(), [])
  const p = profilo.dati


  return (
    <>
      <PageHeader
        titolo="Profilo"
        descrizione="Dati della sessione e metodi di autenticazione a due fattori."
      />

      <div className="grid gap-4 lg:grid-cols-5">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Dati account</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {profilo.errore && <Esito tono="errore">{profilo.errore}</Esito>}

            {profilo.inCorso ? (
              <div className="space-y-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-9 w-full" />
                ))}
              </div>
            ) : (
              <dl className="space-y-4">
                <Voce etichetta="Nome">{p?.fullName?.trim() || '—'}</Voce>
                <Voce etichetta="Email">{p?.email}</Voce>
                <Voce etichetta="Ruolo">
                  <Badge variant={p?.role === 'Admin' ? 'default' : 'secondary'}>{p?.role}</Badge>
                  {p?.isSuperAdmin && (
                    <Badge variant="outline" className="ml-2">
                      Super admin
                    </Badge>
                  )}
                </Voce>
                <Voce etichetta="Identificativo">
                  <code className="bg-muted block rounded px-2 py-1.5 font-mono text-xs break-all select-all">
                    {p?.id ?? session?.userId}
                  </code>
                </Voce>
                <Voce etichetta="Ultimo accesso">
                  {p?.lastLoginUtc
                    ? new Date(p.lastLoginUtc).toLocaleString('it-IT', {
                        dateStyle: 'medium',
                        timeStyle: 'short',
                      })
                    : '—'}
                </Voce>
                <Voce etichetta="Sessione valida fino al">
                  {scadenza?.toLocaleString('it-IT', {
                    dateStyle: 'medium',
                    timeStyle: 'short',
                  }) ?? '—'}
                </Voce>
              </dl>
            )}
          </CardContent>
        </Card>

        <div className="space-y-4 lg:col-span-3">
          <TfaCard />
        </div>
      </div>
    </>
  )
}
