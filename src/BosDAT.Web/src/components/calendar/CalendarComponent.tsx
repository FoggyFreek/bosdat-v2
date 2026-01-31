import React from 'react';
import type { SchedulerProps } from './types';
import { SchedulerHeader } from './SchedulerHeader';
import { DayHeaders } from './DayHeaders';
import { TimeColumn } from './TimeColumn';
import { EventsGrid } from './EventsGrid';

// Standard hours displayed - moved outside component to prevent recreation on every render
const HOURS = [6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22];
const MIN_HOUR = Math.min(...HOURS);
const MAX_HOUR = Math.max(...HOURS);

export const CalendarComponent: React.FC<SchedulerProps> = ({
  title,
  events,
  dates,
  dayStartTime = 9,
  dayEndTime = 21,
  hourHeight = 100,
  colorScheme,
  onNavigatePrevious,
  onNavigateNext,
  onTimeslotClick,
  onDateSelect,
  highlightedDate,
}) => {
  return (
    <div
      className="flex flex-col h-screen bg-white text-slate-600 font-sans"
      role="application"
      aria-label="Weekly calendar scheduler"
    >
      <SchedulerHeader
        title={title}
        onNavigatePrevious={onNavigatePrevious}
        onNavigateNext={onNavigateNext}
      />

      {/* Calendar Grid Container */}
      <div className="flex flex-col flex-1 overflow-x-visible overflow-y-hidden">
        <DayHeaders dates={dates} highlightedDate={highlightedDate} onDateSelect={onDateSelect} />

        {/* Scrollable Body */}
        <div className="flex-1 overflow-y-auto relative">
          <div className="grid grid-cols-[80px_1fr] min-h-full">
            <TimeColumn hours={HOURS} hourHeight={hourHeight} />
            <EventsGrid
              hours={HOURS}
              events={events}
              hourHeight={hourHeight}
              minHour={MIN_HOUR}
              maxHour={MAX_HOUR}
              dayStartTime={dayStartTime}
              dayEndTime={dayEndTime}
              colorScheme={colorScheme}
              onTimeslotClick={onTimeslotClick}
              highlightedDate={highlightedDate}
              dates={dates}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

CalendarComponent.displayName = 'CalendarComponent';
