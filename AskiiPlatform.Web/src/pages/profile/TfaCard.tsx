import { useEffect, useRef, useState } from 'react'
import QRCode from 'qrcode'
import { Check, Copy, KeyRound, Loader2, Mail, ShieldCheck, ShieldOff, Smartphone } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'
import { tfaApi } from '@/api/endpoints'
import { TfaAvailable } from '@/api/types'
import type { AuthenticatorSetupResponse } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { useAzione } from '@/ui/useAzione'
import { useRisorsa } from '@/ui/useRisorsa'

/** Spezza il segreto in gruppi di quattro: va trascritto a mano se il QR non si legge. */
function aGruppi(segreto: string) {
  return segreto.replace(/(.{4})/g, '$1 ').trim()
}

function Qr({ uri }: { uri: string }) {
  const canvas = useRef<HTMLCanvasElement>(null)
  const [errore, setErrore] = useState<string | null>(null)

  useEffect(() => {
    if (!canvas.current) return
    QRCode.toCanvas(canvas.current, uri, { width: 200, margin: 1 }).catch((e: unknown) =>
      setErrore(e instanceof Error ? e.message : 'QR non generato'),
    )
  }, [uri])

  if (errore) {
    return <Esito tono="attenzione">{errore}. Usa il codice testuale qui sotto.</Esito>
  }

  return (
    <div className="bg-white inline-block rounded-lg border p-3">
      <canvas ref={canvas} aria-label="Codice QR per l'app di authenticator" />
    </div>
  )
}

function Associazione({
  setup,
  onFatto,
  onAnnulla,
}: {
  setup: AuthenticatorSetupResponse
  onFatto: () => void
  onAnnulla: () => void
}) {
  const [codice, setCodice] = useState('')
  const [copiato, setCopiato] = useState(false)
  const conferma = useAzione(tfaApi.confermaAuthenticator)

  return (
    <div className="space-y-4 rounded-lg border p-4">
      <div>
        <h4 className="font-medium">1. Inquadra il codice</h4>
        <p className="text-muted-foreground text-sm">
          Apri l'app di authenticator e aggiungi un account inquadrando il QR.
        </p>
      </div>

      <Qr uri={setup.otpauthUri} />

      <div className="space-y-2">
        <Label className="text-sm">Oppure inserisci questo codice a mano</Label>
        <div className="flex items-center gap-2">
          <code className="bg-muted flex-1 rounded px-2 py-1.5 font-mono text-sm break-all select-all">
            {aGruppi(setup.secret)}
          </code>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={async () => {
              await navigator.clipboard.writeText(setup.secret)
              setCopiato(true)
              window.setTimeout(() => setCopiato(false), 2000)
            }}
          >
            {copiato ? <Check /> : <Copy />}
          </Button>
        </div>
        <p className="text-muted-foreground text-xs">
          {setup.digits} cifre, rinnovo ogni {setup.periodSeconds} secondi.
        </p>
      </div>

      <Separator />

      <form
        className="space-y-3"
        onSubmit={async (e) => {
          e.preventDefault()
          const esito = await conferma.esegui(codice)
          if (esito?.result) onFatto()
        }}
        noValidate
      >
        <div>
          <h4 className="font-medium">2. Conferma con un codice</h4>
          <p className="text-muted-foreground text-sm">
            Serve a verificare che l'app sia configurata: senza questa conferma il metodo non viene
            attivato, per non rischiare di restare chiusi fuori.
          </p>
        </div>

        {conferma.errore && <Esito tono="errore">{conferma.errore}</Esito>}

        <div className="space-y-2">
          <Label htmlFor="tfa-conferma">Codice dall'app</Label>
          <Input
            id="tfa-conferma"
            value={codice}
            onChange={(e) => setCodice(e.target.value.replace(/\D/g, '').slice(0, 6))}
            inputMode="numeric"
            autoComplete="one-time-code"
            placeholder="000000"
            className="max-w-40 text-center font-mono tracking-[0.3em]"
            disabled={conferma.inCorso}
          />
        </div>

        <div className="flex gap-2">
          <Button type="submit" disabled={conferma.inCorso || codice.length !== 6}>
            {conferma.inCorso ? <Loader2 className="animate-spin" /> : <ShieldCheck />}
            Attiva
          </Button>
          <Button type="button" variant="ghost" onClick={onAnnulla} disabled={conferma.inCorso}>
            Annulla
          </Button>
        </div>
      </form>
    </div>
  )
}

export function TfaCard() {
  const stato = useRisorsa(() => tfaApi.stato(), [])
  const avvia = useAzione(tfaApi.avviaAuthenticator)
  const disattivaApp = useAzione(tfaApi.disattivaAuthenticator)
  const attivaEmail = useAzione(tfaApi.attivaEmail)
  const disattivaEmail = useAzione(tfaApi.disattivaEmail)

  const [setup, setSetup] = useState<AuthenticatorSetupResponse | null>(null)

  const metodi = stato.dati?.methods ?? []
  const appAttiva = metodi.includes(TfaAvailable.AuthenticatorApp)
  const emailAttiva = metodi.includes(TfaAvailable.EmailOtp)
  const inCorso =
    avvia.inCorso || disattivaApp.inCorso || attivaEmail.inCorso || disattivaEmail.inCorso

  async function ricarica() {
    setSetup(null)
    stato.ricarica()
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle>Autenticazione a due fattori</CardTitle>
            <CardDescription>
              Un secondo fattore rende inutile la sola password in caso di furto.
            </CardDescription>
          </div>
          {stato.dati &&
            (stato.dati.enabled ? (
              <Badge variant="outline" className="shrink-0 gap-1 border-emerald-500/40 text-emerald-700">
                <ShieldCheck className="size-3" />
                Attiva
              </Badge>
            ) : (
              <Badge variant="outline" className="shrink-0 gap-1">
                <ShieldOff className="size-3" />
                Non attiva
              </Badge>
            ))}
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {stato.errore && <Esito tono="errore">{stato.errore}</Esito>}
        {avvia.errore && <Esito tono="errore">{avvia.errore}</Esito>}
        {disattivaApp.errore && <Esito tono="errore">{disattivaApp.errore}</Esito>}
        {attivaEmail.errore && <Esito tono="errore">{attivaEmail.errore}</Esito>}
        {disattivaEmail.errore && <Esito tono="errore">{disattivaEmail.errore}</Esito>}

        {stato.inCorso && (
          <div className="space-y-3">
            <Skeleton className="h-14 w-full" />
            <Skeleton className="h-14 w-full" />
          </div>
        )}

        {!stato.inCorso && stato.dati && (
          <>
            {/* --- app di authenticator --- */}
            <div className="space-y-3">
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                  <Smartphone className="text-muted-foreground mt-0.5 size-4 shrink-0" />
                  <div>
                    <p className="font-medium">App di authenticator</p>
                    <p className="text-muted-foreground text-sm">
                      Codice a 6 cifre generato sul dispositivo, funziona anche senza rete.
                    </p>
                  </div>
                </div>

                {appAttiva ? (
                  <Button
                    variant="outline"
                    size="sm"
                    className="shrink-0"
                    disabled={inCorso}
                    onClick={async () => {
                      const esito = await disattivaApp.esegui()
                      if (esito?.result) void ricarica()
                    }}
                  >
                    {disattivaApp.inCorso ? <Loader2 className="animate-spin" /> : <ShieldOff />}
                    Disattiva
                  </Button>
                ) : (
                  !setup && (
                    <Button
                      size="sm"
                      className="shrink-0"
                      disabled={inCorso}
                      onClick={async () => {
                        const esito = await avvia.esegui()
                        if (esito) setSetup(esito)
                      }}
                    >
                      {avvia.inCorso ? <Loader2 className="animate-spin" /> : <KeyRound />}
                      Configura
                    </Button>
                  )
                )}
              </div>

              {setup && (
                <Associazione
                  setup={setup}
                  onFatto={() => void ricarica()}
                  onAnnulla={() => setSetup(null)}
                />
              )}

              {!setup && stato.dati.authenticatorPending && !appAttiva && (
                <Esito tono="attenzione">
                  Un'associazione era stata avviata ma non confermata: il metodo non è attivo.
                  Premi Configura per ricominciare.
                </Esito>
              )}
            </div>

            <Separator />

            {/* --- codice via email --- */}
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-3">
                <Mail className="text-muted-foreground mt-0.5 size-4 shrink-0" />
                <div>
                  <p className="font-medium">Codice via email</p>
                  <p className="text-muted-foreground text-sm">
                    Codice valido 5 minuti inviato all'indirizzo dell'account. Richiede il server
                    SMTP configurato nelle Impostazioni.
                  </p>
                </div>
              </div>

              <Switch
                checked={emailAttiva}
                disabled={inCorso}
                aria-label="Codice via email"
                onCheckedChange={async (attivo) => {
                  const esito = attivo
                    ? await attivaEmail.esegui()
                    : await disattivaEmail.esegui()
                  if (esito?.result) void ricarica()
                }}
              />
            </div>

            {stato.dati.enabled && (
              <Esito tono="info">
                Al prossimo accesso, dopo la password ti verrà chiesto un codice. Se perdi il
                secondo fattore, un amministratore può azzerarlo dal tuo dettaglio utente.
              </Esito>
            )}
          </>
        )}
      </CardContent>
    </Card>
  )
}
