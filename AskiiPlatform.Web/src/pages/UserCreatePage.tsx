import { useState } from 'react'
import { Loader2, UserPlus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { usersApi } from '@/api/endpoints'
import { ROLE_LIST, Roles } from '@/api/types'
import type { Role } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'

export function UserCreatePage() {
  const [email, setEmail] = useState('')
  const [name, setName] = useState('')
  const [lastName, setLastName] = useState('')
  const [role, setRole] = useState<Role>(Roles.Client)
  const [isActive, setIsActive] = useState(false)

  const azione = useAzione(usersApi.create)

  function svuota() {
    setEmail('')
    setName('')
    setLastName('')
    setRole(Roles.Client)
    setIsActive(false)
  }

  return (
    <>
      <PageHeader
        titolo="Nuovo utente"
        descrizione="La password non si imposta qui: è il backend a generarne una casuale."
      />

      <div className="max-w-2xl space-y-4">
        {azione.errore && <Esito tono="errore">{azione.errore}</Esito>}

        {azione.esito && (
          <Esito tono="successo" titolo="Utente creato">
            <div className="space-y-2">
              <p>
                <span className="font-medium">{azione.esito.email}</span> — ruolo{' '}
                {azione.esito.role}, {azione.esito.isActive ? 'attivo' : 'da attivare'}.
              </p>
              <div>
                <p className="mb-1">Identificativo, da conservare per le operazioni successive:</p>
                <code className="bg-background/60 block rounded px-2 py-1.5 font-mono text-xs break-all select-all">
                  {azione.esito.id}
                </code>
              </div>
              <p className="text-sm">
                La password generata non viene restituita dall'API e non esiste ancora un invio
                email: per rendere l'account utilizzabile va impostata da <em>Cambia password</em>{' '}
                indicando questo identificativo.
              </p>
            </div>
          </Esito>
        )}

        <Card>
          <CardHeader>
            <CardTitle>Dati dell'account</CardTitle>
            <CardDescription>
              L'email viene normalizzata in minuscolo e deve essere univoca.
            </CardDescription>
          </CardHeader>

          <CardContent>
            <form
              className="space-y-6"
              onSubmit={async (e) => {
                e.preventDefault()
                const esito = await azione.esegui({
                  email: email.trim(),
                  name: name.trim() || null,
                  lastName: lastName.trim() || null,
                  role,
                  isActive,
                })
                if (esito) svuota()
              }}
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="crea-email">Email</Label>
                <Input
                  id="crea-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="nome@esempio.it"
                  required
                  disabled={azione.inCorso}
                />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="crea-nome">Nome</Label>
                  <Input
                    id="crea-nome"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={azione.inCorso}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="crea-cognome">Cognome</Label>
                  <Input
                    id="crea-cognome"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    disabled={azione.inCorso}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="crea-ruolo">Ruolo</Label>
                <Select
                  value={role}
                  onValueChange={(v) => setRole(v as Role)}
                  disabled={azione.inCorso}
                >
                  <SelectTrigger id="crea-ruolo" className="w-full sm:w-64">
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
              </div>

              <div className="flex items-start justify-between gap-4 rounded-lg border p-4">
                <div className="space-y-0.5">
                  <Label htmlFor="crea-attivo">Crea l'utente già attivo</Label>
                  <p className="text-muted-foreground text-sm">
                    Se disattivato, l'utente va attivato separatamente.
                  </p>
                </div>
                <Switch
                  id="crea-attivo"
                  checked={isActive}
                  onCheckedChange={setIsActive}
                  disabled={azione.inCorso}
                />
              </div>

              <div className="flex gap-2">
                <Button type="submit" disabled={azione.inCorso || !email}>
                  {azione.inCorso ? <Loader2 className="animate-spin" /> : <UserPlus />}
                  {azione.inCorso ? 'Creazione…' : 'Crea utente'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={svuota}
                  disabled={azione.inCorso}
                >
                  Svuota
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
