import React, { useMemo, useState, useCallback, useRef, useEffect } from 'react';
import type { Event, ColorScheme } from './types';
import { EventHoverNote } from './EventHoverNote';
import { getDecimalHours, getDurationInHours, isValidEventTime } from './utils';

type EventItemProps = {
  event: Event;
  dayIndex: number; // The column index (0-6) where this event should appear
  hourHeight: number;
  minHour: number;
  colorScheme?: ColorScheme;
};

// Move constant outside component to prevent recreation on every render
const DEFAULT_COLOR_SCHEME: ColorScheme = {
  course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
  workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
  trail: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
  holiday: { background: '#fee2e2', border: '#991b1b', textBackground: '#fecaca' },
  absence: { background: '#fefce8', border: '#ca8a04', textBackground: '#fef08a' },
};

const EventItemComponent: React.FC<EventItemProps> = ({
  event,
  dayIndex,
  hourHeight,
  minHour,
  colorScheme,
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

  // Memoize position calculations to ensure component is pure
  const top = useMemo(() => (startTime - minHour) * hourHeight, [startTime, minHour, hourHeight]);
  const height = useMemo(() => Math.max(duration * hourHeight, 20), [duration, hourHeight]); // Minimum height of 20px

  // Memoize color selection to prevent unnecessary recalculations
  const colors = useMemo(
    () => colorScheme?.[event.eventType] || DEFAULT_COLOR_SCHEME[event.eventType] || DEFAULT_COLOR_SCHEME['trail'],
    [colorScheme, event.eventType]
  );

  // Handle mouse enter with delay
  const handleMouseEnter = useCallback(() => {
    hoverTimerRef.current = setTimeout(() => {
      setIsHovered(true);
    }, 300); // 300ms delay
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
      setIsHovered(prev => !prev);
    } else if (e.key === 'Escape') {
      setIsHovered(false);
    }
  }, []);

  if (!isValid) {
    return null; // Don't render invalid events
  }

  return (
    <button
      type="button"
      className="absolute mx-1 p-2 rounded-md border-l-4 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1 text-left"
      style={{
        top: `${top}px`,
        height: `${height}px`,
        left: `${(dayIndex * (100 / 7))}%`,
        width: `calc(${100 / 7}% - 8px)`,
        backgroundColor: colors.background,
        borderLeftColor: colors.border,
        overflow: 'visible',
        zIndex: isHovered ? 9999 : 5,
      }}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onKeyDown={handleKeyDown}
      aria-label={`${event.title}, ${event.eventType}, ${event.frequency}`}
      aria-expanded={isHovered}
    >
      <h3 className="text-xs text-left font-bold text-slate-800 truncate">
        {event.eventType === 'holiday' && (
          <span className="uppercase text-[10px] mr-1">Deadline -</span>
        )}
        {event.title}
      </h3>

      <div className="text-[10px] text-slate-600 mt-1">
        <span
          className="inline-block px-2 py-0.5 rounded mr-2"
          style={{ backgroundColor: colors.textBackground }}
        >
          {event.eventType}
        </span>
        <span
          className="inline-block px-2 py-0.5 rounded"
          style={{ backgroundColor: colors.textBackground }}
        >
          {event.frequency}
        </span>
      </div>

      {/* Hover Note */}
      {isHovered && (
        <EventHoverNote
          event={event}
          colors={colors}
          isLastColumn={dayIndex === 6}
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
        />
      )}
    </button>
  );
};

export const EventItem = React.memo(EventItemComponent);
EventItem.displayName = 'EventItem';
