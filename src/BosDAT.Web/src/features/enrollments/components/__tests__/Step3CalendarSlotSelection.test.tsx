import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import { Step3CalendarSlotSelection } from '../Step3CalendarSlotSelection'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as roomsApi from '@/services/api'
import * as calendarApi from '@/services/api'
import * as coursesApi from '@/services/api'
import * as holidaysApi from '@/services/api'

vi.mock('@/services/api', () => ({
  roomsApi: {
    getAll: vi.fn(),
  },
  calendarApi: {
    getDay: vi.fn(),
    checkAvailability: vi.fn(),
  },
  coursesApi: {
    getAll: vi.fn(),
  },
  holidaysApi: {
    getAll: vi.fn(),
  },
}))

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

const renderWithProviders = (ui: React.ReactElement) => {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <EnrollmentFormProvider>{ui}</EnrollmentFormProvider>
    </QueryClientProvider>
  )
}

describe('Step3CalendarSlotSelection', () => {
  const mockProps = {
    teacherId: 'teacher-1',
    durationMinutes: 60,
  }

  beforeEach(() => {
    vi.clearAllMocks()

    // Default mock implementations
    vi.mocked(roomsApi.roomsApi.getAll).mockResolvedValue([
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
    ])

    vi.mocked(calendarApi.calendarApi.getDay).mockResolvedValue({
      date: '2024-03-20',
      dayOfWeek: 3,
      lessons: [],
      isHoliday: false,
    })

    vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([])

    vi.mocked(holidaysApi.holidaysApi.getAll).mockResolvedValue([])
  })

  it('should render loading state initially', () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('should render summary panel after loading', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      // Summary now uses Step2Summary which gets data from context
      // Check for summary section instead of specific text
      expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
    })
  })

  it('should render day navigation after loading', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      expect(screen.getByLabelText(/previous week/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/next week/i)).toBeInTheDocument()
    })
  })

  it('should fetch rooms on mount', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      expect(roomsApi.roomsApi.getAll).toHaveBeenCalledWith({ activeOnly: true })
    })
  })

  it('should not fetch calendar data until room is selected', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    // Calendar data is only fetched when a room is selected (enabled: !!step3.selectedRoomId)
    // Wait for rooms to load first
    await waitFor(() => {
      expect(roomsApi.roomsApi.getAll).toHaveBeenCalled()
    })

    // Calendar should not be called yet (no room selected)
    expect(calendarApi.calendarApi.getDay).not.toHaveBeenCalled()
  })

  it('should fetch holidays on mount', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      expect(holidaysApi.holidaysApi.getAll).toHaveBeenCalled()
    })
  })

  it('should render calendar grid after loading', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      // Check for time slots
      expect(screen.getByText('08:00')).toBeInTheDocument()
    })
  })

  it('should handle errors gracefully', async () => {
    vi.mocked(roomsApi.roomsApi.getAll).mockRejectedValue(new Error('Failed to fetch rooms'))

    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument()
    })
  })
})
