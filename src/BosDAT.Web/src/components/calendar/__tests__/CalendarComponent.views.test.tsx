import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CalendarComponent } from '../CalendarComponent'
import type { CalendarEvent, EventFrequency, EventType } from '../types'

const createMockDate = (dateStr: string): Date => new Date(dateStr)

const createMockEvent = (overrides?: Partial<CalendarEvent>): CalendarEvent => ({
  id: `mock-event-${Math.random().toString(36).substr(2, 9)}`,
  startDateTime: '2024-01-15T09:00:00',
  endDateTime: '2024-01-15T10:00:00',
  title: 'Piano Lesson',
  frequency: 'weekly' as EventFrequency,
  eventType: 'course' as EventType,
  status: 'Scheduled',
  attendees: ['John Doe'],
  room: 'Room 101',
  ...overrides,
})

describe('CalendarComponent - View Switching', () => {
  const mockDates = [
    createMockDate('2024-01-15T00:00:00'), // Monday
    createMockDate('2024-01-16T00:00:00'), // Tuesday
    createMockDate('2024-01-17T00:00:00'), // Wednesday
    createMockDate('2024-01-18T00:00:00'), // Thursday
    createMockDate('2024-01-19T00:00:00'), // Friday
    createMockDate('2024-01-20T00:00:00'), // Saturday
    createMockDate('2024-01-21T00:00:00'), // Sunday
  ]

  const mockOnViewChange = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('View Selector Visibility', () => {
    it('shows view selector by default when view and onViewChange are provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
          onViewChange={mockOnViewChange}
        />
      )

      expect(screen.getByRole('tablist')).toBeInTheDocument()
    })

    it('hides view selector when showViewSelector is false', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
          onViewChange={mockOnViewChange}
          showViewSelector={false}
        />
      )

      expect(screen.queryByRole('tablist')).not.toBeInTheDocument()
    })

    it('shows view selector even when onViewChange is not provided (SchedulerHeader manages its own view)', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
        />
      )

      // SchedulerHeader now manages view internally, so it shows by default
      expect(screen.getByRole('tablist')).toBeInTheDocument()
    })
  })

  describe('Week View (default)', () => {
    it('renders week view with 7 day headers', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
          onViewChange={mockOnViewChange}
        />
      )

      expect(screen.getByRole('application', { name: /weekly calendar scheduler/i })).toBeInTheDocument()
      // Check 7 day headers are rendered
      expect(screen.getByText('calendar.days.mon')).toBeInTheDocument()
      expect(screen.getByText('calendar.days.sun')).toBeInTheDocument()
    })

    it('defaults to week view when no view prop is provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      expect(screen.getByRole('application', { name: /weekly calendar scheduler/i })).toBeInTheDocument()
    })
  })

  describe('Day View', () => {
    it('renders day view with single day header', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="day"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      expect(screen.getByRole('application', { name: /daily calendar scheduler/i })).toBeInTheDocument()
      // Day view should show the day grid
      expect(screen.getByRole('grid', { name: /day calendar grid/i })).toBeInTheDocument()
    })

    it('shows events only for the selected day', () => {
      const mondayEvent = createMockEvent({
        id: 'monday-event',
        title: 'Monday Lesson',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
      })

      const tuesdayEvent = createMockEvent({
        id: 'tuesday-event',
        title: 'Tuesday Lesson',
        startDateTime: '2024-01-16T09:00:00',
        endDateTime: '2024-01-16T10:00:00',
      })

      render(
        <CalendarComponent
          events={[mondayEvent, tuesdayEvent]}
          dates={mockDates}
          initialView="day"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      expect(screen.getByText('Monday Lesson')).toBeInTheDocument()
      expect(screen.queryByText('Tuesday Lesson')).not.toBeInTheDocument()
    })

    it('renders the selected date number in the day header', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="day"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]} // January 15
        />
      )

      expect(screen.getByText('15')).toBeInTheDocument()
    })
  })

  describe('List View', () => {
    it('renders list view with day header', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      expect(screen.getByRole('application', { name: /calendar list view/i })).toBeInTheDocument()
    })

    it('shows empty state when no events for the day', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      expect(screen.getByText('calendar.list.noEvents')).toBeInTheDocument()
    })

    it('shows events as list items for the selected day', () => {
      const event = createMockEvent({
        id: 'list-event',
        title: 'Piano Lesson',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
      expect(screen.getByRole('list')).toBeInTheDocument()
    })

    it('renders action buttons for scheduled course events', () => {
      const event = createMockEvent({
        id: 'action-event',
        status: 'Scheduled',
        eventType: 'course',
      })

      const mockOnEventAction = vi.fn()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
          onEventAction={mockOnEventAction}
        />
      )

      expect(screen.getByLabelText('lessons.moveLesson')).toBeInTheDocument()
      expect(screen.getByLabelText('lessons.cancelLesson')).toBeInTheDocument()
    })

    it('calls onEventAction when action buttons are clicked', async () => {
      const user = userEvent.setup()
      const event = createMockEvent({
        id: 'action-event',
        status: 'Scheduled',
        eventType: 'course',
      })

      const mockOnEventAction = vi.fn()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
          onEventAction={mockOnEventAction}
        />
      )

      await user.click(screen.getByLabelText('lessons.cancelLesson'))
      expect(mockOnEventAction).toHaveBeenCalledWith(event, 'cancel')
    })
  })

  describe('View Switching', () => {
    it('calls onViewChange when clicking day tab', async () => {
      const user = userEvent.setup()

      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
          onViewChange={mockOnViewChange}
        />
      )

      await user.click(screen.getByRole('tab', { name: /calendar.views.day/i }))
      expect(mockOnViewChange).toHaveBeenCalledWith('day')
    })

    it('calls onViewChange when clicking list tab', async () => {
      const user = userEvent.setup()

      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
          onViewChange={mockOnViewChange}
        />
      )

      await user.click(screen.getByRole('tab', { name: /calendar.views.list/i }))
      expect(mockOnViewChange).toHaveBeenCalledWith('list')
    })

    it('calls onViewChange when clicking week tab from day view', async () => {
      const user = userEvent.setup()

      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="day"
          onViewChange={mockOnViewChange}
          selectedDate={mockDates[0]}
        />
      )

      await user.click(screen.getByRole('tab', { name: /calendar.views.week/i }))
      expect(mockOnViewChange).toHaveBeenCalledWith('week')
    })
  })

  describe('Selected Date Fallback', () => {
    it('uses highlightedDate when selectedDate is not provided', () => {
      const event = createMockEvent({
        id: 'e1',
        title: 'Highlighted Day Event',
        startDateTime: '2024-01-16T09:00:00',
        endDateTime: '2024-01-16T10:00:00',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
          highlightedDate={mockDates[1]} // Tuesday
        />
      )

      expect(screen.getByText('Highlighted Day Event')).toBeInTheDocument()
    })

    it('uses first date in dates array as last fallback', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="list"
          onViewChange={mockOnViewChange}
        />
      )

      // Should render without crashing and show a day header
      expect(screen.getByRole('application')).toBeInTheDocument()
    })
  })
})
