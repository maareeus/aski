/**
 * Tipi allineati ai record dell'API (AskiiPlatform.Api/Features/**).
 * Se cambi un record lato backend, questo file va aggiornato di conseguenza.
 */

export const Roles = {
  Admin: 'Admin',
  Operator: 'Operator',
  Client: 'Client',
} as const

export type Role = (typeof Roles)[keyof typeof Roles]

export const ROLE_LIST: Role[] = [Roles.Admin, Roles.Operator, Roles.Client]

/**
 * L'API non registra JsonStringEnumConverter, quindi gli enum viaggiano come
 * numeri: i valori qui devono rispettare l'ordine di dichiarazione in
 * Features/Auth/AuthEndpoints.cs. Aggiungere un valore in mezzo lato C#
 * cambierebbe il significato di questi numeri.
 *
 * Const object e non `enum` perché il progetto compila con erasableSyntaxOnly.
 */
export const TfaAvailable = {
  EmailOtp: 0,
  AuthenticatorApp: 1,
} as const

export type TfaAvailable = (typeof TfaAvailable)[keyof typeof TfaAvailable]

export const TFA_LABELS: Record<TfaAvailable, string> = {
  [TfaAvailable.EmailOtp]: 'Codice OTP via email (valido 5 minuti)',
  [TfaAvailable.AuthenticatorApp]: 'App di authenticator',
}

/** Busta di risposta degli elenchi paginati (Common/Paging/PagedResult.cs). */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

// --- /user/admin/list ---

/** Chiavi di ordinamento accettate dalla SortMap dell'endpoint. */
export const USER_SORT = ['email', 'lastname', 'role', 'status', 'lastlogin', 'created'] as const
export type UserSort = (typeof USER_SORT)[number]

export interface UserListQuery {
  search?: string
  role?: Role | ''
  isActive?: boolean | ''
  page?: number
  pageSize?: number
  sort?: UserSort
  dir?: 'asc' | 'desc'
}

export interface UserListItem {
  id: string
  email: string
  name: string
  lastName: string
  fullName: string
  role: Role
  isActive: boolean
  isSuperAdmin: boolean
  lastLoginUtc: string | null
  createdAtUtc: string
}

// --- /user/admin/{id} ---

export interface UserDetail {
  id: string
  email: string
  name: string
  lastName: string
  fullName: string
  role: Role
  isActive: boolean
  isSuperAdmin: boolean
  lastLoginUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
  tfA_Availables: TfaAvailable[]
}

// --- /auth/login ---

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResult {
  token: string
  userId: string
  email: string
  fullName: string
  role: Role
}

// --- /user/admin/create ---

export interface CreateUserRequest {
  email: string
  name: string | null
  lastName: string | null
  role: Role
  isActive: boolean
}

export interface CreateUserResult {
  email: string | null
  fullName: string | null
  role: string | null
  isActive: boolean
  result: boolean
  id: string
}

// --- /user/admin/update e /user/update ---

export interface UpdateUserRequest {
  id: string
  email?: string | null
  name?: string | null
  lastName?: string | null
  role?: Role | null
  tfA_Availables?: TfaAvailable[] | null
}

// --- /user/activate, /user/admin/delete, /user/changepassword, update ---

export interface OperationResponse {
  result: boolean
  msg: string
}

export interface ActivateUserRequest {
  userId: string
}

export interface DeleteUserRequest {
  userId: string
}

export interface ChangePasswordRequest {
  id: string
  password: string
  rePassword: string
  oldPassword?: string | null
}

/** RFC 7807, prodotto da ResultsHelper e da GlobalExceptionHandler. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
}
