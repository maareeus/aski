import { Link } from 'react-router-dom'
import { Callout, CalloutText, CalloutTitle } from 'design-react-kit'
import { PageHeader } from '../ui/PageHeader'

export function NotFoundPage() {
  return (
    <>
      <PageHeader titolo="Pagina non trovata" />
      <Callout color="warning">
        <CalloutTitle>404</CalloutTitle>
        <CalloutText>
          L'indirizzo richiesto non corrisponde a nessuna sezione.{' '}
          <Link to="/">Torna al riepilogo</Link>.
        </CalloutText>
      </Callout>
    </>
  )
}
