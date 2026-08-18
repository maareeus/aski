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

// --- /settings ---

/** Nomi delle opzioni noti al backend (Database/Entities/Options.cs). */
export const OPTION = {
  smtpHost: 'smtp_host',
  smtpPort: 'smtp_port',
  smtpUser: 'smtp_user',
  smtpPassword: 'smtp_password',
} as const

export interface SettingItem {
  name: string
  /** Null per le opzioni segrete: usa `hasValue` per sapere se è impostata. */
  value: string | null
  isSecret: boolean
  hasValue: boolean
  lastUpdateUtc: string
}

export interface SettingsResult {
  items: SettingItem[]
}

export interface UpdateSettingsRequest {
  options: Record<string, string>
}

// --- /auth/login ---

export interface LoginRequest {
  email: string
  password: string
}

/** Rispecchia AuthStatus di Features/Auth/AuthEndpoints.cs (enum numerico). */
export const AuthStatus = {
  Unauthorized: 0,
  Ok: 1,
  TfaRequired: 2,
} as const

export type AuthStatus = (typeof AuthStatus)[keyof typeof AuthStatus]

export interface LoginResult {
  status: AuthStatus
  token: string | null
  userId: string | null
  email: string | null
  fullName: string | null
  role: Role | null
  /** Valorizzato solo con status = TfaRequired. */
  challengeToken: string | null
  tfaMethods: TfaAvailable[] | null
}

// --- secondo passaggio del login ---

export interface TfaSendOtpRequest {
  challengeToken: string
}

export interface TfaVerifyRequest {
  challengeToken: string
  method: TfaAvailable
  code: string
}

// --- configurazione 2FA sul proprio account ---

export interface TfaStatusResponse {
  enabled: boolean
  methods: TfaAvailable[]
  /** Segreto generato ma associazione non ancora confermata. */
  authenticatorPending: boolean
}

export interface AuthenticatorSetupResponse {
  secret: string
  otpauthUri: string
  digits: number
  periodSeconds: number
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
  /** Presente solo creando un utente non attivo. */
  activationCode: string | null
  activationEmailSent: boolean
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

/** L'attivazione richiede il codice monouso e la password scelta dall'utente. */
export interface ActivateUserRequest {
  code: string
  password: string
  rePassword: string
}

export interface ResendActivationRequest {
  userId: string
}

export interface ResendActivationResponse {
  result: boolean
  msg: string
  /** In chiaro: l'endpoint è riservato agli Admin. */
  code: string
  emailSent: boolean
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
