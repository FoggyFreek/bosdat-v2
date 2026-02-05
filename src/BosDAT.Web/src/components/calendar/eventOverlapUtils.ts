import type { CalendarEvent } from './types';

export type EventLayout = {
  column: number;
  totalColumns: number;
};

/** Checks if two events have overlapping time ranges */
const doEventsOverlap = (event1: CalendarEvent, event2: CalendarEvent): boolean => {
  const start1 = new Date(event1.startDateTime).getTime();
  const end1 = new Date(event1.endDateTime).getTime();
  const start2 = new Date(event2.startDateTime).getTime();
  const end2 = new Date(event2.endDateTime).getTime();

  return start1 < end2 && start2 < end1;
};

/** Sorts by start time ascending, then by duration descending (longer first) */
const sortEventsByStartAndDuration = (events: CalendarEvent[]): CalendarEvent[] => {
  return events.toSorted((a, b) => {
    const startCompare = new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime();
    if (startCompare !== 0) return startCompare;

    const durationA = new Date(a.endDateTime).getTime() - new Date(a.startDateTime).getTime();
    const durationB = new Date(b.endDateTime).getTime() - new Date(b.startDateTime).getTime();
    return durationB - durationA;
  });
};

/** Removes events from columnMap that ended before the given timestamp */
const removeEndedEvents = (columnMap: Map<number, CalendarEvent>, eventStart: number): void => {
  for (const [col, columnEvent] of columnMap.entries()) {
    const columnEventEnd = new Date(columnEvent.endDateTime).getTime();
    if (columnEventEnd <= eventStart) {
      columnMap.delete(col);
    }
  }
};

/** Finds all events connected by overlaps (A overlaps B, B overlaps C → all grouped) */
const findTransitiveOverlapGroup = (
  event: CalendarEvent,
  allEvents: CalendarEvent[]
): Set<CalendarEvent> => {
  const overlapGroup = new Set<CalendarEvent>();
  const toCheck = [event];

  while (toCheck.length > 0) {
    const current = toCheck.pop()!;
    if (overlapGroup.has(current)) continue;
    overlapGroup.add(current);

    for (const e of allEvents) {
      if (!overlapGroup.has(e) && doEventsOverlap(current, e)) {
        toCheck.push(e);
      }
    }
  }

  return overlapGroup;
};

/** Returns column indices already assigned to other events in the group */
const getColumnsUsedByGroup = (
  overlapGroup: Set<CalendarEvent>,
  currentEvent: CalendarEvent,
  layoutMap: Map<string, EventLayout>
): Set<number> => {
  const columnsUsed = new Set<number>();

  for (const overlapping of overlapGroup) {
    if (overlapping === currentEvent) continue;
    const existingLayout = layoutMap.get(`${overlapping.id}`);
    if (existingLayout) {
      columnsUsed.add(existingLayout.column);
    }
  }

  return columnsUsed;
};

/** Finds lowest column index not occupied by an overlapping or grouped event */
const findFirstAvailableColumn = (
  event: CalendarEvent,
  columnMap: Map<number, CalendarEvent>,
  groupColumnsUsed: Set<number>,
  maxColumns: number
): number => {
  for (let column = 0; column < maxColumns; column++) {
    const columnEvent = columnMap.get(column);
    const isColumnOccupied = columnEvent && doEventsOverlap(event, columnEvent);
    const isColumnUsedByGroup = groupColumnsUsed.has(column);

    if (!isColumnOccupied && !isColumnUsedByGroup) {
      return column;
    }
  }
  return maxColumns;
};

/** Updates totalColumns for all events in group to ensure consistent widths */
const updateOverlapGroupTotalColumns = (
  overlapGroup: Set<CalendarEvent>,
  layoutMap: Map<string, EventLayout>,
  totalColumns: number
): void => {
  for (const overlapping of overlapGroup) {
    const existingLayout = layoutMap.get(`${overlapping.id}`);
    if (existingLayout) {
      existingLayout.totalColumns = Math.max(existingLayout.totalColumns, totalColumns);
    }
  }
};

/**
 * Calculates column layout for overlapping calendar events
 */
export const calculateEventLayout = (events: CalendarEvent[]): Map<string, EventLayout> => {
  const layoutMap = new Map<string, EventLayout>();

  if (events.length === 0) {
    return layoutMap;
  }

  const sortedEvents = sortEventsByStartAndDuration(events);
  const columnMap = new Map<number, CalendarEvent>();

  for (const event of sortedEvents) {
    const eventStart = new Date(event.startDateTime).getTime();
    removeEndedEvents(columnMap, eventStart);

    const overlapGroup = findTransitiveOverlapGroup(event, sortedEvents);
    const groupColumnsUsed = getColumnsUsedByGroup(overlapGroup, event, layoutMap);
    const column = findFirstAvailableColumn(event, columnMap, groupColumnsUsed, sortedEvents.length);

    columnMap.set(column, event);

    const totalColumnsForGroup = groupColumnsUsed.size + 1;

    layoutMap.set(`${event.id}`, {
      column,
      totalColumns: totalColumnsForGroup,
    });

    updateOverlapGroupTotalColumns(overlapGroup, layoutMap, totalColumnsForGroup);
  }

  return layoutMap;
};
