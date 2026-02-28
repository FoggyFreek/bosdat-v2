import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import type { AccountStatus } from '../types'

interface UserStatusBadgeProps {
  readonly status: AccountStatus
}

export function UserStatusBadge({ status }: UserStatusBadgeProps) {
  const { t } = useTranslation()

  const variant = status === 'Active'
    ? 'default'
    : status === 'Suspended'
      ? 'destructive'
      : 'secondary'

  return (
    <Badge variant={variant}>
      {t(`users.status.${status}`)}
    </Badge>
  )
}
