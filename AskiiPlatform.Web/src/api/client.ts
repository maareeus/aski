import type { ProblemDetails } from './types'

const BASE = '/api/v1'

/** Errore con il messaggio già estratto dal ProblemDetails dell'API. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }

  get isUnauthorized() {
    return this.status === 401
  }
}

type TokenReader = () => string | null
type UnauthorizedHandler = () => void

let readToken: TokenReader = () => null
let onUnauthorized: UnauthorizedHandler = () => {}

/**
 * Collega il client allo stato di autenticazione. Lo fa AuthProvider all'avvio,
 * così i moduli api/ non dipendono da React.
 */
export function configureClient(opts: {
  readToken: TokenReader
  onUnauthorized: UnauthorizedHandler
}) {
  readToken = opts.readToken
  onUnauthorized = opts.onUnauthorized
}

async function messaggioDiErrore(res: Response): Promise<string> {
  // L'API risponde con ProblemDetails sia da ResultsHelper sia dal
  // GlobalExceptionHandler; il campo utile è `detail`.
  try {
    const body = (await res.json()) as ProblemDetails
    return body.detail || body.title || `Errore ${res.status}`
  } catch {
    return res.status === 401
      ? 'Sessione non valida o scaduta'
      : `Errore ${res.status} ${res.statusText}`.trim()
  }
}

export async function post<TResponse>(path: string, body: unknown): Promise<TResponse> {
  const token = readToken()

  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(body),
  })

  if (res.status === 401) {
    const msg = await messaggioDiErrore(res)
    // 401 su una chiamata autenticata significa token scaduto o revocato:
    // la sessione locale non è più utilizzabile.
    if (token) onUnauthorized()
    throw new ApiError(401, msg)
  }

  if (!res.ok) {
    throw new ApiError(res.status, await messaggioDiErrore(res))
  }

  if (res.status === 204) return undefined as TResponse
  return (await res.json()) as TResponse
}
