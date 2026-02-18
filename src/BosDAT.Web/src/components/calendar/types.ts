// Type definitions for calendar component

import type { DayOfWeek } from '@/lib/datetime-helpers'

// Re-export for consumers
export type { DayOfWeek }

// Event types for enrollment/scheduling flows
export type EventType =
  | 'course'
  | 'workshop'
  | 'trial'
  | 'holiday'
  | 'absence'
  | 'placeholder';

// Lesson status types for schedule display
export type LessonStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';

// Combined type for Event.eventType - can be either an EventType or a LessonStatus
export type EventCategory = EventType | LessonStatus;

export type EventFrequency = 'weekly' | 'bi-weekly' | 'once';

export type CalendarEvent = {
  id: string;
  startDateTime: string; // ISO 8601 datetime string (e.g., "2024-01-15T09:30:00")
  endDateTime: string;   // ISO 8601 datetime string (e.g., "2024-01-15T10:00:00")
  title: string;
  frequency: EventFrequency;
  eventType: EventType;
  status: LessonStatus;
  attendees: string[]; // persons attending the event
  room?: string;
};

export type EventColors = {
  background: string;
  border: string;
  textBackground: string;
};

export type ColorScheme = Partial<Record<EventCategory, EventColors>>;

export type TimeSlot = {
  date: Date;
  hour: number;
  minute: number;
};

export type DayAvailability = {
  dayOfWeek: DayOfWeek;  // "Sunday", "Monday", etc.
  fromTime: number;   // hour (9 for 09:00)
  untilTime: number;  // hour (22 for 22:00)
};

export type CalendarView = 'week' | 'day' | 'list';

export const calendarViewTranslations = {
  week: 'calendar.views.week',
  day: 'calendar.views.day',
  list: 'calendar.views.list',
} as const satisfies Record<CalendarView, string>;

export type CalendarListAction = 'cancel' | 'move';

export type SchedulerProps = {
  events: CalendarEvent[];
  dates: Date[];
  initialDate?: Date;
  initialView?: CalendarView;
  dayStartTime?: number;
  dayEndTime?: number;
  hourHeight?: number;
  colorScheme?: ColorScheme;
  onDateChange?: (date: Date) => void;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  onDateSelect?: (date: Date) => void;
  highlightedDate?: Date;
  availability?: DayAvailability[];
  onViewChange?: (view: CalendarView) => void;
  showViewSelector?: boolean;
  selectedDate?: Date;
  onEventAction?: (event: CalendarEvent, action: CalendarListAction) => void;
  onEventClick?: (event: CalendarEvent) => void;
};
