import { useState } from 'react'
import { KeyRound, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { usersApi } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

export function ChangePasswordPage() {
  const { session, isAdmin } = useAuth()
  const [suAltroUtente, setSuAltroUtente] = useState(false)
  const [targetId, setTargetId] = useState('')
  const [oldPassword, setOldPassword] = useState('')
  const [password, setPassword] = useState('')
  const [rePassword, setRePassword] = useState('')

  const azione = useAzione(usersApi.changePassword)

  const id = suAltroUtente ? targetId.trim() : (session?.userId ?? '')
  // Il backend salta la verifica della password attuale solo per gli Admin.
  const serveVecchiaPassword = !isAdmin
  const nonCoincidono = password !== '' && rePassword !== '' && password !== rePassword

  const puoInviare =
    !azione.inCorso &&
    id !== '' &&
    password !== '' &&
    rePassword !== '' &&
    !nonCoincidono &&
    (!serveVecchiaPassword || oldPassword !== '')

  return (
    <>
      <PageHeader
        titolo="Cambia password"
        descrizione={
          isAdmin
            ? 'Come Admin puoi cambiare anche la password di altri utenti, senza conoscere quella attuale.'
            : 'Per cambiare la tua password devi indicare quella attuale.'
        }
      />

      <div className="max-w-2xl space-y-4">
        {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}
        {azione.esito?.result && <Esito tono="successo">{azione.esito.msg}</Esito>}

        <Card>
          <CardHeader>
            <CardTitle>Nuova password</CardTitle>
            <CardDescription>
              {suAltroUtente
                ? 'Operazione su un altro account, identificato per id.'
                : `Operazione sul tuo account: ${session?.email ?? ''}`}
            </CardDescription>
          </CardHeader>

          <CardContent>
            <form
              className="space-y-6"
              onSubmit={async (e) => {
                e.preventDefault()
                const esito = await azione.esegui({
                  id,
                  password,
                  rePassword,
                  oldPassword: serveVecchiaPassword ? oldPassword : null,
                })
                if (esito?.result) {
                  setOldPassword('')
                  setPassword('')
                  setRePassword('')
                }
              }}
              noValidate
            >
              {isAdmin && (
                <div className="flex items-start justify-between gap-4 rounded-lg border p-4">
                  <div className="space-y-0.5">
                    <Label htmlFor="pwd-altro">Cambia la password di un altro utente</Label>
                    <p className="text-muted-foreground text-sm">
                      Serve anche per rendere utilizzabile un utente appena creato, la cui password
                      generata non è recuperabile.
                    </p>
                  </div>
                  <Switch
                    id="pwd-altro"
                    checked={suAltroUtente}
                    onCheckedChange={setSuAltroUtente}
                    disabled={azione.inCorso}
                  />
                </div>
              )}

              {suAltroUtente && (
                <div className="space-y-2">
                  <Label htmlFor="pwd-id">Identificativo utente</Label>
                  <Input
                    id="pwd-id"
                    value={targetId}
                    onChange={(e) => setTargetId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                    className="font-mono text-sm"
                    required
                    disabled={azione.inCorso}
                  />
                </div>
              )}

              {serveVecchiaPassword && (
                <div className="space-y-2">
                  <Label htmlFor="pwd-vecchia">Password attuale</Label>
                  <Input
                    id="pwd-vecchia"
                    type="password"
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    autoComplete="current-password"
                    required
                    disabled={azione.inCorso}
                  />
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="pwd-nuova">Nuova password</Label>
                <Input
                  id="pwd-nuova"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                  disabled={azione.inCorso}
                />
                <p className="text-muted-foreground text-sm">
                  L'API non impone requisiti di robustezza: la lunghezza è la difesa più efficace.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="pwd-ripeti">Ripeti la nuova password</Label>
                <Input
                  id="pwd-ripeti"
                  type="password"
                  value={rePassword}
                  onChange={(e) => setRePassword(e.target.value)}
                  autoComplete="new-password"
                  aria-invalid={nonCoincidono}
                  required
                  disabled={azione.inCorso}
                />
                {nonCoincidono && (
                  <p className="text-destructive text-sm">Le due password non corrispondono.</p>
                )}
              </div>

              <Button type="submit" disabled={!puoInviare}>
                {azione.inCorso ? <Loader2 className="animate-spin" /> : <KeyRound />}
                {azione.inCorso ? 'Salvataggio…' : 'Aggiorna password'}
              </Button>
            </form>
          </CardContent>
        </Card>

        {!suAltroUtente && (
          <Esito tono="info">
            Il cambio password non invalida i token già emessi: eventuali sessioni aperte altrove
            restano valide fino alla loro scadenza naturale.
          </Esito>
        )}
      </div>
    </>
  )
}
