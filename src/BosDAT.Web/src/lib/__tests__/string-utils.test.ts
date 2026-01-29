import { describe, it, expect } from 'vitest'
import { getInitials } from '../string-utils'

describe('string-utils', () => {
  describe('getInitials', () => {
    it('should return first two characters for single word', () => {
      expect(getInitials('John')).toBe('JO')
      expect(getInitials('A')).toBe('A')
    })

    it('should return first and last initials for multiple words', () => {
      expect(getInitials('John Smith')).toBe('JS')
      expect(getInitials('John van Smith')).toBe('JS')
      expect(getInitials('Mary Jane Watson')).toBe('MW')
    })

    it('should handle empty string', () => {
      expect(getInitials('')).toBe('')
    })

    it('should handle whitespace-only string', () => {
      expect(getInitials('   ')).toBe('')
    })

    it('should trim whitespace', () => {
      expect(getInitials('  John Smith  ')).toBe('JS')
    })

    it('should handle multiple spaces between words', () => {
      expect(getInitials('John    Smith')).toBe('JS')
    })

    it('should return uppercase initials', () => {
      expect(getInitials('john smith')).toBe('JS')
    })
  })
})
