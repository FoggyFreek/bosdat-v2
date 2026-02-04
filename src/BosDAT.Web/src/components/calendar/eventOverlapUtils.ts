import type { CalendarEvent } from './types';

export type EventLayout = {
  column: number;
  totalColumns: number;
};

/**
 * Checks if two events overlap in time
 */
export const doEventsOverlap = (event1: CalendarEvent, event2: CalendarEvent): boolean => {
  const start1 = new Date(event1.startDateTime).getTime();
  const end1 = new Date(event1.endDateTime).getTime();
  const start2 = new Date(event2.startDateTime).getTime();
  const end2 = new Date(event2.endDateTime).getTime();

  // Events overlap if one starts before the other ends
  // But they don't overlap if they just touch at boundaries
  return start1 < end2 && start2 < end1;
};

/**
 * Calculates layout information for events to display them side-by-side when they overlap
 * Returns a map of event keys to layout information (column index and total columns)
 */
export const calculateEventLayout = (events: CalendarEvent[]): Map<string, EventLayout> => {
  const layoutMap = new Map<string, EventLayout>();

  if (events.length === 0) {
    return layoutMap;
  }

  // Sort events by start time, then by duration (longer events first)
  const sortedEvents = events.toSorted((a, b) => {
    const startCompare = new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime();
    if (startCompare !== 0) return startCompare;

    // If start times are equal, longer events come first
    const durationA = new Date(a.endDateTime).getTime() - new Date(a.startDateTime).getTime();
    const durationB = new Date(b.endDateTime).getTime() - new Date(b.startDateTime).getTime();
    return durationB - durationA;
  });

  // Track which columns are occupied by events that haven't ended yet
  const columns: CalendarEvent[] = [];

  for (const event of sortedEvents) {
    const eventKey = `${event.startDateTime}-${event.title}`;

    // Remove events from columns that have ended before this event starts
    const eventStart = new Date(event.startDateTime).getTime();
    let i = 0;
    while (i < columns.length) {
      const columnEvent = columns[i];
      const columnEventEnd = new Date(columnEvent.endDateTime).getTime();

      if (columnEventEnd <= eventStart) {
        columns.splice(i, 1);
      } else {
        i++;
      }
    }

    // Find the first available column
    let column = 0;
    let placed = false;

    for (let col = 0; col < columns.length; col++) {
      const columnEvent = columns[col];
      if (!doEventsOverlap(event, columnEvent)) {
        column = col;
        columns[col] = event;
        placed = true;
        break;
      }
    }

    // If no column was available, add a new column
    if (!placed) {
      column = columns.length;
      columns.push(event);
    }

    // Find all events that overlap with this event to determine totalColumns
    const overlappingEvents = sortedEvents.filter((e) => doEventsOverlap(event, e));

    // Calculate the maximum number of columns needed for this overlap group
    let maxColumns = column + 1;

    // Check all overlapping events to find the maximum column count needed
    for (const overlapping of overlappingEvents) {
      const overlappingKey = `${overlapping.startDateTime}-${overlapping.title}`;
      const existingLayout = layoutMap.get(overlappingKey);
      if (existingLayout) {
        maxColumns = Math.max(maxColumns, existingLayout.column + 1);
      }
    }

    // Update this event's layout
    layoutMap.set(eventKey, {
      column,
      totalColumns: Math.max(columns.length, maxColumns),
    });

    // Update all overlapping events with the new totalColumns
    for (const overlapping of overlappingEvents) {
      const overlappingKey = `${overlapping.startDateTime}-${overlapping.title}`;
      const existingLayout = layoutMap.get(overlappingKey);
      if (existingLayout) {
        existingLayout.totalColumns = Math.max(
          existingLayout.totalColumns,
          layoutMap.get(eventKey)?.totalColumns ?? 1
        );
      }
    }
  }

  return layoutMap;
};
