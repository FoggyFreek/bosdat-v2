import { useState, useMemo, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CalendarComponent } from '@/components'
import type { CalendarEvent, ColorScheme } from '@/components'
import type { CalendarView, DayAvailability } from '@/components/calendar/types'
import { calendarApi } from '@/features/schedule/api'
import { teachersApi } from '@/features/teachers/api'
import { roomsApi } from '@/features/rooms/api'
import type { Holiday, WeekCalendar, TeacherAbsence } from '@/features/schedule/types'
import { groupLessonsByCourseAndDate, type GroupedLesson } from '@/features/schedule/utils/groupLessons'
import type { TeacherAvailability, TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import { getWeekStart, getWeekDays, formatDateForApi, combineDateAndTime, getHoursFromTimeString, getDateFromDateTime } from '@/lib/datetime-helpers'
import { LessonDetailPanel } from '@/features/lessons/components/LessonDetailPanel'

// --- Constants ---

const statusColorScheme: ColorScheme = {
  Scheduled: {
    background: '#dbeafe',
    border: '#3b82f6',
    textBackground: '#eff6ff',
  },
  Completed: {
    background: '#dcfce7',
    border: '#22c55e',
    textBackground: '#f0fdf4',
  },
  Cancelled: {
    background: '#fee2e2',
    border: '#ef4444',
    textBackground: '#fef2f2',
  },
  NoShow: {
    background: '#fed7aa',
    border: '#f97316',
    textBackground: '#fff7ed',
  },
}

// --- Helpers ---
function splitHolidays(holiday: Holiday): Holiday[] {
  const flattenedHolidays: Holiday[] = [];

    const start = new Date(holiday.startDate);
    const end = new Date(holiday.endDate);

    // Create a tracking date starting at the holiday's beginning
    const current = new Date(start);

    while (current <= end) {
      const dayStart = new Date(current);
      const dayEnd = new Date(current);

      // 1. Determine the start time for THIS specific day
      // If it's the first day, use the original start time. Otherwise, 00:00:00.
      if (current.toDateString() === start.toDateString()) {
        dayStart.setHours(start.getHours(), start.getMinutes(), start.getSeconds());
      } else {
        dayStart.setHours(0, 0, 0, 0);
      }

      // 2. Determine the end time for THIS specific day
      // If it's the last day, use the original end time. Otherwise, 23:59:59.
      if (current.toDateString() === end.toDateString()) {
        dayEnd.setHours(end.getHours(), end.getMinutes(), end.getSeconds());
      } else {
        dayEnd.setHours(23, 59, 59, 999);
      }

      flattenedHolidays.push({
        ...holiday,
        id: new Date(current).getDate(),
        startDate: formatDateForApi(dayStart),
        endDate: formatDateForApi(dayEnd),
      });

      // Move to the next day
      current.setDate(current.getDate() + 1);
      current.setHours(0, 0, 0, 0); // Reset to start of next day for comparison
    }

  return flattenedHolidays;
}

const convertHolidaysToEvents = (holidays: Holiday[]): CalendarEvent[] => {

  const allHolidays = holidays.flatMap(splitHolidays)

  const holidayEvents: CalendarEvent[] = allHolidays.map(holiday => ({
    id: holiday.id.toString(),
    startDateTime: combineDateAndTime(new Date(holiday.startDate), '08:00'),
    endDateTime: combineDateAndTime(new Date(holiday.endDate), '22:00'),
    title: holiday.name,
    frequency: 'once',
    attendees: [],
    eventType: 'holiday',
    status: 'Scheduled',
    roomId: undefined,
  }))

  return holidayEvents
}

const convertGroupedLessonToEvent = (group: GroupedLesson): CalendarEvent => {
  const attendees = [...group.studentNames]
  if (group.teacherName) attendees.push(group.teacherName)

  // Use courseId + date as the event ID for grouped lessons
  const eventId = group.lessons.length > 1
    ? `${group.courseId}:${group.date}`
    : group.lessons[0]?.id ?? `${group.courseId}:${group.date}`

  const workshopOrCourse = group.isWorkshop ? 'workshop' : 'course'
  const eventType = group.isTrial ? 'trial' : workshopOrCourse

  return {
    id: eventId,
    startDateTime: combineDateAndTime(new Date(group.date), group.startTime),
    endDateTime: combineDateAndTime(new Date(group.date), group.endTime),
    title: group.title + (group.isTrial ? ' (Trail)' : ''),
    frequency: group.frequency,
    eventType,
    status: group.status,
    attendees,
    room: group.roomName,
  }
}

const convertTeacherAbsencesToEvents = (absences: TeacherAbsence[]): CalendarEvent[] => {
  const allEvents: CalendarEvent[] = []

  for (const absence of absences) {
    const start = new Date(absence.startDate)
    const end = new Date(absence.endDate)
    const current = new Date(start)

    while (current <= end) {
      const dayStart = new Date(current)
      const dayEnd = new Date(current)
      dayStart.setHours(8, 0, 0, 0)
      dayEnd.setHours(22, 0, 0, 0)

      allEvents.push({
        id: `absence-${absence.id}-${current.toISOString().split('T')[0]}`,
        startDateTime: dayStart,
        endDateTime: dayEnd,
        title: `${absence.personName ?? 'Teacher'} - ${absence.reason}`,
        frequency: 'once',
        attendees: absence.personName ? [absence.personName] : [],
        eventType: 'holiday',
        status: 'Cancelled',
        roomId: undefined,
      })

      current.setDate(current.getDate() + 1)
      current.setHours(0, 0, 0, 0)
    }
  }

  return allEvents
}

const mapToDayAvailability = (availability: TeacherAvailability[]): DayAvailability[] =>
  availability.map((item) => ({
    dayOfWeek: item.dayOfWeek,
    fromTime: getHoursFromTimeString(item.fromTime),
    untilTime: getHoursFromTimeString(item.untilTime),
  }))

// --- Component ---

export const SchedulePage = () => {
  const { t } = useTranslation()
  // State
  const [currentDate, setCurrentDate] = useState(() => getWeekStart(new Date()))
  const [selectedDate, setSelectedDate] = useState<Date>(() => new Date())
  const [filterTeacher, setFilterTeacher] = useState<string>('all')
  const [filterRoom, setFilterRoom] = useState<string>('all')
  const [currentView, setCurrentView] = useState<CalendarView>('week')
  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null)

  // Handlers for SchedulerHeader callbacks
  const handleDateChange = (date: Date) => {
    if (currentView === 'week') {
      // For week view, normalize to week start
      setCurrentDate(getWeekStart(date))
      setSelectedDate(date)
    } else {
      // For day/list view, keep exact date and update week if needed
      setSelectedDate(date)
      const weekStart = getWeekStart(date)
      // Only update currentDate if we've moved to a different week
      if (formatDateForApi(weekStart) !== formatDateForApi(currentDate)) {
        setCurrentDate(weekStart)
      }
    }
  }

  const handleViewChange = (view: CalendarView) => {
    setCurrentView(view)
    if (view !== 'day') setSelectedLessonId(null)
  }

  // A single-lesson event has a plain UUID id (no colon separator)
  const handleEventClick = useCallback((event: CalendarEvent) => {
    if (event.eventType === 'holiday' || event.id.includes(':')) return

    const eventDate = getDateFromDateTime(event.startDateTime)
    setSelectedDate(eventDate)
    setCurrentDate(getWeekStart(eventDate))
    setCurrentView('day')
    setSelectedLessonId(event.id)
  }, [])

  // Queries
  const { data: calendarData, isLoading, isFetching, refetch } = useQuery<WeekCalendar>({
    queryKey: ['calendar', formatDateForApi(currentDate), filterTeacher, filterRoom],
    queryFn: () =>
      calendarApi.getWeek({
        date: formatDateForApi(currentDate),
        teacherId: filterTeacher === 'all' ? undefined : filterTeacher,
        roomId: filterRoom === 'all' ? undefined : Number.parseInt(filterRoom),
      }),
  })

  // Show loading state on initial load OR when fetching with no data
  const showLoading = isLoading || (isFetching && !calendarData)

  const { data: teachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active'],
    queryFn: () => teachersApi.getAll({ activeOnly: true }),
  })

  const { data: rooms = [] } = useQuery<Room[]>({
    queryKey: ['rooms', 'active'],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  const { data: teacherAvailability = [] } = useQuery<TeacherAvailability[]>({
    queryKey: ['teacher-availability', filterTeacher],
    queryFn: () =>
      filterTeacher === 'all' ? Promise.resolve([]) : teachersApi.getAvailability(filterTeacher),
  })

  // Derived state
  const weekDays = useMemo(() => getWeekDays(currentDate), [currentDate])

  const events = useMemo(() => {
    const lessons = calendarData?.lessons ?? []
    const holidays = convertHolidaysToEvents(calendarData?.holidays ?? [])
    const teacherAbsenceEvents = convertTeacherAbsencesToEvents(calendarData?.teacherAbsences ?? [])
    const grouped = groupLessonsByCourseAndDate(lessons)

    return [... holidays, ...teacherAbsenceEvents, ...grouped.map(convertGroupedLessonToEvent)]
  }, [calendarData?.lessons, calendarData?.holidays, calendarData?.teacherAbsences])

  const availability = useMemo(
    () => mapToDayAvailability(teacherAvailability),
    [teacherAvailability]
  )

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('schedule.title')}</h1>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Select value={filterTeacher} onValueChange={setFilterTeacher}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder={t('schedule.filters.allTeachers')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t('schedule.filters.allTeachers')}</SelectItem>
              {teachers.map((teacher) => (
                <SelectItem key={teacher.id} value={teacher.id}>
                  {teacher.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={filterRoom} onValueChange={setFilterRoom}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder={t('schedule.filters.allRooms')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t('schedule.filters.allRooms')}</SelectItem>
              {rooms.map((room) => (
                <SelectItem key={room.id} value={room.id.toString()}>
                  {room.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Button variant="outline" size="icon" onClick={() => refetch()}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {showLoading && (
        <div className="flex items-center justify-center py-16">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {!showLoading && (
        <div className={selectedLessonId && currentView === 'day' ? 'flex gap-0 rounded-lg border overflow-hidden h-[calc(100vh-280px)]' : ''}>
          <div className={selectedLessonId && currentView === 'day' ? 'flex-[3] min-w-0 overflow-hidden' : ''}>
            <CalendarComponent
              events={events}
              dates={weekDays}
              initialDate={currentView === 'week' ? currentDate : selectedDate}
              initialView={currentView}
              selectedDate={currentView === 'week' ? undefined : selectedDate}
              colorScheme={statusColorScheme}
              onDateChange={handleDateChange}
              onViewChange={handleViewChange}
              availability={availability}
              dayStartTime={8}
              dayEndTime={21}
              hourHeight={50}
              onEventClick={handleEventClick}
            />
          </div>
          {selectedLessonId && currentView === 'day' && (
            <div className="flex-[2] min-w-0 overflow-hidden">
              <LessonDetailPanel
                lessonId={selectedLessonId}
                onClose={() => setSelectedLessonId(null)}
              />
            </div>
          )}
        </div>
      )}

      {/* Legend */}
      <Card>
        <CardContent className="py-4">
          <div className="flex flex-wrap gap-4 text-sm">
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-blue-100 border border-blue-300" />
              <span>{t('schedule.legend.scheduled')}</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-green-100 border border-green-300" />
              <span>{t('schedule.legend.completed')}</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-red-100 border border-red-300" />
              <span>{t('schedule.legend.cancelled')}</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-orange-100 border border-orange-300" />
              <span>{t('schedule.legend.noShow')}</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-purple-100 border border-purple-300" />
              <span>{t('absences.teacherAbsent')}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
