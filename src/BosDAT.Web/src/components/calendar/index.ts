// Main component
export { CalendarComponent } from './CalendarComponent';

// Sub-components (exported for advanced usage/customization)
export { SchedulerHeader } from './SchedulerHeader';
export { DayHeaders } from './DayHeaders';
export { TimeColumn } from './TimeColumn';
export { EventsGrid } from './EventsGrid';
export { EventItem } from './EventItem';

// Types
export type {
  Event,
  EventType,
  EventFrequency,
  EventColors,
  ColorScheme,
  SchedulerProps,
  TimeSlot,
  LessonStatus,
  EventCategory,
} from './types';
