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
    <div className="mb-4">
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-2">
        <div>
          <h1 className="h3 mb-1">{titolo}</h1>
          {descrizione && <p className="text-muted mb-0">{descrizione}</p>}
        </div>
        {children}
      </div>
      <hr />
    </div>
  )
}
