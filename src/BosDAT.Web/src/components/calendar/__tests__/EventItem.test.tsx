import { describe, it, expect } from 'vitest';
import { render, screen, waitFor } from '@/test/utils';
import { EventItem } from '../EventItem';
import type { CalendarEvent } from '../types';
import { userEvent } from '@testing-library/user-event';

describe('EventItem', () => {
  const defaultEvent: CalendarEvent = {
    startDateTime: '2024-01-15T09:00:00',
    endDateTime: '2024-01-15T10:00:00',
    title: 'Test Event',
    frequency: 'once',
    eventType: 'course',
    attendees: ['Student 1'],
    room: 'Room A',
  };

  const defaultProps = {
    event: defaultEvent,
    dayIndex: 0,
    hourHeight: 100,
    minHour: 9,
  };

  it('should render event with default layout (no overlaps)', () => {
    const { container } = render(<EventItem {...defaultProps} />);

    expect(screen.getByText('Test Event')).toBeInTheDocument();

    const button = container.querySelector('button');
    expect(button?.style.width).toContain('14.28'); // Full column width
  });

  it('should render event with half width when layout specifies 2 columns', () => {
    const { container } = render(
      <EventItem {...defaultProps} layout={{ column: 0, totalColumns: 2 }} />
    );

    expect(screen.getByText('Test Event')).toBeInTheDocument();

    const button = container.querySelector('button');
    expect(button?.style.width).toContain('7.14'); // Half width
    expect(button?.style.left).toBe('0%'); // First column
  });

  it('should render event with correct position for second column', () => {
    const { container } = render(
      <EventItem {...defaultProps} layout={{ column: 1, totalColumns: 2 }} />
    );

    expect(screen.getByText('Test Event')).toBeInTheDocument();

    const button = container.querySelector('button');
    expect(button?.style.width).toContain('7.14'); // Half width
    expect(button?.style.left).toContain('7.14'); // Second column position
  });

  it('should render event with third width when layout specifies 3 columns', () => {
    const { container } = render(
      <EventItem {...defaultProps} layout={{ column: 0, totalColumns: 3 }} />
    );

    expect(screen.getByText('Test Event')).toBeInTheDocument();

    const button = container.querySelector('button');
    expect(button?.style.width).toContain('4.76'); // Third width (14.28 / 3)
  });

  it('should show hover note after delay', async () => {
    const user = userEvent.setup();
    render(<EventItem {...defaultProps} />);

    const button = screen.getByRole('button');

    await user.hover(button);

    // Wait for hover delay (300ms) and check for tooltip
    await waitFor(
      () => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument();
      },
      { timeout: 500 }
    );

    // Verify attendee is shown via aria-label
    expect(screen.getByLabelText('Student 1')).toBeInTheDocument();
  });

  it('should hide hover note on mouse leave', async () => {
    const user = userEvent.setup();
    render(<EventItem {...defaultProps} />);

    const button = screen.getByRole('button');

    await user.hover(button);

    // Wait for hover note to appear
    await waitFor(
      () => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument();
      },
      { timeout: 500 }
    );

    await user.unhover(button);

    // Hover note should disappear
    await waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });

  it('should maintain hover note behavior with overlapping layout', async () => {
    const user = userEvent.setup();
    render(<EventItem {...defaultProps} layout={{ column: 0, totalColumns: 2 }} />);

    const button = screen.getByRole('button');

    await user.hover(button);

    // Wait for hover note to appear
    await waitFor(
      () => {
        expect(screen.getByRole('tooltip')).toBeInTheDocument();
      },
      { timeout: 500 }
    );

    // Verify event details are shown
    expect(screen.getByText('Room A')).toBeInTheDocument();
    expect(screen.getByLabelText('Student 1')).toBeInTheDocument();
  });

  it('should toggle hover note with keyboard', async () => {
    const user = userEvent.setup();
    render(<EventItem {...defaultProps} />);

    const button = screen.getByRole('button');

    button.focus();
    await user.keyboard('{Enter}');

    // Hover note should appear immediately (no delay for keyboard)
    await waitFor(() => {
      expect(screen.getByRole('tooltip')).toBeInTheDocument();
    });

    expect(screen.getByLabelText('Student 1')).toBeInTheDocument();
  });

  it('should position event correctly for different day indices', () => {
    const { container: container1 } = render(<EventItem {...defaultProps} dayIndex={0} />);
    const { container: container2 } = render(<EventItem {...defaultProps} dayIndex={3} />);

    const button1 = container1.querySelector('button');
    const button2 = container2.querySelector('button');

    expect(button1?.style.left).toBe('0%');
    expect(button2?.style.left).toContain('42.85'); // 3 * 14.28%
  });

  it('should apply correct z-index when hovered', async () => {
    const user = userEvent.setup();
    const { container } = render(<EventItem {...defaultProps} />);

    const button = container.querySelector('button');

    // Default z-index
    expect(button?.className).toContain('z-[5]');

    await user.hover(button!);

    // Wait for hover state
    await waitFor(() => {
      expect(button?.className).toContain('z-[9999]');
    });
  });
});
