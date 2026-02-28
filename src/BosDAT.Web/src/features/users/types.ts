export type AccountStatus = 'Active' | 'PendingFirstLogin' | 'Suspended'
export type UserRole = 'Admin' | 'FinancialAdmin' | 'Teacher' | 'Student'
export type LinkedObjectType = 'Teacher' | 'Student'

export interface UserListItem {
  id: string
  displayName: string
  email: string
  role: UserRole
  accountStatus: AccountStatus
  createdAt: string
  linkedObjectId?: string
  linkedObjectType?: LinkedObjectType
}

export interface UserDetail extends UserListItem {
  hasPendingInvitation: boolean
  invitationExpiresAt?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface InvitationResponse {
  invitationUrl: string
  expiresAt: string
  userId: string
}

export interface ValidateTokenResponse {
  isValid: boolean
  displayName?: string
  email?: string
  expiresAt?: string
}

export interface CreateUserRequest {
  role: UserRole
  displayName: string
  email: string
  linkedObjectId?: string
  linkedObjectType?: LinkedObjectType
}

export interface UpdateDisplayNameRequest {
  displayName: string
}

export interface UpdateStatusRequest {
  accountStatus: AccountStatus
}

export interface UserListQuery {
  search?: string
  role?: UserRole
  accountStatus?: AccountStatus
  page?: number
  pageSize?: number
  sortBy?: string
  sortDesc?: boolean
}
