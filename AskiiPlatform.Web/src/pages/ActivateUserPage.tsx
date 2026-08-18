import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CircleCheck, Eye, EyeOff, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { usersApi } from '@/api/endpoints'
import { Esito } from '@/ui/Esito'
import { useAzione } from '@/ui/useAzione'

/** Deve restare allineata a RegolePassword.LunghezzaMinima lato API. */
const LUNGHEZZA_MINIMA = 12

/**
 * Attivazione dell'account: l'utente presenta il codice ricevuto e scelgie la
 * propria password. È l'utente a deciderla, non l'amministratore, che quindi non
 * la conosce.
 *
 * Pagina pubblica: chi la usa non ha ancora un account utilizzabile.
 */
export function ActivateUserPage() {
  const [params] = useSearchParams()

  // Il codice può arrivare dal link nella mail (?code=...) o essere incollato.
  const [codice, setCodice] = useState(params.get('code') ?? '')
  const [password, setPassword] = useState('')
  const [ripeti, setRipeti] = useState('')
  const [mostra, setMostra] = useState(false)

  const azione = useAzione(usersApi.activate)

  const troppoCorta = password !== '' && password.length < LUNGHEZZA_MINIMA
  const nonCoincidono = password !== '' && ripeti !== '' && password !== ripeti
  const puoInviare =
    !azione.inCorso && codice.trim() !== '' && !troppoCorta && !nonCoincidono && ripeti !== ''

  const completata = azione.esito?.result === true

  return (
    <div className="bg-muted/40 flex min-h-svh items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <div className="flex flex-col items-center gap-3 text-center">
          <div className="bg-primary text-primary-foreground flex size-11 items-center justify-center rounded-xl text-lg font-semibold">
            A
          </div>
          <div>
            <h1 className="text-xl font-semibold tracking-tight">Attiva il tuo account</h1>
            <p className="text-muted-foreground text-sm">Askii Platform</p>
          </div>
        </div>

        {completata ? (
          <Card>
            <CardContent className="space-y-4 pt-6">
              <Esito tono="successo" titolo="Account attivato">
                {azione.esito?.msg}
              </Esito>
              <Button asChild className="w-full">
                <Link to="/login">Vai all'accesso</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle>Scegli la tua password</CardTitle>
              <CardDescription>
                Inserisci il codice che hai ricevuto e imposta la password con cui accederai.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4">
              {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}

              <form
                className="space-y-4"
                onSubmit={(e) => {
                  e.preventDefault()
                  void azione.esegui({
                    code: codice.trim(),
                    password,
                    rePassword: ripeti,
                  })
                }}
                noValidate
              >
                <div className="space-y-2">
                  <Label htmlFor="attiva-codice">Codice di attivazione</Label>
                  <Input
                    id="attiva-codice"
                    value={codice}
                    onChange={(e) => setCodice(e.target.value)}
                    className="font-mono"
                    autoComplete="off"
                    required
                    disabled={azione.inCorso}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="attiva-password">Password</Label>
                  <div className="relative">
                    <Input
                      id="attiva-password"
                      type={mostra ? 'text' : 'password'}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      autoComplete="new-password"
                      aria-invalid={troppoCorta}
                      className="pr-10"
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
                  <p className={troppoCorta ? 'text-destructive text-sm' : 'text-muted-foreground text-sm'}>
                    Almeno {LUNGHEZZA_MINIMA} caratteri. Una frase lunga è più sicura di una parola
                    breve con simboli.
                  </p>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="attiva-ripeti">Ripeti la password</Label>
                  <Input
                    id="attiva-ripeti"
                    type={mostra ? 'text' : 'password'}
                    value={ripeti}
                    onChange={(e) => setRipeti(e.target.value)}
                    autoComplete="new-password"
                    aria-invalid={nonCoincidono}
                    required
                    disabled={azione.inCorso}
                  />
                  {nonCoincidono && (
                    <p className="text-destructive text-sm">Le due password non corrispondono.</p>
                  )}
                </div>

                <Button type="submit" className="w-full" disabled={!puoInviare}>
                  {azione.inCorso ? <Loader2 className="animate-spin" /> : <CircleCheck />}
                  {azione.inCorso ? 'Attivazione…' : 'Attiva account'}
                </Button>
              </form>
            </CardContent>
          </Card>
        )}

        <p className="text-muted-foreground text-center text-sm">
          Hai già un account attivo?{' '}
          <Link to="/login" className="text-foreground underline underline-offset-4">
            Vai all'accesso
          </Link>
        </p>
      </div>
    </div>
  )
}
