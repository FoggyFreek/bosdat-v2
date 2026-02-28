import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Copy, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { usersApi } from '../api'
import type { InvitationResponse, UserDetail } from '../types'
import { UserStatusBadge } from './UserStatusBadge'
import { UserRoleBadge } from './UserRoleBadge'

interface UserDetailPanelProps {
  readonly user: UserDetail
  readonly open: boolean
  readonly onOpenChange: (open: boolean) => void
}

export function UserDetailPanel({ user, open, onOpenChange }: UserDetailPanelProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [displayName, setDisplayName] = useState(user.displayName)
  const [isEditingName, setIsEditingName] = useState(false)
  const [confirmAction, setConfirmAction] = useState<'suspend' | 'activate' | null>(null)
  const [newInvitation, setNewInvitation] = useState<InvitationResponse | null>(null)
  const [copied, setCopied] = useState(false)

  const invalidateUsers = () => {
    queryClient.invalidateQueries({ queryKey: ['users'] })
  }

  const updateNameMutation = useMutation({
    mutationFn: () => usersApi.updateDisplayName(user.id, { displayName }),
    onSuccess: () => {
      setIsEditingName(false)
      invalidateUsers()
    },
  })

  const updateStatusMutation = useMutation({
    mutationFn: (status: 'Active' | 'Suspended') =>
      usersApi.updateStatus(user.id, { accountStatus: status }),
    onSuccess: () => {
      setConfirmAction(null)
      invalidateUsers()
      onOpenChange(false)
    },
  })

  const resendMutation = useMutation({
    mutationFn: () => usersApi.resendInvitation(user.id),
    onSuccess: (data) => setNewInvitation(data),
  })

  const handleCopy = async () => {
    const url = newInvitation?.invitationUrl
    if (!url) return
    await navigator.clipboard.writeText(url)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t('users.detail.title')}</DialogTitle>
          </DialogHeader>

          <div className="space-y-4 py-2">
            {/* Status + Role */}
            <div className="flex gap-2 items-center">
              <UserStatusBadge status={user.accountStatus} />
              <UserRoleBadge role={user.role} />
            </div>

            {/* Email (read-only) */}
            <div className="space-y-1.5">
              <Label>{t('users.fields.email')}</Label>
              <p className="text-sm text-muted-foreground">{user.email}</p>
            </div>

            {/* Display Name */}
            <div className="space-y-1.5">
              <Label htmlFor="detail-display-name">{t('users.fields.displayName')}</Label>
              {isEditingName ? (
                <div className="flex gap-2">
                  <Input
                    id="detail-display-name"
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    maxLength={80}
                  />
                  <Button
                    size="sm"
                    onClick={() => updateNameMutation.mutate()}
                    disabled={updateNameMutation.isPending}
                  >
                    {t('common.actions.save')}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => { setDisplayName(user.displayName); setIsEditingName(false) }}
                  >
                    {t('common.actions.cancel')}
                  </Button>
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <p className="text-sm">{user.displayName}</p>
                  <Button size="sm" variant="ghost" onClick={() => setIsEditingName(true)}>
                    {t('users.detail.editDisplayName')}
                  </Button>
                </div>
              )}
            </div>

            {/* Account Created */}
            <div className="space-y-1.5">
              <Label>{t('users.fields.createdAt')}</Label>
              <p className="text-sm text-muted-foreground">
                {new Date(user.createdAt).toLocaleDateString()}
              </p>
            </div>

            {/* Resend invitation result */}
            {newInvitation && (
              <div className="space-y-2 p-3 bg-muted/50 rounded-md">
                <p className="text-sm font-medium">{t('users.create.invitationUrl')}</p>
                <div className="flex gap-2">
                  <Input value={newInvitation.invitationUrl} readOnly className="font-mono text-xs" />
                  <Button variant="outline" size="icon" onClick={handleCopy}>
                    {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                  </Button>
                </div>
                {copied && <p className="text-xs text-green-600">{t('users.create.linkCopied')}</p>}
              </div>
            )}
          </div>

          <DialogFooter className="flex-col sm:flex-row gap-2">
            {user.accountStatus === 'PendingFirstLogin' && (
              <Button
                variant="outline"
                onClick={() => resendMutation.mutate()}
                disabled={resendMutation.isPending}
              >
                {t('users.detail.resendInvitation')}
              </Button>
            )}
            {user.accountStatus === 'Active' && (
              <Button
                variant="destructive"
                onClick={() => setConfirmAction('suspend')}
              >
                {t('users.detail.suspend')}
              </Button>
            )}
            {user.accountStatus === 'Suspended' && (
              <Button
                variant="default"
                onClick={() => setConfirmAction('activate')}
              >
                {t('users.detail.activate')}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Suspend/Activate confirmation */}
      <AlertDialog open={confirmAction !== null} onOpenChange={() => setConfirmAction(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirmAction === 'suspend'
                ? t('users.detail.confirmSuspend')
                : t('users.detail.confirmActivate')}
            </AlertDialogTitle>
            {confirmAction === 'suspend' && (
              <AlertDialogDescription>
                {t('users.detail.confirmSuspendDesc')}
              </AlertDialogDescription>
            )}
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => updateStatusMutation.mutate(
                confirmAction === 'suspend' ? 'Suspended' : 'Active'
              )}
            >
              {confirmAction === 'suspend' ? t('users.detail.suspend') : t('users.detail.activate')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
