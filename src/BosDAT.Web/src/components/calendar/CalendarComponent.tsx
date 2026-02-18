import React, { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { SchedulerProps } from './types';
import { SchedulerHeader } from './SchedulerHeader';
import { DayHeaders } from './DayHeaders';
import { TimeColumn } from './TimeColumn';
import { EventsGrid } from './EventsGrid';
import { DayEventsGrid } from './DayEventsGrid';
import { CalendarListView } from './CalendarListView';
import { isSameDay } from '@/lib/datetime-helpers';

// Standard hours displayed - moved outside component to prevent recreation on every render
const HOURS = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22];
const MIN_HOUR = Math.min(...HOURS);
const MAX_HOUR = Math.max(...HOURS);

const DAY_LABEL_KEYS = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'] as const;

export const CalendarComponent: React.FC<SchedulerProps> = ({
  events,
  dates,
  dayStartTime = 9,
  dayEndTime = 21,
  hourHeight = 100,
  colorScheme,
  onDateChange,
  onTimeslotClick,
  onDateSelect,
  highlightedDate,
  availability,
  initialView = 'week',
  onViewChange,
  showViewSelector = true,
  selectedDate,
  onEventAction,
  initialDate,
  onEventClick,
}) => {
  const { t } = useTranslation();

  // For day/list views, determine which date to show
  const activeDayDate = useMemo(() => {
    if (selectedDate) return selectedDate;
    if (highlightedDate) return highlightedDate;
    // Default to the first date in the week that matches today, or just the first date
    const today = new Date();
    const todayMatch = dates.find((d) => isSameDay(d, today));
    return todayMatch ?? dates[0];
  }, [selectedDate, highlightedDate, dates]);

  // Format the day header label for day/list views
  const dayHeaderLabel = useMemo(() => {
    if (!activeDayDate) return '';
    const dayKey = DAY_LABEL_KEYS[activeDayDate.getDay()];
    return `${t(`calendar.days.${dayKey}`)} ${activeDayDate.getDate()}`;
  }, [activeDayDate, t]);

  const ariaLabelMap: Record<string, string> = {
    week: 'Weekly calendar scheduler',
    day: 'Daily calendar scheduler',
    list: 'Calendar list view',
  };
  const ariaLabel = ariaLabelMap[initialView] ?? 'Calendar list view';

  return (
    <div
      className="flex flex-col h-full bg-white text-slate-600 font-sans"
      role="application"
      aria-label={ariaLabel}
    >
      <SchedulerHeader
        initialDate={initialDate}
        initialView={initialView}
        onDateChange={onDateChange}
        onViewChange={onViewChange}
        showViewSelector={showViewSelector}
      />

      {/* Week View */}
      {initialView === 'week' && (
        <div className="flex flex-col flex-1 overflow-x-visible overflow-y-hidden">
          <DayHeaders dates={dates} highlightedDate={highlightedDate} onDateSelect={onDateSelect} />

          <div className="flex-1 overflow-y-auto relative">
            <div className="grid grid-cols-[80px_1fr] min-h-full">
              <TimeColumn hours={HOURS} hourHeight={hourHeight} />
              <EventsGrid
                hours={HOURS}
                events={events}
                hourHeight={hourHeight}
                minHour={MIN_HOUR}
                maxHour={MAX_HOUR}
                availability={availability}
                dayStartTime={dayStartTime}
                dayEndTime={dayEndTime}
                colorScheme={colorScheme}
                onTimeslotClick={onTimeslotClick}
                highlightedDate={highlightedDate}
                dates={dates}
                onEventClick={onEventClick}
              />
            </div>
          </div>
        </div>
      )}

      {/* Day View */}
      {initialView === 'day' && activeDayDate && (
        <div className="flex flex-col flex-1 overflow-x-visible overflow-y-hidden">
          {/* Single Day Header */}
          <div className="border-b">
            <div className="grid grid-cols-[80px_1fr]">
              <div className="bg-white" aria-hidden="true" />
              <div className="py-4 text-center bg-sky-100/60">
                <div className="text-xs font-bold text-slate-500 mb-2">{dayHeaderLabel.split(' ')[0]}</div>
                <div className="text-2xl font-light text-slate-700">{activeDayDate.getDate()}</div>
              </div>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto relative">
            <div className="grid grid-cols-[80px_1fr] min-h-full">
              <TimeColumn hours={HOURS} hourHeight={hourHeight} />
              <DayEventsGrid
                hours={HOURS}
                events={events}
                hourHeight={hourHeight}
                minHour={MIN_HOUR}
                maxHour={MAX_HOUR}
                availability={availability}
                dayStartTime={dayStartTime}
                dayEndTime={dayEndTime}
                colorScheme={colorScheme}
                onTimeslotClick={onTimeslotClick}
                selectedDate={activeDayDate}
                onEventClick={onEventClick}
              />
            </div>
          </div>
        </div>
      )}

      {/* List View */}
      {initialView === 'list' && activeDayDate && (
        <div className="flex flex-col flex-1 overflow-y-hidden">
          {/* Single Day Header */}
          <div className="border-b px-6 py-4">
            <h2 className="text-lg font-medium text-slate-700">
              {dayHeaderLabel}
            </h2>
          </div>

          <div className="flex-1 overflow-y-auto">
            <CalendarListView
              events={events}
              selectedDate={activeDayDate}
              colorScheme={colorScheme}
              onEventAction={onEventAction}
            />
          </div>
        </div>
      )}
    </div>
  );
};

CalendarComponent.displayName = 'CalendarComponent';
