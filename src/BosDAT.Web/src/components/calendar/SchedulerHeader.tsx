import React from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ViewSelector } from './ViewSelector';
import type { CalendarView } from './types';

type SchedulerHeaderProps = {
  title: string;
  onNavigatePrevious?: () => void;
  onNavigateNext?: () => void;
  view?: CalendarView;
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

const SchedulerHeaderComponent: React.FC<SchedulerHeaderProps> = ({
  title,
  onNavigatePrevious,
  onNavigateNext,
  view,
  onViewChange,
  showViewSelector = true,
}) => {
  const { t } = useTranslation();

  return (
    <header
      className="flex items-center justify-between p-6"
      role="toolbar"
      aria-label={t('calendar.navigation.calendarNavigation')}
    >
      <div className="flex items-center space-x-8">
        <div className="flex space-x-4">
          <NavigationButton onClick={onNavigatePrevious} ariaLabel={t('calendar.navigation.previous')}>
            <ChevronLeft className="w-6 h-6" />
          </NavigationButton>
          <NavigationButton onClick={onNavigateNext} ariaLabel={t('calendar.navigation.next')}>
            <ChevronRight className="w-6 h-6" />
          </NavigationButton>
        </div>
        <h1 className="text-2xl font-normal text-slate-800">{title}</h1>
      </div>
      {showViewSelector && view && onViewChange && (
        <ViewSelector view={view} onViewChange={onViewChange} />
      )}
    </header>
  );
};

export const SchedulerHeader = React.memo(SchedulerHeaderComponent);
SchedulerHeader.displayName = 'SchedulerHeader';
