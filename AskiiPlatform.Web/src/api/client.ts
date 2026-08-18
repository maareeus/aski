import { tokenCorrente } from '@/auth/sessione'
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

type UnauthorizedHandler = () => void

/**
 * Il token NON passa da qui: viene letto da `tokenCorrente()` a ogni richiesta,
 * così è disponibile anche prima che gli effect di AuthProvider siano girati.
 * Resta configurabile solo la reazione al 401, che per definizione può avvenire
 * soltanto dopo che una richiesta è partita.
 */
let onUnauthorized: UnauthorizedHandler = () => {}

export function configureClient(opts: { onUnauthorized: UnauthorizedHandler }) {
  onUnauthorized = opts.onUnauthorized
}

async function messaggioDiErrore(res: Response): Promise<string> {
  // L'API risponde con ProblemDetails sia da ResultsHelper sia dal
  // GlobalExceptionHandler; il campo utile è `detail`.
  try {
    const body = (await res.json()) as ProblemDetails
    return body.detail || body.title || `Errore ${res.status}`
  } catch {
    // Le risposte di autorizzazione arrivano senza corpo: il messaggio va
    // costruito qui, altrimenti l'utente leggerebbe "Errore 403 Forbidden".
    if (res.status === 401) return 'Sessione non valida o scaduta'
    if (res.status === 403) return 'Non hai i permessi per eseguire questa operazione'
    return `Errore ${res.status} ${res.statusText}`.trim()
  }
}

async function invia<TResponse>(path: string, init: RequestInit): Promise<TResponse> {
  const token = tokenCorrente()

  const res = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      ...init.headers,
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
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

  // Non basta controllare il 204: un endpoint può rispondere 200 con corpo
  // vuoto (Results.Ok() senza valore), e in quel caso res.json() solleverebbe
  // "Unexpected end of JSON input" facendo apparire come errore una scrittura
  // andata a buon fine.
  const testo = await res.text()
  return (testo ? (JSON.parse(testo) as TResponse) : (undefined as TResponse))
}

export function post<TResponse>(path: string, body: unknown): Promise<TResponse> {
  return invia<TResponse>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export type QueryParams = Record<string, string | number | boolean | null | undefined>

/**
 * I parametri null, undefined e stringa vuota vengono omessi, non inviati vuoti:
 * `?isActive=` fallisce il binding di `bool?` lato API con un 400, mentre
 * "filtro non applicato" si esprime togliendo il parametro.
 */
export function get<TResponse>(path: string, params?: QueryParams): Promise<TResponse> {
  const qs = new URLSearchParams()

  for (const [chiave, valore] of Object.entries(params ?? {})) {
    if (valore === null || valore === undefined || valore === '') continue
    qs.set(chiave, String(valore))
  }

  const query = qs.toString()
  return invia<TResponse>(query ? `${path}?${query}` : path, { method: 'GET' })
}
