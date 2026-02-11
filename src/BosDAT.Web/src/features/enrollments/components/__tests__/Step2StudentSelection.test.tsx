import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import { Step2StudentSelection } from '../Step2StudentSelection'
import { EnrollmentFormProvider, useEnrollmentForm } from '../../context/EnrollmentFormContext'
import { courseTypesApi } from '@/features/course-types/api'
import { settingsApi } from '@/features/settings/api'
import { studentsApi } from '@/features/students/api'
import { teachersApi } from '@/features/teachers/api'
import type { CourseType } from '@/features/course-types/types'
import type { StudentList } from '@/features/students/types'
import { useEffect } from 'react'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/settings/api', () => ({
  settingsApi: {
    getByKey: vi.fn(),
  },
}))

vi.mock('@/features/students/api', () => ({
  studentsApi: {
    getAll: vi.fn(),
    hasActiveEnrollments: vi.fn(),
  },
}))

vi.mock('@/features/teachers/api', () => ({
  teachersApi: {
    getAll: vi.fn(),
  },
}))

const mockCourseTypes: CourseType[] = [
  {
    id: 'ct-1',
    instrumentId: 1,
    instrumentName: 'Piano',
    name: 'Piano Individual',
    durationMinutes: 30,
    type: 'Individual',
    maxStudents: 1,
    isActive: true,
    activeCourseCount: 0,
    hasTeachersForCourseType: true,
    currentPricing: null,
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
  {
    id: 'ct-2',
    instrumentId: 2,
    instrumentName: 'Guitar',
    name: 'Guitar Group',
    durationMinutes: 45,
    type: 'Group',
    maxStudents: 4,
    isActive: true,
    activeCourseCount: 0,
    hasTeachersForCourseType: true,
    currentPricing: null,
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
]

const mockStudents: StudentList[] = [
  {
    id: 'student-1',
    fullName: 'John Doe',
    email: 'john@example.com',
    phone: '123-456-7890',
    status: 'Active',
  },
  {
    id: 'student-2',
    fullName: 'Jane Smith',
    email: 'jane@example.com',
    phone: '987-654-3210',
    status: 'Active',
  },
]

// Component to setup step1 data before rendering Step2
const Step2WithStep1Setup = ({
  courseTypeId = 'ct-1',
  startDate = '2024-01-15',
}: {
  courseTypeId?: string
  startDate?: string
}) => {
  const { updateStep1 } = useEnrollmentForm()

  useEffect(() => {
    updateStep1({
      courseTypeId,
      teacherId: 't-1',
      startDate,
    })
  }, [courseTypeId, startDate, updateStep1])

  return <Step2StudentSelection />
}

const renderWithProvider = (courseTypeId = 'ct-1', startDate = '2024-01-15') => {
  return render(
    <EnrollmentFormProvider>
      <Step2WithStep1Setup courseTypeId={courseTypeId} startDate={startDate} />
    </EnrollmentFormProvider>
  )
}

describe('Step2StudentSelection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(courseTypesApi.getAll).mockResolvedValue(mockCourseTypes)
    vi.mocked(settingsApi.getByKey).mockImplementation((key: string) => {
      if (key === 'family_discount_percent') {
        return Promise.resolve({ key, value: '10', type: 'decimal' })
      }
      if (key === 'course_discount_percent') {
        return Promise.resolve({ key, value: '10', type: 'decimal' })
      }
      return Promise.resolve({ key, value: '', type: 'string' })
    })
    vi.mocked(studentsApi.getAll).mockResolvedValue(mockStudents)
    vi.mocked(studentsApi.hasActiveEnrollments).mockResolvedValue(false)
    vi.mocked(teachersApi.getAll).mockResolvedValue([])
  })

  it('renders the lesson configuration summary', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByText('enrollments.step2.lessonConfiguration')).toBeInTheDocument()
    })
  })

  it('renders the student search section', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByText('enrollments.step2.searchStudents')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('enrollments.step2.searchPlaceholder')).toBeInTheDocument()
    })
  })

  it('renders the New Student button', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'enrollments.step2.newStudent' })).toBeInTheDocument()
    })
  })

  it('shows empty state when no students selected', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByText('enrollments.step2.noStudentsSelected')).toBeInTheDocument()
    })
  })

  it('shows validation error when no students selected', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByText(/at least one student must be selected/i)).toBeInTheDocument()
    })
  })

  it('shows minimum character message before searching', async () => {
    renderWithProvider()

    await waitFor(() => {
      expect(screen.getByText('enrollments.step2.searchHint')).toBeInTheDocument()
    })
  })

  it('displays max students info for Group course', async () => {
    renderWithProvider('ct-2')

    await waitFor(() => {
      expect(screen.getByText('enrollments.summary.maxStudents')).toBeInTheDocument()
      expect(screen.getByText('4')).toBeInTheDocument()
    })
  })

  it('displays max students info for Individual course', async () => {
    renderWithProvider('ct-1')

    await waitFor(() => {
      expect(screen.getByText('enrollments.summary.maxStudents')).toBeInTheDocument()
      expect(screen.getByText('1')).toBeInTheDocument()
    })
  })
})
