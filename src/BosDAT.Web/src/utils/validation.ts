/**
 * Validates an email address using safe string operations.
 * Prevents ReDoS by avoiding regex backtracking.
 */
export const validateEmail = (email: string): boolean => {
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
