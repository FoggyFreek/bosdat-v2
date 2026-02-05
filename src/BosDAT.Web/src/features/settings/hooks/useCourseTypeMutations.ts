import { useMutation, useQueryClient } from '@tanstack/react-query'
import { courseTypesApi } from '@/features/course-types/api'
import type { CreateCourseTypePricingVersion } from '@/features/course-types/types'
import type { CourseTypeFormData } from './useCourseTypeForm'

interface ApiError {
  response?: { data?: { message?: string } }
}

const getErrorMessage = (err: ApiError, fallback: string): string => {
  return err.response?.data?.message || fallback
}

export interface UseCourseTypeMutationsOptions {
  onCreateSuccess?: () => void
  onUpdateSuccess?: () => void
  onPricingUpdateSuccess?: () => void
  onPricingVersionSuccess?: () => void
  onArchiveSuccess?: () => void
  onReactivateSuccess?: () => void
  onError?: (error: string) => void
  onPricingVersionError?: (error: string) => void
  getDuration: () => string
}

export interface UseCourseTypeMutationsReturn {
  createMutation: ReturnType<typeof useMutation<unknown, ApiError, CourseTypeFormData>>
  updateMutation: ReturnType<typeof useMutation<unknown, ApiError, { id: string; data: CourseTypeFormData }>>
  updatePricingMutation: ReturnType<typeof useMutation<unknown, ApiError, { id: string; priceAdult: number; priceChild: number }>>
  createPricingVersionMutation: ReturnType<typeof useMutation<unknown, ApiError, { id: string; data: CreateCourseTypePricingVersion }>>
  archiveMutation: ReturnType<typeof useMutation<unknown, ApiError, string>>
  reactivateMutation: ReturnType<typeof useMutation<unknown, ApiError, string>>
  isAnyMutationPending: boolean
}

export const useCourseTypeMutations = ({
  onCreateSuccess,
  onUpdateSuccess,
  onPricingUpdateSuccess,
  onPricingVersionSuccess,
  onArchiveSuccess,
  onReactivateSuccess,
  onError,
  onPricingVersionError,
  getDuration,
}: UseCourseTypeMutationsOptions): UseCourseTypeMutationsReturn => {
  const queryClient = useQueryClient()

  const invalidateCourseTypes = () => {
    queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: CourseTypeFormData) =>
      courseTypesApi.create({
        name: data.name,
        instrumentId: Number.parseInt(data.instrumentId, 10),
        durationMinutes: Number.parseInt(getDuration(), 10),
        type: data.type,
        priceAdult: Number.parseFloat(data.priceAdult),
        priceChild: Number.parseFloat(data.priceChild),
        maxStudents: Number.parseInt(data.maxStudents, 10),
      }),
    onSuccess: () => {
      invalidateCourseTypes()
      onCreateSuccess?.()
    },
    onError: (err: ApiError) => {
      onError?.(getErrorMessage(err, 'Failed to create course type'))
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CourseTypeFormData }) =>
      courseTypesApi.update(id, {
        name: data.name,
        instrumentId: Number.parseInt(data.instrumentId, 10),
        durationMinutes: Number.parseInt(getDuration(), 10),
        type: data.type,
        maxStudents: Number.parseInt(data.maxStudents, 10),
        isActive: data.isActive,
      }),
    onSuccess: () => {
      invalidateCourseTypes()
      onUpdateSuccess?.()
    },
    onError: (err: ApiError) => {
      onError?.(getErrorMessage(err, 'Failed to update course type'))
    },
  })

  const updatePricingMutation = useMutation({
    mutationFn: ({ id, priceAdult, priceChild }: { id: string; priceAdult: number; priceChild: number }) =>
      courseTypesApi.updatePricing(id, { priceAdult, priceChild }),
    onSuccess: () => {
      invalidateCourseTypes()
      onPricingUpdateSuccess?.()
    },
    onError: (err: ApiError) => {
      onError?.(getErrorMessage(err, 'Failed to update pricing'))
    },
  })

  const createPricingVersionMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateCourseTypePricingVersion }) =>
      courseTypesApi.createPricingVersion(id, data),
    onSuccess: () => {
      invalidateCourseTypes()
      onPricingVersionSuccess?.()
    },
    onError: (err: ApiError) => {
      onPricingVersionError?.(getErrorMessage(err, 'Failed to create pricing version'))
    },
  })

  const archiveMutation = useMutation({
    mutationFn: (id: string) => courseTypesApi.delete(id),
    onSuccess: () => {
      invalidateCourseTypes()
      onArchiveSuccess?.()
    },
    onError: (err: ApiError) => {
      onError?.(getErrorMessage(err, 'Failed to archive course type'))
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => courseTypesApi.reactivate(id),
    onSuccess: () => {
      invalidateCourseTypes()
      onReactivateSuccess?.()
    },
    onError: (err: ApiError) => {
      onError?.(getErrorMessage(err, 'Failed to reactivate course type'))
    },
  })

  const isAnyMutationPending =
    createMutation.isPending ||
    updateMutation.isPending ||
    updatePricingMutation.isPending ||
    createPricingVersionMutation.isPending ||
    archiveMutation.isPending ||
    reactivateMutation.isPending

  return {
    createMutation,
    updateMutation,
    updatePricingMutation,
    createPricingVersionMutation,
    archiveMutation,
    reactivateMutation,
    isAnyMutationPending,
  }
}
