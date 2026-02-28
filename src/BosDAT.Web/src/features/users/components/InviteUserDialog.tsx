import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Copy, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { teachersApi } from '@/features/teachers/api'
import { studentsApi } from '@/features/students/api'
import type { Teacher } from '@/features/teachers/types'
import type { Student } from '@/features/students/types'
import { usersApi } from '../api'
import type { CreateUserRequest, InvitationResponse, LinkedObjectType, UserRole } from '../types'

interface InviteUserDialogProps {
  readonly open: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly lockedRole?: UserRole
  readonly linkedObjectId?: string
  readonly linkedObjectType?: LinkedObjectType
  readonly defaultEmail?: string
  readonly defaultDisplayName?: string
}

const MANAGED_ROLES: UserRole[] = ['Admin', 'FinancialAdmin', 'Teacher', 'Student']

export function InviteUserDialog({
  open,
  onOpenChange,
  lockedRole,
  linkedObjectId,
  linkedObjectType,
  defaultEmail = '',
  defaultDisplayName = '',
}: InviteUserDialogProps) {
  const { t } = useTranslation()

  const [role, setRole] = useState<UserRole | ''>(lockedRole ?? '')
  const [displayName, setDisplayName] = useState(defaultDisplayName)
  const [email, setEmail] = useState(defaultEmail)
  const [selectedLinkedId, setSelectedLinkedId] = useState(linkedObjectId ?? '')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [invitationResult, setInvitationResult] = useState<InvitationResponse | null>(null)
  const [copied, setCopied] = useState(false)

  const needsLinkedEntity = role === 'Teacher' || role === 'Student'
  const linkedEntityPreset = !!linkedObjectId

  const { data: teachers = [] } = useQuery<Teacher[]>({
    queryKey: ['teachers'],
    queryFn: () => teachersApi.getAll(),
    enabled: role === 'Teacher' && !linkedEntityPreset,
  })

  const { data: studentsData } = useQuery<{ students: Student[] }>({
    queryKey: ['students'],
    queryFn: () => studentsApi.getAll(),
    enabled: role === 'Student' && !linkedEntityPreset,
  })
  const students = studentsData?.students ?? []

  const createMutation = useMutation({
    mutationFn: (dto: CreateUserRequest) => usersApi.createUser(dto),
    onSuccess: (data) => setInvitationResult(data),
  })

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!role) newErrors.role = t('common.validation.required')
    if (!displayName.trim()) newErrors.displayName = t('common.validation.required')
    if (!email.trim()) {
      newErrors.email = t('common.validation.required')
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      newErrors.email = t('common.validation.invalidEmail')
    }
    if (needsLinkedEntity && !selectedLinkedId) {
      newErrors.linkedObject = t('users.create.linkedObjectRequired')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = () => {
    if (!validate() || !role) return

    const dto: CreateUserRequest = {
      role,
      displayName: displayName.trim(),
      email: email.trim(),
      linkedObjectId: selectedLinkedId || undefined,
      linkedObjectType: selectedLinkedId ? linkedObjectType ?? (role as LinkedObjectType) : undefined,
    }
    createMutation.mutate(dto)
  }

  const handleCopy = async () => {
    if (!invitationResult) return
    await navigator.clipboard.writeText(invitationResult.invitationUrl)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleClose = () => {
    if (!invitationResult) {
      setRole(lockedRole ?? '')
      setDisplayName(defaultDisplayName)
      setEmail(defaultEmail)
      setSelectedLinkedId(linkedObjectId ?? '')
      setErrors({})
      createMutation.reset()
    } else {
      setInvitationResult(null)
      setRole(lockedRole ?? '')
      setDisplayName(defaultDisplayName)
      setEmail(defaultEmail)
      setSelectedLinkedId(linkedObjectId ?? '')
      setErrors({})
      createMutation.reset()
    }
    onOpenChange(false)
  }

  const expiresAt = invitationResult
    ? new Date(invitationResult.expiresAt).toLocaleString()
    : ''

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        {invitationResult ? (
          <>
            <DialogHeader>
              <DialogTitle>{t('users.create.success')}</DialogTitle>
              <DialogDescription>{t('users.create.expiresIn')}</DialogDescription>
            </DialogHeader>
            <div className="space-y-4 py-2">
              <p className="text-sm text-muted-foreground">
                {t('users.create.expiresAtLabel')}: <span className="font-medium">{expiresAt}</span>
              </p>
              <div className="space-y-2">
                <Label>{t('users.create.invitationUrl')}</Label>
                <div className="flex gap-2">
                  <Input value={invitationResult.invitationUrl} readOnly className="font-mono text-xs" />
                  <Button variant="outline" size="icon" onClick={handleCopy}>
                    {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                  </Button>
                </div>
                {copied && (
                  <p className="text-xs text-green-600">{t('users.create.linkCopied')}</p>
                )}
              </div>
            </div>
            <DialogFooter>
              <Button onClick={handleClose}>{t('common.actions.close')}</Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>{t('users.create.title')}</DialogTitle>
              <DialogDescription>{t('users.create.description')}</DialogDescription>
            </DialogHeader>
            <div className="space-y-4 py-2">
              {/* Role */}
              <div className="space-y-1.5">
                <Label htmlFor="role">{t('users.fields.role')}</Label>
                {lockedRole ? (
                  <Input id="role" value={t(`users.roles.${lockedRole}`)} readOnly />
                ) : (
                  <Select value={role} onValueChange={(v) => setRole(v as UserRole)}>
                    <SelectTrigger id="role">
                      <SelectValue placeholder={t('users.filters.selectRole')} />
                    </SelectTrigger>
                    <SelectContent>
                      {MANAGED_ROLES.map((r) => (
                        <SelectItem key={r} value={r}>{t(`users.roles.${r}`)}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
                {errors.role && <p className="text-xs text-destructive">{errors.role}</p>}
              </div>

              {/* Linked entity picker — shown only when role = Teacher/Student and no preset */}
              {needsLinkedEntity && !linkedEntityPreset && (
                <div className="space-y-1.5">
                  <Label htmlFor="linkedObject">
                    {role === 'Teacher' ? t('common.entities.teacher') : t('common.entities.student')}
                  </Label>
                  <Select value={selectedLinkedId} onValueChange={setSelectedLinkedId}>
                    <SelectTrigger id="linkedObject">
                      <SelectValue placeholder={t('users.create.selectLinkedObject')} />
                    </SelectTrigger>
                    <SelectContent>
                      {role === 'Teacher'
                        ? teachers.map((teacher) => (
                          <SelectItem key={teacher.id} value={teacher.id}>
                            {teacher.fullName}
                          </SelectItem>
                        ))
                        : students.map((student) => (
                          <SelectItem key={student.id} value={student.id}>
                            {student.fullName}
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                  {errors.linkedObject && <p className="text-xs text-destructive">{errors.linkedObject}</p>}
                </div>
              )}

              {/* Display Name */}
              <div className="space-y-1.5">
                <Label htmlFor="displayName">{t('users.fields.displayName')}</Label>
                <Input
                  id="displayName"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  maxLength={80}
                />
                {errors.displayName && <p className="text-xs text-destructive">{errors.displayName}</p>}
              </div>

              {/* Email */}
              <div className="space-y-1.5">
                <Label htmlFor="email">{t('users.fields.email')}</Label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
                {errors.email && <p className="text-xs text-destructive">{errors.email}</p>}
              </div>

              {createMutation.isError && (
                <p className="text-sm text-destructive">{t('common.errors.unexpected')}</p>
              )}
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={handleClose}>{t('common.actions.cancel')}</Button>
              <Button onClick={handleSubmit} disabled={createMutation.isPending}>
                {createMutation.isPending ? t('common.actions.saving') : t('users.inviteUser')}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
