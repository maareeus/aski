import type { ReactElement } from 'react'
import { ROLE_LIST } from '../api/types'

/**
 * Il Select del design system tipizza `children` come ReactElement<'option'>,
 * che `Array.map` non produce: la costruzione delle opzioni è centralizzata qui
 * con un'unica asserzione, invece di ripeterla in ogni pagina.
 */
function comeOpzioni(elementi: ReactElement[]): ReactElement<'option'>[] {
  return elementi as ReactElement<'option'>[]
}

export function opzioniRuolo(placeholder?: string): ReactElement<'option'>[] {
  const opzioni = ROLE_LIST.map((r) => (
    <option key={r} value={r}>
      {r}
    </option>
  ))

  if (placeholder) {
    opzioni.unshift(
      <option key="" value="">
        {placeholder}
      </option>,
    )
  }

  return comeOpzioni(opzioni)
}
