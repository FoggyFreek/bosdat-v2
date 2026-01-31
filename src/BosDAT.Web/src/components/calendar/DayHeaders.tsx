import React, { useCallback } from 'react';
import { cn } from '@/lib/utils';
import { isSameDay } from './utils';

type DayHeadersProps = {
  dates: Date[];
  highlightedDate?: Date;
  onDateSelect?: (date: Date) => void;
};

// Move constant outside component to prevent recreation on every render
const DAY_LABELS = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'] as const;

type DayHeaderContentProps = {
  dayLabel: string;
  dayNumber: number;
};

const DayHeaderContent: React.FC<DayHeaderContentProps> = ({ dayLabel, dayNumber }) => (
  <>
    <div className="text-xs font-bold text-slate-500 mb-2">{dayLabel}</div>
    <div className="text-2xl font-light text-slate-700">{dayNumber}</div>
  </>
);

const DayHeadersComponent: React.FC<DayHeadersProps> = ({ dates, highlightedDate, onDateSelect }) => {
  const handleDateClick = useCallback(
    (date: Date) => {
      onDateSelect?.(date);
    },
    [onDateSelect]
  );

  const isClickable = !!onDateSelect;

  return (
    <div className="grid grid-cols-[80px_1fr] border-b">
      <div className="bg-white" aria-hidden="true" />
      <div className="grid grid-cols-7">
        {dates.map((date, index) => {
          const isHighlighted = highlightedDate ? isSameDay(date, highlightedDate) : false;
          const dayLabel = DAY_LABELS[index];
          const dayNumber = date.getDate();

          const className = cn(
            'py-4 text-center transition-colors',
            isHighlighted && 'bg-sky text-primary-foreground',
            isClickable && 'hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2'
          );

          if (isClickable) {
            return (
              <button
                key={date.toISOString()}
                type="button"
                className={className}
                aria-label={`${dayLabel} ${dayNumber}`}
                onClick={() => handleDateClick(date)}
              >
                <DayHeaderContent dayLabel={dayLabel} dayNumber={dayNumber} />
              </button>
            );
          }

          return (
            <div
              key={date.toISOString()}
              className={className}
              aria-label={`${dayLabel} ${dayNumber}`}
            >
              <DayHeaderContent dayLabel={dayLabel} dayNumber={dayNumber} />
            </div>
          );
        })}
      </div>
    </div>
  );
};

export const DayHeaders = React.memo(DayHeadersComponent);
DayHeaders.displayName = 'DayHeaders';
