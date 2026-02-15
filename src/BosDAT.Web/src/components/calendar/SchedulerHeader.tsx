import React, { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ViewSelector } from './ViewSelector';
import { getWeekStart } from '@/lib/datetime-helpers';
import type { CalendarView } from './types';

type SchedulerHeaderProps = {
  initialDate?: Date;
  initialView?: CalendarView;
  onDateChange?: (date: Date) => void;
  onViewChange?: (view: CalendarView) => void;
  showViewSelector?: boolean;
};

const NavigationButton: React.FC<{
  onClick?: () => void;
  ariaLabel: string;
  children: React.ReactNode;
}> = ({ onClick, ariaLabel, children }) => (
  <button
    type="button"
    className={cn(
      'w-8 h-8 flex items-center justify-center border-0 rounded',
      'focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
      'transition-colors',
      onClick
        ? 'cursor-pointer text-slate-600 hover:text-slate-900 hover:bg-slate-100'
        : 'cursor-not-allowed text-slate-300'
    )}
    onClick={onClick}
    disabled={!onClick}
    aria-label={ariaLabel}
  >
    {children}
  </button>
);

const formatDateDisplay = (date: Date): string =>
  date.toLocaleDateString('nl-NL', { day: 'numeric', month: 'short' });

const formatDayViewTitle = (date: Date): string =>
  date.toLocaleDateString('nl-NL', { weekday: 'long', day: 'numeric', month: 'long' });

const SchedulerHeaderComponent: React.FC<SchedulerHeaderProps> = ({
  initialDate,
  initialView = 'week',
  onDateChange,
  onViewChange,
  showViewSelector = true,
}) => {
  const { t } = useTranslation();

  // Internal state
  const [currentDate, setCurrentDate] = useState(() => {
    const date = initialDate || new Date();
    // Only normalize to week start for week view
    return initialView === 'week' ? getWeekStart(date) : date;
  });
  const [currentView, setCurrentView] = useState<CalendarView>(initialView);

  // Calculate week end and week number
  const weekEnd = useMemo(() => {
    const end = new Date(currentDate);
    end.setDate(end.getDate() + 6);
    return end;
  }, [currentDate]);

  const weekNumber = useMemo(() =>
    Math.ceil((currentDate.getDate() - currentDate.getDay() + 1) / 7),
    [currentDate]
  );

  // Generate title based on view
  const title = useMemo(() => {
    if (currentView === 'week') {
      return t('schedule.week', { number: weekNumber });
    }
    if (currentView === 'day') {
      return formatDayViewTitle(currentDate);
    }
    if (currentView === 'list') {
      return formatDayViewTitle(currentDate);
    }
    return t('schedule.title');
  }, [currentView, weekNumber, currentDate, t]);

  // Navigation handlers
  const handleNavigatePrevious = useCallback(() => {
    const newDate = new Date(currentDate);

    if (currentView === 'week') {
      newDate.setDate(newDate.getDate() - 7);
    } else if (currentView === 'day') {
      newDate.setDate(newDate.getDate() - 1);
    } else {
      // list view - go to previous day
      newDate.setDate(newDate.getDate() - 1);
    }

    setCurrentDate(newDate);
    onDateChange?.(newDate);
  }, [currentDate, currentView, onDateChange]);

  const handleNavigateNext = useCallback(() => {
    const newDate = new Date(currentDate);

    if (currentView === 'week') {
      newDate.setDate(newDate.getDate() + 7);
    } else if (currentView === 'day') {
      newDate.setDate(newDate.getDate() + 1);
    } else {
      // list view - go to next day
      newDate.setDate(newDate.getDate() + 1);
    }

    setCurrentDate(newDate);
    onDateChange?.(newDate);
  }, [currentDate, currentView, onDateChange]);

  const handleViewChange = useCallback((view: CalendarView) => {
    setCurrentView(view);
    onViewChange?.(view);
  }, [onViewChange]);

  // Date range subtitle (for week view)
  const dateRangeSubtitle = useMemo(() => {
    if (currentView === 'week') {
      return `${formatDateDisplay(currentDate)} - ${formatDateDisplay(weekEnd)}`;
    }
    return null;
  }, [currentView, currentDate, weekEnd]);

  return (
    <header
      className="flex items-center justify-between p-6"
      role="toolbar"
      aria-label={t('calendar.navigation.calendarNavigation')}
    >
      <div className="flex items-center space-x-8">
        <div className="flex space-x-4">
          <NavigationButton onClick={handleNavigatePrevious} ariaLabel={t('calendar.navigation.previous')}>
            <ChevronLeft className="w-6 h-6" />
          </NavigationButton>
          <NavigationButton onClick={handleNavigateNext} ariaLabel={t('calendar.navigation.next')}>
            <ChevronRight className="w-6 h-6" />
          </NavigationButton>
        </div>
        <div>
          <h1 className="text-2xl font-normal text-slate-800">{title}</h1>
          {dateRangeSubtitle && (
            <p className="text-sm text-muted-foreground">{dateRangeSubtitle}</p>
          )}
        </div>
      </div>
      {showViewSelector && (
        <ViewSelector view={currentView} onViewChange={handleViewChange} />
      )}
    </header>
  );
};

export const SchedulerHeader = React.memo(SchedulerHeaderComponent);
SchedulerHeader.displayName = 'SchedulerHeader';
