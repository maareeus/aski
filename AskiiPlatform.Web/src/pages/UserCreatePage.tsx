import { useState } from 'react'
import {
  Alert,
  Button,
  Card,
  CardBody,
  Col,
  Icon,
  Input,
  Row,
  Select,
  Spinner,
  Toggle,
} from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { Roles } from '../api/types'
import type { Role } from '../api/types'
import { opzioniRuolo } from '../ui/opzioni'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

export function UserCreatePage() {
  const [email, setEmail] = useState('')
  const [name, setName] = useState('')
  const [lastName, setLastName] = useState('')
  const [role, setRole] = useState<Role>(Roles.Client)
  const [isActive, setIsActive] = useState(false)

  const azione = useAzione(usersApi.create)

  function svuota() {
    setEmail('')
    setName('')
    setLastName('')
    setRole(Roles.Client)
    setIsActive(false)
  }

  return (
    <>
      <PageHeader
        titolo="Nuovo utente"
        descrizione="La password non si imposta qui: è il backend a generarne una casuale."
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

              {azione.esito && (
                <Alert color="success" role="status">
                  <h2 className="alert-heading h6">Utente creato</h2>
                  <p className="mb-2">
                    <strong>{azione.esito.email}</strong> — ruolo {azione.esito.role},{' '}
                    {azione.esito.isActive ? 'attivo' : 'da attivare'}.
                  </p>
                  <p className="mb-2">
                    Identificativo, da conservare per le operazioni successive:
                    <br />
                    <code className="user-select-all">{azione.esito.id}</code>
                  </p>
                  <hr />
                  <p className="mb-0 small">
                    La password generata non viene restituita dall'API e non esiste ancora un invio
                    email: per rendere l'account utilizzabile serve impostargliela da{' '}
                    <a href="/password">Cambia password</a> indicando l'identificativo qui sopra.
                  </p>
                </Alert>
              )}

              <form
                onSubmit={async (e) => {
                  e.preventDefault()
                  const esito = await azione.esegui({
                    email: email.trim(),
                    name: name.trim() || null,
                    lastName: lastName.trim() || null,
                    role,
                    isActive,
                  })
                  if (esito) svuota()
                }}
                noValidate
              >
                <Input
                  type="email"
                  label="Email"
                  id="crea-email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  disabled={azione.inCorso}
                  infoText="Viene normalizzata in minuscolo e deve essere univoca."
                />

                <Row>
                  <Col xs="12" md="6">
                    <Input
                      label="Nome"
                      id="crea-nome"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      disabled={azione.inCorso}
                    />
                  </Col>
                  <Col xs="12" md="6">
                    <Input
                      label="Cognome"
                      id="crea-cognome"
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      disabled={azione.inCorso}
                    />
                  </Col>
                </Row>

                {/* Select del kit: onChange riceve direttamente il valore, non l'evento */}
                <Select
                  id="crea-ruolo"
                  label="Ruolo"
                  value={role}
                  onChange={(valore) => setRole(valore as Role)}
                  disabled={azione.inCorso}
                >
                  {opzioniRuolo()}
                </Select>

                <div className="my-4">
                  <Toggle
                    label="Crea l'utente già attivo"
                    id="crea-attivo"
                    checked={isActive}
                    onChange={(e) => setIsActive(e.target.checked)}
                    disabled={azione.inCorso}
                  />
                  <p className="text-muted small mt-1 mb-0">
                    Se lasciato spento, l'utente va attivato separatamente.
                  </p>
                </div>

                <div className="d-flex gap-2">
                  <Button color="primary" type="submit" disabled={azione.inCorso || !email}>
                    {azione.inCorso ? (
                      <>
                        <Spinner active small className="me-2" />
                        Creazione…
                      </>
                    ) : (
                      <>
                        <Icon icon="it-plus-circle" color="white" size="sm" aria-hidden className="me-1" />
                        Crea utente
                      </>
                    )}
                  </Button>
                  <Button color="primary" outline type="button" onClick={svuota} disabled={azione.inCorso}>
                    Svuota
                  </Button>
                </div>
              </form>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </>
  )
}
