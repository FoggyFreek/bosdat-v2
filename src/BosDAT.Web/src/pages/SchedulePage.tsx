import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, RefreshCw, Calendar as CalendarIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { calendarApi, teachersApi, roomsApi } from '@/services/api'
import type { CalendarLesson, WeekCalendar } from '@/features/schedule/types'
import type { TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'

import { cn, formatTime, getDayName } from '@/lib/utils'

function getWeekStart(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  return new Date(d.setDate(diff))
}

function formatDateForApi(date: Date): string {
  return date.toISOString().split('T')[0]
}

function formatDateDisplay(date: Date): string {
  return date.toLocaleDateString('nl-NL', { day: 'numeric', month: 'short' })
}

const hours = Array.from({ length: 14 }, (_, i) => i + 8) // 8:00 - 21:00

function getLessonTooltip(lesson: CalendarLesson): string {
  const parts = [lesson.title, lesson.teacherName]
  if (lesson.roomName) {
    parts.push(lesson.roomName)
  }
  return parts.join('\n')
}

export function SchedulePage() {
  const [currentDate, setCurrentDate] = useState(() => getWeekStart(new Date()))
  const [filterTeacher, setFilterTeacher] = useState<string>('')
  const [filterRoom, setFilterRoom] = useState<string>('')

  const weekEnd = new Date(currentDate)
  weekEnd.setDate(weekEnd.getDate() + 6)

  const { data: calendarData, isLoading, refetch } = useQuery<WeekCalendar>({
    queryKey: ['calendar', formatDateForApi(currentDate), filterTeacher, filterRoom],
    queryFn: () =>
      calendarApi.getWeek({
        date: formatDateForApi(currentDate),
        teacherId: filterTeacher || undefined,
        roomId: filterRoom ? Number.parseInt(filterRoom) : undefined,
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

  const goToToday = () => {
    setCurrentDate(getWeekStart(new Date()))
  }

  // Group lessons by day and time
  const lessonsByDay = (calendarData?.lessons || []).reduce((acc, lesson) => {
    const date = lesson.date
    if (!acc[date]) {
      acc[date] = []
    }
    acc[date].push(lesson)
    return acc
  }, {} as Record<string, CalendarLesson[]>)

  // Generate the 7 days of the week
  const weekDays = Array.from({ length: 7 }, (_, i) => {
    const date = new Date(currentDate)
    date.setDate(date.getDate() + i)
    return date
  })

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Scheduled':
        return 'bg-blue-100 border-blue-300 text-blue-800'
      case 'Completed':
        return 'bg-green-100 border-green-300 text-green-800'
      case 'Cancelled':
        return 'bg-red-100 border-red-300 text-red-800'
      case 'NoShow':
        return 'bg-orange-100 border-orange-300 text-orange-800'
      default:
        return 'bg-gray-100 border-gray-300 text-gray-800'
    }
  }

  const isToday = (date: Date) => {
    const today = new Date()
    return (
      date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear()
    )
  }

  const isHoliday = (date: Date) => {
    const dateStr = formatDateForApi(date)
    return calendarData?.holidays.some(
      (h) => dateStr >= h.startDate && dateStr <= h.endDate
    )
  }

  const getHolidayName = (date: Date) => {
    const dateStr = formatDateForApi(date)
    return calendarData?.holidays.find(
      (h) => dateStr >= h.startDate && dateStr <= h.endDate
    )?.name
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
              <SelectItem value="">All teachers</SelectItem>
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
              <SelectItem value="">All rooms</SelectItem>
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

      <Card>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Button variant="outline" size="icon" onClick={goToPreviousWeek}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="outline" onClick={goToToday}>
                <CalendarIcon className="h-4 w-4 mr-2" />
                Today
              </Button>
              <Button variant="outline" size="icon" onClick={goToNextWeek}>
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
            <CardTitle className="text-lg">
              Week {Math.ceil((currentDate.getDate() - currentDate.getDay() + 1) / 7)}
            </CardTitle>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex items-center justify-center py-16">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[800px]">
                {/* Header row with days */}
                <div className="grid grid-cols-8 border-b">
                  <div className="p-2 text-center text-sm font-medium text-muted-foreground border-r">
                    Time
                  </div>
                  {weekDays.map((date) => (
                    <div
                      key={date.toISOString()}
                      className={cn(
                        'p-2 text-center border-r last:border-r-0',
                        isToday(date) && 'bg-primary/5',
                        isHoliday(date) && 'bg-red-50'
                      )}
                    >
                      <p className="text-sm font-medium">
                        {getDayName(date.getDay()).substring(0, 3)}
                      </p>
                      <p
                        className={cn(
                          'text-lg',
                          isToday(date) && 'font-bold text-primary'
                        )}
                      >
                        {date.getDate()}
                      </p>
                      {isHoliday(date) && (
                        <p className="text-xs text-red-600 truncate">
                          {getHolidayName(date)}
                        </p>
                      )}
                    </div>
                  ))}
                </div>

                {/* Time slots */}
                {hours.map((hour) => (
                  <div key={hour} className="grid grid-cols-8 border-b last:border-b-0">
                    <div className="p-2 text-center text-sm text-muted-foreground border-r">
                      {hour.toString().padStart(2, '0')}:00
                    </div>
                    {weekDays.map((date) => {
                      const dateStr = formatDateForApi(date)
                      const dayLessons = (lessonsByDay[dateStr] || []).filter((lesson) => {
                        const lessonHour = Number.parseInt(lesson.startTime.split(':')[0])
                        return lessonHour === hour
                      })

                      return (
                        <div
                          key={dateStr}
                          className={cn(
                            'p-1 border-r last:border-r-0 min-h-[60px]',
                            isToday(date) && 'bg-primary/5',
                            isHoliday(date) && 'bg-red-50'
                          )}
                        >
                          {dayLessons.map((lesson) => (
                            <div
                              key={lesson.id}
                              className={cn(
                                'p-1.5 rounded text-xs border mb-1 cursor-pointer hover:opacity-80 transition-opacity',
                                getStatusColor(lesson.status)
                              )}
                              title={getLessonTooltip(lesson)}
                            >
                              <p className="font-medium truncate">{lesson.instrumentName}</p>
                              <p className="truncate">
                                {formatTime(lesson.startTime)} - {formatTime(lesson.endTime)}
                              </p>
                              {lesson.studentName && (
                                <p className="truncate text-[10px] opacity-80">
                                  {lesson.studentName}
                                </p>
                              )}
                            </div>
                          ))}
                        </div>
                      )
                    })}
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

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
