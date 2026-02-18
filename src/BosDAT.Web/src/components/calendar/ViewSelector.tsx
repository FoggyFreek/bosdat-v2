import React from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarDays, Clock, List } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { CalendarView } from './types';
import { calendarViewTranslations } from './types';

type ViewSelectorProps = {
  view: CalendarView;
  onViewChange: (view: CalendarView) => void;
};

const VIEW_OPTIONS: { value: CalendarView; icon: typeof CalendarDays }[] = [
  { value: 'week', icon: CalendarDays },
  { value: 'day', icon: Clock },
  { value: 'list', icon: List },
];

const ViewSelectorComponent: React.FC<ViewSelectorProps> = ({ view, onViewChange }) => {
  const { t } = useTranslation();

  return (
    <div
      className="inline-flex rounded-md border border-slate-200 bg-slate-50 p-0.5"
      role="tablist"
      aria-label={t('calendar.views.label')}
    >
      {VIEW_OPTIONS.map(({ value, icon: Icon }) => (
        <button
          key={value}
          type="button"
          role="tab"
          aria-selected={view === value}
          className={cn(
            'inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded transition-colors',
            'focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:ring-offset-1',
            view === value
              ? 'bg-white text-slate-900 shadow-sm'
              : 'text-slate-500 hover:text-slate-700 hover:bg-slate-100'
          )}
          onClick={() => onViewChange(value)}
        >
          <Icon className="w-3.5 h-3.5" aria-hidden="true" />
          <span>{t(calendarViewTranslations[value])}</span>
        </button>
      ))}
    </div>
  );
};

export const ViewSelector = React.memo(ViewSelectorComponent);
ViewSelector.displayName = 'ViewSelector';
