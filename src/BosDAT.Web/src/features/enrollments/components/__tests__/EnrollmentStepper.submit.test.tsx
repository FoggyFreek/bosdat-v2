import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { EnrollmentStepper } from '../EnrollmentStepper'
import { coursesApi } from '@/features/courses/api'
import { enrollmentsApi } from '@/features/enrollments/api'
import { schedulingApi } from '@/features/settings/api'
import type { Course } from '@/features/courses/types'
import type { ManualRunResult } from '@/features/settings/types'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn().mockResolvedValue([
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
    ]),
  },
}))

vi.mock('@/features/courses/api', () => ({
  coursesApi: {
    create: vi.fn(),
  },
}))

vi.mock('@/features/enrollments/api', () => ({
  enrollmentsApi: {
    create: vi.fn(),
  },
}))

vi.mock('@/features/settings/api', () => ({
  schedulingApi: {
    runSingle: vi.fn(),
  },
}))

const mockToast = vi.fn()
vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({ toast: mockToast }),
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

vi.mock('../../context/EnrollmentFormContext', async () => {
  const actual = await vi.importActual('../../context/EnrollmentFormContext')
  return { ...actual }
})

const mockCreatedCourse: Course = {
  id: 'new-course-1',
  teacherId: 't-1',
  teacherName: 'John Smith',
  courseTypeId: 'ct-1',
  courseTypeName: 'Piano Individual',
  instrumentName: 'Piano',
  roomId: 1,
  dayOfWeek: 'Monday',
  startTime: '10:00:00',
  endTime: '10:30:00',
  frequency: 'Weekly',
  weekParity: 'All',
  startDate: '2025-09-01',
  endDate: '2026-06-30',
  status: 'Active',
  isWorkshop: false,
  isTrial: false,
  enrollmentCount: 0,
  enrollments: [],
  createdAt: '2025-01-01T00:00:00',
  updatedAt: '2025-01-01T00:00:00',
}

const mockRunResult: ManualRunResult = {
  scheduleRunId: 'run-1',
  startDate: '2025-09-01',
  endDate: '2025-12-01',
  totalCoursesProcessed: 1,
  totalLessonsCreated: 15,
  totalLessonsSkipped: 0,
  status: 'Success',
}

const defaultFormData = {
  step1: {
    courseTypeId: 'ct-1',
    teacherId: 't-1',
    startDate: '2025-09-01',
    endDate: '2026-06-30',
    isTrial: false,
    recurrence: 'Weekly' as const,
  },
  step2: {
    students: [
      {
        studentId: 's-1',
        studentName: 'Alice Johnson',
        enrolledAt: '2025-09-01',
        discountType: 'Family' as const,
        discountPercentage: 15,
        invoicingPreference: 'Monthly' as const,
        note: 'First year student',
        isEligibleForCourseDiscount: false,
      },
      {
        studentId: 's-2',
        studentName: 'Bob Williams',
        enrolledAt: '2025-09-01',
        discountType: 'None' as const,
        discountPercentage: 0,
        invoicingPreference: 'Quarterly' as const,
        note: '',
        isEligibleForCourseDiscount: false,
      },
    ],
  },
  step3: {
    selectedRoomId: 1,
    selectedDayOfWeek: 1, // Monday
    selectedDate: '2025-09-01',
    selectedStartTime: '10:00',
    selectedEndTime: '10:30',
  },
  step4: {},
}

const defaultContextValue = {
  formData: defaultFormData,
  currentStep: 3,
  updateStep1: vi.fn(),
  updateStep2: vi.fn(),
  updateStep3: vi.fn(),
  addStudent: vi.fn(),
  removeStudent: vi.fn(),
  updateStudent: vi.fn(),
  setCurrentStep: vi.fn(),
  resetForm: vi.fn(),
  isStep1Valid: vi.fn(() => true),
  isStep2Valid: vi.fn(() => ({ isValid: true, errors: [] })),
  isStep3Valid: vi.fn(() => ({ isValid: true, errors: [] })),
}

const mockContextAtStep4 = async (overrides: Record<string, unknown> = {}) => {
  const contextModule = await import('../../context/EnrollmentFormContext')
  return vi.spyOn(contextModule, 'useEnrollmentForm').mockReturnValue({
    ...defaultContextValue,
    ...overrides,
  })
}

describe('EnrollmentStepper - Submit', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockNavigate.mockClear()
    vi.mocked(coursesApi.create).mockResolvedValue(mockCreatedCourse)
    vi.mocked(enrollmentsApi.create).mockResolvedValue({})
    vi.mocked(schedulingApi.runSingle).mockResolvedValue(mockRunResult)
  })

  it('shows Submit button on last step', async () => {
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument()
  })

  it('creates course with correct data on submit', async () => {
    const user = userEvent.setup()
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith({
        courseTypeId: 'ct-1',
        teacherId: 't-1',
        startDate: '2025-09-01',
        endDate: '2026-06-30',
        startTime: '10:00',
        endTime: '10:30',
        isTrial: false,
        roomId: 1,
        frequency: 'Weekly',
        weekParity: 'All',
        dayOfWeek: 'Monday',
      })
    })
  })

  it('enrolls each student after course creation', async () => {
    const user = userEvent.setup()
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(enrollmentsApi.create).toHaveBeenCalledTimes(2)
    })

    expect(enrollmentsApi.create).toHaveBeenCalledWith({
      studentId: 's-1',
      courseId: 'new-course-1',
      discountPercent: 15,
      discountType: 'Family',
      invoicingPreference: 'Monthly',
      notes: 'First year student',
    })

    expect(enrollmentsApi.create).toHaveBeenCalledWith({
      studentId: 's-2',
      courseId: 'new-course-1',
      discountPercent: 0,
      discountType: 'None',
      invoicingPreference: 'Quarterly',
      notes: undefined,
    })
  })

  it('runs scheduling after enrollments', async () => {
    const user = userEvent.setup()
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(schedulingApi.runSingle).toHaveBeenCalledWith('new-course-1')
    })
  })

  it('calls APIs in correct order: create, enroll, schedule', async () => {
    const user = userEvent.setup()
    const callOrder: string[] = []

    vi.mocked(coursesApi.create).mockImplementation(async () => {
      callOrder.push('create')
      return mockCreatedCourse
    })
    vi.mocked(enrollmentsApi.create).mockImplementation(async () => {
      callOrder.push('enrollment')
      return {}
    })
    vi.mocked(schedulingApi.runSingle).mockImplementation(async () => {
      callOrder.push('runSingle')
      return mockRunResult
    })

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(callOrder).toEqual(['create', 'enrollment', 'enrollment', 'runSingle'])
    })
  })

  it('shows Submitting... while in progress', async () => {
    const user = userEvent.setup()

    let resolveCreate!: (value: Course) => void
    vi.mocked(coursesApi.create).mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      })
    )

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    expect(screen.getByRole('button', { name: /submitting/i })).toBeInTheDocument()

    resolveCreate(mockCreatedCourse)
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument()
    })
  })

  it('disables submit button while submitting', async () => {
    const user = userEvent.setup()

    let resolveCreate!: (value: Course) => void
    vi.mocked(coursesApi.create).mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      })
    )

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    expect(screen.getByRole('button', { name: /submitting/i })).toBeDisabled()

    resolveCreate(mockCreatedCourse)
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /submit/i })).not.toBeDisabled()
    })
  })

  it('shows success toast and navigates to course detail on successful submit', async () => {
    const user = userEvent.setup()
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Enrollment created' })
      )
    })

    expect(mockNavigate).toHaveBeenCalledWith('/courses/new-course-1')
  })

  it('handles course creation error gracefully', async () => {
    const user = userEvent.setup()

    vi.mocked(coursesApi.create).mockRejectedValue(new Error('Network error'))

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({ variant: 'destructive' })
      )
    })

    expect(enrollmentsApi.create).not.toHaveBeenCalled()
    expect(schedulingApi.runSingle).not.toHaveBeenCalled()

    expect(screen.getByRole('button', { name: /submit/i })).not.toBeDisabled()
  })

  it('handles enrollment error gracefully', async () => {
    const user = userEvent.setup()

    vi.mocked(enrollmentsApi.create).mockRejectedValue(new Error('Enrollment failed'))

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({ variant: 'destructive' })
      )
    })

    expect(coursesApi.create).toHaveBeenCalled()
    expect(schedulingApi.runSingle).not.toHaveBeenCalled()

    expect(screen.getByRole('button', { name: /submit/i })).not.toBeDisabled()
  })

  it('handles scheduling error gracefully', async () => {
    const user = userEvent.setup()

    vi.mocked(schedulingApi.runSingle).mockRejectedValue(
      new Error('Scheduling failed')
    )

    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(mockToast).toHaveBeenCalledWith(
        expect.objectContaining({ variant: 'destructive' })
      )
    })

    expect(coursesApi.create).toHaveBeenCalled()
    expect(enrollmentsApi.create).toHaveBeenCalledTimes(2)

    expect(screen.getByRole('button', { name: /submit/i })).not.toBeDisabled()
  })

  it('calculates weekParity as Even for biweekly on even ISO week', async () => {
    const user = userEvent.setup()

    // 2025-09-01 is ISO week 36 (even)
    await mockContextAtStep4({
      formData: {
        step1: {
          courseTypeId: 'ct-1',
          teacherId: 't-1',
          startDate: '2025-09-01',
          endDate: '2026-06-30',
          isTrial: false,
          recurrence: 'Biweekly',
        },
        step2: {
          students: [
            {
              studentId: 's-1',
              studentName: 'Alice',
              enrolledAt: '2025-09-01',
              discountType: 'None',
              discountPercentage: 0,
              invoicingPreference: 'Monthly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
          ],
        },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: 1,
          selectedDate: '2025-09-01',
          selectedStartTime: '14:00',
          selectedEndTime: '14:30',
        },
        step4: {},
      },
    })

    render(<EnrollmentStepper />)
    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          frequency: 'Biweekly',
          weekParity: 'Even',
        })
      )
    })
  })

  it('calculates weekParity as Odd for biweekly on odd ISO week', async () => {
    const user = userEvent.setup()

    // 2025-09-08 is ISO week 37 (odd)
    await mockContextAtStep4({
      formData: {
        step1: {
          courseTypeId: 'ct-1',
          teacherId: 't-1',
          startDate: '2025-09-08',
          endDate: '2026-06-30',
          isTrial: false,
          recurrence: 'Biweekly',
        },
        step2: {
          students: [
            {
              studentId: 's-1',
              studentName: 'Alice',
              enrolledAt: '2025-09-08',
              discountType: 'None',
              discountPercentage: 0,
              invoicingPreference: 'Monthly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
          ],
        },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: 1,
          selectedDate: '2025-09-08',
          selectedStartTime: '14:00',
          selectedEndTime: '14:30',
        },
        step4: {},
      },
    })

    render(<EnrollmentStepper />)
    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          frequency: 'Biweekly',
          weekParity: 'Odd',
        })
      )
    })
  })

  it('sets weekParity to All for weekly courses', async () => {
    const user = userEvent.setup()
    await mockContextAtStep4()
    render(<EnrollmentStepper />)

    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          frequency: 'Weekly',
          weekParity: 'All',
        })
      )
    })
  })

  it('sets frequency to Weekly and weekParity to All for trial courses', async () => {
    const user = userEvent.setup()

    await mockContextAtStep4({
      formData: {
        step1: {
          courseTypeId: 'ct-1',
          teacherId: 't-1',
          startDate: '2025-09-01',
          endDate: '2025-09-01',
          recurrence: 'Weekly',
        },
        step2: {
          students: [
            {
              studentId: 's-1',
              studentName: 'Alice',
              enrolledAt: '2025-09-01',
              discountType: 'None',
              discountPercentage: 0,
              invoicingPreference: 'Monthly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
          ],
        },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: 1,
          selectedDate: '2025-09-01',
          selectedStartTime: '14:00',
          selectedEndTime: '14:30',
        },
        step4: {},
      },
    })

    render(<EnrollmentStepper />)
    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          frequency: 'Weekly',
          weekParity: 'All',
        })
      )
    })
  })

  it('passes endDate as undefined when null', async () => {
    const user = userEvent.setup()

    await mockContextAtStep4({
      formData: {
        step1: {
          courseTypeId: 'ct-1',
          teacherId: 't-1',
          startDate: '2025-09-01',
          endDate: null,
          isTrial: false,
          recurrence: 'Weekly',
        },
        step2: {
          students: [
            {
              studentId: 's-1',
              studentName: 'Alice',
              enrolledAt: '2025-09-01',
              discountType: 'None',
              discountPercentage: 0,
              invoicingPreference: 'Monthly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
          ],
        },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: 1,
          selectedDate: '2025-09-01',
          selectedStartTime: '10:00',
          selectedEndTime: '10:30',
        },
        step4: {},
      },
    })

    render(<EnrollmentStepper />)
    await user.click(screen.getByRole('button', { name: /submit/i }))

    await waitFor(() => {
      expect(coursesApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          endDate: undefined,
        })
      )
    })
  })
})
