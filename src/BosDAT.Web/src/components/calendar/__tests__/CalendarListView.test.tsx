import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CalendarListView } from '../CalendarListView'
import type { CalendarEvent, EventFrequency, EventType, ColorScheme, CalendarListAction } from '../types'

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

describe('CalendarListView', () => {
  const selectedDate = new Date('2024-01-15T00:00:00')
  const mockOnEventAction = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders a list of events for the selected day', () => {
    const events = [
      createMockEvent({ id: 'e1', title: 'Piano Lesson', startDateTime: '2024-01-15T09:00:00', endDateTime: '2024-01-15T10:00:00' }),
      createMockEvent({ id: 'e2', title: 'Guitar Class', startDateTime: '2024-01-15T11:00:00', endDateTime: '2024-01-15T12:00:00' }),
    ]

    render(
      <CalendarListView
        events={events}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
    expect(screen.getByText('Guitar Class')).toBeInTheDocument()
  })

  it('filters out events from other days', () => {
    const events = [
      createMockEvent({ id: 'e1', title: 'Monday Lesson', startDateTime: '2024-01-15T09:00:00', endDateTime: '2024-01-15T10:00:00' }),
      createMockEvent({ id: 'e2', title: 'Tuesday Lesson', startDateTime: '2024-01-16T09:00:00', endDateTime: '2024-01-16T10:00:00' }),
    ]

    render(
      <CalendarListView
        events={events}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('Monday Lesson')).toBeInTheDocument()
    expect(screen.queryByText('Tuesday Lesson')).not.toBeInTheDocument()
  })

  it('shows empty state when no events match the selected date', () => {
    render(
      <CalendarListView
        events={[]}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('calendar.list.noEvents')).toBeInTheDocument()
  })

  it('sorts events by start time', () => {
    const events = [
      createMockEvent({ id: 'e1', title: 'Late Lesson', startDateTime: '2024-01-15T14:00:00', endDateTime: '2024-01-15T15:00:00' }),
      createMockEvent({ id: 'e2', title: 'Early Lesson', startDateTime: '2024-01-15T09:00:00', endDateTime: '2024-01-15T10:00:00' }),
      createMockEvent({ id: 'e3', title: 'Mid Lesson', startDateTime: '2024-01-15T11:00:00', endDateTime: '2024-01-15T12:00:00' }),
    ]

    render(
      <CalendarListView
        events={events}
        selectedDate={selectedDate}
      />
    )

    const listItems = screen.getAllByRole('listitem')
    expect(listItems).toHaveLength(3)
    expect(listItems[0]).toHaveTextContent('Early Lesson')
    expect(listItems[1]).toHaveTextContent('Mid Lesson')
    expect(listItems[2]).toHaveTextContent('Late Lesson')
  })

  it('renders the list role with correct aria-label', () => {
    const events = [
      createMockEvent({ id: 'e1' }),
    ]

    render(
      <CalendarListView
        events={events}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByRole('list', { name: /calendar.list.label/i })).toBeInTheDocument()
  })

  it('renders event details including time, room, and attendees', () => {
    const event = createMockEvent({
      id: 'e1',
      title: 'Piano Lesson',
      room: 'Room 101',
      attendees: ['Alice', 'Bob'],
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
    expect(screen.getByText('Room 101')).toBeInTheDocument()
    // Attendee initials
    expect(screen.getByLabelText('Alice')).toBeInTheDocument()
    expect(screen.getByLabelText('Bob')).toBeInTheDocument()
  })

  it('renders move and cancel action buttons for scheduled events', () => {
    const event = createMockEvent({
      id: 'e1',
      status: 'Scheduled',
      eventType: 'course',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
        onEventAction={mockOnEventAction}
      />
    )

    expect(screen.getByLabelText('lessons.moveLesson')).toBeInTheDocument()
    expect(screen.getByLabelText('lessons.cancelLesson')).toBeInTheDocument()
  })

  it('calls onEventAction with move action when move button is clicked', async () => {
    const user = userEvent.setup()
    const event = createMockEvent({
      id: 'e1',
      status: 'Scheduled',
      eventType: 'course',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
        onEventAction={mockOnEventAction}
      />
    )

    await user.click(screen.getByLabelText('lessons.moveLesson'))

    expect(mockOnEventAction).toHaveBeenCalledTimes(1)
    expect(mockOnEventAction).toHaveBeenCalledWith(event, 'move')
  })

  it('calls onEventAction with cancel action when cancel button is clicked', async () => {
    const user = userEvent.setup()
    const event = createMockEvent({
      id: 'e1',
      status: 'Scheduled',
      eventType: 'course',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
        onEventAction={mockOnEventAction}
      />
    )

    await user.click(screen.getByLabelText('lessons.cancelLesson'))

    expect(mockOnEventAction).toHaveBeenCalledTimes(1)
    expect(mockOnEventAction).toHaveBeenCalledWith(event, 'cancel')
  })

  it('does not render action buttons for non-scheduled events', () => {
    const event = createMockEvent({
      id: 'e1',
      status: 'Completed',
      eventType: 'course',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
        onEventAction={mockOnEventAction}
      />
    )

    expect(screen.queryByLabelText('lessons.moveLesson')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('lessons.cancelLesson')).not.toBeInTheDocument()
  })

  it('does not render action buttons for holiday events', () => {
    const event = createMockEvent({
      id: 'e1',
      status: 'Scheduled',
      eventType: 'holiday',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
        onEventAction={mockOnEventAction}
      />
    )

    expect(screen.queryByLabelText('lessons.moveLesson')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('lessons.cancelLesson')).not.toBeInTheDocument()
  })

  it('does not render action buttons when onEventAction is not provided', () => {
    const event = createMockEvent({
      id: 'e1',
      status: 'Scheduled',
      eventType: 'course',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
      />
    )

    expect(screen.queryByLabelText('lessons.moveLesson')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('lessons.cancelLesson')).not.toBeInTheDocument()
  })

  it('deduplicates events with the same id', () => {
    const event = createMockEvent({
      id: 'duplicate-id',
      title: 'Duplicate Lesson',
    })

    render(
      <CalendarListView
        events={[event, { ...event }]}
        selectedDate={selectedDate}
      />
    )

    const items = screen.getAllByText('Duplicate Lesson')
    expect(items).toHaveLength(1)
  })

  it('renders event type and frequency badges', () => {
    const event = createMockEvent({
      id: 'e1',
      eventType: 'course',
      status: 'Scheduled',
      frequency: 'weekly',
    })

    render(
      <CalendarListView
        events={[event]}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('course/Scheduled')).toBeInTheDocument()
    expect(screen.getByText('weekly')).toBeInTheDocument()
  })
})
