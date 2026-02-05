import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useCourseTypeMutations } from '../useCourseTypeMutations'
import { courseTypesApi } from '@/features/course-types/api'
import type { CourseTypeFormData } from '../useCourseTypeForm'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    reactivate: vi.fn(),
    updatePricing: vi.fn(),
    createPricingVersion: vi.fn(),
  },
}))

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

const mockFormData: CourseTypeFormData = {
  name: 'Piano 30min',
  instrumentId: '1',
  durationMinutes: '30',
  customDuration: '',
  type: 'Individual',
  priceAdult: '45.00',
  priceChild: '40.50',
  maxStudents: '1',
  isActive: true,
}

const defaultOptions = {
  getDuration: () => '30',
}

describe('useCourseTypeMutations', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('createMutation', () => {
    it('calls courseTypesApi.create with correct data', async () => {
      vi.mocked(courseTypesApi.create).mockResolvedValue({ id: '1' })
      const onCreateSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onCreateSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.createMutation.mutateAsync(mockFormData)
      })

      expect(courseTypesApi.create).toHaveBeenCalledWith({
        name: 'Piano 30min',
        instrumentId: 1,
        durationMinutes: 30,
        type: 'Individual',
        priceAdult: 45.0,
        priceChild: 40.5,
        maxStudents: 1,
      })
    })

    it('calls onCreateSuccess on success', async () => {
      vi.mocked(courseTypesApi.create).mockResolvedValue({ id: '1' })
      const onCreateSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onCreateSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.createMutation.mutateAsync(mockFormData)
      })

      await waitFor(() => {
        expect(onCreateSuccess).toHaveBeenCalled()
      })
    })

    it('calls onError with message on failure', async () => {
      vi.mocked(courseTypesApi.create).mockRejectedValue({
        response: { data: { message: 'Duplicate name' } },
      })
      const onError = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onError }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        try {
          await result.current.createMutation.mutateAsync(mockFormData)
        } catch {
          // Expected error
        }
      })

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith('Duplicate name')
      })
    })

    it('uses fallback error message when no message in response', async () => {
      vi.mocked(courseTypesApi.create).mockRejectedValue({})
      const onError = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onError }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        try {
          await result.current.createMutation.mutateAsync(mockFormData)
        } catch {
          // Expected error
        }
      })

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith('Failed to create course type')
      })
    })
  })

  describe('updateMutation', () => {
    it('calls courseTypesApi.update with correct data', async () => {
      vi.mocked(courseTypesApi.update).mockResolvedValue({ id: '1' })

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      await act(async () => {
        await result.current.updateMutation.mutateAsync({
          id: '1',
          data: mockFormData,
        })
      })

      expect(courseTypesApi.update).toHaveBeenCalledWith('1', {
        name: 'Piano 30min',
        instrumentId: 1,
        durationMinutes: 30,
        type: 'Individual',
        maxStudents: 1,
        isActive: true,
      })
    })

    it('calls onUpdateSuccess on success', async () => {
      vi.mocked(courseTypesApi.update).mockResolvedValue({ id: '1' })
      const onUpdateSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onUpdateSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.updateMutation.mutateAsync({
          id: '1',
          data: mockFormData,
        })
      })

      await waitFor(() => {
        expect(onUpdateSuccess).toHaveBeenCalled()
      })
    })

    it('calls onError on failure', async () => {
      vi.mocked(courseTypesApi.update).mockRejectedValue({
        response: { data: { message: 'Update failed' } },
      })
      const onError = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onError }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        try {
          await result.current.updateMutation.mutateAsync({
            id: '1',
            data: mockFormData,
          })
        } catch {
          // Expected error
        }
      })

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith('Update failed')
      })
    })
  })

  describe('updatePricingMutation', () => {
    it('calls courseTypesApi.updatePricing with correct data', async () => {
      vi.mocked(courseTypesApi.updatePricing).mockResolvedValue({})

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      await act(async () => {
        await result.current.updatePricingMutation.mutateAsync({
          id: '1',
          priceAdult: 50,
          priceChild: 45,
        })
      })

      expect(courseTypesApi.updatePricing).toHaveBeenCalledWith('1', {
        priceAdult: 50,
        priceChild: 45,
      })
    })

    it('calls onPricingUpdateSuccess on success', async () => {
      vi.mocked(courseTypesApi.updatePricing).mockResolvedValue({})
      const onPricingUpdateSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onPricingUpdateSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.updatePricingMutation.mutateAsync({
          id: '1',
          priceAdult: 50,
          priceChild: 45,
        })
      })

      await waitFor(() => {
        expect(onPricingUpdateSuccess).toHaveBeenCalled()
      })
    })
  })

  describe('createPricingVersionMutation', () => {
    it('calls courseTypesApi.createPricingVersion with correct data', async () => {
      vi.mocked(courseTypesApi.createPricingVersion).mockResolvedValue({})

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      await act(async () => {
        await result.current.createPricingVersionMutation.mutateAsync({
          id: '1',
          data: {
            priceAdult: 50,
            priceChild: 45,
            validFrom: '2024-02-01',
          },
        })
      })

      expect(courseTypesApi.createPricingVersion).toHaveBeenCalledWith('1', {
        priceAdult: 50,
        priceChild: 45,
        validFrom: '2024-02-01',
      })
    })

    it('calls onPricingVersionSuccess on success', async () => {
      vi.mocked(courseTypesApi.createPricingVersion).mockResolvedValue({})
      const onPricingVersionSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onPricingVersionSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.createPricingVersionMutation.mutateAsync({
          id: '1',
          data: {
            priceAdult: 50,
            priceChild: 45,
            validFrom: '2024-02-01',
          },
        })
      })

      await waitFor(() => {
        expect(onPricingVersionSuccess).toHaveBeenCalled()
      })
    })

    it('calls onPricingVersionError on failure', async () => {
      vi.mocked(courseTypesApi.createPricingVersion).mockRejectedValue({
        response: { data: { message: 'Version creation failed' } },
      })
      const onPricingVersionError = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onPricingVersionError }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        try {
          await result.current.createPricingVersionMutation.mutateAsync({
            id: '1',
            data: {
              priceAdult: 50,
              priceChild: 45,
              validFrom: '2024-02-01',
            },
          })
        } catch {
          // Expected error
        }
      })

      await waitFor(() => {
        expect(onPricingVersionError).toHaveBeenCalledWith('Version creation failed')
      })
    })
  })

  describe('archiveMutation', () => {
    it('calls courseTypesApi.delete with correct id', async () => {
      vi.mocked(courseTypesApi.delete).mockResolvedValue(undefined)

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      await act(async () => {
        await result.current.archiveMutation.mutateAsync('1')
      })

      expect(courseTypesApi.delete).toHaveBeenCalledWith('1')
    })

    it('calls onArchiveSuccess on success', async () => {
      vi.mocked(courseTypesApi.delete).mockResolvedValue(undefined)
      const onArchiveSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onArchiveSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.archiveMutation.mutateAsync('1')
      })

      await waitFor(() => {
        expect(onArchiveSuccess).toHaveBeenCalled()
      })
    })

    it('calls onError on failure', async () => {
      vi.mocked(courseTypesApi.delete).mockRejectedValue({
        response: { data: { message: 'Cannot archive' } },
      })
      const onError = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onError }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        try {
          await result.current.archiveMutation.mutateAsync('1')
        } catch {
          // Expected error
        }
      })

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith('Cannot archive')
      })
    })
  })

  describe('reactivateMutation', () => {
    it('calls courseTypesApi.reactivate with correct id', async () => {
      vi.mocked(courseTypesApi.reactivate).mockResolvedValue({})

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      await act(async () => {
        await result.current.reactivateMutation.mutateAsync('1')
      })

      expect(courseTypesApi.reactivate).toHaveBeenCalledWith('1')
    })

    it('calls onReactivateSuccess on success', async () => {
      vi.mocked(courseTypesApi.reactivate).mockResolvedValue({})
      const onReactivateSuccess = vi.fn()

      const { result } = renderHook(
        () => useCourseTypeMutations({ ...defaultOptions, onReactivateSuccess }),
        { wrapper: createWrapper() }
      )

      await act(async () => {
        await result.current.reactivateMutation.mutateAsync('1')
      })

      await waitFor(() => {
        expect(onReactivateSuccess).toHaveBeenCalled()
      })
    })
  })

  describe('isAnyMutationPending', () => {
    it('returns false when no mutations are pending', () => {
      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      expect(result.current.isAnyMutationPending).toBe(false)
    })

    it('tracks pending state correctly across mutations', async () => {
      // This test verifies the isPending logic is set up correctly
      // by checking it returns false initially and after mutations complete
      vi.mocked(courseTypesApi.create).mockResolvedValue({ id: '1' })

      const { result } = renderHook(() => useCourseTypeMutations(defaultOptions), {
        wrapper: createWrapper(),
      })

      expect(result.current.isAnyMutationPending).toBe(false)

      await act(async () => {
        await result.current.createMutation.mutateAsync(mockFormData)
      })

      await waitFor(() => {
        expect(result.current.isAnyMutationPending).toBe(false)
      })
    })
  })
})
