import { api } from '@/services/api'
import type {
  CreateUserRequest,
  InvitationResponse,
  PagedResult,
  UpdateDisplayNameRequest,
  UpdateStatusRequest,
  UserDetail,
  UserListItem,
  UserListQuery,
  ValidateTokenResponse,
} from './types'

export const usersApi = {
  getUsers: (params?: UserListQuery): Promise<PagedResult<UserListItem>> =>
    api.get('/users', { params }).then((r) => r.data),

  getUserById: (id: string): Promise<UserDetail> =>
    api.get(`/users/${id}`).then((r) => r.data),

  createUser: (dto: CreateUserRequest): Promise<InvitationResponse> =>
    api.post('/users', dto).then((r) => r.data),

  updateDisplayName: (id: string, dto: UpdateDisplayNameRequest): Promise<void> =>
    api.patch(`/users/${id}/display-name`, dto).then(() => undefined),

  updateStatus: (id: string, dto: UpdateStatusRequest): Promise<void> =>
    api.patch(`/users/${id}/status`, dto).then(() => undefined),

  resendInvitation: (id: string): Promise<InvitationResponse> =>
    api.post(`/users/${id}/resend-invitation`).then((r) => r.data),
}

export const accountApi = {
  validateToken: (token: string): Promise<ValidateTokenResponse> =>
    api.get('/account/validate-token', { params: { token } }).then((r) => r.data),

  setPassword: (dto: { token: string; password: string }): Promise<void> =>
    api.post('/account/set-password', dto).then(() => undefined),
}
