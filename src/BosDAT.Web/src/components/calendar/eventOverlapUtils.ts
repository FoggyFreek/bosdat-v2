import type { CalendarEvent } from './types';

export type EventLayout = {
  column: number;
  totalColumns: number;
};

const doEventsOverlap = (event1: CalendarEvent, event2: CalendarEvent): boolean => {
  const start1 = new Date(event1.startDateTime).getTime();
  const end1 = new Date(event1.endDateTime).getTime();
  const start2 = new Date(event2.startDateTime).getTime();
  const end2 = new Date(event2.endDateTime).getTime();

  return start1 < end2 && start2 < end1;
};

/**
 * Checks if two events overlap in time
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

    const durationA = new Date(a.endDateTime).getTime() - new Date(a.startDateTime).getTime();
    const durationB = new Date(b.endDateTime).getTime() - new Date(b.startDateTime).getTime();
    return durationB - durationA;
  });

  // Track which column each event is placed in
  const columnMap = new Map<number, CalendarEvent>();

  for (const event of sortedEvents) {
    const eventKey = `${event.id}`;
    const eventStart = new Date(event.startDateTime).getTime();

    // Remove events from columnMap that have ended before this event starts
    for (const [col, columnEvent] of columnMap.entries()) {
      const columnEventEnd = new Date(columnEvent.endDateTime).getTime();
      if (columnEventEnd <= eventStart) {
        columnMap.delete(col);
      }
    }

    // Find all events that transitively overlap with this event
    const overlapGroup = new Set<CalendarEvent>();
    const toCheck = [event];
    
    while (toCheck.length > 0) {
      const current = toCheck.pop()!;
      if (overlapGroup.has(current)) continue;
      overlapGroup.add(current);
      
      for (const e of sortedEvents) {
        if (!overlapGroup.has(e) && doEventsOverlap(current, e)) {
          toCheck.push(e);
        }
      }
    }

    // Find columns already used by events in this overlap group
    const groupColumnsUsed = new Set<number>();
    for (const overlapping of overlapGroup) {
      if (overlapping === event) continue;
      const existingLayout = layoutMap.get(`${overlapping.id}`);
      if (existingLayout) {
        groupColumnsUsed.add(existingLayout.column);
      }
    }

    // Find the first available column
    // Avoid: 1) columns with overlapping active events, 2) columns already used in overlap group
    let column = 0;
    while (column < sortedEvents.length) {
      const columnEvent = columnMap.get(column);

      // Skip if column has an event that overlaps with current event
      if (columnEvent && doEventsOverlap(event, columnEvent)) {
        column++;
        continue;
      }

      // Skip if this column is already used by another event in the overlap group
      if (groupColumnsUsed.has(column)) {
        column++;
        continue;
      }

      break;
    }

    columnMap.set(column, event);

    // Calculate total columns for the entire overlap group
    const allColumnsUsed = new Set([...groupColumnsUsed, column]);
    const totalColumnsForGroup = allColumnsUsed.size;

    // Update this event's layout
    layoutMap.set(eventKey, {
      column,
      totalColumns: totalColumnsForGroup,
    });

    // Update ALL events in the overlap group with the new totalColumns
    for (const overlapping of overlapGroup) {
      const existingLayout = layoutMap.get(`${overlapping.id}`);
      if (existingLayout) {
        existingLayout.totalColumns = Math.max(existingLayout.totalColumns, totalColumnsForGroup);
      }
    }
  }

  return layoutMap;
};
