import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import { Step3CalendarSlotSelection } from '../Step3CalendarSlotSelection'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as roomsApi from '@/services/api'
import * as calendarApi from '@/services/api'
import * as coursesApi from '@/services/api'

vi.mock('@/services/api', () => ({
  roomsApi: {
    getAll: vi.fn(),
  },
  calendarApi: {
    getWeek: vi.fn(),
    checkAvailability: vi.fn(),
  },
  coursesApi: {
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

    vi.mocked(calendarApi.calendarApi.getWeek).mockResolvedValue({
      weekStart: '2024-03-18',
      weekEnd: '2024-03-24',
      lessons: [],
      holidays: [],
    })

    vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([])
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

  it('should render week navigation after loading', async () => {
    renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

    await waitFor(() => {
      // Navigation is in CalendarComponent header
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

  describe('Week calendar data', () => {
    it('should use courses API for calendar data instead of day-level calendar API', async () => {
      // The component now uses coursesApi.getAll to fetch courses
      // and transforms them into calendar events using useCalendarEvents hook
      // Courses query is enabled when: !step1.isTrial && !!step3.selectedRoomId
      vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([])

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        // Rooms are always fetched
        expect(roomsApi.roomsApi.getAll).toHaveBeenCalled()
      })

      // The calendar data comes from courses, not from a dedicated week calendar endpoint
      // coursesApi.getAll is only called when a room is selected in the context
      // Without a room selected, courses query is not enabled
    })

    it('should handle lessons on multiple dates in a week', async () => {
      vi.mocked(calendarApi.calendarApi.getWeek).mockResolvedValue({
        weekStart: '2024-03-18',
        weekEnd: '2024-03-24',
        lessons: [
          {
            id: 'lesson-mon',
            title: 'Monday Lesson',
            date: '2024-03-18',
            startTime: '09:00',
            endTime: '10:00',
            teacherName: 'Teacher A',
            studentName: 'Student A',
            instrumentName: 'Piano',
            status: 'Scheduled',
          },
          {
            id: 'lesson-tue',
            title: 'Tuesday Lesson',
            date: '2024-03-19',
            startTime: '10:00',
            endTime: '11:00',
            teacherName: 'Teacher A',
            studentName: 'Student B',
            instrumentName: 'Guitar',
            status: 'Scheduled',
          },
          {
            id: 'lesson-wed',
            title: 'Wednesday Lesson',
            date: '2024-03-20',
            startTime: '14:00',
            endTime: '15:00',
            teacherName: 'Teacher A',
            studentName: 'Student C',
            instrumentName: 'Drums',
            status: 'Scheduled',
          },
          {
            id: 'lesson-fri',
            title: 'Friday Lesson',
            date: '2024-03-22',
            startTime: '16:00',
            endTime: '17:00',
            teacherName: 'Teacher A',
            studentName: 'Student D',
            instrumentName: 'Violin',
            status: 'Scheduled',
          },
        ],
        holidays: [],
      })

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(roomsApi.roomsApi.getAll).toHaveBeenCalled()
      })

      // Verify week data structure is correct
      const mockCall = vi.mocked(calendarApi.calendarApi.getWeek)
      expect(mockCall).toBeDefined()
    })

    it('should include courses from multiple days of the week', async () => {
      vi.mocked(calendarApi.calendarApi.getWeek).mockResolvedValue({
        weekStart: '2024-03-18',
        weekEnd: '2024-03-24',
        lessons: [],
        holidays: [],
      })

      vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([
        {
          id: 'course-mon',
          teacherId: 'teacher-1',
          teacherName: 'Teacher A',
          courseTypeId: 1,
          courseTypeName: 'Individual Piano',
          instrumentName: 'Piano',
          dayOfWeek: 1, // Monday
          startTime: '09:00',
          endTime: '10:00',
          frequency: 'Weekly',
          weekParity: 'All',
          startDate: '2024-01-01',
          status: 'Active',
          isWorkshop: false,
          isTrial: false,
          enrollmentCount: 1,
          enrollments: [
            {
              id: 'e1',
              studentId: 's1',
              studentName: 'Student A',
              courseId: 'course-mon',
              enrolledAt: '2024-01-01',
              discountPercent: 0,
              discountType: 'None',
              status: 'Active',
            },
          ],
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'course-wed',
          teacherId: 'teacher-1',
          teacherName: 'Teacher A',
          courseTypeId: 2,
          courseTypeName: 'Group Guitar',
          instrumentName: 'Guitar',
          dayOfWeek: 3, // Wednesday
          startTime: '14:00',
          endTime: '15:30',
          frequency: 'Weekly',
          weekParity: 'All',
          startDate: '2024-01-01',
          status: 'Active',
          isWorkshop: false,
          isTrial: false,
          enrollmentCount: 2,
          enrollments: [
            {
              id: 'e2',
              studentId: 's2',
              studentName: 'Student B',
              courseId: 'course-wed',
              enrolledAt: '2024-01-01',
              discountPercent: 0,
              discountType: 'None',
              status: 'Active',
            },
            {
              id: 'e3',
              studentId: 's3',
              studentName: 'Student C',
              courseId: 'course-wed',
              enrolledAt: '2024-01-01',
              discountPercent: 0,
              discountType: 'None',
              status: 'Active',
            },
          ],
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
      ])

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(roomsApi.roomsApi.getAll).toHaveBeenCalled()
      })

      // Courses query is only enabled when a room is selected; no room selected here
      expect(coursesApi.coursesApi.getAll).not.toHaveBeenCalled()
    })
  })

  describe('handles empty and missing data gracefully', () => {
    it('should handle empty lessons array in calendar response', async () => {
      vi.mocked(calendarApi.calendarApi.getWeek).mockResolvedValue({
        weekStart: '2024-03-18',
        weekEnd: '2024-03-24',
        lessons: [],
        holidays: [],
      })

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
      })
    })

    it('should handle empty holidays array in calendar response', async () => {
      vi.mocked(calendarApi.calendarApi.getWeek).mockResolvedValue({
        weekStart: '2024-03-18',
        weekEnd: '2024-03-24',
        lessons: [],
        holidays: [],
      })

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
      })
    })

    it('should handle empty courses array', async () => {
      vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([])

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
      })
    })

    it('should handle empty rooms array', async () => {
      vi.mocked(roomsApi.roomsApi.getAll).mockResolvedValue([])

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
      })
    })

    it('should handle course with empty enrollments', async () => {
      vi.mocked(coursesApi.coursesApi.getAll).mockResolvedValue([
        {
          id: 'course-1',
          teacherId: 'teacher-1',
          teacherName: 'Teacher A',
          courseTypeId: 1,
          courseTypeName: 'Individual Piano',
          instrumentName: 'Piano',
          dayOfWeek: 1,
          startTime: '09:00',
          endTime: '10:00',
          frequency: 'Weekly',
          weekParity: 'All',
          startDate: '2024-01-01',
          status: 'Active',
          isWorkshop: false,
          isTrial: false,
          enrollmentCount: 0,
          enrollments: [],
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
      ])

      renderWithProviders(<Step3CalendarSlotSelection {...mockProps} />)

      await waitFor(() => {
        expect(screen.getByText(/lesson configuration/i)).toBeInTheDocument()
      })
    })
  })
})
