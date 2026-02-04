// Type definitions for calendar component

import type { DayOfWeek } from '@/lib/iso-helpers'

// Re-export for consumers
export type { DayOfWeek }

// Event types for enrollment/scheduling flows
export type EventType =
  | 'course'
  | 'workshop'
  | 'trail'
  | 'holiday'
  | 'absence'
  | 'placeholder';

// Lesson status types for schedule display
export type LessonStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';

// Combined type for Event.eventType - can be either an EventType or a LessonStatus
export type EventCategory = EventType | LessonStatus;

export type EventFrequency = 'weekly' | 'bi-weekly' | 'once';

export type CalendarEvent = {
  startDateTime: string; // ISO 8601 datetime string (e.g., "2024-01-15T09:30:00")
  endDateTime: string;   // ISO 8601 datetime string (e.g., "2024-01-15T10:00:00")
  title: string;
  frequency: EventFrequency;
  eventType: EventCategory;
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

export type SchedulerProps = {
  title: string;
  events: CalendarEvent[];
  dates: Date[];
  dayStartTime?: number;
  dayEndTime?: number;
  hourHeight?: number;
  colorScheme?: ColorScheme;
  onNavigatePrevious?: () => void;
  onNavigateNext?: () => void;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  onDateSelect?: (date: Date) => void;
  highlightedDate?: Date;
  availability?: DayAvailability[];
};
