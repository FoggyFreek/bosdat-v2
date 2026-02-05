import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { EnrollmentStepper } from '../EnrollmentStepper'
import { courseTypesApi } from '@/features/course-types/api'
import { teachersApi } from '@/features/teachers/api'
import { settingsApi } from '@/features/settings/api'
import { studentsApi } from '@/features/students/api'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList, Teacher } from '@/features/teachers/types'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/teachers/api', () => ({
  teachersApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
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
]

const mockTeachers: TeacherList[] = [
  {
    id: 't-1',
    fullName: 'John Smith',
    email: 'john@example.com',
    isActive: true,
    role: 'Teacher',
    instruments: ['Piano'],
    courseTypes: ['ct-1'],
  },
]

const mockTeacher: Teacher = {
  id: 't-1',
  firstName: 'John',
  lastName: 'Smith',
  fullName: 'John Smith',
  email: 'john@example.com',
  phone: '',
  hourlyRate: 50,
  isActive: true,
  role: 'Teacher',
  instruments: [{ id: 1, name: 'Piano', category: 'Keyboard', isActive: true }],
  courseTypes: [{ id: 'ct-1', name: 'Piano Individual', instrumentId: 1, instrumentName: 'Piano', durationMinutes: 30, type: 'Individual' }],
  createdAt: '2024-01-01',
  updatedAt: '2024-01-01',
}

describe('EnrollmentStepper', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(settingsApi.getByKey).mockImplementation((key: string) => {
      if (key === 'family_discount_percent') {
        return Promise.resolve({ key, value: '10', type: 'decimal' })
      }
      if (key === 'course_discount_percent') {
        return Promise.resolve({ key, value: '10', type: 'decimal' })
      }
      return Promise.resolve({ key, value: '', type: 'string' })
    })
    vi.mocked(studentsApi.getAll).mockResolvedValue([])
    vi.mocked(studentsApi.hasActiveEnrollments).mockResolvedValue(false)
    vi.mocked(courseTypesApi.getAll).mockResolvedValue(mockCourseTypes)
    // Mock teachers API to return filtered teachers based on courseTypeId
    vi.mocked(teachersApi.getAll).mockImplementation((params) => {
      if (params?.courseTypeId) {
        const filtered = mockTeachers.filter((t) =>
          t.courseTypes.includes(params.courseTypeId!)
        )
        return Promise.resolve(filtered)
      }
      return Promise.resolve(mockTeachers)
    })
    vi.mocked(teachersApi.getById).mockResolvedValue(mockTeacher)
  })

  it('renders stepper with 4 steps', async () => {
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByText('Lesson Details')).toBeInTheDocument()
      expect(screen.getByText('Students')).toBeInTheDocument()
      expect(screen.getByText('Time Slot')).toBeInTheDocument()
      expect(screen.getByText('Confirmation')).toBeInTheDocument()
    })
  })

  it('renders Step1 component initially', async () => {
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByLabelText(/course type/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/teacher/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/start date/i)).toBeInTheDocument()
    })
  })

  it('shows Next button on step 1', async () => {
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument()
    })
  })

  it('disables Next button when step 1 is incomplete', async () => {
    render(<EnrollmentStepper />)

    await waitFor(() => {
      const nextButton = screen.getByRole('button', { name: /next/i })
      expect(nextButton).toBeDisabled()
    })
  })

  it('enables Next button when step 1 is valid', async () => {
    const user = userEvent.setup()
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
    })

    // Fill out step 1
    const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
    await user.click(courseTypeSelect)
    await user.click(screen.getByRole('option', { name: /piano individual/i }))

    const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
    await user.click(teacherSelect)
    await user.click(screen.getByRole('option', { name: /john smith/i }))

    const startDateInput = screen.getByLabelText(/start date/i)
    await user.clear(startDateInput)
    await user.type(startDateInput, '2024-01-15')

    await waitFor(() => {
      const nextButton = screen.getByRole('button', { name: /next/i })
      expect(nextButton).not.toBeDisabled()
    })
  })

  it('shows Previous button on step 2', async () => {
    const user = userEvent.setup()
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
    })

    // Fill out step 1
    const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
    await user.click(courseTypeSelect)
    await user.click(screen.getByRole('option', { name: /piano individual/i }))

    const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
    await user.click(teacherSelect)
    await user.click(screen.getByRole('option', { name: /john smith/i }))

    const startDateInput = screen.getByLabelText(/start date/i)
    await user.clear(startDateInput)
    await user.type(startDateInput, '2024-01-15')

    // Go to step 2
    await user.click(screen.getByRole('button', { name: /next/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /previous/i })).toBeInTheDocument()
    })
  })

  it('navigates back to step 1 and preserves form data', async () => {
    const user = userEvent.setup()
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
    })

    // Fill out step 1
    const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
    await user.click(courseTypeSelect)
    await user.click(screen.getByRole('option', { name: /piano individual/i }))

    const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
    await user.click(teacherSelect)
    await user.click(screen.getByRole('option', { name: /john smith/i }))

    const startDateInput = screen.getByLabelText(/start date/i)
    await user.clear(startDateInput)
    await user.type(startDateInput, '2024-01-15')

    // Go to step 2
    await user.click(screen.getByRole('button', { name: /next/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /previous/i })).toBeInTheDocument()
    })

    // Go back to step 1
    await user.click(screen.getByRole('button', { name: /previous/i }))

    // Check that form data is preserved
    await waitFor(() => {
      expect(screen.getByText(/piano individual/i)).toBeInTheDocument()
      expect(screen.getByText(/john smith/i)).toBeInTheDocument()
    })
  })

  it('shows placeholder for steps 2-4', async () => {
    const user = userEvent.setup()
    render(<EnrollmentStepper />)

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
    })

    // Fill out step 1
    const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
    await user.click(courseTypeSelect)
    await user.click(screen.getByRole('option', { name: /piano individual/i }))

    const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
    await user.click(teacherSelect)
    await user.click(screen.getByRole('option', { name: /john smith/i }))

    const startDateInput = screen.getByLabelText(/start date/i)
    await user.clear(startDateInput)
    await user.type(startDateInput, '2024-01-15')

    // Go to step 2
    await user.click(screen.getByRole('button', { name: /next/i }))

    await waitFor(() => {
      // Step 2 shows the student selection component with search functionality
      expect(screen.getByText(/search students/i)).toBeInTheDocument()
      expect(screen.getByPlaceholderText(/search by name or email/i)).toBeInTheDocument()
    })
  })
})
