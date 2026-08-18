import type { ReactNode } from 'react'
import { CircleAlert, CircleCheck, Info, TriangleAlert } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

type Tono = 'errore' | 'successo' | 'info' | 'attenzione'

const ICONE = {
  errore: CircleAlert,
  successo: CircleCheck,
  info: Info,
  attenzione: TriangleAlert,
} as const

/**
 * Riquadro di esito o nota. Alert di shadcn ha solo le varianti `default` e
 * `destructive`: successo, info e attenzione si ottengono con classi di colore.
 */
export function Esito({
  tono,
  titolo,
  children,
  className,
}: {
  tono: Tono
  titolo?: ReactNode
  children?: ReactNode
  className?: string
}) {
  const Icona = ICONE[tono]

  const stile: Record<Tono, string> = {
    errore: '',
    successo:
      'border-emerald-500/40 bg-emerald-50 text-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100 [&>svg]:text-emerald-600',
    info: 'border-sky-500/40 bg-sky-50 text-sky-900 dark:bg-sky-950/40 dark:text-sky-100 [&>svg]:text-sky-600',
    attenzione:
      'border-amber-500/40 bg-amber-50 text-amber-900 dark:bg-amber-950/40 dark:text-amber-100 [&>svg]:text-amber-600',
  }

  return (
    <Alert
      variant={tono === 'errore' ? 'destructive' : 'default'}
      className={[stile[tono], className].filter(Boolean).join(' ')}
      role={tono === 'errore' ? 'alert' : 'status'}
    >
      <Icona />
      {titolo && <AlertTitle>{titolo}</AlertTitle>}
      {children && <AlertDescription>{children}</AlertDescription>}
    </Alert>
  )
}
