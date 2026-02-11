import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, within } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { SchedulePage } from '../SchedulePage'
import { calendarApi } from '@/features/schedule/api'
import { teachersApi } from '@/features/teachers/api'
import { roomsApi } from '@/features/rooms/api'
import type { WeekCalendar, CalendarLesson, Holiday } from '@/features/schedule/types'
import type { TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import type { SchedulerProps } from '@/components'

// Mock API modules
vi.mock('@/features/schedule/api', () => ({
  calendarApi: {
    getWeek: vi.fn(),
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

// Mock CalendarComponent to isolate SchedulePage logic
vi.mock('@/components', () => ({
  CalendarComponent: (props: SchedulerProps) => (
    <div data-testid="calendar-component">
      <div data-testid="calendar-title">{props.title}</div>
      <div data-testid="calendar-events-count">{props.events.length}</div>
      <button onClick={props.onNavigatePrevious} aria-label="Previous week">
        Previous
      </button>
      <button onClick={props.onNavigateNext} aria-label="Next week">
        Next
      </button>
    </div>
  ),
}))

describe('SchedulePage', () => {
  // Mock data
  const mockTeachers: TeacherList[] = [
    {
      id: 'teacher-1',
      fullName: 'John Doe',
      email: 'john@example.com',
      isActive: true,
      role: 'Teacher',
      instruments: ['Piano', 'Drums'],
      courseTypes: ['Individual', 'Group'],
    },
    {
      id: 'teacher-2',
      fullName: 'Jane Smith',
      email: 'jane@example.com',
      isActive: true,
      role: 'Teacher',
      instruments: ['Guitar', 'Violin'],
      courseTypes: ['Individual'],
    },
  ]

  const mockRooms: Room[] = [
    {
      id: 1,
      name: 'Room A',
      capacity: 5,
      hasPiano: true,
      hasDrums: false,
      hasAmplifier: false,
      hasMicrophone: false,
      hasWhiteboard: true,
      hasStereo: false,
      hasGuitar: false,
      isActive: true,
      activeCourseCount: 2,
      scheduledLessonCount: 5,
    },
    {
      id: 2,
      name: 'Room B',
      capacity: 3,
      hasPiano: false,
      hasDrums: true,
      hasAmplifier: true,
      hasMicrophone: true,
      hasWhiteboard: false,
      hasStereo: true,
      hasGuitar: true,
      isActive: true,
      activeCourseCount: 1,
      scheduledLessonCount: 3,
    },
  ]

  const mockLessons: CalendarLesson[] = [
    {
      id: 'lesson-1',
      courseId: 'course-1',
      title: 'Piano Lesson',
      date: '2026-01-27', // Monday of current week
      startTime: '10:00',
      endTime: '11:00',
      frequency: 'Weekly',
      studentName: 'Alice Johnson',
      teacherName: 'John Doe',
      isTrial: false,
      isWorkshop: false,
      roomName: 'Room A',
      instrumentName: 'Piano',
      status: 'Scheduled',
    },
    {
      id: 'lesson-2',
      courseId: 'course-2',
      title: 'Guitar Lesson',
      date: '2026-01-28', // Tuesday
      startTime: '14:00',
      endTime: '15:00',
      frequency: 'Weekly',
      studentName: 'Bob Smith',
      teacherName: 'Jane Smith',
      isTrial: false,
      isWorkshop: false,
      roomName: 'Room B',
      instrumentName: 'Guitar',
      status: 'Completed',
    },
    {
      id: 'lesson-3',
      courseId: 'course-3',
      title: 'Drums Lesson',
      date: '2026-01-29', // Wednesday
      startTime: '16:00',
      endTime: '17:00',
      frequency: 'Weekly',
      teacherName: 'John Doe',
      isTrial: false,
      isWorkshop: false,
      roomName: 'Room B',
      instrumentName: 'Drums',
      status: 'Cancelled',
    },
  ]

  const mockHolidays: Holiday[] = [
    {
      id: 1,
      name: 'Summer Break',
      startDate: '2026-07-01',
      endDate: '2026-08-31',
    },
  ]

  const mockCalendarData: WeekCalendar = {
    weekStart: '2026-01-26',
    weekEnd: '2026-02-01',
    lessons: mockLessons,
    holidays: mockHolidays,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    // Set a fixed date for testing (Friday, January 31, 2026)
    vi.useRealTimers()
    vi.setSystemTime(new Date('2026-01-31T12:00:00Z'))

    // Default mock responses
    vi.mocked(calendarApi.getWeek).mockResolvedValue(mockCalendarData)
    vi.mocked(teachersApi.getAll).mockResolvedValue(mockTeachers)
    vi.mocked(roomsApi.getAll).mockResolvedValue(mockRooms)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('Rendering and Initial State', () => {
    it('renders the schedule title', async () => {
      render(<SchedulePage />)

      expect(screen.getByRole('heading', { name: 'schedule.title' })).toBeInTheDocument()
    })

    it('shows loading state initially', () => {
      // Use a pending promise to keep the component in loading state
      vi.mocked(calendarApi.getWeek).mockImplementation(() => new Promise(() => {}))

      render(<SchedulePage />)

      // Look for the spinner element by its class
      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('displays week range in header', async () => {
      render(<SchedulePage />)

      // Current week starts on Monday (26 jan) and ends on Sunday (1 feb)
      await waitFor(() => {
        const dateRange = screen.getByText(/26 jan.*-.*1 feb/i)
        expect(dateRange).toBeInTheDocument()
      })
    })

    it('renders filter controls', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allTeachers')).toBeInTheDocument()
        expect(screen.getByText('schedule.filters.allRooms')).toBeInTheDocument()
      })
    })

    it('renders refresh button', () => {
      render(<SchedulePage />)

      const buttons = screen.getAllByRole('button')
      const refreshButton = buttons.find(btn => btn.querySelector('svg.lucide-refresh-cw'))
      expect(refreshButton).toBeDefined()
    })

    it('renders CalendarComponent with correct props', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByTestId('calendar-component')).toBeInTheDocument()
        expect(screen.getByTestId('calendar-title')).toHaveTextContent('schedule.week')
      })
    })
  })

  describe('Data Conversion', () => {
    it('converts lessons to calendar events correctly', async () => {
      // Use calendar data without holidays to isolate lesson conversion
      vi.mocked(calendarApi.getWeek).mockResolvedValue({
        ...mockCalendarData,
        holidays: [],
      })

      render(<SchedulePage />)

      await waitFor(() => {
        const eventsCount = screen.getByTestId('calendar-events-count')
        expect(eventsCount).toHaveTextContent('3') // 3 mock lessons
      })
    })

    it('handles empty lessons array', async () => {
      vi.mocked(calendarApi.getWeek).mockResolvedValue({
        ...mockCalendarData,
        lessons: [],
        holidays: [],
      })

      render(<SchedulePage />)

      await waitFor(() => {
        const eventsCount = screen.getByTestId('calendar-events-count')
        expect(eventsCount).toHaveTextContent('0')
      })
    })
  })

  describe('Week Navigation', () => {
    it('calls API when navigating to previous week', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByTestId('calendar-component')).toBeInTheDocument()
      })

      const initialCallCount = vi.mocked(calendarApi.getWeek).mock.calls.length

      const user = userEvent.setup({ delay: null })
      const prevButton = screen.getByRole('button', { name: /previous week/i })
      await user.click(prevButton)

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledTimes(initialCallCount + 1)
      })
    })

    it('calls API when navigating to next week', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByTestId('calendar-component')).toBeInTheDocument()
      })

      const initialCallCount = vi.mocked(calendarApi.getWeek).mock.calls.length

      const user = userEvent.setup({ delay: null })
      const nextButton = screen.getByRole('button', { name: /next week/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledTimes(initialCallCount + 1)
      })
    })

    it('updates week range display when navigating', async () => {
      render(<SchedulePage />)

      // Wait for calendar to load
      await waitFor(() => {
        expect(screen.getByTestId('calendar-component')).toBeInTheDocument()
      })

      const initialDateRange = screen.getByText(/\d{1,2} \w{3}.*-.*\d{1,2} \w{3}/i)
      const initialText = initialDateRange.textContent

      const user = userEvent.setup({ delay: null })
      const nextButton = screen.getByRole('button', { name: /next week/i })
      await user.click(nextButton)

      await waitFor(() => {
        const updatedDateRange = screen.getByText(/\d{1,2} \w{3}.*-.*\d{1,2} \w{3}/i)
        expect(updatedDateRange.textContent).not.toBe(initialText)
      })
    })
  })

  describe('Filters', () => {
    it('filters by teacher when selecting from dropdown', async () => {
      const user = userEvent.setup({ delay: null })
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allTeachers')).toBeInTheDocument()
      })

      // Click teacher filter
      const comboboxButtons = screen.getAllByRole('combobox')
      const teacherSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allTeachers')
      )
      await user.click(teacherSelect!)

      // Select a teacher
      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument()
      })

      await user.click(screen.getByText('John Doe'))

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledWith(
          expect.objectContaining({
            teacherId: 'teacher-1',
          })
        )
      })
    })

    it('filters by room when selecting from dropdown', async () => {
      const user = userEvent.setup({ delay: null })
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allRooms')).toBeInTheDocument()
      })

      // Click room filter
      const comboboxButtons = screen.getAllByRole('combobox')
      const roomSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allRooms')
      )
      await user.click(roomSelect!)

      // Select a room
      await waitFor(() => {
        expect(screen.getByText('Room A')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Room A'))

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledWith(
          expect.objectContaining({
            roomId: 1,
          })
        )
      })
    })

    it('clears teacher filter when selecting "All teachers"', async () => {
      const user = userEvent.setup({ delay: null })
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allTeachers')).toBeInTheDocument()
      })

      // First select a teacher
      const comboboxButtons = screen.getAllByRole('combobox')
      const teacherSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allTeachers')
      )
      await user.click(teacherSelect!)

      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument()
      })
      await user.click(screen.getByText('John Doe'))

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledWith(
          expect.objectContaining({
            teacherId: 'teacher-1',
          })
        )
      })

      // Then clear it by selecting "All teachers" again
      const teacherSelectAgain = screen.getAllByRole('combobox').find(btn =>
        btn.textContent?.includes('John Doe')
      )
      await user.click(teacherSelectAgain!)

      await waitFor(() => {
        const allTeachersOptions = screen.getAllByText('schedule.filters.allTeachers')
        expect(allTeachersOptions.length).toBeGreaterThan(0)
      })

      const allTeachersOptions = screen.getAllByText('schedule.filters.allTeachers')
      const allTeachersOption = allTeachersOptions[allTeachersOptions.length - 1]

      await user.click(allTeachersOption)

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledWith(
          expect.objectContaining({
            teacherId: undefined,
          })
        )
      })
    })

    it('combines teacher and room filters', async () => {
      const user = userEvent.setup({ delay: null })
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allTeachers')).toBeInTheDocument()
        expect(screen.getByText('schedule.filters.allRooms')).toBeInTheDocument()
      })

      // Select teacher
      const comboboxButtons = screen.getAllByRole('combobox')
      const teacherSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allTeachers')
      )
      await user.click(teacherSelect!)
      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument()
      })
      await user.click(screen.getByText('John Doe'))

      // Select room
      const roomSelect = screen.getAllByRole('combobox').find(btn =>
        btn.textContent?.includes('schedule.filters.allRooms')
      )
      await user.click(roomSelect!)
      await waitFor(() => {
        expect(screen.getByText('Room A')).toBeInTheDocument()
      })
      await user.click(screen.getByText('Room A'))

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalledWith(
          expect.objectContaining({
            teacherId: 'teacher-1',
            roomId: 1,
          })
        )
      })
    })
  })

  describe('Legend', () => {
    it('displays status legend', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.legend.scheduled')).toBeInTheDocument()
        expect(screen.getByText('schedule.legend.completed')).toBeInTheDocument()
        expect(screen.getByText('schedule.legend.cancelled')).toBeInTheDocument()
        expect(screen.getByText('schedule.legend.noShow')).toBeInTheDocument()
      })
    })

    it('legend items have correct colors', async () => {
      render(<SchedulePage />)

      await waitFor(() => {
        const legendSection = screen.getByText('schedule.legend.scheduled').closest('div')?.parentElement

        const scheduledBox = within(legendSection!).getByText('schedule.legend.scheduled').previousElementSibling
        const completedBox = within(legendSection!).getByText('schedule.legend.completed').previousElementSibling
        const cancelledBox = within(legendSection!).getByText('schedule.legend.cancelled').previousElementSibling
        const noShowBox = within(legendSection!).getByText('schedule.legend.noShow').previousElementSibling

        expect(scheduledBox?.className).toContain('bg-blue-100')
        expect(completedBox?.className).toContain('bg-green-100')
        expect(cancelledBox?.className).toContain('bg-red-100')
        expect(noShowBox?.className).toContain('bg-orange-100')
      })
    })
  })

  describe('Refresh Functionality', () => {
    it('refreshes calendar data when clicking refresh button', async () => {
      const user = userEvent.setup({ delay: null })
      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByTestId('calendar-component')).toBeInTheDocument()
      })

      vi.clearAllMocks()

      // Find and click the refresh button
      const buttons = screen.getAllByRole('button')
      const refreshButton = buttons.find(btn => btn.querySelector('svg.lucide-refresh-cw'))
      expect(refreshButton).toBeDefined()

      await user.click(refreshButton!)

      await waitFor(() => {
        expect(calendarApi.getWeek).toHaveBeenCalled()
      })
    })
  })

  describe('Error States', () => {
    it('handles API errors gracefully', async () => {
      vi.mocked(calendarApi.getWeek).mockRejectedValue(new Error('API Error'))

      render(<SchedulePage />)

      await waitFor(() => {
        // Should still render the page structure even on error
        expect(screen.getByRole('heading', { name: 'schedule.title' })).toBeInTheDocument()
      })
    })

    it('handles empty teacher list', async () => {
      vi.mocked(teachersApi.getAll).mockResolvedValue([])

      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allTeachers')).toBeInTheDocument()
      })

      const user = userEvent.setup({ delay: null })
      const comboboxButtons = screen.getAllByRole('combobox')
      const teacherSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allTeachers')
      )
      await user.click(teacherSelect!)

      // Should only show "All teachers" option
      const allTeachersOptions = screen.getAllByText('schedule.filters.allTeachers')
      expect(allTeachersOptions.length).toBe(2) // Trigger + option
    })

    it('handles empty room list', async () => {
      vi.mocked(roomsApi.getAll).mockResolvedValue([])

      render(<SchedulePage />)

      await waitFor(() => {
        expect(screen.getByText('schedule.filters.allRooms')).toBeInTheDocument()
      })

      const user = userEvent.setup({ delay: null })
      const comboboxButtons = screen.getAllByRole('combobox')
      const roomSelect = comboboxButtons.find(btn =>
        btn.textContent?.includes('schedule.filters.allRooms')
      )
      await user.click(roomSelect!)

      // Should only show "All rooms" option
      const allRoomsOptions = screen.getAllByText('schedule.filters.allRooms')
      expect(allRoomsOptions.length).toBe(2) // Trigger + option
    })
  })
})
