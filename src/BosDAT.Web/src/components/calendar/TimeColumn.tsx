import React from 'react';

type TimeColumnProps = {
  hours: number[];
  hourHeight: number;
};

const formatHour = (hour: number): string => `${String(hour).padStart(2, '0')}:00`;

const TimeColumnComponent: React.FC<TimeColumnProps> = ({ hours, hourHeight }) => {
  return (
    <div className="border-r border-slate-100" aria-label="Time labels">
      {hours.map((hour) => {
        const formattedHour = formatHour(hour);
        return (
          <div
            key={hour}
            className="text-right pr-4 text-xs text-slate-600"
            style={{ height: `${hourHeight}px` }}
          >
            {formattedHour}
          </div>
        );
      })}
    </div>
  );
};

export const TimeColumn = React.memo(TimeColumnComponent);
TimeColumn.displayName = 'TimeColumn';
