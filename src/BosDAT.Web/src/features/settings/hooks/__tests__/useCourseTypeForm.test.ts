import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useCourseTypeForm, DEFAULT_FORM_DATA, DURATION_OPTIONS } from '../useCourseTypeForm'
import { courseTypesApi } from '@/features/course-types/api'
import type { CourseType } from '@/features/course-types/types'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getTeacherCountForInstrument: vi.fn(),
  },
}))

const defaultOptions = {
  childDiscountPercent: 10,
  groupMaxStudents: 6,
  workshopMaxStudents: 12,
}

const mockCourseType: CourseType = {
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
}

describe('useCourseTypeForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(courseTypesApi.getTeacherCountForInstrument).mockResolvedValue(1)
  })

  describe('initial state', () => {
    it('returns default form data', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      expect(result.current.formData).toEqual(DEFAULT_FORM_DATA)
      expect(result.current.showAdd).toBe(false)
      expect(result.current.editId).toBeNull()
      expect(result.current.editingCourseType).toBeNull()
      expect(result.current.useCustomDuration).toBe(false)
      expect(result.current.error).toBeNull()
      expect(result.current.teacherWarning).toBeNull()
    })

    it('exports DURATION_OPTIONS constant', () => {
      expect(DURATION_OPTIONS).toEqual(['20', '30', '40', '45', '50', '60', '90', '120'])
    })
  })

  describe('handleShowAdd', () => {
    it('sets showAdd to true', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.handleShowAdd()
      })

      expect(result.current.showAdd).toBe(true)
    })

    it('calls onDirtyChange with true', () => {
      const onDirtyChange = vi.fn()
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, onDirtyChange })
      )

      act(() => {
        result.current.handleShowAdd()
      })

      expect(onDirtyChange).toHaveBeenCalledWith(true)
    })
  })

  describe('handleInstrumentChange', () => {
    it('updates instrumentId in form data', async () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.handleInstrumentChange('5')
      })

      expect(result.current.formData.instrumentId).toBe('5')
    })

    it('clears teacher warning when no teachers available', async () => {
      vi.mocked(courseTypesApi.getTeacherCountForInstrument).mockResolvedValue(0)
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.handleInstrumentChange('5')
      })

      await waitFor(() => {
        expect(result.current.teacherWarning).toBe('No active teachers teach this instrument')
      })
    })

    it('clears teacher warning when teachers are available', async () => {
      vi.mocked(courseTypesApi.getTeacherCountForInstrument).mockResolvedValue(3)
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.handleInstrumentChange('5')
      })

      await waitFor(() => {
        expect(result.current.teacherWarning).toBeNull()
      })
    })

    it('clears teacher warning when instrumentId is empty', async () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.handleInstrumentChange('')
      })

      expect(result.current.teacherWarning).toBeNull()
    })

    it('handles API error gracefully', async () => {
      vi.mocked(courseTypesApi.getTeacherCountForInstrument).mockRejectedValue(
        new Error('API Error')
      )
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.handleInstrumentChange('5')
      })

      await waitFor(() => {
        expect(result.current.teacherWarning).toBeNull()
      })
    })
  })

  describe('handleTypeChange', () => {
    it('sets maxStudents to 1 for Individual type', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.handleTypeChange('Individual')
      })

      expect(result.current.formData.type).toBe('Individual')
      expect(result.current.formData.maxStudents).toBe('1')
    })

    it('sets maxStudents to groupMaxStudents for Group type', () => {
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, groupMaxStudents: 8 })
      )

      act(() => {
        result.current.handleTypeChange('Group')
      })

      expect(result.current.formData.type).toBe('Group')
      expect(result.current.formData.maxStudents).toBe('8')
    })

    it('sets maxStudents to workshopMaxStudents for Workshop type', () => {
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, workshopMaxStudents: 16 })
      )

      act(() => {
        result.current.handleTypeChange('Workshop')
      })

      expect(result.current.formData.type).toBe('Workshop')
      expect(result.current.formData.maxStudents).toBe('16')
    })
  })

  describe('handleAdultPriceChange', () => {
    it('updates adult price and calculates child price with discount', () => {
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, childDiscountPercent: 10 })
      )

      act(() => {
        result.current.handleAdultPriceChange('100')
      })

      expect(result.current.formData.priceAdult).toBe('100')
      expect(result.current.formData.priceChild).toBe('90.00')
    })

    it('handles empty input', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.handleAdultPriceChange('')
      })

      expect(result.current.formData.priceAdult).toBe('')
      expect(result.current.formData.priceChild).toBe('0.00')
    })

    it('applies correct discount percentage', () => {
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, childDiscountPercent: 20 })
      )

      act(() => {
        result.current.handleAdultPriceChange('50')
      })

      expect(result.current.formData.priceChild).toBe('40.00')
    })
  })

  describe('handleChildPriceChange', () => {
    it('updates child price', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({ ...prev, priceAdult: '100' }))
      })

      act(() => {
        result.current.handleChildPriceChange('80')
      })

      expect(result.current.formData.priceChild).toBe('80')
    })

    it('sets error when child price exceeds adult price', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({ ...prev, priceAdult: '50' }))
      })

      act(() => {
        result.current.handleChildPriceChange('60')
      })

      expect(result.current.error).toBe('Child price cannot be higher than adult price')
    })

    it('clears error when child price is valid', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({ ...prev, priceAdult: '50' }))
      })

      act(() => {
        result.current.handleChildPriceChange('60')
      })

      act(() => {
        result.current.handleChildPriceChange('40')
      })

      expect(result.current.error).toBeNull()
    })
  })

  describe('getDuration', () => {
    it('returns durationMinutes when not using custom duration', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({ ...prev, durationMinutes: '45' }))
      })

      expect(result.current.getDuration()).toBe('45')
    })

    it('returns customDuration when using custom duration', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setUseCustomDuration(true)
        result.current.setFormData((prev) => ({ ...prev, customDuration: '25' }))
      })

      expect(result.current.getDuration()).toBe('25')
    })
  })

  describe('resetForm', () => {
    it('resets all form state to defaults', () => {
      const onDirtyChange = vi.fn()
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, onDirtyChange })
      )

      act(() => {
        result.current.handleShowAdd()
        result.current.setFormData((prev) => ({ ...prev, name: 'Test' }))
        result.current.setError('Some error')
        result.current.setUseCustomDuration(true)
      })

      act(() => {
        result.current.resetForm()
      })

      expect(result.current.formData).toEqual(DEFAULT_FORM_DATA)
      expect(result.current.showAdd).toBe(false)
      expect(result.current.editId).toBeNull()
      expect(result.current.editingCourseType).toBeNull()
      expect(result.current.error).toBeNull()
      expect(result.current.useCustomDuration).toBe(false)
      expect(onDirtyChange).toHaveBeenLastCalledWith(false)
    })
  })

  describe('startEdit', () => {
    it('populates form data from course type', async () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.startEdit(mockCourseType)
      })

      expect(result.current.formData.name).toBe('Piano 30min')
      expect(result.current.formData.instrumentId).toBe('1')
      expect(result.current.formData.durationMinutes).toBe('30')
      expect(result.current.formData.type).toBe('Individual')
      expect(result.current.formData.priceAdult).toBe('45.00')
      expect(result.current.formData.priceChild).toBe('40.50')
      expect(result.current.formData.maxStudents).toBe('1')
      expect(result.current.formData.isActive).toBe(true)
    })

    it('sets editId and editingCourseType', async () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.startEdit(mockCourseType)
      })

      expect(result.current.editId).toBe('1')
      expect(result.current.editingCourseType).toBe(mockCourseType)
    })

    it('sets showAdd to false', async () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.handleShowAdd()
      })

      await act(async () => {
        result.current.startEdit(mockCourseType)
      })

      expect(result.current.showAdd).toBe(false)
    })

    it('sets useCustomDuration for non-standard durations', async () => {
      const customDurationCourseType: CourseType = {
        ...mockCourseType,
        durationMinutes: 25, // Not in DURATION_OPTIONS
      }

      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.startEdit(customDurationCourseType)
      })

      expect(result.current.useCustomDuration).toBe(true)
      expect(result.current.formData.customDuration).toBe('25')
    })

    it('handles course type without current pricing', async () => {
      const noPricingCourseType: CourseType = {
        ...mockCourseType,
        currentPricing: null,
      }

      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      await act(async () => {
        result.current.startEdit(noPricingCourseType)
      })

      expect(result.current.formData.priceAdult).toBe('0.00')
      expect(result.current.formData.priceChild).toBe('0.00')
    })

    it('calls onDirtyChange with true', async () => {
      const onDirtyChange = vi.fn()
      const { result } = renderHook(() =>
        useCourseTypeForm({ ...defaultOptions, onDirtyChange })
      )

      await act(async () => {
        result.current.startEdit(mockCourseType)
      })

      expect(onDirtyChange).toHaveBeenCalledWith(true)
    })
  })

  describe('isFormValid', () => {
    it('returns false when name is empty', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({
          ...prev,
          name: '',
          instrumentId: '1',
          priceAdult: '50',
        }))
      })

      expect(result.current.isFormValid).toBe(false)
    })

    it('returns false when instrumentId is empty', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({
          ...prev,
          name: 'Test',
          instrumentId: '',
          priceAdult: '50',
        }))
      })

      expect(result.current.isFormValid).toBe(false)
    })

    it('returns false when priceAdult is empty', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({
          ...prev,
          name: 'Test',
          instrumentId: '1',
          priceAdult: '',
        }))
      })

      expect(result.current.isFormValid).toBe(false)
    })

    it('returns false when there is an error', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({
          ...prev,
          name: 'Test',
          instrumentId: '1',
          priceAdult: '50',
        }))
        result.current.setError('Some error')
      })

      expect(result.current.isFormValid).toBe(false)
    })

    it('returns true when all required fields are filled and no error', () => {
      const { result } = renderHook(() => useCourseTypeForm(defaultOptions))

      act(() => {
        result.current.setFormData((prev) => ({
          ...prev,
          name: 'Test',
          instrumentId: '1',
          priceAdult: '50',
        }))
      })

      expect(result.current.isFormValid).toBe(true)
    })
  })
})
