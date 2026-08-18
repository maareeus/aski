import { Check, ChevronRight, ShieldCheck, X } from 'lucide-react'
import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import type { UserListItem, UserSort } from '@/api/types'

function dataOra(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('it-IT', { dateStyle: 'short', timeStyle: 'short' })
}

/**
 * `meta.sort` porta la chiave accettata dalla SortMap del backend: le colonne
 * senza quella chiave non sono ordinabili e l'intestazione non mostra il
 * controllo. Così l'elenco degli ordinamenti possibili è dichiarato una volta
 * sola e le due parti non possono divergere in silenzio.
 */
export type ColonnaUtente = ColumnDef<UserListItem> & {
  meta?: { sort?: UserSort; classe?: string }
}

export function colonneUtenti(): ColonnaUtente[] {
  return [
    {
      accessorKey: 'email',
      header: 'Email',
      meta: { sort: 'email' },
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{row.original.email}</span>
          {row.original.isSuperAdmin && (
            <ShieldCheck
              className="text-muted-foreground size-3.5 shrink-0"
              aria-label="Super amministratore"
            />
          )}
        </div>
      ),
    },
    {
      accessorKey: 'fullName',
      header: 'Nome',
      meta: { sort: 'lastname' },
      cell: ({ row }) => row.original.fullName || <span className="text-muted-foreground">—</span>,
    },
    {
      accessorKey: 'role',
      header: 'Ruolo',
      meta: { sort: 'role' },
      cell: ({ row }) => (
        <Badge variant={row.original.role === 'Admin' ? 'default' : 'secondary'}>
          {row.original.role}
        </Badge>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Stato',
      meta: { sort: 'status' },
      cell: ({ row }) =>
        row.original.isActive ? (
          <span className="text-foreground inline-flex items-center gap-1.5 text-sm">
            <Check className="size-3.5 text-emerald-600" />
            Attivo
          </span>
        ) : (
          <span className="text-muted-foreground inline-flex items-center gap-1.5 text-sm">
            <X className="size-3.5" />
            Da attivare
          </span>
        ),
    },
    {
      accessorKey: 'lastLoginUtc',
      header: 'Ultimo accesso',
      meta: { sort: 'lastlogin', classe: 'hidden lg:table-cell' },
      cell: ({ row }) => (
        <span className="text-muted-foreground text-sm">{dataOra(row.original.lastLoginUtc)}</span>
      ),
    },
    {
      // La riga è cliccabile e porta al dettaglio, dove stanno tutte le azioni
      // sul singolo utente: qui basta il segno che si può aprire.
      id: 'apri',
      header: '',
      meta: { classe: 'w-8' },
      cell: () => <ChevronRight className="text-muted-foreground size-4" aria-hidden />,
    },
  ]
}
