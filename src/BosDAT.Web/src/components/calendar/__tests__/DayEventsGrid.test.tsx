import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import { DayEventsGrid } from '../DayEventsGrid'
import type { CalendarEvent, EventFrequency, EventType } from '../types'

const HOURS = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22]
const MIN_HOUR = 8
const MAX_HOUR = 22

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

describe('DayEventsGrid', () => {
  const selectedDate = new Date('2024-01-15T00:00:00') // Monday

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with grid role and day-specific aria-label', () => {
    render(
      <DayEventsGrid
        hours={HOURS}
        events={[]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByRole('grid', { name: /day calendar grid/i })).toBeInTheDocument()
  })

  it('renders events that match the selected date', () => {
    const event = createMockEvent({
      id: 'event-1',
      title: 'Piano Lesson',
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
    })

    render(
      <DayEventsGrid
        hours={HOURS}
        events={[event]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
  })

  it('filters out events not on the selected date', () => {
    const todayEvent = createMockEvent({
      id: 'event-today',
      title: 'Today Lesson',
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
    })

    const otherDayEvent = createMockEvent({
      id: 'event-other',
      title: 'Other Day Lesson',
      startDateTime: '2024-01-16T09:00:00',
      endDateTime: '2024-01-16T10:00:00',
    })

    render(
      <DayEventsGrid
        hours={HOURS}
        events={[todayEvent, otherDayEvent]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    expect(screen.getByText('Today Lesson')).toBeInTheDocument()
    expect(screen.queryByText('Other Day Lesson')).not.toBeInTheDocument()
  })

  it('renders events at full width in single-column layout', () => {
    const event = createMockEvent({
      id: 'event-1',
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
    })

    render(
      <DayEventsGrid
        hours={HOURS}
        events={[event]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    const eventButton = screen.getByRole('button', { name: /piano lesson/i })
    // In day view, dayIndex=0, totalDays=1, so left=0%, width=calc(100% - 8px)
    expect(eventButton.style.left).toBe('0%')
    expect(eventButton.style.width).toBe('calc(100% - 8px)')
  })

  it('renders unavailable time overlay before day start', () => {
    render(
      <DayEventsGrid
        hours={HOURS}
        events={[]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={10}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    const overlay = screen.getByLabelText('Unavailable time before working hours')
    expect(overlay).toBeInTheDocument()
    // Height should be (10 - 8) * 100 = 200px
    expect(overlay.style.height).toBe('200px')
  })

  it('renders unavailable time overlay after day end', () => {
    render(
      <DayEventsGrid
        hours={HOURS}
        events={[]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={20}
        selectedDate={selectedDate}
      />
    )

    const overlay = screen.getByLabelText('Unavailable time after working hours')
    expect(overlay).toBeInTheDocument()
  })

  it('sets correct grid height based on hours and hourHeight', () => {
    render(
      <DayEventsGrid
        hours={HOURS}
        events={[]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    const grid = screen.getByRole('grid')
    // 15 hours * 100px = 1500px
    expect(grid.style.height).toBe('1500px')
  })

  it('renders overlapping events side by side', () => {
    const event1 = createMockEvent({
      id: 'overlap-1',
      title: 'Lesson A',
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
    })

    const event2 = createMockEvent({
      id: 'overlap-2',
      title: 'Lesson B',
      startDateTime: '2024-01-15T09:30:00',
      endDateTime: '2024-01-15T10:30:00',
    })

    render(
      <DayEventsGrid
        hours={HOURS}
        events={[event1, event2]}
        hourHeight={100}
        minHour={MIN_HOUR}
        maxHour={MAX_HOUR}
        dayStartTime={9}
        dayEndTime={21}
        selectedDate={selectedDate}
      />
    )

    const buttons = screen.getAllByRole('button')
    // Both events should be rendered with half width (50% each)
    expect(buttons[0].style.width).toBe('calc(50% - 8px)')
    expect(buttons[1].style.width).toBe('calc(50% - 8px)')
  })
})
