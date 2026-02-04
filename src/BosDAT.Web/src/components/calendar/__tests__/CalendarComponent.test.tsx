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
  attendees: ['John Doe', 'Jane Smith'],
  room: 'Room 101',
  ...overrides,
})

const mockColorScheme: ColorScheme = {
  course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
  workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
  trail: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
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

  const mockOnNavigatePrevious = vi.fn()
  const mockOnNavigateNext = vi.fn()
  const mockOnTimeslotClick = vi.fn()
  const mockOnDateSelect = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders the scheduler with correct title', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
        />
      )

      expect(screen.getByText('Weekly Schedule')).toBeInTheDocument()
    })

    it('renders with application role and aria-label', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
        />
      )

      const scheduler = screen.getByRole('application', { name: /weekly calendar scheduler/i })
      expect(scheduler).toBeInTheDocument()
    })

    it('renders 7 day columns', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
        />
      )

      // Check for day labels (MON, TUE, WED, THU, FRI, SAT, SUN)
      const dayLabels = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN']
      dayLabels.forEach(label => {
        expect(screen.getByText(label)).toBeInTheDocument()
      })
    })

    it('renders correct day numbers', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
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
    it('renders time column with hours 6-22', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
        />
      )

      // Check that hours 6-22 are displayed
      const expectedHours = [6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22]
      expectedHours.forEach(hour => {
        const hourLabel = hour < 10 ? `0${hour}:00` : `${hour}:00`
        expect(screen.getByText(hourLabel)).toBeInTheDocument()
      })
    })

    it('renders grid with role', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
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
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          onNavigatePrevious={mockOnNavigatePrevious}
        />
      )

      const prevButton = screen.getByRole('button', { name: /previous week/i })
      expect(prevButton).toBeInTheDocument()
    })

    it('renders next navigation button', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          onNavigateNext={mockOnNavigateNext}
        />
      )

      const nextButton = screen.getByRole('button', { name: /next week/i })
      expect(nextButton).toBeInTheDocument()
    })

    it('calls onNavigatePrevious when previous button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          onNavigatePrevious={mockOnNavigatePrevious}
        />
      )

      const prevButton = screen.getByRole('button', { name: /previous week/i })
      await user.click(prevButton)

      expect(mockOnNavigatePrevious).toHaveBeenCalledTimes(1)
    })

    it('calls onNavigateNext when next button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          onNavigateNext={mockOnNavigateNext}
        />
      )

      const nextButton = screen.getByRole('button', { name: /next week/i })
      await user.click(nextButton)

      expect(mockOnNavigateNext).toHaveBeenCalledTimes(1)
    })
  })

  describe('Date Selection', () => {
    it('renders date headers as buttons when onDateSelect is provided', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          onDateSelect={mockOnDateSelect}
        />
      )

      const dayButtons = screen.getAllByRole('button').filter(
        btn => btn.getAttribute('aria-label')?.includes('MON') ||
               btn.getAttribute('aria-label')?.includes('TUE') ||
               btn.getAttribute('aria-label')?.includes('WED')
      )
      expect(dayButtons.length).toBeGreaterThan(0)
    })

    it('calls onDateSelect when a date is clicked', async () => {
      const user = userEvent.setup()
      render(
        <CalendarComponent
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
      })

      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[event]}
          dates={mockDates}
          colorScheme={mockColorScheme}
        />
      )

      expect(screen.getByText('course')).toBeInTheDocument()
      expect(screen.getByText('weekly')).toBeInTheDocument()
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
          events={[event]}
          dates={mockDates}
        />
      )

      const eventElement = screen.getByText('Piano Lesson')
      await user.hover(eventElement)

      await waitFor(
        () => {
          expect(screen.getByText('Attendees')).toBeInTheDocument()
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
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
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
          hourHeight={120}
        />
      )

      const grid = screen.getByRole('grid')
      // Grid height should be 17 hours (6-22) * 120px = 2040px
      expect(grid).toHaveStyle({ height: '2040px' })
    })

    it('renders with default hourHeight when not provided', () => {
      render(
        <CalendarComponent
          title="Weekly Schedule"
          events={[]}
          dates={mockDates}
        />
      )

      const grid = screen.getByRole('grid')
      // Grid height should be 17 hours (6-22) * 100px (default) = 1700px
      expect(grid).toHaveStyle({ height: '1700px' })
    })
  })
})
