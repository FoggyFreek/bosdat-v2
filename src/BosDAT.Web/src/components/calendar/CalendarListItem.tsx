import React, { useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Clock, MapPin, Users, ArrowRightLeft, Ban } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { CalendarEvent, ColorScheme, EventColors, EventCategory, CalendarListAction } from './types';
import { formatTimeRange } from '@/lib/datetime-helpers';

type CalendarListItemProps = {
  readonly event: Readonly<CalendarEvent>;
  readonly colorScheme?: ColorScheme;
  readonly onAction?: (event: CalendarEvent, action: CalendarListAction) => void;
};

const DEFAULT_COLORS: Partial<Record<EventCategory, EventColors>> = {
  course: { background: '#eff6ff', border: '#3b82f6', textBackground: '#dbeafe' },
  workshop: { background: '#f0fdf4', border: '#22c55e', textBackground: '#dcfce7' },
  trial: { background: '#fff7ed', border: '#f97316', textBackground: '#ffedd5' },
  holiday: { background: '#fee2e2', border: '#991b1b', textBackground: '#fecaca' },
  absence: { background: '#fefce8', border: '#ca8a04', textBackground: '#fef08a' },
};

const FALLBACK_COLORS: EventColors = { background: '#f3f4f6', border: '#9ca3af', textBackground: '#e5e7eb' };

const getInitials = (name: string): string =>
  name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

const CalendarListItemComponent: React.FC<CalendarListItemProps> = ({
  event,
  colorScheme,
  onAction,
}) => {
  const { t } = useTranslation();

  const colors = useMemo(
    () => colorScheme?.[event.eventType] ?? DEFAULT_COLORS[event.eventType] ?? FALLBACK_COLORS,
    [colorScheme, event.eventType]
  );

  const isScheduled = event.status === 'Scheduled';
  const isActionable = isScheduled && event.eventType !== 'holiday' && event.eventType !== 'absence';

  const handleCancel = useCallback(() => {
    onAction?.(event, 'cancel');
  }, [onAction, event]);

  const handleMove = useCallback(() => {
    onAction?.(event, 'move');
  }, [onAction, event]);

  return (
    <div
      className={cn(
        'rounded-lg border-l-4 p-4 transition-colors',
        'bg-white shadow-sm hover:shadow-md'
      )}
      style={{
        borderLeftColor: colors.border,
        backgroundColor: colors.background,
      }}
    >
      <div className="flex items-start justify-between gap-3">
        {/* Main Content */}
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-slate-800 truncate">{event.title}</h3>

          <div className="mt-2 space-y-1.5">
            {/* Time */}
            <div className="flex items-center text-sm text-slate-600">
              <Clock className="w-4 h-4 mr-2 shrink-0" aria-hidden="true" />
              <span>{formatTimeRange(event.startDateTime, event.endDateTime)}</span>
            </div>

            {/* Room */}
            {event.room && (
              <div className="flex items-center text-sm text-slate-600">
                <MapPin className="w-4 h-4 mr-2 shrink-0" aria-hidden="true" />
                <span>{event.room}</span>
              </div>
            )}

            {/* Attendees */}
            {event.attendees.length > 0 && (
              <div className="flex items-center text-sm text-slate-600">
                <Users className="w-4 h-4 mr-2 shrink-0" aria-hidden="true" />
                <div className="flex items-center gap-1.5">
                  {event.attendees.map((attendee) => (
                    <div
                      key={attendee}
                      className="w-6 h-6 rounded-full bg-white border border-slate-300 flex items-center justify-center text-[10px] font-medium text-slate-700"
                      title={attendee}
                      aria-label={attendee}
                    >
                      {getInitials(attendee)}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Badges */}
          <div className="flex flex-wrap gap-1.5 mt-2.5">
            <span
              className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
              style={{ backgroundColor: colors.textBackground, color: colors.border }}
            >
              {event.eventType}/{event.status}
            </span>
            <span
              className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
              style={{ backgroundColor: colors.textBackground, color: colors.border }}
            >
              {event.frequency}
            </span>
          </div>
        </div>

        {/* Actions */}
        {isActionable && onAction && (
          <div className="flex flex-col gap-1 shrink-0">
            <button
              type="button"
              className={cn(
                'p-2 rounded-md text-slate-500 hover:text-slate-700 hover:bg-slate-100',
                'focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:ring-offset-1',
                'transition-colors'
              )}
              onClick={handleMove}
              title={t('lessons.moveLesson')}
              aria-label={t('lessons.moveLesson')}
            >
              <ArrowRightLeft className="w-4 h-4" />
            </button>
            <button
              type="button"
              className={cn(
                'p-2 rounded-md text-slate-500 hover:text-red-600 hover:bg-red-50',
                'focus:outline-hidden focus:ring-2 focus:ring-red-500 focus:ring-offset-1',
                'transition-colors'
              )}
              onClick={handleCancel}
              title={t('lessons.cancelLesson')}
              aria-label={t('lessons.cancelLesson')}
            >
              <Ban className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export const CalendarListItem = React.memo(CalendarListItemComponent);
CalendarListItem.displayName = 'CalendarListItem';
