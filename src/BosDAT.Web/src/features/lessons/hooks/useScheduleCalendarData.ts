import { useState, useMemo, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { calendarApi } from '@/features/schedule/api'
import { teachersApi } from '@/features/teachers/api'
import { roomsApi } from '@/features/rooms/api'
import type { WeekCalendar } from '@/features/schedule/types'
import type { TeacherAvailability, TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import type { CalendarEvent, DayAvailability, TimeSlot } from '@/components/calendar/types'
import { groupLessonsByCourseAndDate, type GroupedLesson } from '@/features/schedule/utils/groupLessons'
import {
  getWeekStart,
  getWeekDays,
  formatDateForApi,
  combineDateAndTime,
  getHoursFromTimeString,
  calculateEndTime,
} from '@/lib/datetime-helpers'

interface UseScheduleCalendarDataProps {
  initialDate: Date
  initialTeacherId?: string
  initialRoomId?: number
  courseTypeId?: string
  durationMinutes: number
}

const convertGroupedLessonToEvent = (group: GroupedLesson): CalendarEvent => {
  const attendees = [...group.studentNames]
  if (group.teacherName) attendees.push(group.teacherName)

  const eventId =
    group.lessons.length > 1
      ? `${group.courseId}:${group.date}`
      : group.lessons[0]?.id ?? `${group.courseId}:${group.date}`

  return {
    id: eventId,
    startDateTime: combineDateAndTime(new Date(group.date), group.startTime),
    endDateTime: combineDateAndTime(new Date(group.date), group.endTime),
    title: group.title + (group.isTrial ? ' (Trial)' : ''),
    frequency: group.frequency,
    eventType: group.isTrial ? 'trial' : group.isWorkshop ? 'workshop' : 'course',
    status: group.status,
    attendees,
    room: group.roomName,
  }
}

const mapToDayAvailability = (availability: TeacherAvailability[]): DayAvailability[] =>
  availability.map((item) => ({
    dayOfWeek: item.dayOfWeek,
    fromTime: getHoursFromTimeString(item.fromTime),
    untilTime: getHoursFromTimeString(item.untilTime),
  }))

const statusColorScheme = {
  Scheduled: { background: '#dbeafe', border: '#3b82f6', textBackground: '#eff6ff' },
  Completed: { background: '#dcfce7', border: '#22c55e', textBackground: '#f0fdf4' },
  Cancelled: { background: '#fee2e2', border: '#ef4444', textBackground: '#fef2f2' },
  NoShow: { background: '#fed7aa', border: '#f97316', textBackground: '#fff7ed' },
  placeholder: { background: '#f3e8ff', border: '#9333ea', textBackground: '#e9d5ff' },
}

export const useScheduleCalendarData = ({
  initialDate,
  initialTeacherId,
  initialRoomId,
  durationMinutes,
}: UseScheduleCalendarDataProps) => {
  const [currentDate, setCurrentDate] = useState(() => getWeekStart(initialDate))
  const [filterTeacher, setFilterTeacher] = useState<string>(initialTeacherId ?? 'all')
  const [filterRoom, setFilterRoom] = useState<string>(
    initialRoomId ? initialRoomId.toString() : 'all'
  )
  const [selectedSlot, setSelectedSlot] = useState<{
    date: string
    startTime: string
    endTime: string
  } | null>(null)

  const weekDays = useMemo(() => getWeekDays(currentDate), [currentDate])

  // Calendar data query
  const { data: calendarData, isLoading: isCalendarLoading } = useQuery<WeekCalendar>({
    queryKey: [
      'calendar',
      formatDateForApi(currentDate),
      filterTeacher,
      filterRoom,
    ],
    queryFn: () =>
      calendarApi.getWeek({
        date: formatDateForApi(currentDate),
        teacherId: filterTeacher !== 'all' ? filterTeacher : undefined,
        roomId: filterRoom !== 'all' ? Number.parseInt(filterRoom) : undefined,
      }),
  })

  // Teachers for filter dropdown - filter by courseTypeId if provided
  const { data: teachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active'],
    queryFn: () => teachersApi.getAll({ activeOnly: true }),
  })

  // Rooms for filter dropdown
  const { data: rooms = [] } = useQuery<Room[]>({
    queryKey: ['rooms', 'active'],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  // Teacher availability
  const { data: teacherAvailability = [] } = useQuery<TeacherAvailability[]>({
    queryKey: ['teacher-availability', filterTeacher],
    queryFn: () =>
      filterTeacher === 'all'
        ? Promise.resolve([])
        : teachersApi.getAvailability(filterTeacher),
  })

  const availability = useMemo(
    () => mapToDayAvailability(teacherAvailability),
    [teacherAvailability]
  )

  // Build events from calendar data
  const events = useMemo(() => {
    const lessons = calendarData?.lessons ?? []
    const grouped = groupLessonsByCourseAndDate(lessons)
    return grouped.map(convertGroupedLessonToEvent)
  }, [calendarData?.lessons])

  // Navigation
  const goToPreviousWeek = useCallback(() => {
    setCurrentDate((prev) => {
      const newDate = new Date(prev)
      newDate.setDate(newDate.getDate() - 7)
      return newDate
    })
  }, [])

  const goToNextWeek = useCallback(() => {
    setCurrentDate((prev) => {
      const newDate = new Date(prev)
      newDate.setDate(newDate.getDate() + 7)
      return newDate
    })
  }, [])

  // Timeslot click -> select a slot
  const handleTimeslotClick = useCallback(
    (timeslot: TimeSlot) => {
      const { hour, minute } = timeslot
      const roundedMinute = Math.round(minute / 10) * 10
      const finalHour = roundedMinute === 60 ? hour + 1 : hour
      const finalMinute = roundedMinute === 60 ? 0 : roundedMinute
      const startTime = `${String(finalHour).padStart(2, '0')}:${String(finalMinute).padStart(2, '0')}`
      const endTime = calculateEndTime(startTime, durationMinutes)
      const date = formatDateForApi(timeslot.date)

      setSelectedSlot({ date, startTime, endTime })
    },
    [durationMinutes]
  )

  return {
    currentDate,
    setCurrentDate,
    weekDays,
    events,
    teachers,
    rooms,
    availability,
    filterTeacher,
    setFilterTeacher,
    filterRoom,
    setFilterRoom,
    selectedSlot,
    setSelectedSlot,
    isLoading: isCalendarLoading,
    colorScheme: statusColorScheme,
    goToPreviousWeek,
    goToNextWeek,
    handleTimeslotClick,
    highlightedDate: initialDate,
  }
}
