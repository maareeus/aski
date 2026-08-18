import { useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { Alert, Button, Card, CardBody, Col, Container, Icon, Input, Row, Spinner } from 'design-react-kit'
import { useAuth } from '../auth/AuthContext'
import { useAzione } from '../ui/useAzione'

export function LoginPage() {
  const { isAuthenticated, login, motivoUscita } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [mostraPassword, setMostraPassword] = useState(false)

  const azione = useAzione(login)

  if (isAuthenticated) {
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from && from !== '/login' ? from : '/'} replace />
  }

  return (
    <Container className="my-5">
      <Row className="justify-content-center">
        <Col xs="12" md="8" lg="5">
          <div className="text-center mb-4">
            <h1 className="h3">Askii Platform</h1>
            <p className="text-muted">Accedi al pannello di amministrazione</p>
          </div>

          <Card shadow="sm">
            <CardBody>
              {motivoUscita === 'scaduta' && (
                <Alert color="warning">
                  La sessione è scaduta. Il token ha una validità di 8 ore e non viene rinnovato:
                  effettua di nuovo l'accesso.
                </Alert>
              )}

              {azione.errore && (
                <Alert color="danger" role="alert">
                  {azione.errore}
                </Alert>
              )}

              <form
                onSubmit={(e) => {
                  e.preventDefault()
                  void azione.esegui(email, password)
                }}
                noValidate
              >
                <Input
                  type="email"
                  label="Email"
                  id="login-email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="username"
                  required
                  disabled={azione.inCorso}
                />

                <Input
                  type={mostraPassword ? 'text' : 'password'}
                  label="Password"
                  id="login-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  required
                  disabled={azione.inCorso}
                />

                <div className="form-check mb-4">
                  <Input
                    type="checkbox"
                    id="mostra-password"
                    checked={mostraPassword}
                    onChange={(e) => setMostraPassword(e.target.checked)}
                    label="Mostra password"
                  />
                </div>

                <Button
                  color="primary"
                  type="submit"
                  className="w-100"
                  disabled={azione.inCorso || !email || !password}
                >
                  {azione.inCorso ? (
                    <>
                      <Spinner active small className="me-2" />
                      Accesso in corso…
                    </>
                  ) : (
                    <>
                      <Icon icon="it-unlocked" color="white" size="sm" aria-hidden className="me-1" />
                      Accedi
                    </>
                  )}
                </Button>
              </form>
            </CardBody>
          </Card>

          <p className="text-muted small text-center mt-3">
            Devi attivare un account appena creato?{' '}
            <a href="/utenti/attiva">Vai all'attivazione</a>
          </p>
        </Col>
      </Row>
    </Container>
  )
}
