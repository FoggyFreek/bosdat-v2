import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useCourseTypesData } from '../useCourseTypesData'
import { courseTypesApi } from '@/features/course-types/api'
import { instrumentsApi } from '@/features/instruments/api'
import { settingsApi } from '@/features/settings/api'
import type { CourseType } from '@/features/course-types/types'
import type { Instrument } from '@/features/instruments/types'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/instruments/api', () => ({
  instrumentsApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/settings/api', () => ({
  settingsApi: {
    getAll: vi.fn(),
  },
}))

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

const mockCourseTypes: CourseType[] = [
  {
    id: '1',
    name: 'Piano 30min',
    instrumentId: 1,
    instrumentName: 'Piano',
    durationMinutes: 30,
    type: 'Individual',
    maxStudents: 1,
    isActive: true,
    activeCourseCount: 2,
    hasTeachersForCourseType: true,
    currentPricing: {
      id: 'p1',
      courseTypeId: '1',
      priceAdult: 45.0,
      priceChild: 40.5,
      validFrom: '2024-01-01',
      validUntil: null,
      isCurrent: true,
      createdAt: '2024-01-01',
    },
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
]

const mockInstruments: Instrument[] = [
  { id: 1, name: 'Piano', category: 'Keyboard', isActive: true },
  { id: 2, name: 'Guitar', category: 'String', isActive: true },
]

const mockSettings = [
  { key: 'child_discount_percent', value: '15' },
  { key: 'group_max_students', value: '8' },
  { key: 'workshop_max_students', value: '16' },
]

describe('useCourseTypesData', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns loading state initially', () => {
    vi.mocked(courseTypesApi.getAll).mockReturnValue(new Promise(() => {}))
    vi.mocked(instrumentsApi.getAll).mockReturnValue(new Promise(() => {}))
    vi.mocked(settingsApi.getAll).mockReturnValue(new Promise(() => {}))

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isLoading).toBe(true)
    expect(result.current.courseTypes).toEqual([])
    expect(result.current.instruments).toEqual([])
    expect(result.current.settings).toEqual([])
  })

  it('fetches and returns course types, instruments, and settings', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue(mockCourseTypes)
    vi.mocked(instrumentsApi.getAll).mockResolvedValue(mockInstruments)
    vi.mocked(settingsApi.getAll).mockResolvedValue(mockSettings)

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.courseTypes).toEqual(mockCourseTypes)
    expect(result.current.instruments).toEqual(mockInstruments)
    expect(result.current.settings).toEqual(mockSettings)
  })

  it('calculates childDiscountPercent from settings', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue(mockSettings)

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.childDiscountPercent).toBe(15)
  })

  it('uses default value for childDiscountPercent when setting not found', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue([])

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.childDiscountPercent).toBe(10)
  })

  it('calculates groupMaxStudents from settings', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue(mockSettings)

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.groupMaxStudents).toBe(8)
  })

  it('uses default value for groupMaxStudents when setting not found', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue([])

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.groupMaxStudents).toBe(6)
  })

  it('calculates workshopMaxStudents from settings', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue(mockSettings)

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.workshopMaxStudents).toBe(16)
  })

  it('uses default value for workshopMaxStudents when setting not found', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue([])

    const { result } = renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.workshopMaxStudents).toBe(12)
  })

  it('calls instrumentsApi.getAll with activeOnly: true', async () => {
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue([])

    renderHook(() => useCourseTypesData(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(instrumentsApi.getAll).toHaveBeenCalledWith({ activeOnly: true })
    })
  })
})
