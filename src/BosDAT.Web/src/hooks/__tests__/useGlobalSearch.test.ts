import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { createElement } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useGlobalSearch } from '../useGlobalSearch'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

vi.mock('@/features/students/api', () => ({
  studentsApi: {
    getAll: vi.fn().mockResolvedValue([
      { id: 's1', fullName: 'Alice Smith', email: 'alice@test.com', status: 'Active' },
      { id: 's2', fullName: 'Bob Jones', email: 'bob@test.com', status: 'Active' },
    ]),
  },
}))

vi.mock('@/features/teachers/api', () => ({
  teachersApi: {
    getAll: vi.fn().mockResolvedValue([
      { id: 't1', fullName: 'Carol Teacher', email: 'carol@test.com', isActive: true },
      { id: 't2', fullName: 'Dave Inactive', email: 'dave@test.com', isActive: false },
    ]),
  },
}))

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return createElement(
      QueryClientProvider,
      { client: queryClient },
      createElement(
        BrowserRouter,
        { future: { v7_startTransition: true, v7_relativeSplatPath: true } },
        children
      )
    )
  }
}

describe('useGlobalSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns empty results when term is empty', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    expect(result.current.results).toEqual([])
    expect(result.current.isLoading).toBe(false)
  })

  it('returns empty results when term is less than 2 characters', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    act(() => {
      result.current.setTerm('a')
    })

    expect(result.current.results).toEqual([])
  })

  it('exposes term and setTerm', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    expect(result.current.term).toBe('')

    act(() => {
      result.current.setTerm('test')
    })

    expect(result.current.term).toBe('test')
  })

  it('exposes isOpen and close', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    expect(result.current.isOpen).toBe(false)

    act(() => {
      result.current.setTerm('test')
    })

    expect(result.current.isOpen).toBe(true)

    act(() => {
      result.current.close()
    })

    expect(result.current.isOpen).toBe(false)
  })

  it('tracks activeIndex for keyboard navigation', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    expect(result.current.activeIndex).toBe(-1)

    act(() => {
      result.current.setActiveIndex(2)
    })

    expect(result.current.activeIndex).toBe(2)
  })

  it('navigates to student detail on select', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    act(() => {
      result.current.setTerm('test')
    })

    act(() => {
      result.current.onSelect({ id: 's1', type: 'student', name: 'Alice', subtitle: 'alice@test.com' })
    })

    expect(mockNavigate).toHaveBeenCalledWith('/students/s1')
    expect(result.current.term).toBe('')
    expect(result.current.isOpen).toBe(false)
  })

  it('navigates to teacher detail on select', () => {
    const { result } = renderHook(() => useGlobalSearch(), { wrapper: createWrapper() })

    act(() => {
      result.current.setTerm('test')
    })

    act(() => {
      result.current.onSelect({ id: 't1', type: 'teacher', name: 'Carol', subtitle: 'carol@test.com' })
    })

    expect(mockNavigate).toHaveBeenCalledWith('/teachers/t1')
    expect(result.current.term).toBe('')
    expect(result.current.isOpen).toBe(false)
  })
})
