import { Link } from 'react-router-dom'
import { CircleCheck, Pencil, Trash2, UserPlus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'

const SCORCIATOIE = [
  { to: '/utenti/modifica', label: 'Modifica utente', icon: Pencil },
  { to: '/utenti/attiva', label: 'Attiva utente', icon: CircleCheck },
  { to: '/utenti/elimina', label: 'Elimina utente', icon: Trash2 },
]

/**
 * Non c'è un endpoint di lettura: nessun GET /user, nessun GET /user/{id}.
 * La pagina dichiara il vincolo invece di mostrare una tabella con dati finti,
 * e indica cosa serve lato API perché l'elenco funzioni.
 */
export function UsersListPage() {
  return (
    <>
      <PageHeader
        titolo="Elenco utenti"
        descrizione="La sezione è predisposta, ma l'API non espone ancora la lettura degli utenti."
      >
        <Button asChild>
          <Link to="/utenti/nuovo">
            <UserPlus />
            Nuovo utente
          </Link>
        </Button>
      </PageHeader>

      <div className="max-w-3xl space-y-4">
        <Esito tono="errore" titolo="Endpoint mancante">
          <div className="space-y-3">
            <p>
              AskiiPlatform.Api espone solo operazioni di scrittura. Per popolare questa tabella
              servono due endpoint che oggi non esistono:
            </p>
            <ul className="list-disc space-y-1 pl-5">
              <li>
                <code className="font-mono text-xs">GET /api/v1/user/admin/list</code> — elenco
                paginato con id, email, nome, ruolo, stato e ultimo accesso
              </li>
              <li>
                <code className="font-mono text-xs">GET /api/v1/user/admin/{'{id}'}</code> — singolo
                utente, per precompilare la maschera di modifica invece di far digitare il GUID
              </li>
            </ul>
            <p>
              Gli indici su <code className="font-mono text-xs">Role</code> e{' '}
              <code className="font-mono text-xs">IsActive</code> in{' '}
              <code className="font-mono text-xs">UserConfiguration.cs</code> sono già pronti per
              filtrare l'elenco.
            </p>
          </div>
        </Esito>

        <Card>
          <CardHeader>
            <CardTitle>Nel frattempo</CardTitle>
            <CardDescription>
              Le operazioni su un utente esistente richiedono il suo identificativo, restituito dalla
              creazione.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2">
            {SCORCIATOIE.map((s) => (
              <Button key={s.to} asChild variant="outline" size="sm">
                <Link to={s.to}>
                  <s.icon />
                  {s.label}
                </Link>
              </Button>
            ))}
          </CardContent>
        </Card>
      </div>
    </>
  )
}
