import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowDown, ArrowUp, ChevronsUpDown, RotateCcw, Search, UserPlus } from 'lucide-react'
import { flexRender, getCoreRowModel, useReactTable } from '@tanstack/react-table'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { usersApi } from '@/api/endpoints'
import { ROLE_LIST } from '@/api/types'
import type { Role, UserSort } from '@/api/types'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useDebounce } from '@/ui/useDebounce'
import { useRisorsa } from '@/ui/useRisorsa'
import { colonneUtenti } from './users/colonne'
import { useUserListQuery } from './users/useUserListQuery'

const TUTTI = '__tutti__'

export function UsersListPage() {
  const navigate = useNavigate()
  const { query, aggiorna, ordinaPer, azzera, filtriAttivi, PAGE_SIZE_AMMESSE } =
    useUserListQuery()

  // La casella di ricerca è reattiva, la chiamata no: senza debounce
  // partirebbe una richiesta per ogni tasto premuto.
  const [ricercaLocale, setRicercaLocale] = useState(query.search ?? '')
  const ricercaRitardata = useDebounce(ricercaLocale, 300)

  useEffect(() => {
    if (ricercaRitardata !== (query.search ?? '')) aggiorna({ search: ricercaRitardata })
  }, [ricercaRitardata, query.search, aggiorna])

  // Se i filtri vengono azzerati dal bottone, la casella deve seguirli.
  useEffect(() => {
    if ((query.search ?? '') === '' && ricercaLocale !== '' && ricercaRitardata === ricercaLocale) {
      setRicercaLocale('')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query.search])

  const risorsa = useRisorsa(() => usersApi.list(query), [
    query.search,
    query.role,
    query.isActive,
    query.page,
    query.pageSize,
    query.sort,
    query.dir,
  ])

  const colonne = colonneUtenti()
  const dati = risorsa.dati

  const tabella = useReactTable({
    data: dati?.items ?? [],
    columns: colonne,
    getCoreRowModel: getCoreRowModel(),
    // Filtri, ordinamento e paginazione li fa il server. Senza questi flag
    // TanStack filtrerebbe le sole righe già ricevute: l'utente cercherebbe un
    // utente che sta a pagina 4 e concluderebbe che non esiste, senza errori.
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
    rowCount: dati?.totalCount ?? 0,
  })

  const daRiga = dati && dati.totalCount > 0 ? (dati.page - 1) * dati.pageSize + 1 : 0
  const aRiga = dati ? Math.min(dati.page * dati.pageSize, dati.totalCount) : 0

  return (
    <>
      <PageHeader
        titolo="Elenco utenti"
        descrizione="Ricerca, filtri, ordinamento e paginazione sono eseguiti dall'API."
      >
        <Button asChild>
          <Link to="/utenti/nuovo">
            <UserPlus />
            Nuovo utente
          </Link>
        </Button>
      </PageHeader>

      <div className="space-y-4">
        {/* --- barra dei filtri --- */}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1 space-y-1.5">
            <Label htmlFor="filtro-ricerca" className="text-xs">
              Cerca
            </Label>
            <div className="relative">
              <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
              <Input
                id="filtro-ricerca"
                value={ricercaLocale}
                onChange={(e) => setRicercaLocale(e.target.value)}
                placeholder="Email, nome o cognome"
                className="pl-8"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filtro-ruolo" className="text-xs">
              Ruolo
            </Label>
            <Select
              value={query.role === '' ? TUTTI : query.role}
              onValueChange={(v) => aggiorna({ role: v === TUTTI ? '' : (v as Role) })}
            >
              <SelectTrigger id="filtro-ruolo" className="w-full sm:w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TUTTI}>Tutti</SelectItem>
                {ROLE_LIST.map((r) => (
                  <SelectItem key={r} value={r}>
                    {r}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filtro-stato" className="text-xs">
              Stato
            </Label>
            <Select
              value={query.isActive === '' ? TUTTI : String(query.isActive)}
              onValueChange={(v) => aggiorna({ isActive: v === TUTTI ? '' : v === 'true' })}
            >
              <SelectTrigger id="filtro-stato" className="w-full sm:w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TUTTI}>Tutti</SelectItem>
                <SelectItem value="true">Attivi</SelectItem>
                <SelectItem value="false">Da attivare</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {filtriAttivi && (
            <Button variant="ghost" onClick={azzera}>
              <RotateCcw />
              Azzera
            </Button>
          )}
        </div>

        {risorsa.errore && <Esito tono="errore">{risorsa.errore}</Esito>}

        {/* --- tabella --- */}
        <Card className="overflow-hidden py-0">
          <CardContent className="px-0">
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  {tabella.getHeaderGroups().map((gruppo) => (
                    <TableRow key={gruppo.id}>
                      {gruppo.headers.map((header) => {
                        const meta = header.column.columnDef.meta as
                          | { sort?: UserSort; classe?: string }
                          | undefined
                        const ordinabile = meta?.sort !== undefined
                        const attiva = ordinabile && query.sort === meta!.sort

                        return (
                          <TableHead key={header.id} className={meta?.classe}>
                            {ordinabile ? (
                              <button
                                type="button"
                                onClick={() => ordinaPer(meta!.sort!)}
                                className="hover:text-foreground -mx-2 flex items-center gap-1 rounded px-2 py-1"
                                aria-sort={
                                  attiva
                                    ? query.dir === 'asc'
                                      ? 'ascending'
                                      : 'descending'
                                    : 'none'
                                }
                              >
                                {flexRender(header.column.columnDef.header, header.getContext())}
                                {attiva ? (
                                  query.dir === 'asc' ? (
                                    <ArrowUp className="size-3.5" />
                                  ) : (
                                    <ArrowDown className="size-3.5" />
                                  )
                                ) : (
                                  <ChevronsUpDown className="size-3.5 opacity-40" />
                                )}
                              </button>
                            ) : (
                              flexRender(header.column.columnDef.header, header.getContext())
                            )}
                          </TableHead>
                        )
                      })}
                    </TableRow>
                  ))}
                </TableHeader>

                <TableBody>
                  {risorsa.inCorso &&
                    Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={`scheletro-${i}`}>
                        {colonne.map((_c, j) => (
                          <TableCell key={j}>
                            <Skeleton className="h-4 w-full" />
                          </TableCell>
                        ))}
                      </TableRow>
                    ))}

                  {!risorsa.inCorso && tabella.getRowModel().rows.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={colonne.length} className="h-28 text-center">
                        <p className="text-muted-foreground">
                          {filtriAttivi
                            ? 'Nessun utente corrisponde ai filtri impostati.'
                            : 'Nessun utente presente.'}
                        </p>
                      </TableCell>
                    </TableRow>
                  )}

                  {!risorsa.inCorso &&
                    tabella.getRowModel().rows.map((riga) => (
                      <TableRow
                        key={riga.id}
                        onClick={() => navigate(`/utenti/${riga.original.id}`)}
                        className="cursor-pointer"
                      >
                        {riga.getVisibleCells().map((cella) => {
                          const meta = cella.column.columnDef.meta as
                            | { classe?: string }
                            | undefined
                          return (
                            <TableCell key={cella.id} className={meta?.classe}>
                              {flexRender(cella.column.columnDef.cell, cella.getContext())}
                            </TableCell>
                          )
                        })}
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>

        {/* --- paginazione --- */}
        <div className="flex flex-col-reverse items-center justify-between gap-3 sm:flex-row">
          <p className="text-muted-foreground text-sm" aria-live="polite">
            {dati && dati.totalCount > 0
              ? `${daRiga}–${aRiga} di ${dati.totalCount}`
              : 'Nessun risultato'}
          </p>

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Label htmlFor="per-pagina" className="text-muted-foreground text-sm font-normal">
                Per pagina
              </Label>
              <Select
                value={String(query.pageSize)}
                onValueChange={(v) => aggiorna({ pageSize: Number(v) })}
              >
                <SelectTrigger id="per-pagina" size="sm" className="w-[4.5rem]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZE_AMMESSE.map((n) => (
                    <SelectItem key={n} value={String(n)}>
                      {n}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <span className="text-muted-foreground text-sm">
                Pagina {dati?.page ?? 1} di {Math.max(dati?.totalPages ?? 1, 1)}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => aggiorna({ page: (dati?.page ?? 1) - 1 })}
                disabled={!dati?.hasPrevious || risorsa.inCorso}
              >
                Precedente
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => aggiorna({ page: (dati?.page ?? 1) + 1 })}
                disabled={!dati?.hasNext || risorsa.inCorso}
              >
                Successiva
              </Button>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
