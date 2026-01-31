import React from 'react';
import { isSameDay } from './utils';

type DayHeadersProps = {
  dates: Date[];
  highlightedDate?: Date;
  onDateSelect?: (date: Date) => void;
};

// Move constant outside component to prevent recreation on every render
const DAY_LABELS = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

const DayHeadersComponent: React.FC<DayHeadersProps> = ({ dates, highlightedDate, onDateSelect }) => {
  const handleDateClick = (date: Date) => {
    if (onDateSelect) {
      onDateSelect(date);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent, date: Date) => {
    if ((e.key === 'Enter' || e.key === ' ') && onDateSelect) {
      e.preventDefault();
      onDateSelect(date);
    }
  };

  return (
    <div className="grid grid-cols-[80px_1fr] border-b" role="row">
      <div className="bg-white" aria-hidden="true" />
      <div className="grid grid-cols-7" role="rowgroup">
        {dates.map((date, index) => {
          const isHighlighted = highlightedDate ? isSameDay(date, highlightedDate) : false;
          const isClickable = !!onDateSelect;
          return (
            <div
              key={date.toISOString()}
              className={`py-4 text-center transition-colors ${
                isHighlighted ? 'bg-primary text-primary-foreground' : ''
              } ${
                isClickable ? 'cursor-pointer hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2' : ''
              }`}
              role={isClickable ? 'button' : 'columnheader'}
              tabIndex={isClickable ? 0 : undefined}
              aria-label={`${DAY_LABELS[index]} ${date.getDate()}`}
              onClick={() => handleDateClick(date)}
              onKeyDown={(e) => handleKeyDown(e, date)}
            >
              <div className="text-xs font-bold text-slate-500 mb-2">
                {DAY_LABELS[index]}
              </div>
              <div className="text-2xl font-light text-slate-700">{date.getDate()}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export const DayHeaders = React.memo(DayHeadersComponent);
DayHeaders.displayName = 'DayHeaders';
