import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Plus, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { usersApi } from '../api'
import type { AccountStatus, UserDetail, UserListItem, UserListQuery, UserRole } from '../types'
import { UserStatusBadge } from './UserStatusBadge'
import { UserRoleBadge } from './UserRoleBadge'
import { InviteUserDialog } from './InviteUserDialog'
import { UserDetailPanel } from './UserDetailPanel'

const ALL_ROLES: Array<{ value: UserRole; label: string }> = [
  { value: 'Admin', label: 'Admin' },
  { value: 'FinancialAdmin', label: 'Financial Admin' },
  { value: 'Teacher', label: 'Teacher' },
  { value: 'Student', label: 'Student' },
]

const ALL_STATUSES: Array<{ value: AccountStatus; label: string }> = [
  { value: 'Active', label: 'Active' },
  { value: 'PendingFirstLogin', label: 'Pending First Login' },
  { value: 'Suspended', label: 'Suspended' },
]

export function ManageUsersSection() {
  const { t } = useTranslation()

  const [query, setQuery] = useState<UserListQuery>({ page: 1, pageSize: 20 })
  const [search, setSearch] = useState('')
  const [showInviteDialog, setShowInviteDialog] = useState(false)
  const [selectedUser, setSelectedUser] = useState<UserDetail | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['users', query],
    queryFn: () => usersApi.getUsers(query),
  })

  const { data: selectedUserDetail } = useQuery({
    queryKey: ['users', selectedUser?.id],
    queryFn: () => usersApi.getUserById(selectedUser!.id),
    enabled: !!selectedUser,
  })

  const users = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const page = data?.page ?? 1
  const pageSize = data?.pageSize ?? 20
  const totalPages = Math.ceil(totalCount / pageSize)

  const handleSearch = (value: string) => {
    setSearch(value)
    setQuery((prev) => ({ ...prev, search: value || undefined, page: 1 }))
  }

  const handleRoleFilter = (value: string) => {
    setQuery((prev) => ({
      ...prev,
      role: value === '__all' ? undefined : (value as UserRole),
      page: 1
    }))
  }

  const handleStatusFilter = (value: string) => {
    setQuery((prev) => ({
      ...prev,
      accountStatus: value === '__all' ? undefined : (value as AccountStatus),
      page: 1
    }))
  }

  const handleRowClick = (user: UserListItem) => {
    setSelectedUser(user as UserDetail)
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>{t('users.title')}</CardTitle>
            <CardDescription>{t('users.subtitle')}</CardDescription>
          </div>
          <Button size="sm" onClick={() => setShowInviteDialog(true)}>
            <Plus className="h-4 w-4 mr-2" />
            {t('users.createUser')}
          </Button>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Filters */}
          <div className="flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder={t('common.actions.search')}
                value={search}
                onChange={(e) => handleSearch(e.target.value)}
                className="pl-8"
              />
            </div>
            <Select onValueChange={handleRoleFilter} defaultValue="__all">
              <SelectTrigger className="w-full sm:w-[160px]">
                <SelectValue placeholder={t('users.filters.allRoles')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all">{t('users.filters.allRoles')}</SelectItem>
                {ALL_ROLES.map((r) => (
                  <SelectItem key={r.value} value={r.value}>
                    {t(`users.roles.${r.value}`)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select onValueChange={handleStatusFilter} defaultValue="__all">
              <SelectTrigger className="w-full sm:w-[180px]">
                <SelectValue placeholder={t('users.filters.allStatuses')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all">{t('users.filters.allStatuses')}</SelectItem>
                {ALL_STATUSES.map((s) => (
                  <SelectItem key={s.value} value={s.value}>
                    {t(`users.status.${s.value}`)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Table */}
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
            </div>
          ) : users.length === 0 ? (
            <p className="text-muted-foreground text-sm py-4">{t('users.empty')}</p>
          ) : (
            <div className="rounded-md border">
              <div className="grid grid-cols-4 gap-4 px-4 py-3 text-sm font-medium text-muted-foreground border-b">
                <span>{t('users.fields.displayName')}</span>
                <span>{t('users.fields.email')}</span>
                <span>{t('users.fields.role')}</span>
                <span>{t('users.fields.status')}</span>
              </div>
              {users.map((user) => (
                <button
                  key={user.id}
                  onClick={() => handleRowClick(user)}
                  className="grid grid-cols-4 gap-4 px-4 py-3 text-sm w-full text-left hover:bg-muted/50 transition-colors border-b last:border-b-0"
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') handleRowClick(user)
                  }}
                >
                  <span className="font-medium truncate">{user.displayName}</span>
                  <span className="text-muted-foreground truncate">{user.email}</span>
                  <span><UserRoleBadge role={user.role} /></span>
                  <span><UserStatusBadge status={user.accountStatus} /></span>
                </button>
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>
                {t('common.pagination.showing', {
                  from: (page - 1) * pageSize + 1,
                  to: Math.min(page * pageSize, totalCount),
                  total: totalCount
                })}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setQuery((prev) => ({ ...prev, page: prev.page! - 1 }))}
                >
                  {t('common.pagination.previous')}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setQuery((prev) => ({ ...prev, page: prev.page! + 1 }))}
                >
                  {t('common.pagination.next')}
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <InviteUserDialog
        open={showInviteDialog}
        onOpenChange={setShowInviteDialog}
      />

      {selectedUserDetail && (
        <UserDetailPanel
          user={selectedUserDetail}
          open={!!selectedUser}
          onOpenChange={(open) => { if (!open) setSelectedUser(null) }}
        />
      )}
    </>
  )
}
