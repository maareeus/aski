import { Link } from 'react-router-dom'
import { Badge, Callout, CalloutText, CalloutTitle, Card, CardBody, CardText, CardTitle, Col, Icon, Row } from 'design-react-kit'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../ui/PageHeader'

function formattaData(d: Date | null) {
  if (!d) return '—'
  return d.toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' })
}

export function DashboardPage() {
  const { session, isAdmin, scadenza } = useAuth()

  const azioni = [
    { to: '/utenti/nuovo', label: 'Nuovo utente', icon: 'it-plus-circle' as const, soloAdmin: true },
    { to: '/utenti/modifica', label: 'Modifica utente', icon: 'it-pencil' as const, soloAdmin: true },
    { to: '/utenti/attiva', label: 'Attiva utente', icon: 'it-check-circle' as const, soloAdmin: false },
    { to: '/password', label: 'Cambia password', icon: 'it-key' as const, soloAdmin: false },
  ].filter((a) => !a.soloAdmin || isAdmin)

  return (
    <>
      <PageHeader
        titolo="Riepilogo"
        descrizione="Stato della sessione corrente e scorciatoie alle operazioni disponibili."
      />

      <Row className="mb-4">
        <Col xs="12" md="6" xl="4" className="mb-3">
          <Card shadow="sm" fullHeight>
            <CardBody>
              <CardTitle tag="h2" className="h6 text-muted text-uppercase">
                Utente collegato
              </CardTitle>
              <CardText>
                <strong className="d-block">{session?.fullName?.trim() || '—'}</strong>
                <span className="text-muted">{session?.email}</span>
              </CardText>
              <Badge color={isAdmin ? 'primary' : 'secondary'} pill>
                {session?.role}
              </Badge>
            </CardBody>
          </Card>
        </Col>

        <Col xs="12" md="6" xl="4" className="mb-3">
          <Card shadow="sm" fullHeight>
            <CardBody>
              <CardTitle tag="h2" className="h6 text-muted text-uppercase">
                Sessione
              </CardTitle>
              <CardText>
                Scade il <strong>{formattaData(scadenza)}</strong>
              </CardText>
              <p className="text-muted small mb-0">
                Alla scadenza l'accesso viene chiuso automaticamente: il token non è rinnovabile.
              </p>
            </CardBody>
          </Card>
        </Col>

        <Col xs="12" md="6" xl="4" className="mb-3">
          <Card shadow="sm" fullHeight>
            <CardBody>
              <CardTitle tag="h2" className="h6 text-muted text-uppercase">
                Identificativo
              </CardTitle>
              <CardText>
                <code className="user-select-all">{session?.userId}</code>
              </CardText>
              <p className="text-muted small mb-0">
                Serve alle operazioni su utente, che identificano la risorsa per id.
              </p>
            </CardBody>
          </Card>
        </Col>
      </Row>

      <h2 className="h5 mb-3">Operazioni</h2>
      <Row className="mb-4">
        {azioni.map((a) => (
          <Col xs="12" sm="6" xl="3" key={a.to} className="mb-3">
            <Card shadow="sm" fullHeight>
              <CardBody className="d-flex flex-column">
                <Icon icon={a.icon} color="primary" size="lg" aria-hidden className="mb-2" />
                <CardTitle tag="h3" className="h6">
                  {a.label}
                </CardTitle>
                <Link to={a.to} className="mt-auto">
                  Apri
                </Link>
              </CardBody>
            </Card>
          </Col>
        ))}
      </Row>

      <Callout color="warning">
        <CalloutTitle>Dati aggregati non disponibili</CalloutTitle>
        <CalloutText>
          Questa pagina mostra solo informazioni che arrivano dalla risposta di login, perché l'API
          non espone ancora endpoint di lettura: non esiste un <code>GET /user</code> per elencare
          gli utenti né un <code>GET /user/{'{id}'}</code> per leggerne uno. Finché non ci sono,
          qualunque conteggio (utenti totali, attivi, per ruolo) sarebbe inventato.
        </CalloutText>
      </Callout>
    </>
  )
}
