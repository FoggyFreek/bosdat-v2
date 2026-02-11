import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import type { LedgerEntryType, LedgerEntryStatus } from '@/features/students/types'
import { ledgerEntryTypeTranslations, ledgerEntryStatusTranslations } from '@/features/students/types'

const ENTRY_TYPE_STYLES: Record<LedgerEntryType, string> = {
  Credit: 'bg-green-100 text-green-800',
  Debit: 'bg-red-100 text-red-800',
}

const STATUS_STYLES: Record<LedgerEntryStatus, string> = {
  Open: 'bg-blue-100 text-blue-800',
  Applied: 'bg-gray-100 text-gray-800',
  PartiallyApplied: 'bg-yellow-100 text-yellow-800',
  Reversed: 'bg-red-100 text-red-800',
}

const badgeBaseClasses = 'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium'

interface EntryTypeBadgeProps {
  readonly type: LedgerEntryType
}

export function EntryTypeBadge({ type }: EntryTypeBadgeProps) {
  const { t } = useTranslation()
  return (
    <span className={cn(badgeBaseClasses, ENTRY_TYPE_STYLES[type])}>
      {t(ledgerEntryTypeTranslations[type])}
    </span>
  )
}

interface StatusBadgeProps {
  readonly status: LedgerEntryStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const { t } = useTranslation()
  return (
    <span className={cn(badgeBaseClasses, STATUS_STYLES[status])}>
      {t(ledgerEntryStatusTranslations[status])}
    </span>
  )
}

interface EntryAmountProps {
  readonly amount: number
  readonly entryType: LedgerEntryType
}

export function EntryAmount({ amount, entryType }: EntryAmountProps) {
  const isCredit = entryType === 'Credit'
  return (
    <p className={cn('font-medium', isCredit ? 'text-green-600' : 'text-red-600')}>
      {isCredit ? '+' : '-'}€{amount.toFixed(2)}
    </p>
  )
}
