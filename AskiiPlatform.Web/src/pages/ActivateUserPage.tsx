import { useState } from 'react'
import { Alert, Button, Card, CardBody, Col, Container, Icon, Input, Row, Spinner } from 'design-react-kit'
import { usersApi } from '../api/endpoints'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../ui/PageHeader'
import { useAzione } from '../ui/useAzione'

/**
 * Raggiungibile anche senza autenticazione, perché l'endpoint è anonimo.
 * Quando l'utente non è collegato la pagina si presenta da sola, senza layout.
 */
export function ActivateUserPage() {
  const { isAuthenticated } = useAuth()
  const [userId, setUserId] = useState('')
  const azione = useAzione(usersApi.activate)

  const contenuto = (
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
          }}
          noValidate
        >
          <Input
            label="Identificativo utente"
            id="attiva-id"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            required
            disabled={azione.inCorso}
            placeholder="00000000-0000-0000-0000-000000000000"
          />

          <Button color="primary" type="submit" disabled={azione.inCorso || !userId}>
            {azione.inCorso ? (
              <>
                <Spinner active small className="me-2" />
                Attivazione…
              </>
            ) : (
              <>
                <Icon icon="it-check-circle" color="white" size="sm" aria-hidden className="me-1" />
                Attiva utente
              </>
            )}
          </Button>
        </form>

        <Alert color="warning" className="mt-4 mb-0">
          <h2 className="alert-heading h6">Nota di sicurezza</h2>
          <p className="mb-0">
            Questo endpoint è anonimo e l'identificativo è l'unica informazione richiesta: chi lo
            conosce può attivare l'account. Con l'introduzione del codice di attivazione questa
            maschera dovrà chiedere quel codice al posto dell'id.
          </p>
        </Alert>
      </CardBody>
    </Card>
  )

  if (isAuthenticated) {
    return (
      <>
        <PageHeader titolo="Attiva utente" descrizione="Abilita l'accesso a un account creato non attivo." />
        <Row>
          <Col xs="12" lg="8">
            {contenuto}
          </Col>
        </Row>
      </>
    )
  }

  return (
    <Container className="my-5">
      <Row className="justify-content-center">
        <Col xs="12" md="8" lg="6">
          <div className="text-center mb-4">
            <h1 className="h3">Attivazione account</h1>
            <p className="text-muted">Askii Platform</p>
          </div>
          {contenuto}
          <p className="text-center mt-3">
            <a href="/login">Torna all'accesso</a>
          </p>
        </Col>
      </Row>
    </Container>
  )
}
