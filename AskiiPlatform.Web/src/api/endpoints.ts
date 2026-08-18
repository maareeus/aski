import { get, post } from './client'
import type {
  ActivateUserRequest,
  PagedResult,
  UserListItem,
  UserListQuery,
  UserDetail,
  SettingsResult,
  UpdateSettingsRequest,
  AuthenticatorSetupResponse,
  TfaSendOtpRequest,
  TfaStatusResponse,
  TfaVerifyRequest,
  ResendActivationRequest,
  ResendActivationResponse,
  MeResult,
  UserStatsResult,
  ChangePasswordRequest,
  CreateUserRequest,
  CreateUserResult,
  DeleteUserRequest,
  LoginRequest,
  LoginResult,
  OperationResponse,
  UpdateUserRequest,
} from './types'

/**
 * Un metodo per ogni endpoint esposto da AskiiPlatform.Api.
 * Sono tutti POST: è la convenzione scelta dal backend, anche per delete.
 */

export const authApi = {
  /** Con 2FA attiva risponde TfaRequired e un challengeToken, senza token d'accesso. */
  login: (body: LoginRequest) => post<LoginResult>('/auth/login', body),

  /** Invia il codice via email. Richiede il challengeToken, non un bearer. */
  sendOtp: (body: TfaSendOtpRequest) => post<OperationResponse>('/auth/tfa/send-otp', body),

  /** Completa il login: restituisce il token d'accesso. */
  verifyTfa: (body: TfaVerifyRequest) => post<LoginResult>('/auth/tfa/verify', body),
}

export const tfaApi = {
  stato: () => get<TfaStatusResponse>('/user/tfa'),

  avviaAuthenticator: () =>
    post<AuthenticatorSetupResponse>('/user/tfa/authenticator/start', {}),

  confermaAuthenticator: (code: string) =>
    post<OperationResponse>('/user/tfa/authenticator/confirm', { code }),

  disattivaAuthenticator: () => post<OperationResponse>('/user/tfa/authenticator/disable', {}),

  attivaEmail: () => post<OperationResponse>('/user/tfa/email/enable', {}),

  disattivaEmail: () => post<OperationResponse>('/user/tfa/email/disable', {}),

  /** Admin: azzera la 2FA di un utente che ha perso il secondo fattore. */
  resetAdmin: (userId: string) => post<OperationResponse>('/user/admin/tfa/reset', { userId }),
}

export const settingsApi = {
  /** Admin. Il valore delle opzioni segrete non viene restituito. */
  get: () => get<SettingsResult>('/settings'),

  /** Admin. Le chiavi non presenti a db vengono ignorate senza errore. */
  update: (body: UpdateSettingsRequest) => post<void>('/settings/update', body),
}

export const usersApi = {
  /** Profilo del chiamante letto dal db: non invecchia come quello in locale. */
  me: () => get<MeResult>('/user/me'),

  /** Admin. Conteggi per il riepilogo. */
  stats: () => get<UserStatsResult>('/user/admin/stats'),

  /** Admin. Filtri, ordinamento e paginazione sono tutti lato server. */
  list: (query: UserListQuery) =>
    get<PagedResult<UserListItem>>('/user/admin/list', { ...query }),

  /** Admin. 404 se l'identificativo non esiste. */
  get: (id: string) => get<UserDetail>(`/user/admin/${id}`),

  /** Admin. La password viene generata dal backend e non è restituita. */
  create: (body: CreateUserRequest) => post<CreateUserResult>('/user/admin/create', body),

  /** Anonimo: codice monouso più la password scelta dall'utente. */
  activate: (body: ActivateUserRequest) => post<OperationResponse>('/user/activate', body),

  /** Admin: rigenera il codice, lo invia per email e lo restituisce. */
  resendActivation: (body: ResendActivationRequest) =>
    post<ResendActivationResponse>('/user/admin/activation/resend', body),

  /** Admin: anagrafica, ruolo, email, metodi 2FA. */
  adminUpdate: (body: UpdateUserRequest) => post<OperationResponse>('/user/admin/update', body),

  /** Utente autenticato su se stesso: il backend applica solo TFA_Availables. */
  selfUpdate: (body: UpdateUserRequest) => post<OperationResponse>('/user/update', body),

  /** Admin. */
  remove: (body: DeleteUserRequest) => post<OperationResponse>('/user/admin/delete', body),

  /** Autenticato. Per l'utente su se stesso serve oldPassword; l'admin ne è esente. */
  changePassword: (body: ChangePasswordRequest) =>
    post<OperationResponse>('/user/changepassword', body),
}
