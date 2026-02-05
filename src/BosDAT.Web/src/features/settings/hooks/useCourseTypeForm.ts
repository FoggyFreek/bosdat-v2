import { useState, useCallback } from 'react'
import { courseTypesApi } from '@/features/course-types/api'
import type { CourseType, CourseTypeCategory } from '@/features/course-types/types'

export const DURATION_OPTIONS = ['20', '30', '40', '45', '50', '60', '90', '120'] as const

export interface CourseTypeFormData {
  name: string
  instrumentId: string
  durationMinutes: string
  customDuration: string
  type: CourseTypeCategory
  priceAdult: string
  priceChild: string
  maxStudents: string
  isActive: boolean
}

export const DEFAULT_FORM_DATA: CourseTypeFormData = {
  name: '',
  instrumentId: '',
  durationMinutes: '30',
  customDuration: '',
  type: 'Individual',
  priceAdult: '',
  priceChild: '',
  maxStudents: '1',
  isActive: true,
}

export interface UseCourseTypeFormOptions {
  childDiscountPercent: number
  groupMaxStudents: number
  workshopMaxStudents: number
  onDirtyChange?: (dirty: boolean) => void
}

export interface UseCourseTypeFormReturn {
  formData: CourseTypeFormData
  setFormData: React.Dispatch<React.SetStateAction<CourseTypeFormData>>
  showAdd: boolean
  editId: string | null
  editingCourseType: CourseType | null
  useCustomDuration: boolean
  setUseCustomDuration: (value: boolean) => void
  error: string | null
  setError: (error: string | null) => void
  teacherWarning: string | null
  handleInstrumentChange: (value: string) => void
  handleTypeChange: (value: string) => void
  handleAdultPriceChange: (value: string) => void
  handleChildPriceChange: (value: string) => void
  getDuration: () => string
  resetForm: () => void
  startEdit: (courseType: CourseType) => void
  handleShowAdd: () => void
  isFormValid: boolean
}

export const useCourseTypeForm = ({
  childDiscountPercent,
  groupMaxStudents,
  workshopMaxStudents,
  onDirtyChange,
}: UseCourseTypeFormOptions): UseCourseTypeFormReturn => {
  const [formData, setFormData] = useState<CourseTypeFormData>(DEFAULT_FORM_DATA)
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [editingCourseType, setEditingCourseType] = useState<CourseType | null>(null)
  const [useCustomDuration, setUseCustomDuration] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [teacherWarning, setTeacherWarning] = useState<string | null>(null)

  const checkTeachersForInstrument = useCallback(async (instrumentId: string) => {
    if (!instrumentId) {
      setTeacherWarning(null)
      return
    }
    try {
      const result = await courseTypesApi.getTeacherCountForInstrument(Number.parseInt(instrumentId, 10))
      if (result === 0) {
        setTeacherWarning('No active teachers teach this instrument')
      } else {
        setTeacherWarning(null)
      }
    } catch {
      setTeacherWarning(null)
    }
  }, [])

  const handleInstrumentChange = useCallback((value: string) => {
    setFormData(prev => ({ ...prev, instrumentId: value }))
    checkTeachersForInstrument(value)
  }, [checkTeachersForInstrument])

  const handleTypeChange = useCallback((value: string) => {
    let maxStudents = '1'
    if (value === 'Group') maxStudents = groupMaxStudents.toString()
    if (value === 'Workshop') maxStudents = workshopMaxStudents.toString()
    setFormData(prev => ({ ...prev, type: value as CourseTypeCategory, maxStudents }))
  }, [groupMaxStudents, workshopMaxStudents])

  const handleAdultPriceChange = useCallback((value: string) => {
    const adultPrice = Number.parseFloat(value) || 0
    const childPrice = (adultPrice * (1 - childDiscountPercent / 100)).toFixed(2)
    setFormData(prev => ({ ...prev, priceAdult: value, priceChild: childPrice }))
  }, [childDiscountPercent])

  const handleChildPriceChange = useCallback((value: string) => {
    setFormData(prev => {
      const newData = { ...prev, priceChild: value }
      if (Number.parseFloat(value) > Number.parseFloat(prev.priceAdult)) {
        setError('Child price cannot be higher than adult price')
      } else {
        setError(null)
      }
      return newData
    })
  }, [])

  const getDuration = useCallback(() => {
    return useCustomDuration ? formData.customDuration : formData.durationMinutes
  }, [useCustomDuration, formData.customDuration, formData.durationMinutes])

  const resetForm = useCallback(() => {
    setFormData(DEFAULT_FORM_DATA)
    setShowAdd(false)
    setEditId(null)
    setEditingCourseType(null)
    setError(null)
    setTeacherWarning(null)
    setUseCustomDuration(false)
    onDirtyChange?.(false)
  }, [onDirtyChange])

  const startEdit = useCallback((lt: CourseType) => {
    const isCustom = !DURATION_OPTIONS.includes(lt.durationMinutes.toString() as typeof DURATION_OPTIONS[number])
    const currentPricing = lt.currentPricing
    setFormData({
      name: lt.name,
      instrumentId: lt.instrumentId.toString(),
      durationMinutes: isCustom ? '30' : lt.durationMinutes.toString(),
      customDuration: isCustom ? lt.durationMinutes.toString() : '',
      type: lt.type,
      priceAdult: currentPricing?.priceAdult.toFixed(2) ?? '0.00',
      priceChild: currentPricing?.priceChild.toFixed(2) ?? '0.00',
      maxStudents: lt.maxStudents.toString(),
      isActive: lt.isActive,
    })
    setUseCustomDuration(isCustom)
    setEditId(lt.id)
    setEditingCourseType(lt)
    setShowAdd(false)
    onDirtyChange?.(true)
    checkTeachersForInstrument(lt.instrumentId.toString())
  }, [checkTeachersForInstrument, onDirtyChange])

  const handleShowAdd = useCallback(() => {
    resetForm()
    setShowAdd(true)
    onDirtyChange?.(true)
  }, [resetForm, onDirtyChange])

  const isFormValid = Boolean(formData.name && formData.instrumentId && formData.priceAdult && !error)

  return {
    formData,
    setFormData,
    showAdd,
    editId,
    editingCourseType,
    useCustomDuration,
    setUseCustomDuration,
    error,
    setError,
    teacherWarning,
    handleInstrumentChange,
    handleTypeChange,
    handleAdultPriceChange,
    handleChildPriceChange,
    getDuration,
    resetForm,
    startEdit,
    handleShowAdd,
    isFormValid,
  }
}
