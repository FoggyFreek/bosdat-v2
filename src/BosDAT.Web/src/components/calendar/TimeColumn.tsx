import React from 'react';

type TimeColumnProps = {
  hours: number[];
  hourHeight: number;
};

const TimeColumnComponent: React.FC<TimeColumnProps> = ({ hours, hourHeight }) => {
  return (
    <div className="border-r border-slate-100" aria-label="Time labels">
      {hours.map((hour) => (
        <div
          key={hour}
          className="text-right pr-4 text-xs text-slate-600"
          style={{ height: `${hourHeight}px` }}
          aria-label={`${String(hour).padStart(2, '0')}:00`}
        >
          {String(hour).padStart(2, '0')}:00
        </div>
      ))}
    </div>
  );
};

export const TimeColumn = React.memo(TimeColumnComponent);
TimeColumn.displayName = 'TimeColumn';
