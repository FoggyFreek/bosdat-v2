import { describe, it, expect } from 'vitest';
import { calculateEventLayout, doEventsOverlap } from '../eventOverlapUtils';
import type { CalendarEvent } from '../types';

describe('doEventsOverlap', () => {
  it('should return true for overlapping events', () => {
    const event1: CalendarEvent = {
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
      title: 'Event 1',
      frequency: 'once',
      eventType: 'course',
      attendees: [],
    };

    const event2: CalendarEvent = {
      startDateTime: '2024-01-15T09:30:00',
      endDateTime: '2024-01-15T10:30:00',
      title: 'Event 2',
      frequency: 'once',
      eventType: 'workshop',
      attendees: [],
    };

    expect(doEventsOverlap(event1, event2)).toBe(true);
  });

  it('should return false for non-overlapping events', () => {
    const event1: CalendarEvent = {
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
      title: 'Event 1',
      frequency: 'once',
      eventType: 'course',
      attendees: [],
    };

    const event2: CalendarEvent = {
      startDateTime: '2024-01-15T10:00:00',
      endDateTime: '2024-01-15T11:00:00',
      title: 'Event 2',
      frequency: 'once',
      eventType: 'workshop',
      attendees: [],
    };

    expect(doEventsOverlap(event1, event2)).toBe(false);
  });

  it('should return true when one event contains another', () => {
    const event1: CalendarEvent = {
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T11:00:00',
      title: 'Event 1',
      frequency: 'once',
      eventType: 'course',
      attendees: [],
    };

    const event2: CalendarEvent = {
      startDateTime: '2024-01-15T09:30:00',
      endDateTime: '2024-01-15T10:00:00',
      title: 'Event 2',
      frequency: 'once',
      eventType: 'workshop',
      attendees: [],
    };

    expect(doEventsOverlap(event1, event2)).toBe(true);
  });

  it('should return false when events just touch at boundaries', () => {
    const event1: CalendarEvent = {
      startDateTime: '2024-01-15T09:00:00',
      endDateTime: '2024-01-15T10:00:00',
      title: 'Event 1',
      frequency: 'once',
      eventType: 'course',
      attendees: [],
    };

    const event2: CalendarEvent = {
      startDateTime: '2024-01-15T10:00:00',
      endDateTime: '2024-01-15T11:00:00',
      title: 'Event 2',
      frequency: 'once',
      eventType: 'workshop',
      attendees: [],
    };

    expect(doEventsOverlap(event1, event2)).toBe(false);
  });
});

describe('calculateEventLayout', () => {
  it('should return no layout for single event', () => {
    const events: CalendarEvent[] = [
      {
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
    ];

    const layout = calculateEventLayout(events);
    const key = '2024-01-15T09:00:00-Event 1';

    expect(layout.get(key)).toEqual({
      column: 0,
      totalColumns: 1,
    });
  });

  it('should assign columns for two overlapping events', () => {
    const events: CalendarEvent[] = [
      {
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
    ];

    const layout = calculateEventLayout(events);
    const key1 = '2024-01-15T09:00:00-Event 1';
    const key2 = '2024-01-15T09:30:00-Event 2';

    expect(layout.get(key1)).toEqual({
      column: 0,
      totalColumns: 2,
    });

    expect(layout.get(key2)).toEqual({
      column: 1,
      totalColumns: 2,
    });
  });

  it('should handle three overlapping events', () => {
    const events: CalendarEvent[] = [
      {
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T11:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T10:00:00',
        endDateTime: '2024-01-15T11:30:00',
        title: 'Event 3',
        frequency: 'once',
        eventType: 'trail',
        attendees: [],
      },
    ];

    const layout = calculateEventLayout(events);
    const key1 = '2024-01-15T09:00:00-Event 1';
    const key2 = '2024-01-15T09:30:00-Event 2';
    const key3 = '2024-01-15T10:00:00-Event 3';

    // All three overlap, so they need 3 columns
    expect(layout.get(key1)?.totalColumns).toBe(3);
    expect(layout.get(key2)?.totalColumns).toBe(3);
    expect(layout.get(key3)?.totalColumns).toBe(3);

    // Each should have a unique column
    const columns = [
      layout.get(key1)?.column,
      layout.get(key2)?.column,
      layout.get(key3)?.column,
    ];
    expect(new Set(columns).size).toBe(3);
  });

  it('should handle non-overlapping events separately', () => {
    const events: CalendarEvent[] = [
      {
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T10:00:00',
        endDateTime: '2024-01-15T11:00:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
    ];

    const layout = calculateEventLayout(events);
    const key1 = '2024-01-15T09:00:00-Event 1';
    const key2 = '2024-01-15T10:00:00-Event 2';

    // Non-overlapping events should each have their own full column
    expect(layout.get(key1)).toEqual({
      column: 0,
      totalColumns: 1,
    });

    expect(layout.get(key2)).toEqual({
      column: 0,
      totalColumns: 1,
    });
  });

  it('should handle mixed overlapping and non-overlapping events', () => {
    const events: CalendarEvent[] = [
      {
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
      {
        startDateTime: '2024-01-15T11:00:00',
        endDateTime: '2024-01-15T12:00:00',
        title: 'Event 3',
        frequency: 'once',
        eventType: 'trail',
        attendees: [],
      },
    ];

    const layout = calculateEventLayout(events);
    const key1 = '2024-01-15T09:00:00-Event 1';
    const key2 = '2024-01-15T09:30:00-Event 2';
    const key3 = '2024-01-15T11:00:00-Event 3';

    // First two overlap
    expect(layout.get(key1)).toEqual({
      column: 0,
      totalColumns: 2,
    });

    expect(layout.get(key2)).toEqual({
      column: 1,
      totalColumns: 2,
    });

    // Third is alone
    expect(layout.get(key3)).toEqual({
      column: 0,
      totalColumns: 1,
    });
  });

  it('should handle empty events array', () => {
    const events: CalendarEvent[] = [];
    const layout = calculateEventLayout(events);

    expect(layout.size).toBe(0);
  });
});
