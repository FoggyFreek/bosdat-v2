import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import type { AccountStatus } from '../types'

interface UserStatusBadgeProps {
  readonly status: AccountStatus
}

export function UserStatusBadge({ status }: UserStatusBadgeProps) {
  const { t } = useTranslation()

  const variantMap: Record<string, 'default' | 'destructive' | 'secondary'> = {
    Active: 'default',
    Suspended: 'destructive',
    Inactive: 'secondary',
  }
  const variant = variantMap[status] ?? 'secondary'

  return (
    <Badge variant={variant}>
      {t(`users.status.${status}`)}
    </Badge>
  )
}
