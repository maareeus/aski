import { useState } from 'react'
import { Check, Copy, Eye, EyeOff, KeyRound, Loader2, Shuffle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { usersApi } from '@/api/endpoints'
import { Esito } from '@/ui/Esito'
import { useAzione } from '@/ui/useAzione'

/**
 * Stesso alfabeto di Common/Helpers/SecretGenerator.cs: esclude i caratteri
 * ambigui (I, l, 1, O, 0) perché queste password vengono lette e ricopiate.
 */
const ALFABETO = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'

/** 16 caratteri su 57 simboli = ~93 bit di entropia. */
function generaPassword(lunghezza = 16): string {
  const byte = new Uint32Array(lunghezza)
  crypto.getRandomValues(byte)
  // Il modulo introduce un bias trascurabile con 2^32 valori su 57 simboli.
  return Array.from(byte, (n) => ALFABETO[n % ALFABETO.length]).join('')
}

export function ResetPasswordDialog({
  userId,
  email,
  onFatto,
}: {
  userId: string
  email: string
  onFatto?: () => void
}) {
  const [aperto, setAperto] = useState(false)
  const [password, setPassword] = useState('')
  const [ripeti, setRipeti] = useState('')
  const [mostra, setMostra] = useState(false)
  const [copiato, setCopiato] = useState(false)
  const [generata, setGenerata] = useState(false)

  const azione = useAzione(usersApi.changePassword)

  function reimposta() {
    setPassword('')
    setRipeti('')
    setMostra(false)
    setCopiato(false)
    setGenerata(false)
    azione.reset()
  }

  function genera() {
    const nuova = generaPassword()
    setPassword(nuova)
    setRipeti(nuova)
    setMostra(true)
    setGenerata(true)
    setCopiato(false)
  }

  async function copia() {
    await navigator.clipboard.writeText(password)
    setCopiato(true)
    window.setTimeout(() => setCopiato(false), 2000)
  }

  const nonCoincidono = password !== '' && ripeti !== '' && password !== ripeti
  const puoInviare = !azione.inCorso && password !== '' && ripeti !== '' && !nonCoincidono

  return (
    <Dialog
      open={aperto}
      onOpenChange={(v) => {
        setAperto(v)
        if (!v) reimposta()
      }}
    >
      <DialogTrigger asChild>
        <Button variant="outline" className="w-full justify-start">
          <KeyRound />
          Reimposta password
        </Button>
      </DialogTrigger>

      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Reimposta password</DialogTitle>
          <DialogDescription>
            Imposti direttamente la nuova password di <strong>{email}</strong>. Come Admin non devi
            conoscere quella attuale.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}

          {azione.esito?.result ? (
            <Esito tono="successo" titolo="Password aggiornata">
              {generata ? (
                <div className="space-y-2">
                  <p>Comunicala all'utente ora: non sarà più recuperabile.</p>
                  <div className="flex items-center gap-2">
                    <code className="bg-background/60 flex-1 rounded px-2 py-1.5 font-mono text-sm select-all">
                      {password}
                    </code>
                    <Button type="button" variant="outline" size="sm" onClick={copia}>
                      {copiato ? <Check /> : <Copy />}
                    </Button>
                  </div>
                </div>
              ) : (
                azione.esito.msg
              )}
            </Esito>
          ) : (
            <form
              className="space-y-4"
              onSubmit={async (e) => {
                e.preventDefault()
                const esito = await azione.esegui({
                  id: userId,
                  password,
                  rePassword: ripeti,
                  oldPassword: null,
                })
                if (esito?.result) onFatto?.()
              }}
              noValidate
            >
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="reset-password">Nuova password</Label>
                  <Button type="button" variant="ghost" size="sm" onClick={genera}>
                    <Shuffle />
                    Genera
                  </Button>
                </div>
                <div className="relative">
                  <Input
                    id="reset-password"
                    type={mostra ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => {
                      setPassword(e.target.value)
                      setGenerata(false)
                    }}
                    autoComplete="new-password"
                    className="pr-10 font-mono"
                    required
                    disabled={azione.inCorso}
                  />
                  <button
                    type="button"
                    onClick={() => setMostra((v) => !v)}
                    className="text-muted-foreground hover:text-foreground absolute inset-y-0 right-0 flex w-10 items-center justify-center"
                    aria-label={mostra ? 'Nascondi password' : 'Mostra password'}
                  >
                    {mostra ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
                <p className="text-muted-foreground text-sm">
                  L'API non impone requisiti di robustezza: la lunghezza è la difesa più efficace.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="reset-ripeti">Ripeti la password</Label>
                <Input
                  id="reset-ripeti"
                  type={mostra ? 'text' : 'password'}
                  value={ripeti}
                  onChange={(e) => setRipeti(e.target.value)}
                  autoComplete="new-password"
                  aria-invalid={nonCoincidono}
                  className="font-mono"
                  required
                  disabled={azione.inCorso}
                />
                {nonCoincidono && (
                  <p className="text-destructive text-sm">Le due password non corrispondono.</p>
                )}
              </div>

              <DialogFooter>
                <Button type="submit" disabled={!puoInviare}>
                  {azione.inCorso ? <Loader2 className="animate-spin" /> : <KeyRound />}
                  Reimposta
                </Button>
              </DialogFooter>
            </form>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
