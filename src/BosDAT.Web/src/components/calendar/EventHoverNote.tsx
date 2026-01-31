import React from 'react';
import { Clock, MapPin } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { CalendarEvent, EventColors } from './types';
import { formatTimeRange } from './utils';

type EventHoverNoteProps = {
  event: CalendarEvent;
  colors: EventColors;
  isLastColumn: boolean;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
};

const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
};

const EventHoverNoteComponent: React.FC<EventHoverNoteProps> = ({
  event,
  colors,
  isLastColumn,
  onMouseEnter,
  onMouseLeave,
}) => {
  return (
    <div
      className={cn(
        'absolute top-0 z-[9999] bg-white rounded-lg shadow-lg border border-slate-200 p-3 min-w-[200px]',
        isLastColumn ? 'right-full mr-2' : 'left-full ml-2'
      )}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      role="tooltip"
      aria-label={`Details for ${event.title}`}
    >
      {/* Attendees */}
      {event.attendees && event.attendees.length > 0 && (
        <div className="mb-3">
          <div className="text-[10px] font-semibold text-slate-600 mb-2">Attendees</div>
          <div className="flex flex-wrap gap-1.5">
            {event.attendees.map((attendee) => (
              <div
                key={attendee}
                className="w-7 h-7 rounded-full bg-white border-2 border-slate-300 flex items-center justify-center text-[10px] font-medium text-slate-700"
                title={attendee}
                aria-label={attendee}
              >
                {getInitials(attendee)}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Time */}
      <div className="flex items-center text-[11px] text-slate-600 mb-2">
        <Clock className="w-3.5 h-3.5 mr-1.5" aria-hidden="true" />
        <span>{formatTimeRange(event.startDateTime, event.endDateTime)}</span>
      </div>

      {/* Room */}
      {event.room && (
        <div className="flex items-center text-[11px] text-slate-600 mb-2">
          <MapPin className="w-3.5 h-3.5 mr-1.5" aria-hidden="true" />
          <span>{event.room}</span>
        </div>
      )}

      {/* Labels */}
      <div className="flex gap-2 mt-2">
        <span
          className="inline-block px-2 py-1 rounded text-[10px] font-medium"
          style={{ backgroundColor: colors.textBackground, color: colors.border }}
        >
          {event.eventType}
        </span>
        <span
          className="inline-block px-2 py-1 rounded text-[10px] font-medium"
          style={{ backgroundColor: colors.textBackground, color: colors.border }}
        >
          {event.frequency}
        </span>
      </div>
    </div>
  );
};

export const EventHoverNote = React.memo(EventHoverNoteComponent);
EventHoverNote.displayName = 'EventHoverNote';
