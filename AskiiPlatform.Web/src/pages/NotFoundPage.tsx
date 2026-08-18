import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { PageHeader } from '@/ui/PageHeader'

export function NotFoundPage() {
  return (
    <>
      <PageHeader
        titolo="Pagina non trovata"
        descrizione="L'indirizzo richiesto non corrisponde a nessuna sezione."
      />
      <Button asChild variant="outline">
        <Link to="/">Torna al riepilogo</Link>
      </Button>
    </>
  )
}
