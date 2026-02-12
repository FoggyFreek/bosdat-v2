// Main component
export { CalendarComponent } from './CalendarComponent';

// Sub-components (exported for advanced usage/customization)
export { SchedulerHeader } from './SchedulerHeader';
export { DayHeaders } from './DayHeaders';
export { TimeColumn } from './TimeColumn';
export { EventsGrid } from './EventsGrid';
export { EventItem } from './EventItem';
export { ViewSelector } from './ViewSelector';
export { DayEventsGrid } from './DayEventsGrid';
export { CalendarListView } from './CalendarListView';
export { CalendarListItem } from './CalendarListItem';

// Types
export type {
  CalendarEvent,
  EventType,
  EventFrequency,
  EventColors,
  ColorScheme,
  SchedulerProps,
  TimeSlot,
  LessonStatus,
  EventCategory,
  DayAvailability,
  CalendarView,
  CalendarListAction,
} from './types';
