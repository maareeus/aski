import type { ReactNode } from 'react'

export function PageHeader({
  titolo,
  descrizione,
  children,
}: {
  titolo: string
  descrizione?: ReactNode
  children?: ReactNode
}) {
  return (
    <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight">{titolo}</h1>
        {descrizione && <p className="text-muted-foreground text-sm">{descrizione}</p>}
      </div>
      {children}
    </div>
  )
}
