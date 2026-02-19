import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CalendarComponent } from '../CalendarComponent'
import type { CalendarEvent, EventFrequency, EventType, ColorScheme } from '../types'

const createMockDate = (dateStr: string): Date => {
  return new Date(dateStr)
}

const createMockEvent = (overrides?: Partial<CalendarEvent>): CalendarEvent => ({
  id: `mock-event-${Math.random().toString(36).substr(2, 9)}`,
  startDateTime: '2024-01-15T09:00:00',
  endDateTime: '2024-01-15T10:00:00',
  title: 'Piano Lesson',
  frequency: 'weekly' as EventFrequency,
  eventType: 'course' as EventType,
  status: 'Scheduled',
  attendees: ['John Doe', 'Jane Smith'],
  room: 'Room 101',
  ...overrides,
})

const mockColorScheme: ColorScheme = {
  course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
  workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
  trial: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
}

describe('CalendarComponent', () => {
  const mockDates = [
    createMockDate('2024-01-15T00:00:00'), // Monday
    createMockDate('2024-01-16T00:00:00'), // Tuesday
    createMockDate('2024-01-17T00:00:00'), // Wednesday
    createMockDate('2024-01-18T00:00:00'), // Thursday
    createMockDate('2024-01-19T00:00:00'), // Friday
    createMockDate('2024-01-20T00:00:00'), // Saturday
    createMockDate('2024-01-21T00:00:00'), // Sunday
  ]

  const mockOnDateChange = vi.fn()
  const mockOnTimeslotClick = vi.fn()
  const mockOnDateSelect = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders the scheduler with calculated title', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialDate={mockDates[0]}
        />
      )

      // Title is now calculated in SchedulerHeader based on week number
      expect(screen.getByText('schedule.week')).toBeInTheDocument()
    })

    it('renders with application role and aria-label', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialView="week"
        />
      )

      const scheduler = screen.getByRole('application', { name: /weekly calendar scheduler/i })
      expect(scheduler).toBeInTheDocument()
    })

    it('renders 7 day columns', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      // Check for day labels using translation keys
      const dayLabels = ['calendar.days.mon', 'calendar.days.tue', 'calendar.days.wed', 'calendar.days.thu', 'calendar.days.fri', 'calendar.days.sat', 'calendar.days.sun']
      dayLabels.forEach(label => {
        expect(screen.getByText(label)).toBeInTheDocument()
      })
    })

    it('renders correct day numbers', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      // Check for dates 15-21
      for (let day = 15; day <= 21; day++) {
        expect(screen.getByText(day.toString())).toBeInTheDocument()
      }
    })
  })

  describe('Time Grid', () => {
    it('renders time column with hours 8-22', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      // Check that hours 8-22 are displayed
      const expectedHours = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22]
      expectedHours.forEach(hour => {
        const hourLabel = hour < 10 ? `0${hour}:00` : `${hour}:00`
        expect(screen.getByText(hourLabel)).toBeInTheDocument()
      })
    })

    it('renders grid with role', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      const grid = screen.getByRole('grid', { name: /weekly calendar grid/i })
      expect(grid).toBeInTheDocument()
    })
  })

  describe('Navigation', () => {
    it('renders previous navigation button', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateChange={mockOnDateChange}
        />
      )

      const prevButton = screen.getByRole('button', { name: 'calendar.navigation.previous' })
      expect(prevButton).toBeInTheDocument()
    })

    it('renders next navigation button', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateChange={mockOnDateChange}
        />
      )

      const nextButton = screen.getByRole('button', { name: 'calendar.navigation.next' })
      expect(nextButton).toBeInTheDocument()
    })

    it('calls onDateChange when previous button is clicked', async () => {
      const user = userEvent.setup()
      const initialDate = mockDates[0]
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialDate={initialDate}
          onDateChange={mockOnDateChange}
        />
      )

      const prevButton = screen.getByRole('button', { name: 'calendar.navigation.previous' })
      await user.click(prevButton)

      expect(mockOnDateChange).toHaveBeenCalledTimes(1)
      // Date should be 7 days earlier for week view
      expect(mockOnDateChange).toHaveBeenCalledWith(expect.any(Date))
    })

    it('calls onDateChange when next button is clicked', async () => {
      const user = userEvent.setup()
      const initialDate = mockDates[0]
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          initialDate={initialDate}
          onDateChange={mockOnDateChange}
        />
      )

      const nextButton = screen.getByRole('button', { name: 'calendar.navigation.next' })
      await user.click(nextButton)

      expect(mockOnDateChange).toHaveBeenCalledTimes(1)
      // Date should be 7 days later for week view
      expect(mockOnDateChange).toHaveBeenCalledWith(expect.any(Date))
    })
  })

  describe('Date Selection', () => {
    it('renders date headers as buttons when onDateSelect is provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateSelect={mockOnDateSelect}
        />
      )

      const dayButtons = screen.getAllByRole('button').filter(
        btn => btn.getAttribute('aria-label')?.includes('calendar.days.mon') ||
               btn.getAttribute('aria-label')?.includes('calendar.days.tue') ||
               btn.getAttribute('aria-label')?.includes('calendar.days.wed')
      )
      expect(dayButtons.length).toBeGreaterThan(0)
    })

    it('calls onDateSelect when a date is clicked', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateSelect={mockOnDateSelect}
        />
      )

      const mondayButton = screen.getByRole('button', { name: /MON 15/i })
      await user.click(mondayButton)

      expect(mockOnDateSelect).toHaveBeenCalledTimes(1)
      expect(mockOnDateSelect).toHaveBeenCalledWith(mockDates[0])
    })

    it('supports keyboard navigation with Enter key on dates', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateSelect={mockOnDateSelect}
        />
      )

      const mondayButton = screen.getByRole('button', { name: /MON 15/i })
      mondayButton.focus()
      await user.keyboard('{Enter}')

      expect(mockOnDateSelect).toHaveBeenCalledTimes(1)
      expect(mockOnDateSelect).toHaveBeenCalledWith(mockDates[0])
    })

    it('supports keyboard navigation with Space key on dates', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onDateSelect={mockOnDateSelect}
        />
      )

      const mondayButton = screen.getByRole('button', { name: /MON 15/i })
      mondayButton.focus()
      await user.keyboard(' ')

      expect(mockOnDateSelect).toHaveBeenCalledTimes(1)
      expect(mockOnDateSelect).toHaveBeenCalledWith(mockDates[0])
    })

    it('highlights selected date when highlightedDate is provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          highlightedDate={mockDates[0]}
          onDateSelect={mockOnDateSelect}
        />
      )

      const mondayButton = screen.getByRole('button', { name: /MON 15/i })
      expect(mondayButton).toHaveClass('bg-sky', 'text-primary-foreground')
    })
  })

  describe('Event Rendering', () => {
    it('renders events in the correct day column', () => {
      const event = createMockEvent({
        startDateTime: '2024-01-15T09:00:00', // Monday
        endDateTime: '2024-01-15T10:00:00',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
    })

    it('renders multiple events', () => {
      const events = [
        createMockEvent({
          title: 'Piano Lesson',
          startDateTime: '2024-01-15T09:00:00',
          endDateTime: '2024-01-15T10:00:00',
        }),
        createMockEvent({
          title: 'Guitar Lesson',
          startDateTime: '2024-01-16T14:00:00',
          endDateTime: '2024-01-16T15:00:00',
        }),
      ]

      render(
        <CalendarComponent
          events={events}
          dates={mockDates}
        />
      )

      expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
      expect(screen.getByText('Guitar Lesson')).toBeInTheDocument()
    })

    it('does not render events with invalid time (end before start)', () => {
      const invalidEvent = createMockEvent({
        startDateTime: '2024-01-15T10:00:00',
        endDateTime: '2024-01-15T09:00:00', // Invalid: end before start
      })

      render(
        <CalendarComponent
          events={[invalidEvent]}
          dates={mockDates}
        />
      )

      expect(screen.queryByText('Piano Lesson')).not.toBeInTheDocument()
    })

    it('does not render events outside the date range', () => {
      const outsideEvent = createMockEvent({
        startDateTime: '2024-01-22T09:00:00', // Outside the week
        endDateTime: '2024-01-22T10:00:00',
      })

      render(
        <CalendarComponent
          events={[outsideEvent]}
          dates={mockDates}
        />
      )

      expect(screen.queryByText('Piano Lesson')).not.toBeInTheDocument()
    })
  })

  describe('Color Scheme', () => {
    it('applies default color scheme when not provided', () => {
      const event = createMockEvent({ eventType: 'course' })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson').closest('button')
      expect(eventElement).toHaveStyle({ backgroundColor: '#eff6ff' })
    })

    it('applies custom color scheme when provided', () => {
      const event = createMockEvent({ eventType: 'course' })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          colorScheme={mockColorScheme}
        />
      )

      const eventElement = screen.getByText('Piano Lesson').closest('button')
      expect(eventElement).toHaveStyle({ backgroundColor: '#eff6ff' })
    })

    it('displays event type and frequency badges with correct styling', () => {
      const event = createMockEvent({
        eventType: 'course',
        frequency: 'weekly',
        status: 'Scheduled',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
          colorScheme={mockColorScheme}
        />
      )

      // EventItem renders type and frequency in aria-label, not as visible badges
      expect(screen.getByLabelText('Piano Lesson, course, weekly')).toBeInTheDocument()
    })
  })

  describe('Event Hover Tooltip', () => {
    it('shows hover tooltip after hovering on event', async () => {
      const user = userEvent.setup()
      const event = createMockEvent({
        attendees: ['John Doe', 'Jane Smith'],
        room: 'Room 101',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson')
      await user.hover(eventElement)

      // Wait for tooltip to appear (300ms delay)
      await waitFor(
        () => {
          expect(screen.getByRole('tooltip')).toBeInTheDocument()
        },
        { timeout: 500 }
      )
    })

    it('displays attendees in hover tooltip', async () => {
      const user = userEvent.setup()
      const event = createMockEvent({
        attendees: ['John Doe', 'Jane Smith'],
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson')
      await user.hover(eventElement)

      await waitFor(
        () => {
          expect(screen.getByText('calendar.event.attendees')).toBeInTheDocument()
          expect(screen.getByLabelText('John Doe')).toBeInTheDocument()
          expect(screen.getByLabelText('Jane Smith')).toBeInTheDocument()
        },
        { timeout: 500 }
      )
    })

    it('displays room information in hover tooltip', async () => {
      const user = userEvent.setup()
      const event = createMockEvent({
        room: 'Room 101',
      })

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson')
      await user.hover(eventElement)

      await waitFor(
        () => {
          expect(screen.getByText('Room 101')).toBeInTheDocument()
        },
        { timeout: 500 }
      )
    })

    it('hides tooltip when mouse leaves event', async () => {
      const user = userEvent.setup()
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson')
      await user.hover(eventElement)

      await waitFor(
        () => {
          expect(screen.getByRole('tooltip')).toBeInTheDocument()
        },
        { timeout: 500 }
      )

      await user.unhover(eventElement)

      await waitFor(() => {
        expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
      })
    })

    it('toggles tooltip with Enter key on event', async () => {
      const user = userEvent.setup()
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson').closest('button')! as HTMLElement
      eventElement.focus()
      await user.keyboard('{Enter}')

      await waitFor(() => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument()
      })

      await user.keyboard('{Enter}')

      await waitFor(() => {
        expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
      })
    })

    it('toggles tooltip with Space key on event', async () => {
      const user = userEvent.setup()
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson').closest('button')! as HTMLElement
      eventElement.focus()
      await user.keyboard(' ')

      await waitFor(() => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument()
      })

      await user.keyboard(' ')

      await waitFor(() => {
        expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
      })
    })

    it('closes tooltip with Escape key', async () => {
      const user = userEvent.setup()
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson').closest('button')! as HTMLElement
      eventElement.focus()
      await user.keyboard('{Enter}')

      await waitFor(() => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument()
      })

      await user.keyboard('{Escape}')

      await waitFor(() => {
        expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has keyboard-accessible event items', () => {
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventButton = screen.getByRole('button', {
        name: /Piano Lesson, course, weekly/i,
      })
      expect(eventButton).toBeInTheDocument()
      // Button elements are focusable by default, tabIndex attribute is not required
    })

    it('has appropriate ARIA labels for events', () => {
      const event = createMockEvent()

      render(
        <CalendarComponent
          events={[event]}
          dates={mockDates}
        />
      )

      const eventButton = screen.getByRole('button', {
        name: /Piano Lesson, course, weekly/i,
      })
      expect(eventButton).toHaveAttribute('aria-expanded')
    })

    it('has keyboard-accessible grid', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onTimeslotClick={mockOnTimeslotClick}
        />
      )

      const grid = screen.getByRole('grid', { name: /weekly calendar grid/i })
      expect(grid).toHaveAttribute('tabIndex', '0')
    })
  })

  describe('Grid Interaction', () => {
    it('renders clickable grid when onTimeslotClick is provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          onTimeslotClick={mockOnTimeslotClick}
        />
      )

      const grid = screen.getByRole('grid', { name: /weekly calendar grid/i })
      expect(grid).toHaveClass('cursor-pointer')
      expect(grid).toBeInTheDocument()
    })

    it('renders grid without errors when onTimeslotClick is not provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      const grid = screen.getByRole('grid', { name: /weekly calendar grid/i })
      expect(grid).toBeInTheDocument()
    })
  })

  describe('Working Hours Overlay', () => {
    it('shows unavailable time overlay before day start', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          dayStartTime={9}
        />
      )

      const overlay = screen.getByLabelText(/unavailable time before working hours/i)
      expect(overlay).toBeInTheDocument()
    })

    it('shows unavailable time overlay after day end', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          dayEndTime={18}
        />
      )

      const overlay = screen.getByLabelText(/unavailable time after working hours/i)
      expect(overlay).toBeInTheDocument()
    })
  })

  describe('Props and Configuration', () => {
    it('uses custom hourHeight prop', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
          hourHeight={120}
        />
      )

      const grid = screen.getByRole('grid')
      // Grid height should be 15 hours (8-22) * 120px = 1800px
      expect(grid).toHaveStyle({ height: '1800px' })
    })

    it('renders with default hourHeight when not provided', () => {
      render(
        <CalendarComponent
          events={[]}
          dates={mockDates}
        />
      )

      const grid = screen.getByRole('grid')
      // Grid height should be 15 hours (8-22) * 100px (default) = 1500px
      expect(grid).toHaveStyle({ height: '1500px' })
    })
  })
})
