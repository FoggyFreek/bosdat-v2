import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { Step3Summary } from '../Step3Summary'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'

const mockRooms = [
  {
    id: 1,
    name: 'Room 1',
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
  {
    id: 2,
    name: 'Room 2',
    capacity: 5,
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
  {
    id: 3,
    name: 'Room 3',
    capacity: 15,
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
]

const mockStudents = [
  {
    studentId: 's1',
    studentName: 'Alice Johnson',
    enrolledAt: '2025-09-01',
    discountType: 'None' as const,
    discountPercentage: 0,
    invoicingPreference: 'Monthly' as const,
    note: '',
    isEligibleForCourseDiscount: false,
  },
  {
    studentId: 's2',
    studentName: 'Bob Williams',
    enrolledAt: '2025-09-01',
    discountType: 'None' as const,
    discountPercentage: 0,
    invoicingPreference: 'Monthly' as const,
    note: '',
    isEligibleForCourseDiscount: false,
  },
]

const mockUpdateStep3 = vi.fn()

vi.mock('../../context/EnrollmentFormContext', async () => {
  const actual = await vi.importActual('../../context/EnrollmentFormContext')
  return {
    ...actual,
  }
})

const renderWithProvider = (ui: React.ReactElement) => {
  return render(<EnrollmentFormProvider>{ui}</EnrollmentFormProvider>)
}

describe('Step3Summary', () => {
  it('should render lesson configuration section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/enrollments.step2.lessonConfiguration/i)).toBeInTheDocument()
  })

  it('should render selected students section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/enrollments.step3.selectedStudents/i)).toBeInTheDocument()
  })

  it('should render room selection section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/enrollments.step3.roomSelection/i)).toBeInTheDocument()
  })

  it('should render room selector dropdown', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByRole('combobox')).toBeInTheDocument()
  })

  it('should render student initials when students are in context', async () => {
    // We mock useEnrollmentForm to return students
    vi.spyOn(
      await import('../../context/EnrollmentFormContext'),
      'useEnrollmentForm'
    ).mockReturnValue({
      formData: {
        step1: {
          courseTypeId: null,
          teacherId: null,
          startDate: null,
          endDate: null,
          recurrence: 'Weekly',
        },
        step2: { students: mockStudents },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: null,
          selectedDate: null,
          selectedStartTime: null,
          selectedEndTime: null,
        },
        step4: {},
      },
      currentStep: 2,
      updateStep1: vi.fn(),
      updateStep2: vi.fn(),
      updateStep3: mockUpdateStep3,
      addStudent: vi.fn(),
      removeStudent: vi.fn(),
      updateStudent: vi.fn(),
      syncStartDate: vi.fn(),
      setCurrentStep: vi.fn(),
      resetForm: vi.fn(),
      isStep1Valid: vi.fn(() => false),
      isStep2Valid: vi.fn(() => ({ isValid: true, errors: [] })),
      isStep3Valid: vi.fn(() => ({ isValid: true, errors: [] })),
    })

    render(<Step3Summary rooms={mockRooms} />)

    // AJ = Alice Johnson, BW = Bob Williams
    expect(screen.getByText('AJ')).toBeInTheDocument()
    expect(screen.getByText('BW')).toBeInTheDocument()
  })

  it('should call updateStep3 when room is selected', async () => {
    vi.spyOn(
      await import('../../context/EnrollmentFormContext'),
      'useEnrollmentForm'
    ).mockReturnValue({
      formData: {
        step1: {
          courseTypeId: null,
          teacherId: null,
          startDate: null,
          endDate: null,
          recurrence: 'Weekly',
        },
        step2: { students: [] },
        step3: {
          selectedRoomId: null,
          selectedDayOfWeek: null,
          selectedDate: null,
          selectedStartTime: null,
          selectedEndTime: null,
        },
        step4: {},
      },
      currentStep: 2,
      updateStep1: vi.fn(),
      updateStep2: vi.fn(),
      updateStep3: mockUpdateStep3,
      addStudent: vi.fn(),
      removeStudent: vi.fn(),
      updateStudent: vi.fn(),
      syncStartDate: vi.fn(),
      setCurrentStep: vi.fn(),
      resetForm: vi.fn(),
      isStep1Valid: vi.fn(() => false),
      isStep2Valid: vi.fn(() => ({ isValid: true, errors: [] })),
      isStep3Valid: vi.fn(() => ({ isValid: true, errors: [] })),
    })

    render(<Step3Summary rooms={mockRooms} />)

    const user = userEvent.setup()
    const combobox = screen.getByRole('combobox')
    await user.click(combobox)
    const option = screen.getByText('Room 1')
    await user.click(option)

    expect(mockUpdateStep3).toHaveBeenCalledWith({ selectedRoomId: 1 })
  })

  describe('handles undefined arrays gracefully', () => {
    it('should render with undefined rooms array', () => {
      renderWithProvider(<Step3Summary rooms={undefined as unknown as typeof mockRooms} />)

      expect(screen.getByText(/enrollments.step3.selectedStudents/i)).toBeInTheDocument()
      expect(screen.getByText(/enrollments.step3.roomSelection/i)).toBeInTheDocument()
    })

    it('should render with empty rooms array', () => {
      renderWithProvider(<Step3Summary rooms={[]} />)

      expect(screen.getByText(/enrollments.step3.selectedStudents/i)).toBeInTheDocument()
      expect(screen.getByText(/enrollments.step3.roomSelection/i)).toBeInTheDocument()
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })

    it('should render with undefined students in context', () => {
      // The component should handle when step2.students is undefined
      renderWithProvider(<Step3Summary rooms={mockRooms} />)

      // Should render the selected students section even with no students
      expect(screen.getByText(/enrollments.step3.selectedStudents/i)).toBeInTheDocument()
    })
  })
})
