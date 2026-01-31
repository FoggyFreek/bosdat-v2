import React, { useState, useCallback, useMemo, useRef } from 'react';
import { EventItem } from './EventItem';
import type { Event, ColorScheme, TimeSlot } from './types';
import { getDateFromDateTime, isSameDay, isValidEventTime } from './utils';

type EventsGridProps = {
  hours: number[];
  events: Event[];
  hourHeight: number;
  minHour: number;
  maxHour: number;
  daystartTime: number;
  dayendTime: number;
  colorScheme?: ColorScheme;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  highlightedDate?: Date;
  dates: Date[];
};

const EventsGridComponent: React.FC<EventsGridProps> = ({
  hours,
  events,
  hourHeight,
  minHour,
  maxHour,
  daystartTime,
  dayendTime,
  colorScheme,
  onTimeslotClick,
  highlightedDate,
  dates,
}) => {
  // Memoize calculations to ensure component is pure and idempotent
  const totalHeight = useMemo(() => hours.length * hourHeight, [hours.length, hourHeight]);

  const [hoveredSlot, setHoveredSlot] = useState<{ day: number; hour: number; minute: number } | null>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  // Calculate timeslot from mouse position using event delegation
  const getTimeslotFromEvent = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (!gridRef.current) return null;

    const rect = gridRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    const dayIndex = Math.floor((x / rect.width) * 7);
    if (dayIndex < 0 || dayIndex >= 7) return null;

    const totalMinutesFromTop = (y / hourHeight) * 60;
    const hour = minHour + Math.floor(totalMinutesFromTop / 60);
    const minute = Math.floor((totalMinutesFromTop % 60) / 10) * 10; // Round to nearest 10 minutes

    if (hour < minHour || hour > maxHour) return null;

    return { day: dayIndex, hour, minute };
  }, [hourHeight, minHour, maxHour]);

  // Use event delegation for click handling
  const handleGridClick = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (!onTimeslotClick) return;

    const timeslot = getTimeslotFromEvent(e);
    if (timeslot && dates[timeslot.day]) {
      onTimeslotClick({
        date: dates[timeslot.day],
        hour: timeslot.hour,
        minute: timeslot.minute
      });
    }
  }, [onTimeslotClick, getTimeslotFromEvent, dates]);

  // Use event delegation for hover handling
  const handleGridMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    const timeslot = getTimeslotFromEvent(e);
    if (timeslot) {
      setHoveredSlot(timeslot);
    }
  }, [getTimeslotFromEvent]);

  const handleGridMouseLeave = useCallback(() => {
    setHoveredSlot(null);
  }, []);

  // Handle keyboard navigation
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLDivElement>) => {
    if (!onTimeslotClick || !hoveredSlot) return;

    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      if (dates[hoveredSlot.day]) {
        onTimeslotClick({
          date: dates[hoveredSlot.day],
          hour: hoveredSlot.hour,
          minute: hoveredSlot.minute
        });
      }
    }
  }, [onTimeslotClick, hoveredSlot, dates]);

  // Filter valid events
  const validEvents = useMemo(() =>
    events.filter(event => isValidEventTime(event.startDateTime, event.endDateTime)),
    [events]
  );

  return (
    <div
      ref={gridRef}
      className="relative grid grid-cols-7 cursor-pointer"
      style={{
        height: `${totalHeight}px`,
        backgroundPosition: '0 0'
      }}
      onClick={handleGridClick}
      onMouseMove={handleGridMouseMove}
      onMouseLeave={handleGridMouseLeave}
      onKeyDown={handleKeyDown}
      role="grid"
      aria-label="Weekly calendar grid"
      tabIndex={0}
    >
      {/* Highlighted Date Column Background */}
      {highlightedDate !== undefined && dates.map((date, dayIndex) => {
        if (isSameDay(date, highlightedDate)) {
          return (
            <div
              key={`highlight-${dayIndex}`}
              className="absolute bg-sky-100 opacity-60 z-[4]"
              style={{
                left: `${(dayIndex / 7) * 100}%`,
                width: `${100 / 7}%`,
                top: 0,
                height: '100%',
              }}
              aria-hidden="true"
            />
          );
        }
        return null;
      })}

      {/* Horizontal Grid Lines - Every 10 Minutes */}
      {hours.map((hour) =>
        [0, 10, 20, 30, 40, 50].map((minute) => (
          <div
            key={`${hour}-${minute}`}
            className={`absolute left-0 right-0 border-t ${minute === 0 ? 'border-slate-400' : 'border-slate-200'
              }`}
            style={{
              top: `${((hour - minHour) + minute / 60) * hourHeight}px`
            }}
            aria-hidden="true"
          />
        )
      ))}

      {/* Vertical Grid Lines */}
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={`grid-line-${i}`} className="border-r border-slate-300 h-full" aria-hidden="true" />
      ))}

      {/* Hover Indicator */}
      {hoveredSlot && (
        <div
          className="absolute bg-blue-200 opacity-50 pointer-events-none z-[3]"
          style={{
            left: `${(hoveredSlot.day / 7) * 100}%`,
            width: `${100 / 7}%`,
            top: `${((hoveredSlot.hour - minHour) + hoveredSlot.minute / 60) * hourHeight}px`,
            height: `${(10 / 60) * hourHeight}px`,
          }}
          aria-hidden="true"
        />
      )}

      {/* Unavailable Time Overlay - Before Day Start */}
      {daystartTime > minHour && (
        <div
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-b border-gray-300 col-span-7 pointer-events-none"
          style={{
            top: 0,
            height: `${(daystartTime - minHour) * hourHeight}px`,
          }}
          aria-label="Unavailable time before working hours"
        />
      )}

      {/* Unavailable Time Overlay - After Day End */}
      {dayendTime < maxHour && (
        <div
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-t border-gray-300 col-span-7 pointer-events-none"
          style={{
            top: `${(dayendTime - minHour) * hourHeight}px`,
            height: `${(maxHour - dayendTime + 1) * hourHeight}px`,
          }}
          aria-label="Unavailable time after working hours"
        />
      )}

      {/* Events */}
      {validEvents.map((event) => {
        // Extract the date from the event's datetime
        const eventDate = getDateFromDateTime(event.startDateTime);

        // Find which day column this event belongs to based on full date comparison
        const dayIndex = dates.findIndex(date => isSameDay(date, eventDate));

        // Skip events that don't fall within the current week's date range
        if (dayIndex === -1) return null;

        return (
          <EventItem
            key={`${event.startDateTime}-${event.title}`}
            event={event}
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
