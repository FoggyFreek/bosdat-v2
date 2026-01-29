import { useQuery } from '@tanstack/react-query'
import { instrumentsApi, courseTypesApi, settingsApi } from '@/services/api'
import type { Instrument } from '@/features/instruments/types'
import type { CourseType } from '@/features/course-types/types'

interface Setting {
  key: string
  value: string
}

export interface CourseTypesData {
  courseTypes: CourseType[]
  instruments: Instrument[]
  settings: Setting[]
  isLoading: boolean
  childDiscountPercent: number
  groupMaxStudents: number
  workshopMaxStudents: number
}

export const useCourseTypesData = (): CourseTypesData => {
  const { data: courseTypes = [], isLoading: isLoadingCourseTypes } = useQuery<CourseType[]>({
    queryKey: ['courseTypes'],
    queryFn: () => courseTypesApi.getAll(),
  })

  const { data: instruments = [], isLoading: isLoadingInstruments } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll({ activeOnly: true }),
  })

  const { data: settings = [], isLoading: isLoadingSettings } = useQuery<Setting[]>({
    queryKey: ['settings'],
    queryFn: () => settingsApi.getAll(),
  })

  const childDiscountPercent = Number.parseFloat(
    settings.find(s => s.key === 'child_discount_percent')?.value || '10'
  )
  const groupMaxStudents = Number.parseInt(
    settings.find(s => s.key === 'group_max_students')?.value || '6',
    10
  )
  const workshopMaxStudents = Number.parseInt(
    settings.find(s => s.key === 'workshop_max_students')?.value || '12',
    10
  )

  return {
    courseTypes,
    instruments,
    settings,
    isLoading: isLoadingCourseTypes || isLoadingInstruments || isLoadingSettings,
    childDiscountPercent,
    groupMaxStudents,
    workshopMaxStudents,
  }
}
