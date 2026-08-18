import { useState } from 'react'
import { Alert, Badge, Button, Card, CardBody, Col, Icon, Input, Row, Spinner } from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { TFA_LABELS, TfaAvailable } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

const OPZIONI_TFA = [TfaAvailable.EmailOtp, TfaAvailable.AuthenticatorApp]

export function ProfilePage() {
  const { session, isAdmin, scadenza } = useAuth()
  const [tfa, setTfa] = useState<TfaAvailable[]>([])
  const azione = useAzione(usersApi.selfUpdate)

  function toggleTfa(v: TfaAvailable, attivo: boolean) {
    setTfa((prec) => (attivo ? [...new Set([...prec, v])] : prec.filter((x) => x !== v)))
  }

  return (
    <>
      <PageHeader titolo="Profilo" descrizione="Dati della sessione e metodi di autenticazione a due fattori." />

      <Row>
        <Col xs="12" lg="5" className="mb-4">
          <Card shadow="sm" fullHeight>
            <CardBody>
              <h2 className="h6 text-muted text-uppercase">Dati account</h2>
              <dl className="mb-0">
                <dt>Nome</dt>
                <dd>{session?.fullName?.trim() || '—'}</dd>
                <dt>Email</dt>
                <dd>{session?.email}</dd>
                <dt>Ruolo</dt>
                <dd>
                  <Badge color={isAdmin ? 'primary' : 'secondary'} pill>
                    {session?.role}
                  </Badge>
                </dd>
                <dt>Identificativo</dt>
                <dd>
                  <code className="user-select-all">{session?.userId}</code>
                </dd>
                <dt>Sessione valida fino al</dt>
                <dd className="mb-0">
                  {scadenza?.toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' }) ?? '—'}
                </dd>
              </dl>

              <Alert color="info" className="mt-3 mb-0 small">
                Questi dati provengono dalla risposta di login conservata in locale. L'API non
                espone un endpoint <code>GET /me</code> per rileggerli aggiornati.
              </Alert>
            </CardBody>
          </Card>
        </Col>

        <Col xs="12" lg="7">
          <Card shadow="sm">
            <CardBody>
              <h2 className="h6 text-muted text-uppercase">Autenticazione a due fattori</h2>

              {azione.errore && (
                <Alert color="danger" role="alert">
                  {azione.errore}
                </Alert>
              )}
              {azione.esito?.result && (
                <Alert color="success" role="status">
                  {azione.esito.msg}
                </Alert>
              )}

              <p className="text-muted">
                Seleziona i metodi da abilitare. L'elenco inviato sostituisce quello attuale:
                deselezionare tutto disattiva la 2FA.
              </p>

              <form
                onSubmit={(e) => {
                  e.preventDefault()
                  if (!session) return
                  void azione.esegui({ id: session.userId, tfA_Availables: tfa })
                }}
              >
                <fieldset className="mb-4">
                  <legend className="visually-hidden">Metodi disponibili</legend>
                  {OPZIONI_TFA.map((v) => (
                    <div className="form-check" key={v}>
                      <Input
                        type="checkbox"
                        id={`prof-tfa-${v}`}
                        checked={tfa.includes(v)}
                        onChange={(e) => toggleTfa(v, e.target.checked)}
                        disabled={azione.inCorso}
                        label={TFA_LABELS[v]}
                      />
                    </div>
                  ))}
                </fieldset>

                <Button color="primary" type="submit" disabled={azione.inCorso}>
                  {azione.inCorso ? (
                    <>
                      <Spinner active small className="me-2" />
                      Salvataggio…
                    </>
                  ) : (
                    <>
                      <Icon icon="it-settings" color="white" size="sm" aria-hidden className="me-1" />
                      Salva preferenze 2FA
                    </>
                  )}
                </Button>
              </form>

              <Alert color="warning" className="mt-4 mb-0">
                <h3 className="alert-heading h6">Selezione non precompilata</h3>
                <p className="mb-0">
                  Le caselle partono sempre vuote perché non esiste un endpoint per leggere i
                  metodi già configurati: quello che vedi è ciò che stai per inviare, non lo stato
                  attuale sul server. Il flusso di verifica del secondo fattore al login non è
                  ancora attivo lato API.
                </p>
              </Alert>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </>
  )
}
