import { Link } from 'react-router-dom'
import { Callout, CalloutText, CalloutTitle, Icon } from 'design-react-kit'
import { PageHeader } from '../ui/PageHeader'

/**
 * Non c'è un endpoint di lettura: nessun GET /user, nessun GET /user/{id}.
 * Questa pagina dichiara il vincolo invece di mostrare una tabella con dati
 * finti, e indica cosa serve lato API perché l'elenco funzioni.
 */
export function UsersListPage() {
  return (
    <>
      <PageHeader
        titolo="Elenco utenti"
        descrizione="La sezione è predisposta, ma l'API non espone ancora la lettura degli utenti."
      >
        {/* Link con l'aspetto di un bottone: resta una navigazione, non un'azione */}
        <Link to="/utenti/nuovo" className="btn btn-primary">
          <Icon icon="it-plus-circle" color="white" size="sm" aria-hidden className="me-1" />
          Nuovo utente
        </Link>
      </PageHeader>

      <Callout color="danger">
        <CalloutTitle>Endpoint mancante</CalloutTitle>
        <CalloutText>
          <p>
            AskiiPlatform.Api espone solo operazioni di scrittura. Per popolare questa tabella
            servono due endpoint che oggi non esistono:
          </p>
          <ul>
            <li>
              <code>GET /api/v1/user/admin/list</code> — elenco utenti, preferibilmente paginato,
              con <code>Id</code>, <code>Email</code>, <code>FullName</code>, <code>Role</code>,{' '}
              <code>IsActive</code>, <code>LastLoginUtc</code>
            </li>
            <li>
              <code>GET /api/v1/user/admin/{'{id}'}</code> — singolo utente, per precompilare la
              maschera di modifica invece di far digitare il GUID a mano
            </li>
          </ul>
          <p className="mb-0">
            Gli indici su <code>Role</code> e <code>IsActive</code> in{' '}
            <code>UserConfiguration.cs</code> sono già pronti per filtrare l'elenco. Appena gli
            endpoint sono disponibili, questa pagina e le maschere di modifica ed eliminazione
            possono passare dalla ricerca per id a una selezione dalla lista.
          </p>
        </CalloutText>
      </Callout>

      <div className="mt-4">
        <h2 className="h5">Nel frattempo</h2>
        <p className="text-muted">
          Le operazioni su un utente esistente richiedono il suo identificativo, che viene
          restituito dalla creazione:
        </p>
        <ul>
          <li>
            <Link to="/utenti/modifica">Modifica utente</Link> — anagrafica, ruolo, email, metodi 2FA
          </li>
          <li>
            <Link to="/utenti/attiva">Attiva utente</Link>
          </li>
          <li>
            <Link to="/utenti/elimina">Elimina utente</Link>
          </li>
        </ul>
      </div>
    </>
  )
}
