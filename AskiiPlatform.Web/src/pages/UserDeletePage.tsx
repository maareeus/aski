import { useState } from 'react'
import { Loader2, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { usersApi } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

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

      <div className="max-w-2xl space-y-4">
        {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}
        {azione.esito?.result && <Esito tono="successo">{azione.esito.msg}</Esito>}

        <Card className="border-destructive/30">
          <CardHeader>
            <CardTitle className="text-destructive">Eliminazione definitiva</CardTitle>
            <CardDescription>
              Il super amministratore non è eliminabile, e non puoi eliminare te stesso.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form
              className="space-y-6"
              onSubmit={(e) => {
                e.preventDefault()
                void azione.esegui({ userId: userId.trim() })
                setConfermato(false)
              }}
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="del-id">Identificativo utente</Label>
                <Input
                  id="del-id"
                  value={userId}
                  onChange={(e) => {
                    setUserId(e.target.value)
                    setConfermato(false)
                  }}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  className="font-mono text-sm"
                  required
                  disabled={azione.inCorso}
                />
              </div>

              {seStesso && (
                <Esito tono="attenzione">
                  Questo è il tuo identificativo. L'API rifiuta l'autocancellazione.
                </Esito>
              )}

              <div className="flex items-start gap-2.5">
                <Checkbox
                  id="del-conferma"
                  checked={confermato}
                  onCheckedChange={(c) => setConfermato(c === true)}
                  disabled={azione.inCorso || !userId}
                />
                <Label htmlFor="del-conferma" className="font-normal leading-snug">
                  Confermo di voler eliminare definitivamente questo utente
                </Label>
              </div>

              <Button
                type="submit"
                variant="destructive"
                disabled={azione.inCorso || !userId || !confermato}
              >
                {azione.inCorso ? <Loader2 className="animate-spin" /> : <Trash2 />}
                {azione.inCorso ? 'Eliminazione…' : 'Elimina definitivamente'}
              </Button>
            </form>
          </CardContent>
        </Card>

        <p className="text-muted-foreground text-sm">
          Se l'identificativo non esiste, l'API risponde con lo stesso messaggio del tentativo di
          autocancellazione: i due casi non sono distinguibili.
        </p>
      </div>
    </>
  )
}
