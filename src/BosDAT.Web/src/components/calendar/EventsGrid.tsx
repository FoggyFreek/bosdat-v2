import React, { useState, useCallback, useMemo, useRef } from 'react';
import { cn } from '@/lib/utils';
import { EventItem } from './EventItem';
import type { CalendarEvent, ColorScheme, TimeSlot } from './types';
import { getDateFromDateTime, isSameDay, isValidEventTime } from '@/lib/iso-helpers';

type EventsGridProps = {
  hours: number[];
  events: CalendarEvent[];
  hourHeight: number;
  minHour: number;
  maxHour: number;
  dayStartTime: number;
  dayEndTime: number;
  colorScheme?: ColorScheme;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  highlightedDate?: Date;
  dates: Date[];
};

// Named constants
const DAYS_IN_WEEK = 7;
const MINUTES_PER_HOUR = 60;
const MINUTE_INTERVAL = 10;

// Static array for column indices (prevents recreation on render)
const COLUMN_DIVIDER_INDICES = [1, 2, 3, 4, 5, 6] as const;

// Static array for minute marks within an hour
const MINUTE_MARKS = [0, 10, 20, 30, 40, 50] as const;

const EventsGridComponent: React.FC<EventsGridProps> = ({
  hours,
  events,
  hourHeight,
  minHour,
  maxHour,
  dayStartTime,
  dayEndTime,
  colorScheme,
  onTimeslotClick,
  highlightedDate,
  dates,
}) => {
  const totalHeight = useMemo(() => hours.length * hourHeight, [hours.length, hourHeight]);

  const [hoveredSlot, setHoveredSlot] = useState<{ day: number; hour: number; minute: number } | null>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  // Calculate timeslot from mouse position using event delegation
  const getTimeslotFromEvent = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!gridRef.current) return null;

      const rect = gridRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      const dayIndex = Math.floor((x / rect.width) * DAYS_IN_WEEK);
      if (dayIndex < 0 || dayIndex >= DAYS_IN_WEEK) return null;

      const totalMinutesFromTop = (y / hourHeight) * MINUTES_PER_HOUR;
      const hour = minHour + Math.floor(totalMinutesFromTop / MINUTES_PER_HOUR);
      const minute = Math.floor((totalMinutesFromTop % MINUTES_PER_HOUR) / MINUTE_INTERVAL) * MINUTE_INTERVAL;

      if (hour < minHour || hour > maxHour) return null;

      return { day: dayIndex, hour, minute };
    },
    [hourHeight, minHour, maxHour]
  );

  // Use event delegation for click handling
  const handleGridClick = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!onTimeslotClick) return;

      const timeslot = getTimeslotFromEvent(e);
      if (timeslot && dates[timeslot.day]) {
        onTimeslotClick({
          date: dates[timeslot.day],
          hour: timeslot.hour,
          minute: timeslot.minute,
        });
      }
    },
    [onTimeslotClick, getTimeslotFromEvent, dates]
  );

  // Use event delegation for hover handling
  const handleGridMouseMove = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      const timeslot = getTimeslotFromEvent(e);
      if (timeslot) {
        setHoveredSlot(timeslot);
      }
    },
    [getTimeslotFromEvent]
  );

  const handleGridMouseLeave = useCallback(() => {
    setHoveredSlot(null);
  }, []);

  // Handle keyboard navigation
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLDivElement>) => {
      if (!onTimeslotClick || !hoveredSlot) return;

      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        if (dates[hoveredSlot.day]) {
          onTimeslotClick({
            date: dates[hoveredSlot.day],
            hour: hoveredSlot.hour,
            minute: hoveredSlot.minute,
          });
        }
      }
    },
    [onTimeslotClick, hoveredSlot, dates]
  );

  // Filter valid events
  const validEvents = useMemo(
    () => events.filter((event) => isValidEventTime(event.startDateTime, event.endDateTime)),
    [events]
  );

  // Memoize highlighted column index
  const highlightedDayIndex = useMemo(() => {
    if (!highlightedDate) return -1;
    return dates.findIndex((date) => isSameDay(date, highlightedDate));
  }, [highlightedDate, dates]);

  const columnWidthPercent = 100 / DAYS_IN_WEEK;

  return (
    <div
      ref={gridRef}
      className="relative grid grid-cols-7 cursor-pointer"
      style={{ height: `${totalHeight}px` }}
      onClick={handleGridClick}
      onMouseMove={handleGridMouseMove}
      onMouseLeave={handleGridMouseLeave}
      onKeyDown={handleKeyDown}
      role="grid"
      aria-label="Weekly calendar grid"
      tabIndex={0}
    >
      {/* Highlighted Date Column Background */}
      {highlightedDayIndex >= 0 && (
        <div
          className="absolute bg-sky-100 opacity-60 z-[4] h-full"
          style={{
            left: `${(highlightedDayIndex / DAYS_IN_WEEK) * 100}%`,
            width: `${columnWidthPercent}%`,
          }}
          aria-hidden="true"
        />
      )}

      {/* Horizontal Grid Lines - Every 10 Minutes */}
      {hours.map((hour) =>
        MINUTE_MARKS.map((minute) => (
          <div
            key={`${hour}-${minute}`}
            className={cn(
              'absolute left-0 right-0 border-t',
              minute === 0 ? 'border-slate-400' : 'border-slate-200'
            )}
            style={{
              top: `${(hour - minHour + minute / MINUTES_PER_HOUR) * hourHeight}px`,
            }}
            aria-hidden="true"
          />
        ))
      )}

      {/* Vertical Grid Lines */}
      {COLUMN_DIVIDER_INDICES.map((columnNumber) => (
        <div
          key={`grid-line-col-${columnNumber}`}
          className="border-r border-slate-300 h-full"
          aria-hidden="true"
        />
      ))}

      {/* Hover Indicator */}
      {hoveredSlot && (
        <div
          className="absolute bg-blue-200 opacity-50 pointer-events-none z-[3]"
          style={{
            left: `${(hoveredSlot.day / DAYS_IN_WEEK) * 100}%`,
            width: `${columnWidthPercent}%`,
            top: `${(hoveredSlot.hour - minHour + hoveredSlot.minute / MINUTES_PER_HOUR) * hourHeight}px`,
            height: `${(MINUTE_INTERVAL / MINUTES_PER_HOUR) * hourHeight}px`,
          }}
          aria-hidden="true"
        />
      )}

      {/* Unavailable Time Overlay - Before Day Start */}
      {dayStartTime > minHour && (
        <div
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-b border-gray-300 col-span-7 pointer-events-none"
          style={{
            top: 0,
            height: `${(dayStartTime - minHour) * hourHeight}px`,
          }}
          aria-label="Unavailable time before working hours"
        />
      )}

      {/* Unavailable Time Overlay - After Day End */}
      {dayEndTime < maxHour && (
        <div
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-t border-gray-300 col-span-7 pointer-events-none"
          style={{
            top: `${(dayEndTime - minHour) * hourHeight}px`,
            height: `${(maxHour - dayEndTime + 1) * hourHeight}px`,
          }}
          aria-label="Unavailable time after working hours"
        />
      )}

      {/* Events */}
      {validEvents.map((e) => {
        const eventDate = getDateFromDateTime(e.startDateTime);
        const dayIndex = eventDate.getDay() - 1; // Adjusting so Monday=0, Sunday=6

        // Skip events that don't fall within the current week's date range
        if (dayIndex === -1) return null;

        return (
          <EventItem
            key={`${e.startDateTime}-${e.title}`}
            event={e}
            dayIndex={dayIndex}
            hourHeight={hourHeight}
            minHour={minHour}
            colorScheme={colorScheme}
          />
        );
      })}
    </div>
  );
};

export const EventsGrid = React.memo(EventsGridComponent);
EventsGrid.displayName = 'EventsGrid';
