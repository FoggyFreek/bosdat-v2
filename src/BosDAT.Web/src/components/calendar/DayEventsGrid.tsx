import React, { useState, useCallback, useMemo, useRef } from 'react';
import { cn } from '@/lib/utils';
import { EventItem } from './EventItem';
import type { CalendarEvent, ColorScheme, TimeSlot, DayAvailability } from './types';
import { getDateFromDateTime, isSameDay, isValidEventTime } from '@/lib/datetime-helpers';
import { calculateEventLayout } from './eventOverlapUtils';

type DayEventsGridProps = {
  hours: number[];
  events: CalendarEvent[];
  hourHeight: number;
  minHour: number;
  maxHour: number;
  dayStartTime: number;
  dayEndTime: number;
  colorScheme?: ColorScheme;
  onTimeslotClick?: (timeslot: TimeSlot) => void;
  selectedDate: Date;
  availability?: DayAvailability[];
  onEventClick?: (event: CalendarEvent) => void;
};

const MINUTES_PER_HOUR = 60;
const MINUTE_INTERVAL = 10;
const MINUTE_MARKS = [0, 10, 20, 30, 40, 50] as const;

const DayEventsGridComponent: React.FC<DayEventsGridProps> = ({
  hours,
  events,
  hourHeight,
  minHour,
  maxHour,
  dayStartTime,
  dayEndTime,
  colorScheme,
  onTimeslotClick,
  selectedDate,
  availability,
  onEventClick,
}) => {
  const totalHeight = useMemo(() => hours.length * hourHeight, [hours.length, hourHeight]);
  const [hoveredSlot, setHoveredSlot] = useState<{ hour: number; minute: number } | null>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  const getTimeslotFromEvent = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!gridRef.current) return null;

      const rect = gridRef.current.getBoundingClientRect();
      const y = e.clientY - rect.top;

      const totalMinutesFromTop = (y / hourHeight) * MINUTES_PER_HOUR;
      const hour = minHour + Math.floor(totalMinutesFromTop / MINUTES_PER_HOUR);
      const minute = Math.floor((totalMinutesFromTop % MINUTES_PER_HOUR) / MINUTE_INTERVAL) * MINUTE_INTERVAL;

      if (hour < minHour || hour > maxHour) return null;

      return { hour, minute };
    },
    [hourHeight, minHour, maxHour]
  );

  const handleGridClick = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!onTimeslotClick) return;

      const timeslot = getTimeslotFromEvent(e);
      if (timeslot) {
        onTimeslotClick({
          date: selectedDate,
          hour: timeslot.hour,
          minute: timeslot.minute,
        });
      }
    },
    [onTimeslotClick, getTimeslotFromEvent, selectedDate]
  );

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

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLDivElement>) => {
      if (!onTimeslotClick || !hoveredSlot) return;

      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        onTimeslotClick({
          date: selectedDate,
          hour: hoveredSlot.hour,
          minute: hoveredSlot.minute,
        });
      }
    },
    [onTimeslotClick, hoveredSlot, selectedDate]
  );

  // Filter events for the selected day only
  const dayEvents = useMemo(
    () =>
      events
        .filter((event) => isValidEventTime(event.startDateTime, event.endDateTime))
        .filter((value, index, self) => index === self.findIndex((t) => t.id === value.id))
        .filter((event) => {
          const eventDate = getDateFromDateTime(event.startDateTime);
          return isSameDay(eventDate, selectedDate);
        }),
    [events, selectedDate]
  );

  const eventLayouts = useMemo(() => calculateEventLayout(dayEvents), [dayEvents]);

  // Get availability for the selected day
  const dayAvailability = useMemo(() => {
    if (!availability) return null;
    const dayNumber = selectedDate.getDay();
    const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const;
    const dayName = dayNames[dayNumber];
    return availability.find((a) => a.dayOfWeek === dayName) ?? null;
  }, [availability, selectedDate]);

  return (
    <div
      ref={gridRef}
      className="relative cursor-pointer"
      style={{ height: `${totalHeight}px` }}
      onClick={handleGridClick}
      onMouseMove={handleGridMouseMove}
      onMouseLeave={handleGridMouseLeave}
      onKeyDown={handleKeyDown}
      role="grid"
      aria-label="Day calendar grid"
      tabIndex={0}
    >
      {/* Horizontal Grid Lines */}
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

      {/* Hover Indicator */}
      {hoveredSlot && (
        <div
          className="absolute bg-blue-200 opacity-50 pointer-events-none z-3 left-0 right-0"
          style={{
            top: `${(hoveredSlot.hour - minHour + hoveredSlot.minute / MINUTES_PER_HOUR) * hourHeight}px`,
            height: `${(MINUTE_INTERVAL / MINUTES_PER_HOUR) * hourHeight}px`,
          }}
          aria-hidden="true"
        />
      )}

      {/* Unavailable Time Overlay - Before Day Start */}
      {dayStartTime > minHour && (
        <div
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-b border-gray-300 pointer-events-none"
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
          className="absolute left-0 right-0 bg-gray-300 opacity-60 border-t border-gray-300 pointer-events-none"
          style={{
            top: `${(dayEndTime - minHour) * hourHeight}px`,
            height: `${(maxHour - dayEndTime + 1) * hourHeight}px`,
          }}
          aria-label="Unavailable time after working hours"
        />
      )}

      {/* Teacher Availability Overlay */}
      {dayAvailability && (() => {
        const isFullDayUnavailable = dayAvailability.fromTime === 0 && dayAvailability.untilTime === 0;

        if (isFullDayUnavailable) {
          return (
            <div
              className="absolute left-0 right-0 bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                top: `${(dayStartTime - minHour) * hourHeight}px`,
                height: `${(dayEndTime - dayStartTime) * hourHeight}px`,
              }}
              aria-label="Unavailable day"
            />
          );
        }

        const overlays = [];
        if (dayAvailability.fromTime > minHour) {
          overlays.push(
            <div
              key="unavail-before"
              className="absolute left-0 right-0 bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                top: `${(dayStartTime - minHour) * hourHeight}px`,
                height: `${(dayAvailability.fromTime - dayStartTime) * hourHeight}px`,
              }}
              aria-hidden="true"
            />
          );
        }
        if (dayAvailability.untilTime < maxHour) {
          overlays.push(
            <div
              key="unavail-after"
              className="absolute left-0 right-0 bg-gray-300 opacity-60 pointer-events-none z-2"
              style={{
                top: `${(dayAvailability.untilTime - minHour) * hourHeight}px`,
                height: `${(dayEndTime - dayAvailability.untilTime) * hourHeight}px`,
              }}
              aria-hidden="true"
            />
          );
        }
        return overlays;
      })()}

      {/* Events - render as single-column (dayIndex=0 in a 1-column grid) */}
      {dayEvents.map((e) => {
        const eventKey = `${e.id}`;
        const layout = eventLayouts.get(eventKey);

        return (
          <EventItem
            key={eventKey}
            event={e}
            dayIndex={0}
            totalDays={1}
            hourHeight={hourHeight}
            minHour={minHour}
            colorScheme={colorScheme}
            layout={layout}
            onEventClick={onEventClick}
          />
        );
      })}
    </div>
  );
};

export const DayEventsGrid = React.memo(DayEventsGridComponent);
DayEventsGrid.displayName = 'DayEventsGrid';
