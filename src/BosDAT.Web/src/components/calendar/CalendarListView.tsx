import React, { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarListItem } from './CalendarListItem';
import type { CalendarEvent, ColorScheme, CalendarListAction } from './types';
import { getDateFromDateTime, isSameDay, isValidEventTime } from '@/lib/datetime-helpers';

type CalendarListViewProps = {
  events: CalendarEvent[];
  selectedDate: Date;
  colorScheme?: ColorScheme;
  onEventAction?: (event: CalendarEvent, action: CalendarListAction) => void;
};

const CalendarListViewComponent: React.FC<CalendarListViewProps> = ({
  events,
  selectedDate,
  colorScheme,
  onEventAction,
}) => {
  const { t } = useTranslation();

  const dayEvents = useMemo(
    () =>
      events
        .filter((event) => isValidEventTime(event.startDateTime, event.endDateTime))
        .filter((value, index, self) => index === self.findIndex((e) => e.id === value.id))
        .filter((event) => {
          const eventDate = getDateFromDateTime(event.startDateTime);
          return isSameDay(eventDate, selectedDate);
        })
        .toSorted((a, b) =>
          new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime()
        ),
    [events, selectedDate]
  );

  if (dayEvents.length === 0) {
    return (
      <div className="flex items-center justify-center py-16 text-slate-500" role="status">
        <p>{t('calendar.list.noEvents')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3 p-4" role="list" aria-label={t('calendar.list.label')}>
      {dayEvents.map((event) => (
        <div key={event.id} role="listitem">
          <CalendarListItem
            event={event}
            colorScheme={colorScheme}
            onAction={onEventAction}
          />
        </div>
      ))}
    </div>
  );
};

export const CalendarListView = React.memo(CalendarListViewComponent);
CalendarListView.displayName = 'CalendarListView';
