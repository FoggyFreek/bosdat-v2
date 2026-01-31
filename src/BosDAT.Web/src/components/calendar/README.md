# Calendar Component

A reusable weekly calendar scheduler component for displaying and managing events.

## Usage

The calendar component is now available globally and can be imported from multiple pages:

### Basic Import

```tsx
// Option 1: Import from the calendar module directly
import { CalendarComponent } from '@/components/calendar'
import type { Event, ColorScheme, TimeSlot } from '@/components/calendar'

// Option 2: Import from the main components barrel
import { CalendarComponent } from '@/components'
import type { Event, ColorScheme, TimeSlot } from '@/components'
```

### Example Usage

```tsx
import { CalendarComponent } from '@/components'
import type { Event, ColorScheme } from '@/components'

const MyPage = () => {
  const events: Event[] = [
    {
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
      title: 'Piano Lesson',
      frequency: 'weekly',
      eventType: 'course',
      attendees: ['John Doe'],
      room: 'Room 101',
    },
  ]

  const colorScheme: ColorScheme = {
    course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
    workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
    trail: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
  }

  const handleTimeslotClick = (timeSlot: TimeSlot) => {
    console.log('Timeslot clicked:', timeSlot)
  }

  return (
    <CalendarComponent
      title="Weekly Schedule"
      events={events}
      dates={weekDates}
      colorScheme={colorScheme}
      onTimeslotClick={handleTimeslotClick}
      onNavigatePrevious={() => console.log('Previous week')}
      onNavigateNext={() => console.log('Next week')}
    />
  )
}
```

## Props

### Required Props

- `title: string` - Calendar header title
- `events: Event[]` - Array of events to display
- `dates: Date[]` - Array of 7 dates representing the week (Monday-Sunday)

### Optional Props

- `daystartTime?: number` - Working day start hour (default: 9)
- `dayendTime?: number` - Working day end hour (default: 21)
- `hourHeight?: number` - Height in pixels for each hour slot (default: 100)
- `colorScheme?: ColorScheme` - Custom color scheme for event types
- `onNavigatePrevious?: () => void` - Callback for previous week navigation
- `onNavigateNext?: () => void` - Callback for next week navigation
- `onTimeslotClick?: (timeslot: TimeSlot) => void` - Callback when clicking on a timeslot
- `onDateSelect?: (date: Date) => void` - Callback when selecting a date header
- `highlightedDate?: Date` - Date to highlight in the calendar

## Types

### Event
```tsx
{
  startDateTime: string       // ISO 8601 format
  endDateTime: string         // ISO 8601 format
  title: string
  frequency: string           // e.g., 'weekly', 'daily'
  eventType: string          // e.g., 'course', 'workshop', 'trail'
  attendees?: string[]
  room?: string
}
```

### TimeSlot
```tsx
{
  date: Date
  startTime: string    // Format: 'HH:mm'
  endTime: string      // Format: 'HH:mm'
}
```

### ColorScheme
```tsx
{
  [eventType: string]: {
    background: string      // Hex color for event background
    border: string          // Hex color for event border
    textBackground: string  // Hex color for text background
  }
}
```

## Features

- ✅ Weekly calendar view with time slots
- ✅ Custom color schemes for different event types
- ✅ Event hover tooltips with attendees and room info
- ✅ Clickable timeslots for creating new events
- ✅ Date selection for navigation
- ✅ Working hours overlay (grays out non-working hours)
- ✅ Fully keyboard accessible
- ✅ Responsive design
- ✅ ARIA labels and roles for screen readers

## Utilities

The calendar module also exports utility functions and sub-components for advanced usage:

```tsx
import {
  SchedulerHeader,
  DayHeaders,
  TimeColumn,
  EventsGrid,
  EventItem,
} from '@/components/calendar'
```

## Helper Functions

For common calendar operations, use the utilities from `@/lib/calendar-utils`:

```tsx
import {
  getWeekStart,
  getWeekDays,
  formatDateForApi,
  calculateEndTime,
} from '@/lib/calendar-utils'

// Get the start of the current week
const weekStart = getWeekStart(new Date())

// Get all 7 days of the week
const weekDays = getWeekDays(weekStart)
```
