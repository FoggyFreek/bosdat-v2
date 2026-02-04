import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils'
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

const renderWithProvider = (ui: React.ReactElement) => {
  return render(<EnrollmentFormProvider>{ui}</EnrollmentFormProvider>)
}

describe('Step3Summary', () => {
  it('should render lesson configuration section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
  })

  it('should render selected students section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/selected students/i)).toBeInTheDocument()
  })

  it('should render room selection section', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByText(/room selection/i)).toBeInTheDocument()
  })

  it('should render room selector dropdown', () => {
    renderWithProvider(<Step3Summary rooms={mockRooms} />)

    expect(screen.getByRole('combobox')).toBeInTheDocument()
  })

  describe('handles undefined arrays gracefully', () => {
    it('should render with undefined rooms array', () => {
      renderWithProvider(<Step3Summary rooms={undefined as unknown as typeof mockRooms} />)

      expect(screen.getByText(/selected students/i)).toBeInTheDocument()
      expect(screen.getByText(/room selection/i)).toBeInTheDocument()
    })

    it('should render with empty rooms array', () => {
      renderWithProvider(<Step3Summary rooms={[]} />)

      expect(screen.getByText(/selected students/i)).toBeInTheDocument()
      expect(screen.getByText(/room selection/i)).toBeInTheDocument()
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })

    it('should render with undefined students in context', () => {
      // The component should handle when step2.students is undefined
      renderWithProvider(<Step3Summary rooms={mockRooms} />)

      // Should render the selected students section even with no students
      expect(screen.getByText(/selected students/i)).toBeInTheDocument()
    })
  })
})
