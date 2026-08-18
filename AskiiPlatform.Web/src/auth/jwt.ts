/**
 * Lettura del JWT lato client, per il solo scopo di conoscere la scadenza.
 * L'identità (id, email, ruolo) arriva dalla response di login, non dal token:
 * TokenService mescola nomi brevi (`sub`, `email`) e URI WS-* per nome e ruolo,
 * quindi dipendere da quelle chiavi renderebbe il client fragile.
 *
 * Qui non si valida nulla: la firma è verificata solo dal backend.
 */

interface JwtPayload {
  exp?: number
  [claim: string]: unknown
}

function decodeBase64Url(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=')
  const binary = atob(padded)
  // Il payload può contenere caratteri non ASCII (es. nomi accentati).
  const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0))
  return new TextDecoder().decode(bytes)
}

export function readPayload(token: string): JwtPayload | null {
  const parts = token.split('.')
  if (parts.length !== 3) return null
  try {
    return JSON.parse(decodeBase64Url(parts[1])) as JwtPayload
  } catch {
    return null
  }
}

/** Istante di scadenza, o null se il token non la dichiara. */
export function expiresAt(token: string): Date | null {
  const exp = readPayload(token)?.exp
  return typeof exp === 'number' ? new Date(exp * 1000) : null
}

/** Considera scaduto anche ciò che scade entro `margineSecondi`. */
export function isExpired(token: string, margineSecondi = 30): boolean {
  const scadenza = expiresAt(token)
  if (!scadenza) return false // senza exp non possiamo dire che sia scaduto
  return scadenza.getTime() - margineSecondi * 1000 <= Date.now()
}
