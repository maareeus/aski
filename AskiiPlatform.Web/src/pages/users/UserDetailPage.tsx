import { useCallback, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  ArrowLeft,
  Check,
  CircleCheck,
  Copy,
  Loader2,
  ShieldCheck,
  ShieldOff,
  Trash2,
} from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { tfaApi, usersApi } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAzione } from '@/ui/useAzione'
import { useRisorsa } from '@/ui/useRisorsa'
import { ResetPasswordDialog } from './ResetPasswordDialog'
import { UserForm } from './UserForm'
import type { ValoriUtente } from './UserForm'

function dataOra(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' })
}

export function UserDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { session } = useAuth()

  const risorsa = useRisorsa(() => usersApi.get(id), [id])
  const utente = risorsa.dati

  const salva = useAzione(usersApi.adminUpdate)
  const attiva = useAzione(usersApi.activate)
  const elimina = useAzione(usersApi.remove)
  const resetTfa = useAzione(tfaApi.resetAdmin)

  const [idCopiato, setIdCopiato] = useState(false)
  const copiaId = useCallback(async () => {
    await navigator.clipboard.writeText(id)
    setIdCopiato(true)
    window.setTimeout(() => setIdCopiato(false), 2000)
  }, [id])

  const seStesso = utente?.id === session?.userId

  if (risorsa.inCorso) {
    return (
      <>
        <PageHeader titolo="Dettaglio utente" />
        <div className="max-w-3xl space-y-4">
          <Skeleton className="h-9 w-64" />
          <Skeleton className="h-72 w-full" />
        </div>
      </>
    )
  }

  if (risorsa.errore || !utente) {
    return (
      <>
        <PageHeader titolo="Dettaglio utente" />
        <div className="max-w-2xl space-y-4">
          <Esito tono="errore" titolo="Utente non disponibile">
            {risorsa.errore ?? 'Nessun utente con questo identificativo.'}
          </Esito>
          <Button asChild variant="outline">
            <Link to="/users">
              <ArrowLeft />
              Torna all'elenco
            </Link>
          </Button>
        </div>
      </>
    )
  }

  const valoriIniziali: ValoriUtente = {
    email: utente.email,
    name: utente.name,
    lastName: utente.lastName,
    role: utente.role,
    isActive: utente.isActive,
    tfa: utente.tfA_Availables ?? [],
  }

  return (
    <>
      <div className="mb-4">
        <Button asChild variant="ghost" size="sm" className="-ml-2">
          <Link to="/users">
            <ArrowLeft />
            Elenco utenti
          </Link>
        </Button>
      </div>

      <PageHeader
        titolo={utente.fullName || utente.email}
        descrizione={
          <span className="flex flex-wrap items-center gap-2">
            <span>{utente.email}</span>
            <Badge variant={utente.role === 'Admin' ? 'default' : 'secondary'}>{utente.role}</Badge>
            {utente.isActive ? (
              <Badge variant="outline" className="border-emerald-500/40 text-emerald-700">
                Attivo
              </Badge>
            ) : (
              <Badge variant="outline">Da attivare</Badge>
            )}
            {utente.isSuperAdmin && (
              <Badge variant="outline" className="gap-1">
                <ShieldCheck className="size-3" />
                Super admin
              </Badge>
            )}
          </span>
        }
      />

      <div className="grid max-w-5xl gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          {salva.errore && <Esito tono="errore">{salva.errore}</Esito>}
          {salva.esito?.result && <Esito tono="successo">{salva.esito.msg}</Esito>}

          {/* La chiave rimonta il form quando il dettaglio viene ricaricato,
              così i campi ripartono dai valori appena salvati. */}
          <UserForm
            key={utente.updatedAtUtc ?? utente.createdAtUtc}
            modalita="modifica"
            valoriIniziali={valoriIniziali}
            superAdmin={utente.isSuperAdmin}
            inCorso={salva.inCorso}
            onSubmit={async (valori) => {
              const esito = await salva.esegui({
                id: utente.id,
                email: valori.email !== utente.email ? valori.email : null,
                name: valori.name,
                lastName: valori.lastName,
                role: utente.isSuperAdmin ? null : valori.role,
                tfA_Availables: valori.tfa,
              })
              if (esito?.result) risorsa.ricarica()
            }}
          />
        </div>

        {/* --- colonna azioni --- */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-sm">Azioni</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {!utente.isActive && (
                <>
                  {attiva.errore && <Esito tono="errore">{attiva.errore}</Esito>}
                  <Button
                    variant="outline"
                    className="w-full justify-start"
                    disabled={attiva.inCorso}
                    onClick={async () => {
                      const esito = await attiva.esegui({ userId: utente.id })
                      if (esito?.result) risorsa.ricarica()
                    }}
                  >
                    {attiva.inCorso ? <Loader2 className="animate-spin" /> : <CircleCheck />}
                    Attiva utente
                  </Button>
                </>
              )}

              <ResetPasswordDialog userId={utente.id} email={utente.email} />

              {/* Recupero: l'utente che ha perso il secondo fattore non può
                  rientrare da solo. */}
              {resetTfa.errore && <Esito tono="errore">{resetTfa.errore}</Esito>}
              {resetTfa.esito?.result && <Esito tono="successo">{resetTfa.esito.msg}</Esito>}
              <Button
                variant="outline"
                className="w-full justify-start"
                disabled={resetTfa.inCorso}
                onClick={async () => {
                  const esito = await resetTfa.esegui(utente.id)
                  if (esito?.result) risorsa.ricarica()
                }}
              >
                {resetTfa.inCorso ? <Loader2 className="animate-spin" /> : <ShieldOff />}
                Azzera 2FA
              </Button>

              <Button variant="outline" className="w-full justify-start" onClick={copiaId}>
                {idCopiato ? <Check /> : <Copy />}
                {idCopiato ? 'Identificativo copiato' : 'Copia identificativo'}
              </Button>

              <Separator className="my-3" />

              {elimina.errore && <Esito tono="errore">{elimina.errore}</Esito>}

              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button
                    variant="outline"
                    className="text-destructive hover:bg-destructive/10 w-full justify-start"
                    disabled={utente.isSuperAdmin || seStesso || elimina.inCorso}
                  >
                    {elimina.inCorso ? <Loader2 className="animate-spin" /> : <Trash2 />}
                    Elimina utente
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>Eliminare {utente.email}?</AlertDialogTitle>
                    <AlertDialogDescription>
                      La cancellazione è definitiva: non esiste soft-delete né ripristino.
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>Annulla</AlertDialogCancel>
                    <AlertDialogAction
                      onClick={async () => {
                        const esito = await elimina.esegui({ userId: utente.id })
                        if (esito?.result) navigate('/users', { replace: true })
                      }}
                    >
                      Elimina definitivamente
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>

              {utente.isSuperAdmin && (
                <p className="text-muted-foreground text-sm">
                  Il super amministratore non è eliminabile.
                </p>
              )}
              {seStesso && !utente.isSuperAdmin && (
                <p className="text-muted-foreground text-sm">
                  Non puoi eliminare il tuo stesso account.
                </p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-sm">Cronologia</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="space-y-3 text-sm">
                <div>
                  <dt className="text-muted-foreground text-xs tracking-wide uppercase">Creato</dt>
                  <dd>{dataOra(utente.createdAtUtc)}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-xs tracking-wide uppercase">
                    Ultima modifica
                  </dt>
                  <dd>{dataOra(utente.updatedAtUtc)}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-xs tracking-wide uppercase">
                    Ultimo accesso
                  </dt>
                  <dd>{dataOra(utente.lastLoginUtc)}</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </div>
      </div>
    </>
  )
}
