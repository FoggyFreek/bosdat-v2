import { useMemo } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CalendarComponent } from '@/components/calendar/CalendarComponent'
import type { CalendarEvent } from '@/components/calendar/types'
import { useToast } from '@/hooks/use-toast'
import { lessonsApi } from '@/features/lessons/api'
import { useScheduleCalendarData } from '@/features/lessons/hooks/useScheduleCalendarData'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import type { Lesson } from '@/features/lessons/types'

function LessonSummaryCard({ lesson }: { lesson: Lesson }) {
  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">Scheduled Event</h3>
      <div className="grid gap-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">Course:</span>
          <span className="font-medium">
            {lesson.instrumentName} – {lesson.courseTypeName}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Date:</span>
          <span className="font-medium">{formatDate(lesson.scheduledDate)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Time:</span>
          <span className="font-medium">
            {formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Teacher:</span>
          <span className="font-medium">{lesson.teacherName}</span>
        </div>
        {lesson.roomName && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Room:</span>
            <span className="font-medium">{lesson.roomName}</span>
          </div>
        )}
        {lesson.studentName && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Student:</span>
            <span className="font-medium">{lesson.studentName}</span>
          </div>
        )}
      </div>
    </div>
  )
}

function getDurationMinutes(startTime: string, endTime: string): number {
  const [sh, sm] = startTime.split(':').map(Number)
  const [eh, em] = endTime.split(':').map(Number)
  return (eh * 60 + em) - (sh * 60 + sm)
}

export function MoveLessonPage() {
  const { id: courseId, lessonId } = useParams<{ id: string; lessonId: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const queryClient = useQueryClient()

  // Fetch the lesson to move
  const { data: lesson, isLoading: isLessonLoading } = useQuery<Lesson>({
    queryKey: ['lesson', lessonId],
    queryFn: () => lessonsApi.getById(lessonId!),
    enabled: !!lessonId,
  })

  const durationMinutes = lesson ? getDurationMinutes(lesson.startTime, lesson.endTime) : 30

  const initialDate = useMemo(
    () => (lesson ? new Date(lesson.scheduledDate) : new Date()),
    [lesson]
  )

  const calendar = useScheduleCalendarData({
    initialDate,
    initialTeacherId: lesson?.teacherId,
    initialRoomId: lesson?.roomId ?? undefined,
    durationMinutes,
  })

  // Create a transparent placeholder for the original lesson position
  const originalEventPlaceholder: CalendarEvent | null = useMemo(() => {
    if (!lesson) return null
    const date = lesson.scheduledDate
    return {
      id: `original-${lesson.id}`,
      startDateTime: `${date}T${lesson.startTime}`,
      endDateTime: `${date}T${lesson.endTime}`,
      title: `${lesson.instrumentName} – ${lesson.courseTypeName} (original)`,
      frequency: 'once',
      eventType: 'placeholder' as const,
      status: 'Scheduled' as const,
      attendees: lesson.studentName ? [lesson.studentName] : [],
      room: lesson.roomName,
    }
  }, [lesson])

  // Selected slot placeholder event
  const selectedSlotEvent: CalendarEvent | null = useMemo(() => {
    if (!calendar.selectedSlot) return null
    return {
      id: 'selected-slot',
      startDateTime: `${calendar.selectedSlot.date}T${calendar.selectedSlot.startTime}:00`,
      endDateTime: `${calendar.selectedSlot.date}T${calendar.selectedSlot.endTime}:00`,
      title: 'New timeslot',
      frequency: 'once',
      eventType: 'placeholder' as const,
      status: 'Scheduled' as const,
      attendees: [],
    }
  }, [calendar.selectedSlot])

  // Combine all events
  const allEvents = useMemo(() => {
    const evts = [...calendar.events]
    if (originalEventPlaceholder) evts.push(originalEventPlaceholder)
    if (selectedSlotEvent) evts.push(selectedSlotEvent)
    return evts
  }, [calendar.events, originalEventPlaceholder, selectedSlotEvent])

  // Move mutation
  const moveMutation = useMutation({
    mutationFn: (data: {
      scheduledDate: string
      startTime: string
      endTime: string
      teacherId: string
      roomId?: number
    }) =>
      lessonsApi.update(lessonId!, {
        studentId: lesson!.studentId,
        teacherId: data.teacherId,
        roomId: data.roomId,
        scheduledDate: data.scheduledDate,
        startTime: `${data.startTime}:00`,
        endTime: `${data.endTime}:00`,
        status: lesson!.status,
        cancellationReason: lesson!.cancellationReason,
        notes: lesson!.notes,
      }),
    onSuccess: () => {
      toast({
        title: 'Lesson moved',
        description: 'The lesson has been moved to the new timeslot.',
      })
      queryClient.invalidateQueries({ queryKey: ['course', courseId, 'lessons'] })
      queryClient.invalidateQueries({ queryKey: ['lesson', lessonId] })
      navigate(`/courses/${courseId}`)
    },
    onError: () => {
      toast({
        title: 'Error',
        description: 'Failed to move the lesson.',
        variant: 'destructive',
      })
    },
  })

  const handleSubmit = () => {
    if (!calendar.selectedSlot || !lesson) return

    const teacherId =
      calendar.filterTeacher !== 'all' ? calendar.filterTeacher : lesson.teacherId
    const roomId =
      calendar.filterRoom !== 'all'
        ? Number.parseInt(calendar.filterRoom)
        : lesson.roomId ?? undefined

    moveMutation.mutate({
      scheduledDate: calendar.selectedSlot.date,
      startTime: calendar.selectedSlot.startTime,
      endTime: calendar.selectedSlot.endTime,
      teacherId,
      roomId,
    })
  }

  if (isLessonLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!lesson) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Lesson not found</p>
        <Button asChild className="mt-4">
          <Link to={`/courses/${courseId}`}>Back to Course</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to={`/courses/${courseId}`}>
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <h1 className="text-2xl font-bold">Move Lesson</h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
        {/* Left panel: Summary + Filters + Submit */}
        <div className="space-y-4">
          <LessonSummaryCard lesson={lesson} />

          {/* Filters */}
          <div className="rounded-lg border bg-muted/50 p-4 space-y-3">
            <h3 className="font-medium text-sm">Filters</h3>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground">Teacher</label>
              <Select value={calendar.filterTeacher} onValueChange={calendar.setFilterTeacher}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All teachers" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All teachers</SelectItem>
                  {calendar.teachers.map((teacher) => (
                    <SelectItem key={teacher.id} value={teacher.id}>
                      {teacher.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground">Room</label>
              <Select value={calendar.filterRoom} onValueChange={calendar.setFilterRoom}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All rooms" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All rooms</SelectItem>
                  {calendar.rooms.map((room) => (
                    <SelectItem key={room.id} value={room.id.toString()}>
                      {room.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Selected slot summary */}
          {calendar.selectedSlot && (
            <div className="rounded-lg border border-primary bg-primary/5 p-4">
              <h3 className="font-medium text-sm mb-2">New Timeslot</h3>
              <div className="grid gap-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Date:</span>
                  <span className="font-medium">{formatDate(calendar.selectedSlot.date)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Time:</span>
                  <span className="font-medium">
                    {calendar.selectedSlot.startTime} – {calendar.selectedSlot.endTime}
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Submit */}
          <Button
            className="w-full"
            disabled={!calendar.selectedSlot || moveMutation.isPending}
            onClick={handleSubmit}
          >
            {moveMutation.isPending ? 'Moving...' : 'Move Lesson'}
          </Button>
        </div>

        {/* Right panel: Calendar */}
        <div className="min-h-[600px]">
          {calendar.isLoading && (
            <div className="flex items-center justify-center py-16">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          )}
          {!calendar.isLoading && (
            <CalendarComponent
              title="Schedule"
              events={allEvents}
              dates={calendar.weekDays}
              colorScheme={calendar.colorScheme}
              onNavigatePrevious={calendar.goToPreviousWeek}
              onNavigateNext={calendar.goToNextWeek}
              onTimeslotClick={calendar.handleTimeslotClick}
              availability={calendar.availability}
              highlightedDate={calendar.highlightedDate}
              dayStartTime={8}
              dayEndTime={22}
              hourHeight={80}
            />
          )}
        </div>
      </div>
    </div>
  )
}
