import { useState } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { Eye, EyeOff, Loader2, LogIn } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/auth/AuthContext'
import type { TfaAvailable } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { useAzione } from '@/ui/useAzione'
import { TfaStep } from './login/TfaStep'

export function LoginPage() {
  const { isAuthenticated, login, motivoUscita } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [mostraPassword, setMostraPassword] = useState(false)

  // Quando il primo passaggio richiede il secondo fattore, la sfida vive qui:
  // non va in localStorage, dura cinque minuti e non è un token d'accesso.
  const [sfida, setSfida] = useState<{ token: string; metodi: TfaAvailable[] } | null>(null)

  const azione = useAzione(async (email: string, password: string) => {
    const esito = await login(email, password)
    if (esito.stato === 'tfaRichiesta') {
      setSfida({ token: esito.challengeToken, metodi: esito.metodi })
    }
    return esito
  })

  if (isAuthenticated) {
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from && from !== '/login' ? from : '/'} replace />
  }

  return (
    <div className="bg-muted/40 flex min-h-svh items-center justify-center p-4">
      <div className="w-full max-w-sm space-y-6">
        <div className="flex flex-col items-center gap-3 text-center">
          <div className="bg-primary text-primary-foreground flex size-11 items-center justify-center rounded-xl text-lg font-semibold">
            A
          </div>
          <div>
            <h1 className="text-xl font-semibold tracking-tight">Askii Platform</h1>
            <p className="text-muted-foreground text-sm">Pannello di amministrazione</p>
          </div>
        </div>

        {sfida ? (
          <TfaStep
            challengeToken={sfida.token}
            metodi={sfida.metodi}
            onAnnulla={() => {
              setSfida(null)
              setPassword('')
              azione.reset()
            }}
          />
        ) : (
        <Card>
          <CardHeader>
            <CardTitle>Accedi</CardTitle>
            <CardDescription>Inserisci le credenziali del tuo account.</CardDescription>
          </CardHeader>

          <CardContent className="space-y-4">
            {motivoUscita === 'scaduta' && (
              <Esito tono="attenzione" titolo="Sessione scaduta">
                Il token dura 8 ore e non viene rinnovato: effettua di nuovo l'accesso.
              </Esito>
            )}

            {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}

            <form
              className="space-y-4"
              onSubmit={(e) => {
                e.preventDefault()
                void azione.esegui(email, password)
              }}
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="login-email">Email</Label>
                <Input
                  id="login-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="username"
                  placeholder="nome@esempio.it"
                  required
                  disabled={azione.inCorso}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="login-password">Password</Label>
                <div className="relative">
                  <Input
                    id="login-password"
                    type={mostraPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    autoComplete="current-password"
                    className="pr-10"
                    required
                    disabled={azione.inCorso}
                  />
                  <button
                    type="button"
                    onClick={() => setMostraPassword((v) => !v)}
                    className="text-muted-foreground hover:text-foreground absolute inset-y-0 right-0 flex w-10 items-center justify-center rounded-r-md"
                    aria-label={mostraPassword ? 'Nascondi password' : 'Mostra password'}
                  >
                    {mostraPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
              </div>

              <Button
                type="submit"
                className="w-full"
                disabled={azione.inCorso || !email || !password}
              >
                {azione.inCorso ? <Loader2 className="animate-spin" /> : <LogIn />}
                {azione.inCorso ? 'Accesso in corso…' : 'Accedi'}
              </Button>
            </form>
          </CardContent>
        </Card>
        )}

        <p className="text-muted-foreground text-center text-sm">
          Devi attivare un account appena creato?{' '}
          <Link to="/activate" className="text-foreground underline underline-offset-4">
            Vai all'attivazione
          </Link>
        </p>
      </div>
    </div>
  )
}
