import { describe, it, expect } from 'vitest';
import { calculateEventLayout } from '../eventOverlapUtils';
import type { CalendarEvent } from '../types';

const makeEvent = (
  id: string,
  start: string,
  end: string,
  overrides: Partial<CalendarEvent> = {},
): CalendarEvent => ({
  id,
  startDateTime: start,
  endDateTime: end,
  title: id,
  frequency: 'once',
  eventType: 'course',
  attendees: [],
  ...overrides,
});

describe('calculateEventLayout', () => {
  it('should return empty map for no events', () => {
    expect(calculateEventLayout([])).toHaveProperty('size', 0);
  });

  it('should return single column for a lone event', () => {
    const layout = calculateEventLayout([
      makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
    ]);

    expect(layout.get('a')).toEqual({ column: 0, totalColumns: 1 });
  });

  it('should assign separate columns for two overlapping events', () => {
    const layout = calculateEventLayout([
      makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
      makeEvent('b', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
    ]);

    expect(layout.get('a')).toEqual({ column: 0, totalColumns: 2 });
    expect(layout.get('b')).toEqual({ column: 1, totalColumns: 2 });
  });

  it('should treat abutting events as non-overlapping (end === start)', () => {
    const layout = calculateEventLayout([
      makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
      makeEvent('b', '2024-01-15T10:00:00', '2024-01-15T11:00:00'),
    ]);

    expect(layout.get('a')).toEqual({ column: 0, totalColumns: 1 });
    expect(layout.get('b')).toEqual({ column: 0, totalColumns: 1 });
  });

  it('should handle one event fully contained within another', () => {
    const layout = calculateEventLayout([
      makeEvent('outer', '2024-01-15T09:00:00', '2024-01-15T11:00:00'),
      makeEvent('inner', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
    ]);

    expect(layout.get('outer')).toEqual({ column: 0, totalColumns: 2 });
    expect(layout.get('inner')).toEqual({ column: 1, totalColumns: 2 });
  });

  it('should handle three mutually overlapping events', () => {
    const layout = calculateEventLayout([
      makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T11:00:00'),
      makeEvent('b', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
      makeEvent('c', '2024-01-15T10:00:00', '2024-01-15T11:30:00'),
    ]);

    expect(layout.get('a')?.totalColumns).toBe(3);
    expect(layout.get('b')?.totalColumns).toBe(3);
    expect(layout.get('c')?.totalColumns).toBe(3);
    expect(new Set([
      layout.get('a')?.column,
      layout.get('b')?.column,
      layout.get('c')?.column,
    ]).size).toBe(3);
  });

  it('should isolate non-overlapping groups independently', () => {
    const layout = calculateEventLayout([
      makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
      makeEvent('b', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
      makeEvent('c', '2024-01-15T11:00:00', '2024-01-15T12:00:00'),
    ]);

    expect(layout.get('a')).toEqual({ column: 0, totalColumns: 2 });
    expect(layout.get('b')).toEqual({ column: 1, totalColumns: 2 });
    expect(layout.get('c')).toEqual({ column: 0, totalColumns: 1 });
  });

  describe('transitive overlap chain', () => {
    // A (10:00-12:00) overlaps B (10:30-13:00), B overlaps C (12:30-13:00),
    // but A and C do NOT directly overlap.  The algorithm must still group all
    // three via the B bridge so every event gets totalColumns = 3 (33 % width).
    it('should group transitively connected events even without direct overlap', () => {
      const layout = calculateEventLayout([
        makeEvent('a', '2024-01-15T10:00:00', '2024-01-15T12:00:00'),
        makeEvent('b', '2024-01-15T10:30:00', '2024-01-15T13:00:00'),
        makeEvent('c', '2024-01-15T12:30:00', '2024-01-15T13:00:00'),
      ]);

      expect(layout.get('a')).toEqual({ column: 0, totalColumns: 3 });
      expect(layout.get('b')).toEqual({ column: 1, totalColumns: 3 });
      expect(layout.get('c')).toEqual({ column: 2, totalColumns: 3 });
    });

    it('should not merge two groups separated by a gap', () => {
      // {a, b} and {c, d} are each internally overlapping but the two pairs
      // have no transitive connection.
      const layout = calculateEventLayout([
        makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
        makeEvent('b', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
        makeEvent('c', '2024-01-15T11:00:00', '2024-01-15T12:00:00'),
        makeEvent('d', '2024-01-15T11:30:00', '2024-01-15T12:30:00'),
      ]);

      expect(layout.get('a')?.totalColumns).toBe(2);
      expect(layout.get('b')?.totalColumns).toBe(2);
      expect(layout.get('c')?.totalColumns).toBe(2);
      expect(layout.get('d')?.totalColumns).toBe(2);
    });

    it('should handle a longer chain A-B-C-D where only adjacent pairs overlap', () => {
      // A overlaps B, B overlaps C, C overlaps D — all four are transitively
      // connected so every event should share 4 columns.
      const layout = calculateEventLayout([
        makeEvent('a', '2024-01-15T09:00:00', '2024-01-15T10:00:00'),
        makeEvent('b', '2024-01-15T09:30:00', '2024-01-15T10:30:00'),
        makeEvent('c', '2024-01-15T10:15:00', '2024-01-15T11:00:00'),
        makeEvent('d', '2024-01-15T10:45:00', '2024-01-15T11:30:00'),
      ]);

      expect(layout.get('a')?.totalColumns).toBe(4);
      expect(layout.get('b')?.totalColumns).toBe(4);
      expect(layout.get('c')?.totalColumns).toBe(4);
      expect(layout.get('d')?.totalColumns).toBe(4);
      expect(new Set([
        layout.get('a')?.column,
        layout.get('b')?.column,
        layout.get('c')?.column,
        layout.get('d')?.column,
      ]).size).toBe(4);
    });
  });

  describe('sort stability', () => {
    it('should produce the same layout regardless of input order', () => {
      const events = [
        makeEvent('a', '2024-01-15T10:00:00', '2024-01-15T12:00:00'),
        makeEvent('b', '2024-01-15T10:30:00', '2024-01-15T13:00:00'),
        makeEvent('c', '2024-01-15T12:30:00', '2024-01-15T13:00:00'),
      ];

      const layoutForward = calculateEventLayout(events);
      const layoutReversed = calculateEventLayout([...events].reverse());

      expect(layoutReversed.get('a')).toEqual(layoutForward.get('a'));
      expect(layoutReversed.get('b')).toEqual(layoutForward.get('b'));
      expect(layoutReversed.get('c')).toEqual(layoutForward.get('c'));
    });

    it('should sort same-start events by duration descending (longer event gets column 0)', () => {
      const layout = calculateEventLayout([
        makeEvent('short', '2024-01-15T10:00:00', '2024-01-15T10:30:00'),
        makeEvent('long', '2024-01-15T10:00:00', '2024-01-15T12:00:00'),
      ]);

      expect(layout.get('long')?.column).toBe(0);
      expect(layout.get('short')?.column).toBe(1);
    });
  });
});
