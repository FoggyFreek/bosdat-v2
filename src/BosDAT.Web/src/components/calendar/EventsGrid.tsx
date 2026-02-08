import React, { useState, useCallback, useMemo, useRef } from 'react';
import { cn } from '@/lib/utils';
import { EventItem } from './EventItem';
import type { CalendarEvent, ColorScheme, TimeSlot, DayAvailability } from './types';
import { getDateFromDateTime, isSameDay, isValidEventTime, dayNameToNumber } from '@/lib/datetime-helpers';
import { calculateEventLayout } from './eventOverlapUtils';

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
  availability?: DayAvailability[];
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
  availability,
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

  // Calculate layout for overlapping events - grouped by day
  const eventLayouts = useMemo(() => {
    // Group events by day
    const eventsByDay = new Map<number, CalendarEvent[]>();

    validEvents.forEach((event) => {
      const eventDate = getDateFromDateTime(event.startDateTime);
      const dayIndex = eventDate.getDay() - 1 === -1 ? 6 : eventDate.getDay() - 1; // Adjusting so Monday=0, Sunday=6

      if (dayIndex !== -1) {
        if (!eventsByDay.has(dayIndex)) {
          eventsByDay.set(dayIndex, []);
        }
        eventsByDay.get(dayIndex)?.push(event);
      }
    });

    // Calculate layout for each day independently
    const allLayouts = new Map<string, { column: number; totalColumns: number }>();

    eventsByDay.forEach((dayEvents) => {
      const dayLayout = calculateEventLayout(dayEvents);
      dayLayout.forEach((layout, key) => {
        allLayouts.set(key, layout);
      });
    });

    return allLayouts;
  }, [validEvents]);

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
          className="absolute bg-sky-100 opacity-60 z-4 h-full"
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
          className="absolute bg-blue-200 opacity-50 pointer-events-none z-3"
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

      {/* Per-Day Teacher Availability Overlays */}
      {availability?.map((dayAvail) => {
        // Map dayOfWeek ("Sunday", "Monday", etc.) to column index
        // Grid shows Monday(col 0) to Sunday(col 6), so adjust:
        // Sunday -> column 6, Monday -> column 0, etc.
        const dayNumber = dayNameToNumber(dayAvail.dayOfWeek);
        const columnIndex = dayNumber === 0 ? 6 : dayNumber - 1;
        const isFullDayUnavailable = dayAvail.fromTime === 0 && dayAvail.untilTime === 0;

        if (isFullDayUnavailable) {
          // Full column overlay for unavailable day
          return (
            <div
              key={`unavail-full-${dayAvail.dayOfWeek}`}
              className="absolute bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                left: `${(columnIndex / DAYS_IN_WEEK) * 100}%`,
                width: `${columnWidthPercent}%`,
                top: `${(dayStartTime - minHour) * hourHeight}px`,
                height: `${(dayEndTime - dayStartTime) * hourHeight}px`,
              }}
              aria-label={`Unavailable on ${dayAvail.dayOfWeek}`}
            />
          );
        }

        // Partial day - overlay before fromTime and after untilTime
        const overlays = [];

        // Overlay before teacher's start time
        if (dayAvail.fromTime > minHour) {
          overlays.push(
            <div
              key={`unavail-before-${dayAvail.dayOfWeek}`}
              className="absolute bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                left: `${(columnIndex / DAYS_IN_WEEK) * 100}%`,
                width: `${columnWidthPercent}%`,
                top: `${(dayStartTime - minHour) * hourHeight}px`,
                height: `${(dayAvail.fromTime - dayStartTime) * hourHeight}px`,
              }}
              aria-hidden="true"
            />
          );
        }

        // Overlay after teacher's end time
        if (dayAvail.untilTime < maxHour) {
          overlays.push(
            <div
              key={`unavail-after-${dayAvail.dayOfWeek}`}
              className="absolute bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                left: `${(columnIndex / DAYS_IN_WEEK) * 100}%`,
                width: `${columnWidthPercent}%`,
                top: `${(dayAvail.untilTime - minHour) * hourHeight}px`,
                height: `${(dayEndTime - dayAvail.untilTime) * hourHeight}px`,
              }}
              aria-hidden="true"
            />
          );
        }

        return overlays;
      })}

      {/* Events */}
      {validEvents.map((e) => {
        const eventDate = getDateFromDateTime(e.startDateTime);
        const dayIndex = eventDate.getDay() - 1 === -1 ? 6 : eventDate.getDay() - 1; // Adjusting so Monday=0, Sunday=6

        // Skip events that don't fall within the current week's date range
        if (dayIndex === -1) return null;

        // Verify that the event's date actually matches one of the dates in the dates array
        const isInDateRange = dates.some((date) => isSameDay(date, eventDate));
        if (!isInDateRange) return null;

        const eventKey = `${e.id}`;
        const layout = eventLayouts.get(eventKey);

        return (
          <EventItem
            key={eventKey}
            event={e}
            dayIndex={dayIndex}
            hourHeight={hourHeight}
            minHour={minHour}
            colorScheme={colorScheme}
            layout={layout}
          />
        );
      })}
    </div>
  );
};

export const EventsGrid = React.memo(EventsGridComponent);
EventsGrid.displayName = 'EventsGrid';
