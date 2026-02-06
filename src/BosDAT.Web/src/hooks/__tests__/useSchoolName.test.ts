import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@/test/utils'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement } from 'react'
import { useSchoolName } from '../useSchoolName'

vi.mock('@/features/settings/api', () => ({
  settingsApi: {
    getByKey: vi.fn(),
  },
}))

import { settingsApi } from '@/features/settings/api'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return ({ children }: { children: React.ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children)
}

describe('useSchoolName', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns school name from settings', async () => {
    vi.mocked(settingsApi.getByKey).mockResolvedValue({
      key: 'school_name',
      value: 'Test Music School',
    })

    const { result } = renderHook(() => useSchoolName(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.schoolName).toBe('Test Music School')
    expect(settingsApi.getByKey).toHaveBeenCalledWith('school_name')
  })

  it('returns empty string while loading', () => {
    vi.mocked(settingsApi.getByKey).mockReturnValue(new Promise(() => {}))

    const { result } = renderHook(() => useSchoolName(), {
      wrapper: createWrapper(),
    })

    expect(result.current.schoolName).toBe('')
    expect(result.current.isLoading).toBe(true)
  })

  it('returns empty string when data is undefined', async () => {
    vi.mocked(settingsApi.getByKey).mockResolvedValue(undefined as never)

    const { result } = renderHook(() => useSchoolName(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.schoolName).toBe('')
  })
})
