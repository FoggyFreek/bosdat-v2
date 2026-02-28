import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import type { UserRole } from '../types'

interface UserRoleBadgeProps {
  readonly role: UserRole | string
}

export function UserRoleBadge({ role }: UserRoleBadgeProps) {
  const { t } = useTranslation()

  return (
    <Badge variant="outline">
      {t(`users.roles.${role}`, role)}
    </Badge>
  )
}
