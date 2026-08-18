import { get, post } from './client'
import type {
  ActivateUserRequest,
  PagedResult,
  UserListItem,
  UserListQuery,
  UserDetail,
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
  login: (body: LoginRequest) => post<LoginResult>('/auth/login', body),
}

export const usersApi = {
  /** Admin. Filtri, ordinamento e paginazione sono tutti lato server. */
  list: (query: UserListQuery) =>
    get<PagedResult<UserListItem>>('/user/admin/list', { ...query }),

  /** Admin. 404 se l'identificativo non esiste. */
  get: (id: string) => get<UserDetail>(`/user/admin/${id}`),

  /** Admin. La password viene generata dal backend e non è restituita. */
  create: (body: CreateUserRequest) => post<CreateUserResult>('/user/admin/create', body),

  /** Anonimo: richiede solo lo userId. */
  activate: (body: ActivateUserRequest) => post<OperationResponse>('/user/activate', body),

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
