import { useEffect, useRef, useState } from 'react'
import { ArrowLeft, KeyRound, Loader2, Mail, Send, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { authApi } from '@/api/endpoints'
import { TFA_LABELS, TfaAvailable } from '@/api/types'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { useAzione } from '@/ui/useAzione'

const CIFRE_TOTP = 6

/**
 * Secondo passaggio del login. Riceve la sfida emessa dal primo e la usa per
 * completare la verifica: la sessione non esiste ancora, quindi tutto passa da
 * quel token e non da un bearer.
 */
export function TfaStep({
  challengeToken,
  metodi,
  onAnnulla,
}: {
  challengeToken: string
  metodi: TfaAvailable[]
  onAnnulla: () => void
}) {
  const { completaTfa } = useAuth()

  // Se è disponibile l'app si parte da lì: è immediata, l'email richiede un invio.
  const [metodo, setMetodo] = useState<TfaAvailable>(
    metodi.includes(TfaAvailable.AuthenticatorApp)
      ? TfaAvailable.AuthenticatorApp
      : (metodi[0] ?? TfaAvailable.EmailOtp),
  )
  const [codice, setCodice] = useState('')

  const verifica = useAzione(completaTfa)
  const invio = useAzione(authApi.sendOtp)

  const campoCodice = useRef<HTMLInputElement>(null)
  useEffect(() => {
    campoCodice.current?.focus()
  }, [metodo])

  const inCorso = verifica.inCorso || invio.inCorso
  const codicePronto = codice.length === CIFRE_TOTP

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ShieldCheck className="size-4" />
          Verifica in due passaggi
        </CardTitle>
        <CardDescription>
          {metodo === TfaAvailable.AuthenticatorApp
            ? 'Inserisci il codice mostrato dalla tua app di authenticator.'
            : 'Ti inviamo un codice via email, valido 5 minuti.'}
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {verifica.errore && <Esito tono="errore">{verifica.errore}</Esito>}
        {invio.errore && <Esito tono="errore">{invio.errore}</Esito>}
        {invio.esito?.result && <Esito tono="successo">{invio.esito.msg}</Esito>}

        {/* Selettore del metodo, solo se l'utente ne ha più di uno */}
        {metodi.length > 1 && (
          <div className="flex gap-2">
            {metodi.map((m) => (
              <Button
                key={m}
                type="button"
                variant={metodo === m ? 'default' : 'outline'}
                size="sm"
                className="flex-1"
                onClick={() => {
                  setMetodo(m)
                  setCodice('')
                  verifica.reset()
                  invio.reset()
                }}
                disabled={inCorso}
              >
                {m === TfaAvailable.AuthenticatorApp ? <KeyRound /> : <Mail />}
                {m === TfaAvailable.AuthenticatorApp ? 'App' : 'Email'}
              </Button>
            ))}
          </div>
        )}

        {metodo === TfaAvailable.EmailOtp && (
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={() => void invio.esegui({ challengeToken })}
            disabled={inCorso}
          >
            {invio.inCorso ? <Loader2 className="animate-spin" /> : <Send />}
            {invio.esito?.result ? 'Invia di nuovo il codice' : 'Invia il codice'}
          </Button>
        )}

        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault()
            void verifica.esegui(challengeToken, metodo, codice)
          }}
          noValidate
        >
          <div className="space-y-2">
            <Label htmlFor="tfa-codice">Codice a {CIFRE_TOTP} cifre</Label>
            <Input
              id="tfa-codice"
              ref={campoCodice}
              value={codice}
              // Solo cifre e lunghezza fissa: evita di inviare richieste che
              // il backend rifiuterebbe comunque per formato.
              onChange={(e) => setCodice(e.target.value.replace(/\D/g, '').slice(0, CIFRE_TOTP))}
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder="000000"
              className="text-center font-mono text-lg tracking-[0.4em]"
              disabled={inCorso}
            />
            <p className="text-muted-foreground text-sm">{TFA_LABELS[metodo]}</p>
          </div>

          <Button type="submit" className="w-full" disabled={inCorso || !codicePronto}>
            {verifica.inCorso ? <Loader2 className="animate-spin" /> : <ShieldCheck />}
            {verifica.inCorso ? 'Verifica…' : 'Verifica e accedi'}
          </Button>
        </form>

        <Button type="button" variant="ghost" className="w-full" onClick={onAnnulla} disabled={inCorso}>
          <ArrowLeft />
          Usa un altro account
        </Button>
      </CardContent>
    </Card>
  )
}
