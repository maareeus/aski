import { useState } from 'react'
import { Loader2, ShieldCheck } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { usersApi } from '@/api/endpoints'
import { TFA_LABELS, TfaAvailable } from '@/api/types'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

const OPZIONI_TFA = [TfaAvailable.EmailOtp, TfaAvailable.AuthenticatorApp]

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
  const [tfa, setTfa] = useState<TfaAvailable[]>([])
  const azione = useAzione(usersApi.selfUpdate)

  function toggleTfa(v: TfaAvailable, attivo: boolean) {
    setTfa((prec) => (attivo ? [...new Set([...prec, v])] : prec.filter((x) => x !== v)))
  }

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
          {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}
          {azione.esito?.result && <Esito tono="successo">{azione.esito.msg}</Esito>}

          <Card>
            <CardHeader>
              <CardTitle>Autenticazione a due fattori</CardTitle>
              <CardDescription>
                L'elenco inviato sostituisce quello attuale: deselezionare tutto disattiva la 2FA.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form
                className="space-y-6"
                onSubmit={(e) => {
                  e.preventDefault()
                  if (!session) return
                  void azione.esegui({ id: session.userId, tfA_Availables: tfa })
                }}
              >
                <div className="space-y-3">
                  {OPZIONI_TFA.map((v) => (
                    <div key={v} className="flex items-start gap-2.5 rounded-lg border p-3">
                      <Checkbox
                        id={`prof-tfa-${v}`}
                        checked={tfa.includes(v)}
                        onCheckedChange={(c) => toggleTfa(v, c === true)}
                        disabled={azione.inCorso}
                      />
                      <Label htmlFor={`prof-tfa-${v}`} className="font-normal leading-snug">
                        {TFA_LABELS[v]}
                      </Label>
                    </div>
                  ))}
                </div>

                <Button type="submit" disabled={azione.inCorso}>
                  {azione.inCorso ? <Loader2 className="animate-spin" /> : <ShieldCheck />}
                  {azione.inCorso ? 'Salvataggio…' : 'Salva preferenze 2FA'}
                </Button>
              </form>
            </CardContent>
          </Card>

          <Esito tono="attenzione" titolo="Selezione non precompilata">
            Le caselle partono sempre vuote perché non esiste un endpoint per leggere i metodi già
            configurati: quello che vedi è ciò che stai per inviare, non lo stato attuale sul server.
            Il flusso di verifica del secondo fattore al login non è ancora attivo lato API.
          </Esito>
        </div>
      </div>
    </>
  )
}
