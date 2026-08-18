import { useState } from 'react'
import { Link } from 'react-router-dom'
import { CircleCheck, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { usersApi } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

/**
 * Raggiungibile anche senza autenticazione, perché l'endpoint è anonimo:
 * quando l'utente non è collegato la pagina si presenta da sola, senza layout.
 */
export function ActivateUserPage() {
  const { isAuthenticated } = useAuth()
  const [userId, setUserId] = useState('')
  const azione = useAzione(usersApi.activate)

  const modulo = (
    <div className="space-y-4">
      {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}
      {azione.esito?.result && <Esito tono="successo">{azione.esito.msg}</Esito>}

      <Card>
        <CardHeader>
          <CardTitle>Attivazione</CardTitle>
          <CardDescription>
            Inserisci l'identificativo dell'account da abilitare.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault()
              void azione.esegui({ userId: userId.trim() })
            }}
            noValidate
          >
            <div className="space-y-2">
              <Label htmlFor="attiva-id">Identificativo utente</Label>
              <Input
                id="attiva-id"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                className="font-mono text-sm"
                required
                disabled={azione.inCorso}
              />
            </div>

            <Button type="submit" disabled={azione.inCorso || !userId}>
              {azione.inCorso ? <Loader2 className="animate-spin" /> : <CircleCheck />}
              {azione.inCorso ? 'Attivazione…' : 'Attiva utente'}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Esito tono="attenzione" titolo="Nota di sicurezza">
        Questo endpoint è anonimo e l'identificativo è l'unica informazione richiesta: chi lo conosce
        può attivare l'account. Con l'introduzione del codice di attivazione questa maschera dovrà
        chiedere quel codice al posto dell'id.
      </Esito>
    </div>
  )

  if (isAuthenticated) {
    return (
      <>
        <PageHeader
          titolo="Attiva utente"
          descrizione="Abilita l'accesso a un account creato non attivo."
        />
        <div className="max-w-2xl">{modulo}</div>
      </>
    )
  }

  return (
    <div className="bg-muted/40 flex min-h-svh items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <div className="text-center">
          <h1 className="text-xl font-semibold tracking-tight">Attivazione account</h1>
          <p className="text-muted-foreground text-sm">Askii Platform</p>
        </div>
        {modulo}
        <p className="text-center text-sm">
          <Link to="/login" className="text-foreground underline underline-offset-4">
            Torna all'accesso
          </Link>
        </p>
      </div>
    </div>
  )
}
