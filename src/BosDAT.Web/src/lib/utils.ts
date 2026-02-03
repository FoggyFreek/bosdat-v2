import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

// ============================================================================
// UI Utilities
// ============================================================================

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('nl-NL', {
    style: 'currency',
    currency: 'EUR',
  }).format(amount)
}

/**
 * Validates an email address using safe string operations.
 * Prevents ReDoS by avoiding regex backtracking.
 */
export function validateEmail(email: string): boolean {
  if (email.length > 254) return false
  const parts = email.split('@')
  if (parts.length !== 2) return false
  const [local, domain] = parts
  if (!local || local.length > 64 || !domain || domain.length > 253) return false
  if (local.includes(' ') || domain.includes(' ')) return false
  if (!domain.includes('.')) return false
  const domainParts = domain.split('.')
  return domainParts.every(part => part.length > 0 && part.length <= 63)
}

// ============================================================================
// Date/Time Utilities (Re-exported from iso-helpers)
// ============================================================================

/**
 * @deprecated Import directly from '@/lib/iso-helpers' instead.
 * These exports are maintained for backward compatibility only.
 */
export {
  formatDate,
  formatTime,
  getDayNameFromNumber as getDayName,
} from './iso-helpers'
