// Main component
export { default as CalendarComponent } from './CalendarComponent';

// Sub-components (exported for advanced usage/customization)
export { SchedulerHeader } from './SchedulerHeader';
export { DayHeaders } from './DayHeaders';
export { TimeColumn } from './TimeColumn';
export { EventsGrid } from './EventsGrid';
export { EventItem } from './EventItem';

// Types
export type { Event, EventColors, ColorScheme, SchedulerProps, TimeSlot } from './types';
