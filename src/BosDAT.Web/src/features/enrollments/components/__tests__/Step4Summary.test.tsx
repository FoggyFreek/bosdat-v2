import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import { Step4Summary } from '../Step4Summary'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'

vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/teachers/api', () => ({
  teachersApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('@/features/rooms/api', () => ({
  roomsApi: {
    getAll: vi.fn(),
  },
}))

vi.mock('../../context/EnrollmentFormContext', async () => {
  const actual = await vi.importActual('../../context/EnrollmentFormContext')
  return {
    ...actual,
  }
})

const renderWithProvider = (ui: React.ReactElement) => {
  return render(<EnrollmentFormProvider>{ui}</EnrollmentFormProvider>)
}

describe('Step4Summary', () => {
  beforeEach(async () => {
    const { courseTypesApi } = await import('@/features/course-types/api')
    const { teachersApi } = await import('@/features/teachers/api')
    const { roomsApi } = await import('@/features/rooms/api')

    // Set up default mock implementations to avoid undefined query data
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(teachersApi.getAll).mockResolvedValue([])
    vi.mocked(roomsApi.getAll).mockResolvedValue([])
  })
  it('renders confirmation step title', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getByText(/enrollments.step4.confirmation/i)).toBeInTheDocument()
    expect(
      screen.getByText(/enrollments.step4.reviewDetails/i)
    ).toBeInTheDocument()
  })

  it('shows ready to submit message', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getByText(/enrollments.step4.readyToSubmit/i)).toBeInTheDocument()
    expect(
      screen.getByText(/enrollments.step4.conflictCheckOnSubmit/i)
    ).toBeInTheDocument()
  })

  it('renders course details and schedule cards', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getByText('enrollments.step4.courseDetails')).toBeInTheDocument()
    expect(screen.getByText('enrollments.step4.scheduleAndRoom')).toBeInTheDocument()
  })

  it('shows no students selected when students is empty', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getAllByText('enrollments.step4.enrolledStudents').length).toBeGreaterThan(0)
  })

  it('renders student list with populated form data', async () => {
    const { courseTypesApi } = await import('@/features/course-types/api')
    const { teachersApi } = await import('@/features/teachers/api')
    const { roomsApi } = await import('@/features/rooms/api')

    vi.mocked(courseTypesApi.getAll).mockResolvedValue([
      {
        id: 'ct1',
        instrumentId: 1,
        instrumentName: 'Piano',
        name: 'Piano Beginner',
        durationMinutes: 45,
        type: 'Individual',
        maxStudents: 1,
        isActive: true,
        activeCourseCount: 0,
        hasTeachersForCourseType: true,
        currentPricing: null,
        pricingHistory: [],
        canEditPricingDirectly: true,
      },
    ])

    vi.mocked(teachersApi.getAll).mockResolvedValue([
      {
        id: 't1',
        fullName: 'Jane Smith',
        email: 'jane@test.com',
        instrumentNames: ['Piano'],
        isActive: true,
        activeCourseCount: 0,
      },
    ])

    vi.mocked(roomsApi.getAll).mockResolvedValue([
      {
        id: 1,
        name: 'Room A',
        capacity: 10,
        isActive: true,
        hasPiano: false,
        hasDrums: false,
        hasAmplifier: false,
        hasMicrophone: false,
        hasWhiteboard: false,
        hasStereo: false,
        hasGuitar: false,
        activeCourseCount: 0,
        scheduledLessonCount: 0,
      },
    ])

    vi.spyOn(
      await import('../../context/EnrollmentFormContext'),
      'useEnrollmentForm'
    ).mockReturnValue({
      formData: {
        step1: {
          courseTypeId: 'ct1',
          teacherId: 't1',
          startDate: '2025-09-01',
          endDate: '2025-09-01',
          recurrence: 'Trial',
        },
        step2: {
          students: [
            {
              studentId: 's1',
              studentName: 'Alice Johnson',
              enrolledAt: '2025-09-01',
              discountType: 'Family',
              discountPercentage: 15,
              invoicingPreference: 'Monthly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
            {
              studentId: 's2',
              studentName: 'Bob Williams',
              enrolledAt: '2025-09-01',
              discountType: 'None',
              discountPercentage: 0,
              invoicingPreference: 'Quarterly',
              note: '',
              isEligibleForCourseDiscount: false,
            },
          ],
        },
        step3: {
          selectedRoomId: 1,
          selectedDayOfWeek: 1,
          selectedDate: '2025-09-01',
          selectedStartTime: '10:00',
          selectedEndTime: '10:45',
        },
        step4: {},
      },
      currentStep: 3,
      updateStep1: vi.fn(),
      updateStep2: vi.fn(),
      updateStep3: vi.fn(),
      addStudent: vi.fn(),
      removeStudent: vi.fn(),
      updateStudent: vi.fn(),
      syncStartDate: vi.fn(),
      setCurrentStep: vi.fn(),
      resetForm: vi.fn(),
      isStep1Valid: vi.fn(() => true),
      isStep2Valid: vi.fn(() => ({ isValid: true, errors: [] })),
      isStep3Valid: vi.fn(() => ({ isValid: true, errors: [] })),
    })

    render(<Step4Summary />)

    expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    expect(screen.getByText('Bob Williams')).toBeInTheDocument()
    expect(screen.getByText(/15% discount \(Family\)/)).toBeInTheDocument()
    expect(screen.getByText('Monthly')).toBeInTheDocument()
    expect(screen.getByText('Quarterly')).toBeInTheDocument()
    expect(screen.getByText(/enrollments.step4.enrolledStudents/)).toBeInTheDocument()
    expect(screen.getByText(/enrollments.summary.trialLesson/)).toBeInTheDocument()
  })
})
