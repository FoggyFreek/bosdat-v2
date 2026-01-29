/**
 * String utility functions
 */

/**
 * Get initials from a full name
 * - Single word: first two characters
 * - Multiple words: first character of first and last word
 */
export const getInitials = (name: string): string => {
  const parts = name.trim().split(/\s+/)
  if (parts.length === 0) return ''
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}
