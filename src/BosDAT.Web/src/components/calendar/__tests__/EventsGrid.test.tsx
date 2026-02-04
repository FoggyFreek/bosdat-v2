import { describe, it, expect } from 'vitest';
import { render, screen } from '@/test/utils';
import { EventsGrid } from '../EventsGrid';
import type { CalendarEvent } from '../types';

describe('EventsGrid', () => {
  const dates = [
    new Date('2024-01-15'), // Monday
    new Date('2024-01-16'), // Tuesday
    new Date('2024-01-17'), // Wednesday
    new Date('2024-01-18'), // Thursday
    new Date('2024-01-19'), // Friday
    new Date('2024-01-20'), // Saturday
    new Date('2024-01-21'), // Sunday
  ];

  const defaultProps = {
    hours: [9, 10, 11, 12, 13, 14, 15, 16, 17],
    events: [] as CalendarEvent[],
    hourHeight: 100,
    minHour: 9,
    maxHour: 17,
    dayStartTime: 9,
    dayEndTime: 17,
    dates,
  };

  it('should render overlapping events side-by-side', () => {
    const overlappingEvents: CalendarEvent[] = [
      {
        id: 'event-1',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        id: 'event-2',
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
    ];

    const { container } = render(<EventsGrid {...defaultProps} events={overlappingEvents} />);

    // Both events should be rendered
    expect(screen.getByText('Event 1')).toBeInTheDocument();
    expect(screen.getByText('Event 2')).toBeInTheDocument();

    // Get the event buttons
    const eventButtons = container.querySelectorAll('button[aria-label*="Event"]');
    expect(eventButtons).toHaveLength(2);

    // Verify they have different widths (should be ~7.14% each instead of 14.28%)
    const event1Style = (eventButtons[0] as HTMLElement).style;
    const event2Style = (eventButtons[1] as HTMLElement).style;

    // Both should have reduced width since they overlap
    expect(event1Style.width).toContain('7.14');
    expect(event2Style.width).toContain('7.14');

    // They should have different left positions
    expect(event1Style.left).not.toBe(event2Style.left);
  });

  it('should render three overlapping events with equal widths', () => {
    const threeOverlapping: CalendarEvent[] = [
      {
        id: 'event-1',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T11:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        id: 'event-2',
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
      {
        id: 'event-3',
        startDateTime: '2024-01-15T10:00:00',
        endDateTime: '2024-01-15T11:30:00',
        title: 'Event 3',
        frequency: 'once',
        eventType: 'trail',
        attendees: [],
      },
    ];

    const { container } = render(<EventsGrid {...defaultProps} events={threeOverlapping} />);

    // All events should be rendered
    expect(screen.getByText('Event 1')).toBeInTheDocument();
    expect(screen.getByText('Event 2')).toBeInTheDocument();
    expect(screen.getByText('Event 3')).toBeInTheDocument();

    // Get the event buttons
    const eventButtons = container.querySelectorAll('button[aria-label*="Event"]');
    expect(eventButtons).toHaveLength(3);

    // Each should have ~4.76% width (14.28% / 3)
    const styles = Array.from(eventButtons).map((btn) => (btn as HTMLElement).style);
    styles.forEach((style) => {
      expect(style.width).toContain('4.76');
    });
  });

  it('should render non-overlapping events at full width', () => {
    const nonOverlapping: CalendarEvent[] = [
      {
        id: 'event-1',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        id: 'event-2',
        startDateTime: '2024-01-15T10:00:00',
        endDateTime: '2024-01-15T11:00:00',
        title: 'Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
    ];

    const { container } = render(<EventsGrid {...defaultProps} events={nonOverlapping} />);

    // Both events should be rendered
    expect(screen.getByText('Event 1')).toBeInTheDocument();
    expect(screen.getByText('Event 2')).toBeInTheDocument();

    // Get the event buttons
    const eventButtons = container.querySelectorAll('button[aria-label*="Event"]');
    expect(eventButtons).toHaveLength(2);

    // Both should have full width since they don't overlap
    const styles = Array.from(eventButtons).map((btn) => (btn as HTMLElement).style);
    styles.forEach((style) => {
      expect(style.width).toContain('14.28');
    });
  });

  it('should handle events on different days independently', () => {
    const differentDays: CalendarEvent[] = [
      {
        id: 'mon-event-1',
        startDateTime: '2024-01-15T09:00:00',
        endDateTime: '2024-01-15T10:00:00',
        title: 'Monday Event 1',
        frequency: 'once',
        eventType: 'course',
        attendees: [],
      },
      {
        id: 'mon-event-2',
        startDateTime: '2024-01-15T09:30:00',
        endDateTime: '2024-01-15T10:30:00',
        title: 'Monday Event 2',
        frequency: 'once',
        eventType: 'workshop',
        attendees: [],
      },
      {
        id: 'tue-event-1',
        startDateTime: '2024-01-16T09:00:00',
        endDateTime: '2024-01-16T10:00:00',
        title: 'Tuesday Event',
        frequency: 'once',
        eventType: 'trail',
        attendees: [],
      },
    ];

    const { container } = render(<EventsGrid {...defaultProps} events={differentDays} />);

    // All events should be rendered
    expect(screen.getByText('Monday Event 1')).toBeInTheDocument();
    expect(screen.getByText('Monday Event 2')).toBeInTheDocument();
    expect(screen.getByText('Tuesday Event')).toBeInTheDocument();

    const eventButtons = container.querySelectorAll('button[aria-label*="Event"]');
    expect(eventButtons).toHaveLength(3);

    // Monday events should be side-by-side (half width each)
    // Tuesday event should be full width
    const mondayEvent1 = screen.getByText('Monday Event 1').closest('button') as HTMLElement;
    const mondayEvent2 = screen.getByText('Monday Event 2').closest('button') as HTMLElement;
    const tuesdayEvent = screen.getByText('Tuesday Event').closest('button') as HTMLElement;

    expect(mondayEvent1.style.width).toContain('7.14');
    expect(mondayEvent2.style.width).toContain('7.14');
    expect(tuesdayEvent.style.width).toContain('14.28');
  });
});
