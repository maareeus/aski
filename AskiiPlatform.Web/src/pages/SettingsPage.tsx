import { useMemo, useState } from 'react'
import { Eye, EyeOff, Loader2, Mail, RotateCcw, Save } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { settingsApi } from '@/api/endpoints'
import { OPTION } from '@/api/types'
import type { SettingItem } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'
import { useRisorsa } from '@/ui/useRisorsa'

/**
 * Etichette e tipo di campo per le opzioni note. Le opzioni che il backend
 * espone senza essere elencate qui vengono mostrate comunque, con il loro nome
 * tecnico: così un'opzione nuova lato API non resta invisibile.
 */
const DESCRITTORI: Record<string, { etichetta: string; aiuto?: string; tipo?: 'number' }> = {
  [OPTION.smtpHost]: { etichetta: 'Host SMTP', aiuto: 'Es. smtp.example.com' },
  [OPTION.smtpPort]: { etichetta: 'Porta SMTP', aiuto: 'Tipicamente 587 con STARTTLS, 465 con TLS', tipo: 'number' },
  [OPTION.smtpUser]: { etichetta: 'Utente SMTP' },
  [OPTION.smtpPassword]: { etichetta: 'Password SMTP' },
}

const GRUPPI: { titolo: string; descrizione: string; icona: typeof Mail; opzioni: string[] }[] = [
  {
    titolo: 'Posta in uscita',
    descrizione:
      'Server SMTP usato per le comunicazioni verso gli utenti, come i codici di attivazione.',
    icona: Mail,
    opzioni: [OPTION.smtpHost, OPTION.smtpPort, OPTION.smtpUser, OPTION.smtpPassword],
  },
]

function CampoOpzione({
  voce,
  valore,
  onChange,
  disabilitato,
}: {
  voce: SettingItem
  valore: string
  onChange: (v: string) => void
  disabilitato: boolean
}) {
  const [mostra, setMostra] = useState(false)
  const descrittore = DESCRITTORI[voce.name]
  const etichetta = descrittore?.etichetta ?? voce.name

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <Label htmlFor={`opt-${voce.name}`}>{etichetta}</Label>
        {!descrittore && (
          <Badge variant="outline" className="font-mono text-xs">
            {voce.name}
          </Badge>
        )}
        {voce.isSecret && voce.hasValue && valore === '' && (
          <Badge variant="secondary" className="text-xs">
            Configurata
          </Badge>
        )}
      </div>

      <div className="relative">
        <Input
          id={`opt-${voce.name}`}
          type={voce.isSecret && !mostra ? 'password' : (descrittore?.tipo ?? 'text')}
          value={valore}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabilitato}
          className={voce.isSecret ? 'pr-10' : undefined}
          autoComplete={voce.isSecret ? 'new-password' : 'off'}
          placeholder={
            voce.isSecret
              ? voce.hasValue
                ? 'Lascia vuoto per non cambiarla'
                : 'Non impostata'
              : undefined
          }
        />
        {voce.isSecret && (
          <button
            type="button"
            onClick={() => setMostra((v) => !v)}
            className="text-muted-foreground hover:text-foreground absolute inset-y-0 right-0 flex w-10 items-center justify-center"
            aria-label={mostra ? 'Nascondi' : 'Mostra'}
          >
            {mostra ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
          </button>
        )}
      </div>

      {descrittore?.aiuto && <p className="text-muted-foreground text-sm">{descrittore.aiuto}</p>}
      {voce.isSecret && (
        <p className="text-muted-foreground text-sm">
          Il valore attuale non viene restituito dall'API. Scrivine uno nuovo per sostituirlo.
        </p>
      )}
    </div>
  )
}

export function SettingsPage() {
  const risorsa = useRisorsa(() => settingsApi.get(), [])
  const salva = useAzione(settingsApi.update)

  // Le modifiche non ancora salvate, indicizzate per nome opzione.
  const [modifiche, setModifiche] = useState<Record<string, string>>({})

  const voci = risorsa.dati?.items ?? []
  const perNome = useMemo(() => new Map(voci.map((v) => [v.name, v])), [voci])

  /**
   * Le segrete partono sempre vuote, perché il loro valore non arriva: un campo
   * vuoto significa "non toccare", non "svuota".
   */
  const valoreDi = (voce: SettingItem) =>
    modifiche[voce.name] ?? (voce.isSecret ? '' : (voce.value ?? ''))

  const daInviare = Object.entries(modifiche).filter(([nome, valore]) => {
    const voce = perNome.get(nome)
    if (!voce) return false
    if (voce.isSecret) return valore !== ''
    return valore !== (voce.value ?? '')
  })

  const nonSalvate = daInviare.length

  // Opzioni esposte dall'API ma non previste da nessun gruppo.
  const nomiRaggruppati = new Set(GRUPPI.flatMap((g) => g.opzioni))
  const altre = voci.filter((v) => !nomiRaggruppati.has(v.name))

  return (
    <>
      <PageHeader
        titolo="Impostazioni"
        descrizione="Configurazione dell'applicazione, salvata nella tabella Options."
      >
        {nonSalvate > 0 && (
          <Button variant="ghost" onClick={() => setModifiche({})} disabled={salva.inCorso}>
            <RotateCcw />
            Annulla modifiche
          </Button>
        )}
      </PageHeader>

      <div className="max-w-2xl space-y-4">
        {risorsa.errore && <Esito tono="errore">{risorsa.errore}</Esito>}
        {salva.errore && <Esito tono="errore">{salva.errore}</Esito>}
        {salva.esito !== null && nonSalvate === 0 && (
          <Esito tono="successo">Impostazioni salvate.</Esito>
        )}

        {risorsa.inCorso && (
          <Card>
            <CardContent className="space-y-4 pt-6">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="space-y-2">
                  <Skeleton className="h-4 w-28" />
                  <Skeleton className="h-9 w-full" />
                </div>
              ))}
            </CardContent>
          </Card>
        )}

        {!risorsa.inCorso && voci.length === 0 && !risorsa.errore && (
          <Esito tono="attenzione" titolo="Nessuna opzione">
            La tabella Options è vuota. Le voci vengono inserite all'avvio dell'API da{' '}
            <code className="font-mono">Options.Seed()</code>: se non ci sono, l'inizializzazione
            non è andata a buon fine.
          </Esito>
        )}

        {!risorsa.inCorso && voci.length > 0 && (
          <form
            className="space-y-4"
            onSubmit={async (e) => {
              e.preventDefault()
              const esito = await salva.esegui({ options: Object.fromEntries(daInviare) })
              if (esito !== null) {
                setModifiche({})
                risorsa.ricarica()
              }
            }}
          >
            {GRUPPI.map((gruppo) => {
              const presenti = gruppo.opzioni.map((n) => perNome.get(n)).filter(Boolean) as SettingItem[]
              if (presenti.length === 0) return null

              return (
                <Card key={gruppo.titolo}>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <gruppo.icona className="size-4" />
                      {gruppo.titolo}
                    </CardTitle>
                    <CardDescription>{gruppo.descrizione}</CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    {presenti.map((voce) => (
                      <CampoOpzione
                        key={voce.name}
                        voce={voce}
                        valore={valoreDi(voce)}
                        disabilitato={salva.inCorso}
                        onChange={(v) => setModifiche((prec) => ({ ...prec, [voce.name]: v }))}
                      />
                    ))}
                  </CardContent>
                </Card>
              )
            })}

            {altre.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle>Altre opzioni</CardTitle>
                  <CardDescription>
                    Presenti a database ma non ancora previste da questa schermata.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {altre.map((voce) => (
                    <CampoOpzione
                      key={voce.name}
                      voce={voce}
                      valore={valoreDi(voce)}
                      disabilitato={salva.inCorso}
                      onChange={(v) => setModifiche((prec) => ({ ...prec, [voce.name]: v }))}
                    />
                  ))}
                </CardContent>
              </Card>
            )}

            <div className="flex items-center gap-3">
              <Button type="submit" disabled={salva.inCorso || nonSalvate === 0}>
                {salva.inCorso ? <Loader2 className="animate-spin" /> : <Save />}
                {salva.inCorso ? 'Salvataggio…' : 'Salva impostazioni'}
              </Button>
              {nonSalvate > 0 && (
                <p className="text-muted-foreground text-sm">
                  {nonSalvate === 1 ? '1 modifica non salvata' : `${nonSalvate} modifiche non salvate`}
                </p>
              )}
            </div>
          </form>
        )}

        <Esito tono="attenzione" titolo="Come sono conservate">
          I valori stanno in chiaro nella tabella <code className="font-mono">Options</code>, password
          SMTP compresa: chi legge il database la legge. Per un ambiente di produzione servirebbe un
          secret store, o almeno la cifratura del campo. L'API la nasconde in lettura, ma questo
          protegge solo dal transito, non dall'archiviazione.
        </Esito>
      </div>
    </>
  )
}
