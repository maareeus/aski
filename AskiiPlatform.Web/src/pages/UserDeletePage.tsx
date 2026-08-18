import { useState } from 'react'
import { Alert, Button, Card, CardBody, Col, Icon, Input, Row, Spinner } from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

export function UserDeletePage() {
  const { session } = useAuth()
  const [userId, setUserId] = useState('')
  const [confermato, setConfermato] = useState(false)
  const azione = useAzione(usersApi.remove)

  const seStesso = userId.trim().toLowerCase() === session?.userId.toLowerCase()

  return (
    <>
      <PageHeader
        titolo="Elimina utente"
        descrizione="La cancellazione è definitiva: non esiste soft-delete né ripristino."
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
                  void azione.esegui({ userId: userId.trim() })
                  setConfermato(false)
                }}
                noValidate
              >
                <Input
                  label="Identificativo utente"
                  id="del-id"
                  value={userId}
                  onChange={(e) => {
                    setUserId(e.target.value)
                    setConfermato(false)
                  }}
                  required
                  disabled={azione.inCorso}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />

                {seStesso && (
                  <Alert color="warning">
                    Questo è il tuo identificativo. L'API rifiuta l'autocancellazione.
                  </Alert>
                )}

                <div className="form-check mb-4">
                  <Input
                    type="checkbox"
                    id="del-conferma"
                    checked={confermato}
                    onChange={(e) => setConfermato(e.target.checked)}
                    disabled={azione.inCorso || !userId}
                    label="Confermo di voler eliminare definitivamente questo utente"
                  />
                </div>

                <Button
                  color="danger"
                  type="submit"
                  disabled={azione.inCorso || !userId || !confermato}
                >
                  {azione.inCorso ? (
                    <>
                      <Spinner active small className="me-2" />
                      Eliminazione…
                    </>
                  ) : (
                    <>
                      <Icon icon="it-delete" color="white" size="sm" aria-hidden className="me-1" />
                      Elimina definitivamente
                    </>
                  )}
                </Button>
              </form>

              <p className="text-muted small mt-4 mb-0">
                Il super amministratore non è eliminabile. Se l'identificativo non esiste, l'API
                risponde con lo stesso messaggio del tentativo di autocancellazione: i due casi non
                sono distinguibili.
              </p>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </>
  )
}
