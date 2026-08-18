import { useState } from 'react'
import { Loader2, Save } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Separator } from '@/components/ui/separator'
import { Switch } from '@/components/ui/switch'
import { usersApi } from '@/api/endpoints'
import { ROLE_LIST, TFA_LABELS, TfaAvailable } from '@/api/types'
import type { Role } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

const OPZIONI_TFA = [TfaAvailable.EmailOtp, TfaAvailable.AuthenticatorApp]
const NON_MODIFICARE = '__invariato__'

export function UserUpdatePage() {
  const [id, setId] = useState('')
  const [email, setEmail] = useState('')
  const [name, setName] = useState('')
  const [lastName, setLastName] = useState('')
  const [role, setRole] = useState<string>(NON_MODIFICARE)
  const [tfa, setTfa] = useState<TfaAvailable[]>([])
  const [modificaTfa, setModificaTfa] = useState(false)

  const azione = useAzione(usersApi.adminUpdate)

  function toggleTfa(v: TfaAvailable, attivo: boolean) {
    setTfa((prec) => (attivo ? [...new Set([...prec, v])] : prec.filter((x) => x !== v)))
  }

  const nessunCampo =
    !email && !name && !lastName && role === NON_MODIFICARE && !modificaTfa

  return (
    <>
      <PageHeader
        titolo="Modifica utente"
        descrizione="I campi lasciati vuoti non vengono toccati: l'API applica solo quelli valorizzati."
      />

      <div className="max-w-2xl space-y-4">
        {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}
        {azione.esito?.result && <Esito tono="successo">{azione.esito.msg}</Esito>}

        <form
          onSubmit={(e) => {
            e.preventDefault()
            void azione.esegui({
              id: id.trim(),
              email: email.trim() || null,
              name: name.trim() || null,
              lastName: lastName.trim() || null,
              role: role === NON_MODIFICARE ? null : (role as Role),
              tfA_Availables: modificaTfa ? tfa : null,
            })
          }}
          noValidate
          className="space-y-4"
        >
          <Card>
            <CardHeader>
              <CardTitle>Utente da modificare</CardTitle>
              <CardDescription>
                Senza un endpoint di lettura l'identificativo va inserito a mano. Viene restituito
                dalla creazione.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <Label htmlFor="mod-id">Identificativo</Label>
                <Input
                  id="mod-id"
                  value={id}
                  onChange={(e) => setId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  className="font-mono text-sm"
                  required
                  disabled={azione.inCorso}
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Campi da aggiornare</CardTitle>
              <CardDescription>Lascia vuoto ciò che non deve cambiare.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="mod-email">Nuova email</Label>
                <Input
                  id="mod-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={azione.inCorso}
                />
                <p className="text-muted-foreground text-sm">
                  Se già assegnata a un altro utente l'operazione fallisce.
                </p>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="mod-nome">Nome</Label>
                  <Input
                    id="mod-nome"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={azione.inCorso}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="mod-cognome">Cognome</Label>
                  <Input
                    id="mod-cognome"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    disabled={azione.inCorso}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="mod-ruolo">Ruolo</Label>
                <Select value={role} onValueChange={setRole} disabled={azione.inCorso}>
                  <SelectTrigger id="mod-ruolo" className="w-full sm:w-64">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={NON_MODIFICARE}>Non modificare</SelectItem>
                    {ROLE_LIST.map((r) => (
                      <SelectItem key={r} value={r}>
                        {r}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-muted-foreground text-sm">
                  Il super amministratore non può essere declassato: l'API rifiuta il cambio.
                </p>
              </div>

              <Separator />

              <div className="space-y-4">
                <div className="flex items-start justify-between gap-4">
                  <div className="space-y-0.5">
                    <Label htmlFor="mod-tfa-abilita">Sovrascrivi i metodi 2FA</Label>
                    <p className="text-muted-foreground text-sm">
                      L'elenco selezionato sostituisce quello esistente: non selezionare nulla
                      equivale a disattivare la 2FA.
                    </p>
                  </div>
                  <Switch
                    id="mod-tfa-abilita"
                    checked={modificaTfa}
                    onCheckedChange={setModificaTfa}
                    disabled={azione.inCorso}
                  />
                </div>

                {modificaTfa && (
                  <div className="space-y-3 rounded-lg border p-4">
                    {OPZIONI_TFA.map((v) => (
                      <div key={v} className="flex items-center gap-2.5">
                        <Checkbox
                          id={`mod-tfa-${v}`}
                          checked={tfa.includes(v)}
                          onCheckedChange={(c) => toggleTfa(v, c === true)}
                          disabled={azione.inCorso}
                        />
                        <Label htmlFor={`mod-tfa-${v}`} className="font-normal">
                          {TFA_LABELS[v]}
                        </Label>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          <div className="space-y-2">
            <Button type="submit" disabled={azione.inCorso || !id || nessunCampo}>
              {azione.inCorso ? <Loader2 className="animate-spin" /> : <Save />}
              {azione.inCorso ? 'Salvataggio…' : 'Salva modifiche'}
            </Button>
            {nessunCampo && id && (
              <p className="text-muted-foreground text-sm">
                Valorizza almeno un campo da modificare.
              </p>
            )}
          </div>
        </form>
      </div>
    </>
  )
}
