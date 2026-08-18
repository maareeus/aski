import { Link, useNavigate } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { usersApi } from '@/api/endpoints'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'
import { UserForm, VALORI_VUOTI } from './UserForm'

export function UserCreatePage() {
  const navigate = useNavigate()
  const azione = useAzione(usersApi.create)

  return (
    <>
      <div className="mb-4">
        <Button asChild variant="ghost" size="sm" className="-ml-2">
          <Link to="/utenti">
            <ArrowLeft />
            Elenco utenti
          </Link>
        </Button>
      </div>

      <PageHeader
        titolo="Nuovo utente"
        descrizione="La password non si imposta qui: il backend ne genera una casuale, che poi va reimpostata dal dettaglio."
      />

      <div className="max-w-3xl space-y-4">
        {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}

        <UserForm
          modalita="creazione"
          valoriIniziali={VALORI_VUOTI}
          inCorso={azione.inCorso}
          onSubmit={async (valori) => {
            const esito = await azione.esegui({
              email: valori.email.trim(),
              name: valori.name.trim() || null,
              lastName: valori.lastName.trim() || null,
              role: valori.role,
              isActive: valori.isActive,
            })

            // Si va al dettaglio dell'utente creato: è lì che si reimposta la
            // password, unico modo per rendere l'account utilizzabile.
            if (esito) navigate(`/utenti/${esito.id}`, { replace: true })
          }}
        />
      </div>
    </>
  )
}
