// Type definitions for calendar component
export type Event = {
  startDateTime: string; // ISO 8601 datetime string (e.g., "2024-01-15T09:30:00")
  endDateTime: string;   // ISO 8601 datetime string (e.g., "2024-01-15T10:00:00")
  title: string;
  frequency: string; // weekly or bi-weekly
  eventType: string; // trail, course, workshop, absence, holiday
  attendees: string[]; // persons attending the event
  room?: string;
};

export type EventColors = {
  background: string;
  border: string;
  textBackground: string;
};

export type ColorScheme = {
  [eventType: string]: EventColors;
};

export type TimeSlot = {
  date: Date;
  hour: number;
  minute: number;
};

export type SchedulerProps = {
  title: string;
  events: Event[];
  dates: Date[];
  daystartTime?: number;
  dayendTime?: number;
  hourHeight?: number;
  colorScheme?: ColorScheme;
  onNavigatePrevious?: () => void;
  onNavigateNext?: () => void;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  onDateSelect?: (date: Date) => void;
  highlightedDate?: Date;
};
