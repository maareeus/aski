import { useState } from 'react'
import { Loader2, Save, UserPlus } from 'lucide-react'
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
import { ROLE_LIST, Roles, TFA_LABELS, TfaAvailable } from '@/api/types'
import type { Role } from '@/api/types'

const OPZIONI_TFA = [TfaAvailable.EmailOtp, TfaAvailable.AuthenticatorApp]

export interface ValoriUtente {
  email: string
  name: string
  lastName: string
  role: Role
  /** Solo in creazione: in modifica l'attivazione è un'azione a parte. */
  isActive: boolean
  /** Solo in modifica: la creazione non accetta metodi 2FA. */
  tfa: TfaAvailable[]
}

export const VALORI_VUOTI: ValoriUtente = {
  email: '',
  name: '',
  lastName: '',
  role: Roles.Client,
  isActive: false,
  tfa: [],
}

/**
 * Maschera condivisa fra creazione e modifica. I campi differiscono perché
 * l'API li tratta in modo diverso — la creazione accetta IsActive e ignora i
 * metodi 2FA, la modifica il contrario — ma l'aspetto è lo stesso.
 */
export function UserForm({
  modalita,
  valoriIniziali,
  superAdmin = false,
  inCorso,
  onSubmit,
  children,
}: {
  modalita: 'creazione' | 'modifica'
  valoriIniziali: ValoriUtente
  superAdmin?: boolean
  inCorso: boolean
  onSubmit: (valori: ValoriUtente) => void
  children?: React.ReactNode
}) {
  const [valori, setValori] = useState(valoriIniziali)
  const inModifica = modalita === 'modifica'

  const campo = <K extends keyof ValoriUtente>(chiave: K, valore: ValoriUtente[K]) =>
    setValori((prec) => ({ ...prec, [chiave]: valore }))

  function toggleTfa(v: TfaAvailable, attivo: boolean) {
    campo('tfa', attivo ? [...new Set([...valori.tfa, v])] : valori.tfa.filter((x) => x !== v))
  }

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit(valori)
      }}
      noValidate
    >
      <Card>
        <CardHeader>
          <CardTitle>Anagrafica</CardTitle>
          <CardDescription>
            L'email viene normalizzata in minuscolo e deve essere univoca.
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="utente-email">Email</Label>
            <Input
              id="utente-email"
              type="email"
              value={valori.email}
              onChange={(e) => campo('email', e.target.value)}
              placeholder="nome@esempio.it"
              required
              disabled={inCorso}
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="utente-nome">Nome</Label>
              <Input
                id="utente-nome"
                value={valori.name}
                onChange={(e) => campo('name', e.target.value)}
                disabled={inCorso}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="utente-cognome">Cognome</Label>
              <Input
                id="utente-cognome"
                value={valori.lastName}
                onChange={(e) => campo('lastName', e.target.value)}
                disabled={inCorso}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="utente-ruolo">Ruolo</Label>
            <Select
              value={valori.role}
              onValueChange={(v) => campo('role', v as Role)}
              disabled={inCorso || superAdmin}
            >
              <SelectTrigger id="utente-ruolo" className="w-full sm:w-64">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ROLE_LIST.map((r) => (
                  <SelectItem key={r} value={r}>
                    {r}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {superAdmin && (
              <p className="text-muted-foreground text-sm">
                Il super amministratore non può cambiare ruolo: l'API rifiuta la modifica.
              </p>
            )}
          </div>

          {!inModifica && (
            <>
              <Separator />
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-0.5">
                  <Label htmlFor="utente-attivo">Crea l'utente già attivo</Label>
                  <p className="text-muted-foreground text-sm">
                    Se disattivato, va attivato in un secondo momento dal suo dettaglio.
                  </p>
                </div>
                <Switch
                  id="utente-attivo"
                  checked={valori.isActive}
                  onCheckedChange={(c) => campo('isActive', c)}
                  disabled={inCorso}
                />
              </div>
            </>
          )}

          {inModifica && (
            <>
              <Separator />
              <fieldset className="space-y-3">
                <legend className="text-sm font-medium">Autenticazione a due fattori</legend>
                <p className="text-muted-foreground text-sm">
                  L'elenco salvato sostituisce quello attuale: nessuna casella selezionata
                  disattiva la 2FA.
                </p>
                {OPZIONI_TFA.map((v) => (
                  <div key={v} className="flex items-center gap-2.5">
                    <Checkbox
                      id={`utente-tfa-${v}`}
                      checked={valori.tfa.includes(v)}
                      onCheckedChange={(c) => toggleTfa(v, c === true)}
                      disabled={inCorso}
                    />
                    <Label htmlFor={`utente-tfa-${v}`} className="font-normal">
                      {TFA_LABELS[v]}
                    </Label>
                  </div>
                ))}
              </fieldset>
            </>
          )}
        </CardContent>
      </Card>

      {children}

      <div className="flex gap-2">
        <Button type="submit" disabled={inCorso || !valori.email}>
          {inCorso ? <Loader2 className="animate-spin" /> : inModifica ? <Save /> : <UserPlus />}
          {inCorso ? 'Salvataggio…' : inModifica ? 'Salva modifiche' : 'Crea utente'}
        </Button>
      </div>
    </form>
  )
}
