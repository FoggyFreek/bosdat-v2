import React, { useMemo, useState, useCallback, useRef, useEffect } from 'react';
import { cn } from '@/lib/utils';
import type { CalendarEvent, ColorScheme, EventColors, EventCategory } from './types';
import { EventHoverNote } from './EventHoverNote';
import { getDecimalHours, getDurationInHours, isValidEventTime } from '@/lib/datetime-helpers';
import type { EventLayout } from './eventOverlapUtils';

type EventItemProps = {
  readonly event: Readonly<CalendarEvent>;
  readonly dayIndex: number;
  readonly hourHeight: number;
  readonly minHour: number;
  readonly colorScheme?: ColorScheme;
  readonly layout?: EventLayout;
};

// Named constants for magic numbers
const HOVER_DELAY_MS = 300;
const MIN_EVENT_HEIGHT_PX = 20;
const DAYS_IN_WEEK = 7;
const LAST_DAY_INDEX = 6;

// Default colors for event types - uses Partial since not all categories need defaults
// LessonStatus colors should be provided via colorScheme prop
const DEFAULT_COLOR_SCHEME: Partial<Record<EventCategory, EventColors>> = {
  course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
  workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
  trial: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
  holiday: { background: '#fee2e2', border: '#991b1b', textBackground: '#fecaca' },
  absence: { background: '#fefce8', border: '#ca8a04', textBackground: '#fef08a' },
};

// Fallback color when no matching scheme is found
const FALLBACK_COLORS: EventColors = { background: '#f3f4f6', border: '#9ca3af', textBackground: '#e5e7eb' };

const EventItemComponent: React.FC<EventItemProps> = ({
  event,
  dayIndex,
  hourHeight,
  minHour,
  colorScheme,
  layout,
}) => {
  const [isHovered, setIsHovered] = useState(false);
  const hoverTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (hoverTimerRef.current) {
        clearTimeout(hoverTimerRef.current);
      }
    };
  }, []);

  // Validate event times
  const isValid = useMemo(
    () => isValidEventTime(event.startDateTime, event.endDateTime),
    [event.startDateTime, event.endDateTime]
  );

  // Calculate start time and duration from datetime values
  const startTime = useMemo(() => getDecimalHours(event.startDateTime), [event.startDateTime]);
  const duration = useMemo(
    () => getDurationInHours(event.startDateTime, event.endDateTime),
    [event.startDateTime, event.endDateTime]
  );

  // Memoize position calculations
  const top = useMemo(() => (startTime - minHour) * hourHeight, [startTime, minHour, hourHeight]);
  const height = useMemo(
    () => Math.max(duration * hourHeight, MIN_EVENT_HEIGHT_PX),
    [duration, hourHeight]
  );

   // Memoize color selection
  const colors = useMemo(
    () => colorScheme?.[event.eventType] ?? DEFAULT_COLOR_SCHEME[event.eventType] ?? FALLBACK_COLORS,
    [colorScheme, event.eventType]
  );

  // Handle mouse enter with delay
  const handleMouseEnter = useCallback(() => {
    hoverTimerRef.current = setTimeout(() => {
      setIsHovered(true);
    }, HOVER_DELAY_MS);
  }, []);

  // Handle mouse leave
  const handleMouseLeave = useCallback(() => {
    if (hoverTimerRef.current) {
      clearTimeout(hoverTimerRef.current);
      hoverTimerRef.current = null;
    }
    setIsHovered(false);
  }, []);

  // Handle keyboard interaction
  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setIsHovered((prev) => !prev);
    } else if (e.key === 'Escape') {
      setIsHovered(false);
    }
  }, []);

  if (!isValid) {
    return null;
  }

  const columnWidthPercent = 100 / DAYS_IN_WEEK;

  // Calculate position and width based on layout
  const column = layout?.column ?? 0;
  const totalColumns = layout?.totalColumns ?? 1;

  // Calculate the width per column within the day
  const widthPerColumn = columnWidthPercent / totalColumns;

  // Calculate left position: base day position + offset for column
  const leftPosition = dayIndex * columnWidthPercent + column * widthPerColumn;

  return (
    <button
      type="button"
      className={cn(
        'absolute mx-1 p-2 rounded-md border-l-4 text-left overflow-visible',
        'focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:ring-offset-1',
        isHovered ? 'z-9999' : 'z-5'
      )}
      style={{
        top: `${top}px`,
        height: `${height-2}px`,
        left: `${leftPosition}%`,
        width: `calc(${widthPerColumn}% - 8px)`,
        backgroundColor: colors.background,
        borderLeftColor: colors.border,
      }}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onKeyDown={handleKeyDown}
      aria-label={`${event.title}, ${event.eventType}, ${event.frequency}`}
      aria-expanded={isHovered}
    >
      <h3 className="text-xs text-left  font-bold text-slate-800 truncate">
        {event.title} 
      </h3>

      {duration > 0.5 && (
        <div className="text-[10px] text-slate-600 mt-1">
          <span
            className="inline-block px-2 py-0.5 rounded mr-2"
            style={{ backgroundColor: colors.textBackground }}
          >
            {event.eventType}/{event.status}
          </span>
          <span
            className="inline-block px-2 py-0.5 rounded"
            style={{ backgroundColor: colors.textBackground }}
          >
            {event.frequency}
          </span>
        </div>
      )}

      {/* Hover Note */}
      {isHovered && (
        <EventHoverNote
          event={event}
          colors={colors}
          isLastColumn={dayIndex === LAST_DAY_INDEX}
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
        />
      )}
    </button>
  );
};

export const EventItem = React.memo(EventItemComponent);
EventItem.displayName = 'EventItem';
