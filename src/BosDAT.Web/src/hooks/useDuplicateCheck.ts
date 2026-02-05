import { useState, useCallback, useRef } from 'react'
import { studentsApi } from '@/features/students/api'
import type { CheckDuplicatesDto, DuplicateCheckResult, DuplicateMatch } from '@/features/students/types'

interface UseDuplicateCheckOptions {
  debounceMs?: number
  excludeId?: string
}

interface UseDuplicateCheckReturn {
  duplicates: DuplicateMatch[]
  hasDuplicates: boolean
  isChecking: boolean
  error: string | null
  checkDuplicates: (data: Omit<CheckDuplicatesDto, 'excludeId'>) => void
  clearDuplicates: () => void
  acknowledgedDuplicates: boolean
  acknowledgeDuplicates: () => void
  resetAcknowledgement: () => void
}

export function useDuplicateCheck(options: UseDuplicateCheckOptions = {}): UseDuplicateCheckReturn {
  const { debounceMs = 500, excludeId } = options

  const [duplicates, setDuplicates] = useState<DuplicateMatch[]>([])
  const [isChecking, setIsChecking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [acknowledgedDuplicates, setAcknowledgedDuplicates] = useState(false)

  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const lastRequestRef = useRef<string | null>(null)

  const checkDuplicates = useCallback(
    (data: Omit<CheckDuplicatesDto, 'excludeId'>) => {
      // Clear any pending debounce
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current)
      }

      // Create a unique key for this request
      const requestKey = JSON.stringify(data)

      // Skip if data hasn't changed
      if (requestKey === lastRequestRef.current) {
        return
      }

      // Skip if essential fields are missing
      if (!data.firstName?.trim() || !data.lastName?.trim() || !data.email?.trim()) {
        setDuplicates([])
        return
      }

      debounceTimerRef.current = setTimeout(async () => {
        lastRequestRef.current = requestKey
        setIsChecking(true)
        setError(null)

        try {
          const result: DuplicateCheckResult = await studentsApi.checkDuplicates({
            ...data,
            excludeId,
          })

          setDuplicates(result.duplicates)
          // Reset acknowledgement when new duplicates are found
          if (result.hasDuplicates) {
            setAcknowledgedDuplicates(false)
          }
        } catch (err) {
          console.error('Failed to check duplicates:', err)
          setError('Failed to check for duplicates')
          setDuplicates([])
        } finally {
          setIsChecking(false)
        }
      }, debounceMs)
    },
    [debounceMs, excludeId]
  )

  const clearDuplicates = useCallback(() => {
    setDuplicates([])
    setError(null)
    setAcknowledgedDuplicates(false)
    lastRequestRef.current = null
  }, [])

  const acknowledgeDuplicates = useCallback(() => {
    setAcknowledgedDuplicates(true)
  }, [])

  const resetAcknowledgement = useCallback(() => {
    setAcknowledgedDuplicates(false)
  }, [])

  return {
    duplicates,
    hasDuplicates: duplicates.length > 0,
    isChecking,
    error,
    checkDuplicates,
    clearDuplicates,
    acknowledgedDuplicates,
    acknowledgeDuplicates,
    resetAcknowledgement,
  }
}
