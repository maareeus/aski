import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
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
  const { session, isAdmin, scadenza } = useAuth()


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
            <dl className="space-y-4">
              <Voce etichetta="Nome">{session?.fullName?.trim() || '—'}</Voce>
              <Voce etichetta="Email">{session?.email}</Voce>
              <Voce etichetta="Ruolo">
                <Badge variant={isAdmin ? 'default' : 'secondary'}>{session?.role}</Badge>
              </Voce>
              <Voce etichetta="Identificativo">
                <code className="bg-muted block rounded px-2 py-1.5 font-mono text-xs break-all select-all">
                  {session?.userId}
                </code>
              </Voce>
              <Voce etichetta="Sessione valida fino al">
                {scadenza?.toLocaleString('it-IT', {
                  dateStyle: 'medium',
                  timeStyle: 'short',
                }) ?? '—'}
              </Voce>
            </dl>

            <Esito tono="info">
              Questi dati provengono dalla risposta di login conservata in locale: l'API non espone
              un endpoint <code className="font-mono">GET /me</code> per rileggerli aggiornati.
            </Esito>
          </CardContent>
        </Card>

        <div className="space-y-4 lg:col-span-3">
          <TfaCard />
        </div>
      </div>
    </>
  )
}
