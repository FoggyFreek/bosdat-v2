import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { Step1LessonDetails } from '../Step1LessonDetails'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'
import { courseTypesApi, teachersApi } from '@/services/api'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'

vi.mock('@/services/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
  },
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
  {
    id: 'ct-3',
    instrumentId: 1,
    instrumentName: 'Piano',
    name: 'Piano Workshop',
    durationMinutes: 120,
    type: 'Workshop',
    maxStudents: 10,
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
    courseTypes: ['ct-1', 'ct-3'],
  },
  {
    id: 't-2',
    fullName: 'Jane Doe',
    email: 'jane@example.com',
    isActive: true,
    role: 'Teacher',
    instruments: ['Guitar'],
    courseTypes: ['ct-2'],
  },
]

const renderWithProvider = (ui: React.ReactElement) => {
  return render(
    <EnrollmentFormProvider>
      {ui}
    </EnrollmentFormProvider>
  )
}

describe('Step1LessonDetails', () => {
  beforeEach(() => {
    vi.clearAllMocks()
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
  })

  describe('AC1: CourseType dropdown', () => {
    it('renders CourseType dropdown', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByLabelText(/course type/i)).toBeInTheDocument()
      })
    })

    it('populates CourseType dropdown with active course types', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByLabelText(/course type/i)).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)

      await waitFor(() => {
        expect(screen.getByRole('option', { name: /piano individual/i })).toBeInTheDocument()
        expect(screen.getByRole('option', { name: /guitar group/i })).toBeInTheDocument()
        expect(screen.getByRole('option', { name: /piano workshop/i })).toBeInTheDocument()
      })
    })
  })

  describe('AC2: Teacher dropdown filtered by CourseType', () => {
    it('renders Teacher dropdown', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByLabelText(/teacher/i)).toBeInTheDocument()
      })
    })

    it('disables Teacher dropdown when no CourseType selected', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
        expect(teacherSelect).toBeDisabled()
      })
    })

    it('filters teachers by selected CourseType', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      // Select Piano Individual course type
      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      // Teacher dropdown should be enabled and show filtered teachers
      const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
      expect(teacherSelect).not.toBeDisabled()

      await user.click(teacherSelect)

      await waitFor(() => {
        // John Smith teaches Piano Individual (ct-1)
        expect(screen.getByRole('option', { name: /john smith/i })).toBeInTheDocument()
        // Jane Doe does not teach Piano Individual
        expect(screen.queryByRole('option', { name: /jane doe/i })).not.toBeInTheDocument()
      })
    })
  })

  describe('AC3: Day of week display', () => {
    it('renders Start Date field', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByLabelText(/start date/i)).toBeInTheDocument()
      })
    })

    it('shows day of week when start date is selected', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByLabelText(/start date/i)).toBeInTheDocument()
      })

      const dateInput = screen.getByLabelText(/start date/i)
      await user.clear(dateInput)
      await user.type(dateInput, '2024-01-15') // January 15, 2024 is a Monday

      await waitFor(() => {
        expect(screen.getByText(/monday/i)).toBeInTheDocument()
      })
    })
  })

  describe('AC4/AC5: Trail toggle visibility', () => {
    it('shows Trail toggle for Individual course type', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      await waitFor(() => {
        expect(screen.getByLabelText(/trial lesson/i)).toBeInTheDocument()
      })
    })

    it('shows Trail toggle for Group course type', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /guitar group/i }))

      await waitFor(() => {
        expect(screen.getByLabelText(/trial lesson/i)).toBeInTheDocument()
      })
    })

    it('hides Trail toggle for Workshop course type', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano workshop/i }))

      await waitFor(() => {
        expect(screen.queryByLabelText(/trial lesson/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('AC6: Recurrence options', () => {
    it('shows recurrence options (weekly/bi-weekly)', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('radio', { name: /^weekly$/i })).toBeInTheDocument()
        expect(screen.getByRole('radio', { name: /bi-weekly/i })).toBeInTheDocument()
      })
    })

    it('defaults to Weekly recurrence', async () => {
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        const weeklyRadio = screen.getByRole('radio', { name: /^weekly$/i })
        expect(weeklyRadio).toBeChecked()
      })
    })

    it('allows switching to Bi-weekly recurrence', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('radio', { name: /bi-weekly/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('radio', { name: /bi-weekly/i }))

      await waitFor(() => {
        expect(screen.getByRole('radio', { name: /bi-weekly/i })).toBeChecked()
        expect(screen.getByRole('radio', { name: /^weekly$/i })).not.toBeChecked()
      })
    })
  })

  describe('AC7: End date required for Workshop', () => {
    it('requires end date for Workshop', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano workshop/i }))

      await waitFor(() => {
        const endDateInput = screen.getByLabelText(/end date/i)
        expect(endDateInput).toHaveAttribute('required')
      })
    })
  })

  describe('AC8: End date optional for Individual/Group', () => {
    it('makes end date optional for Individual', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      await waitFor(() => {
        const endDateInput = screen.getByLabelText(/end date/i)
        expect(endDateInput).not.toHaveAttribute('required')
      })
    })

    it('makes end date optional for Group', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /guitar group/i }))

      await waitFor(() => {
        const endDateInput = screen.getByLabelText(/end date/i)
        expect(endDateInput).not.toHaveAttribute('required')
      })
    })
  })

  describe('Trial lesson behavior', () => {
    it('sets end date equal to start date when trial is enabled', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      // Select Individual course type
      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      // Set start date
      const startDateInput = screen.getByLabelText(/start date/i)
      await user.clear(startDateInput)
      await user.type(startDateInput, '2024-01-15')

      // Enable trial
      await user.click(screen.getByLabelText(/trial lesson/i))

      await waitFor(() => {
        const endDateInput = screen.getByLabelText(/end date/i) as HTMLInputElement
        expect(endDateInput.value).toBe('2024-01-15')
      })
    })

    it('disables end date when trial is enabled', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      // Select Individual course type
      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      // Enable trial
      await user.click(screen.getByLabelText(/trial lesson/i))

      await waitFor(() => {
        const endDateInput = screen.getByLabelText(/end date/i)
        expect(endDateInput).toBeDisabled()
      })
    })
  })

  describe('AC9: Data persistence', () => {
    it('maintains form data when values are changed', async () => {
      const user = userEvent.setup()
      renderWithProvider(<Step1LessonDetails />)

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /course type/i })).toBeInTheDocument()
      })

      // Select course type
      const courseTypeSelect = screen.getByRole('combobox', { name: /course type/i })
      await user.click(courseTypeSelect)
      await user.click(screen.getByRole('option', { name: /piano individual/i }))

      // Select teacher
      const teacherSelect = screen.getByRole('combobox', { name: /teacher/i })
      await user.click(teacherSelect)
      await user.click(screen.getByRole('option', { name: /john smith/i }))

      // Set start date
      const startDateInput = screen.getByLabelText(/start date/i)
      await user.clear(startDateInput)
      await user.type(startDateInput, '2024-01-15')

      // Verify values are set
      await waitFor(() => {
        expect(screen.getByText(/piano individual/i)).toBeInTheDocument()
        expect(screen.getByText(/john smith/i)).toBeInTheDocument()
      })
    })
  })
})
