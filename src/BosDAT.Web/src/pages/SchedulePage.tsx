import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'
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
import type { DayAvailability } from '@/components/calendar/types'
import { calendarApi, teachersApi, roomsApi } from '@/services/api'
import type { WeekCalendar } from '@/features/schedule/types'
import { groupLessonsByCourseAndDate, type GroupedLesson } from '@/features/schedule/utils/groupLessons'
import type { TeacherAvailability, TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import { getWeekStart, getWeekDays, formatDateForApi, combineDateAndTime, getHoursFromTimeString } from '@/lib/iso-helpers'

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

const convertGroupedLessonToEvent = (group: GroupedLesson): CalendarEvent => {
  const attendees = [...group.studentNames]
  if (group.teacherName) attendees.push(group.teacherName)

  // Use courseId + date as the event ID for grouped lessons
  const eventId = group.lessons.length > 1
    ? `${group.courseId}:${group.date}`
    : group.lessons[0]?.id ?? `${group.courseId}:${group.date}`

  return {
    id: eventId,
    startDateTime: combineDateAndTime(new Date(group.date), group.startTime),
    endDateTime: combineDateAndTime(new Date(group.date), group.endTime),
    title: group.instrumentName || group.title,
    frequency: 'weekly',
    eventType: group.status,
    attendees,
    room: group.roomName,
  }
}

const formatDateDisplay = (date: Date): string =>
  date.toLocaleDateString('nl-NL', { day: 'numeric', month: 'short' })

const mapToDayAvailability = (availability: TeacherAvailability[]): DayAvailability[] =>
  availability.map((item) => ({
    dayOfWeek: item.dayOfWeek,
    fromTime: getHoursFromTimeString(item.fromTime),
    untilTime: getHoursFromTimeString(item.untilTime),
  }))

// --- Component ---

export const SchedulePage = () => {
  // State
  const [currentDate, setCurrentDate] = useState(() => getWeekStart(new Date()))
  const [filterTeacher, setFilterTeacher] = useState<string>('all')
  const [filterRoom, setFilterRoom] = useState<string>('all')

  // Queries
  const { data: calendarData, isLoading, refetch } = useQuery<WeekCalendar>({
    queryKey: ['calendar', formatDateForApi(currentDate), filterTeacher, filterRoom],
    queryFn: () =>
      calendarApi.getWeek({
        date: formatDateForApi(currentDate),
        teacherId: filterTeacher !== 'all' ? filterTeacher : undefined,
        roomId: filterRoom !== 'all' ? Number.parseInt(filterRoom) : undefined,
      }),
  })

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
  const weekEnd = useMemo(() => {
    const end = new Date(currentDate)
    end.setDate(end.getDate() + 6)
    return end
  }, [currentDate])

  const weekDays = useMemo(() => getWeekDays(currentDate), [currentDate])

  const events = useMemo(() => {
    const lessons = calendarData?.lessons ?? []
    const grouped = groupLessonsByCourseAndDate(lessons)
    return grouped.map(convertGroupedLessonToEvent)
  }, [calendarData?.lessons])

  const availability = useMemo(
    () => mapToDayAvailability(teacherAvailability),
    [teacherAvailability]
  )

  // Handlers
  const goToPreviousWeek = () => {
    const newDate = new Date(currentDate)
    newDate.setDate(newDate.getDate() - 7)
    setCurrentDate(newDate)
  }

  const goToNextWeek = () => {
    const newDate = new Date(currentDate)
    newDate.setDate(newDate.getDate() + 7)
    setCurrentDate(newDate)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Schedule</h1>
          <p className="text-muted-foreground">
            {formatDateDisplay(currentDate)} - {formatDateDisplay(weekEnd)}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Select value={filterTeacher} onValueChange={setFilterTeacher}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="All teachers" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All teachers</SelectItem>
              {teachers.map((teacher) => (
                <SelectItem key={teacher.id} value={teacher.id}>
                  {teacher.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={filterRoom} onValueChange={setFilterRoom}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="All rooms" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All rooms</SelectItem>
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

      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {!isLoading && (
        <CalendarComponent
          title={`Week ${Math.ceil((currentDate.getDate() - currentDate.getDay() + 1) / 7)}`}
          events={events}
          dates={weekDays}
          colorScheme={statusColorScheme}
          onNavigatePrevious={goToPreviousWeek}
          onNavigateNext={goToNextWeek}
          availability={availability}
          dayStartTime={8}
          dayEndTime={21}
          hourHeight={100}
        />
      )}

      {/* Legend */}
      <Card>
        <CardContent className="py-4">
          <div className="flex flex-wrap gap-4 text-sm">
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-blue-100 border border-blue-300" />
              <span>Scheduled</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-green-100 border border-green-300" />
              <span>Completed</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-red-100 border border-red-300" />
              <span>Cancelled</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded bg-orange-100 border border-orange-300" />
              <span>No Show</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
