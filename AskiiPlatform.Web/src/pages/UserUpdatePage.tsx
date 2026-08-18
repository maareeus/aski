import { useState } from 'react'
import { Alert, Button, Card, CardBody, Col, Icon, Input, Row, Select, Spinner } from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { TFA_LABELS, TfaAvailable } from '../api/types'
import type { Role } from '../api/types'
import { opzioniRuolo } from '../ui/opzioni'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

const OPZIONI_TFA = [TfaAvailable.EmailOtp, TfaAvailable.AuthenticatorApp]

export function UserUpdatePage() {
  const [id, setId] = useState('')
  const [email, setEmail] = useState('')
  const [name, setName] = useState('')
  const [lastName, setLastName] = useState('')
  const [role, setRole] = useState<'' | Role>('')
  const [tfa, setTfa] = useState<TfaAvailable[]>([])
  const [modificaTfa, setModificaTfa] = useState(false)

  const azione = useAzione(usersApi.adminUpdate)

  function toggleTfa(v: TfaAvailable, attivo: boolean) {
    setTfa((prec) => (attivo ? [...new Set([...prec, v])] : prec.filter((x) => x !== v)))
  }

  const nessunCampo = !email && !name && !lastName && !role && !modificaTfa

  return (
    <>
      <PageHeader
        titolo="Modifica utente"
        descrizione="I campi lasciati vuoti non vengono toccati: l'API applica solo quelli valorizzati."
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
                onSubmit={(e) => {
                  e.preventDefault()
                  void azione.esegui({
                    id: id.trim(),
                    email: email.trim() || null,
                    name: name.trim() || null,
                    lastName: lastName.trim() || null,
                    role: role || null,
                    tfA_Availables: modificaTfa ? tfa : null,
                  })
                }}
                noValidate
              >
                <Input
                  label="Identificativo utente"
                  id="mod-id"
                  value={id}
                  onChange={(e) => setId(e.target.value)}
                  required
                  disabled={azione.inCorso}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  infoText="GUID restituito dalla creazione. Senza un endpoint di lettura va inserito a mano."
                />

                <hr />

                <Input
                  type="email"
                  label="Nuova email"
                  id="mod-email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={azione.inCorso}
                  infoText="Se già assegnata a un altro utente l'operazione fallisce."
                />

                <Row>
                  <Col xs="12" md="6">
                    <Input
                      label="Nome"
                      id="mod-nome"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      disabled={azione.inCorso}
                    />
                  </Col>
                  <Col xs="12" md="6">
                    <Input
                      label="Cognome"
                      id="mod-cognome"
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      disabled={azione.inCorso}
                    />
                  </Col>
                </Row>

                <Select
                  id="mod-ruolo"
                  label="Ruolo"
                  value={role}
                  onChange={(valore) => setRole(valore as '' | Role)}
                  disabled={azione.inCorso}
                >
                  {opzioniRuolo('Non modificare')}
                </Select>
                <p className="text-muted small">
                  Il super amministratore non può essere declassato: l'API rifiuta il cambio.
                </p>

                <fieldset className="mt-4">
                  <legend className="h6">Autenticazione a due fattori</legend>
                  <div className="form-check">
                    <Input
                      type="checkbox"
                      id="mod-tfa-abilita"
                      checked={modificaTfa}
                      onChange={(e) => setModificaTfa(e.target.checked)}
                      disabled={azione.inCorso}
                      label="Sovrascrivi i metodi 2FA"
                    />
                  </div>
                  <p className="text-muted small">
                    Se attivo, l'elenco selezionato <strong>sostituisce</strong> quello esistente:
                    non selezionare nulla equivale a disattivare la 2FA.
                  </p>

                  {modificaTfa &&
                    OPZIONI_TFA.map((v) => (
                      <div className="form-check" key={v}>
                        <Input
                          type="checkbox"
                          id={`mod-tfa-${v}`}
                          checked={tfa.includes(v)}
                          onChange={(e) => toggleTfa(v, e.target.checked)}
                          disabled={azione.inCorso}
                          label={TFA_LABELS[v]}
                        />
                      </div>
                    ))}
                </fieldset>

                <div className="mt-4">
                  <Button
                    color="primary"
                    type="submit"
                    disabled={azione.inCorso || !id || nessunCampo}
                  >
                    {azione.inCorso ? (
                      <>
                        <Spinner active small className="me-2" />
                        Salvataggio…
                      </>
                    ) : (
                      <>
                        <Icon icon="it-pencil" color="white" size="sm" aria-hidden className="me-1" />
                        Salva modifiche
                      </>
                    )}
                  </Button>
                  {nessunCampo && id && (
                    <p className="text-muted small mt-2 mb-0">
                      Valorizza almeno un campo da modificare.
                    </p>
                  )}
                </div>
              </form>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </>
  )
}
