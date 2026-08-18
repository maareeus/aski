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
          <Link to="/users">
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

        {azione.esito?.activationCode && (
          <Esito
            tono={azione.esito.activationEmailSent ? 'successo' : 'attenzione'}
            titolo="Codice di attivazione"
          >
            <div className="space-y-2">
              <p>
                {azione.esito.activationEmailSent
                  ? `Inviato a ${azione.esito.email}. L'utente scegliera la propria password durante l'attivazione.`
                  : "L'invio per email non e riuscito: comunica il codice manualmente."}
              </p>
              <code className="bg-background/60 block rounded px-2 py-1.5 font-mono text-xs break-all select-all">
                {azione.esito.activationCode}
              </code>
            </div>
          </Esito>
        )}

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

            // Con un utente non attivo si resta qui, perché il codice di
            // attivazione va letto o copiato: al dettaglio non verrebbe più
            // mostrato. Se è già attivo non c'è nulla da leggere.
            if (esito && esito.isActive) navigate(`/users/${esito.id}`, { replace: true })
          }}
        />
      </div>
    </>
  )
}
