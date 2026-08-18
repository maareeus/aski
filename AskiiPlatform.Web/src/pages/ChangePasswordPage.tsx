import { useState } from 'react'
import { Alert, Button, Card, CardBody, Col, Icon, Input, Row, Spinner, Toggle } from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

export function ChangePasswordPage() {
  const { session, isAdmin } = useAuth()
  const [suAltroUtente, setSuAltroUtente] = useState(false)
  const [targetId, setTargetId] = useState('')
  const [oldPassword, setOldPassword] = useState('')
  const [password, setPassword] = useState('')
  const [rePassword, setRePassword] = useState('')

  const azione = useAzione(usersApi.changePassword)

  const id = suAltroUtente ? targetId.trim() : (session?.userId ?? '')
  // Il backend salta la verifica della password attuale solo per gli Admin.
  const serveVecchiaPassword = !isAdmin
  const nonCoincidono = password !== '' && rePassword !== '' && password !== rePassword

  const puoInviare =
    !azione.inCorso &&
    id !== '' &&
    password !== '' &&
    rePassword !== '' &&
    !nonCoincidono &&
    (!serveVecchiaPassword || oldPassword !== '')

  return (
    <>
      <PageHeader
        titolo="Cambia password"
        descrizione={
          isAdmin
            ? 'Come Admin puoi cambiare anche la password di altri utenti, senza conoscere quella attuale.'
            : 'Per cambiare la tua password devi indicare quella attuale.'
        }
      />

      <Row>
        <Col xs="12" lg="8">
          <Card shadow="sm">
            <CardBody>
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

              <form
                onSubmit={async (e) => {
                  e.preventDefault()
                  const esito = await azione.esegui({
                    id,
                    password,
                    rePassword,
                    oldPassword: serveVecchiaPassword ? oldPassword : null,
                  })
                  if (esito?.result) {
                    setOldPassword('')
                    setPassword('')
                    setRePassword('')
                  }
                }}
                noValidate
              >
                {isAdmin && (
                  <div className="mb-4">
                    <Toggle
                      label="Cambia la password di un altro utente"
                      id="pwd-altro"
                      checked={suAltroUtente}
                      onChange={(e) => setSuAltroUtente(e.target.checked)}
                      disabled={azione.inCorso}
                    />
                  </div>
                )}

                {suAltroUtente ? (
                  <Input
                    label="Identificativo utente"
                    id="pwd-id"
                    value={targetId}
                    onChange={(e) => setTargetId(e.target.value)}
                    required
                    disabled={azione.inCorso}
                    placeholder="00000000-0000-0000-0000-000000000000"
                    infoText="Serve anche per rendere utilizzabile un utente appena creato, la cui password generata non è recuperabile."
                  />
                ) : (
                  <p className="text-muted">
                    Operazione sul tuo account: <code>{session?.email}</code>
                  </p>
                )}

                {serveVecchiaPassword && (
                  <Input
                    type="password"
                    label="Password attuale"
                    id="pwd-vecchia"
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    autoComplete="current-password"
                    required
                    disabled={azione.inCorso}
                  />
                )}

                <Input
                  type="password"
                  label="Nuova password"
                  id="pwd-nuova"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                  disabled={azione.inCorso}
                  infoText="L'API non impone requisiti di robustezza: la lunghezza è la difesa più efficace."
                />

                <Input
                  type="password"
                  label="Ripeti la nuova password"
                  id="pwd-ripeti"
                  value={rePassword}
                  onChange={(e) => setRePassword(e.target.value)}
                  autoComplete="new-password"
                  required
                  disabled={azione.inCorso}
                  valid={nonCoincidono ? false : undefined}
                  validationText={nonCoincidono ? 'Le due password non corrispondono.' : undefined}
                />

                <Button color="primary" type="submit" disabled={!puoInviare}>
                  {azione.inCorso ? (
                    <>
                      <Spinner active small className="me-2" />
                      Salvataggio…
                    </>
                  ) : (
                    <>
                      <Icon icon="it-key" color="white" size="sm" aria-hidden className="me-1" />
                      Aggiorna password
                    </>
                  )}
                </Button>
              </form>

              {!suAltroUtente && (
                <Alert color="info" className="mt-4 mb-0">
                  Il cambio password non invalida i token già emessi: eventuali sessioni aperte
                  altrove restano valide fino alla loro scadenza naturale.
                </Alert>
              )}
            </CardBody>
          </Card>
        </Col>
      </Row>
    </>
  )
}
