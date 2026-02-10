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
import { coursesApi } from '@/features/courses/api'
import { useScheduleCalendarData } from '@/features/lessons/hooks/useScheduleCalendarData'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import type { Course } from '@/features/courses/types'

function CourseSummaryMini({ course }: { course: Course }) {
  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">Course Details</h3>
      <div className="grid gap-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">Course:</span>
          <span className="font-medium">
            {course.instrumentName} – {course.courseTypeName}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Teacher:</span>
          <span className="font-medium">{course.teacherName}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Regular time:</span>
          <span className="font-medium">
            {course.dayOfWeek} {formatTime(course.startTime)} – {formatTime(course.endTime)}
          </span>
        </div>
        {course.roomName && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Room:</span>
            <span className="font-medium">{course.roomName}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span className="text-muted-foreground">Frequency:</span>
          <span className="font-medium">{course.frequency}</span>
        </div>
      </div>
    </div>
  )
}

function getDurationMinutes(startTime: string, endTime: string): number {
  const [sh, sm] = startTime.split(':').map(Number)
  const [eh, em] = endTime.split(':').map(Number)
  return eh * 60 + em - (sh * 60 + sm)
}

export function AddLessonPage() {
  const { id: courseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const queryClient = useQueryClient()

  // Fetch the course
  const { data: course, isLoading: isCourseLoading } = useQuery<Course>({
    queryKey: ['course', courseId],
    queryFn: () => coursesApi.getById(courseId!),
    enabled: !!courseId,
  })

  const durationMinutes = course ? getDurationMinutes(course.startTime, course.endTime) : 30

  const initialDate = useMemo(() => new Date(), [])

  const calendar = useScheduleCalendarData({
    initialDate,
    initialTeacherId: course?.teacherId,
    initialRoomId: course?.roomId ?? undefined,
    courseTypeId: course?.courseTypeId,
    durationMinutes,
  })

  // Selected slot placeholder event
  const selectedSlotEvent: CalendarEvent | null = useMemo(() => {
    if (!calendar.selectedSlot) return null
    return {
      id: 'selected-slot',
      startDateTime: `${calendar.selectedSlot.date}T${calendar.selectedSlot.startTime}:00`,
      endDateTime: `${calendar.selectedSlot.date}T${calendar.selectedSlot.endTime}:00`,
      title: 'Extra lesson',
      frequency: 'once',
      eventType: 'placeholder' as const,
      status: 'Scheduled' as const,
      attendees: [],
    }
  }, [calendar.selectedSlot])

  const allEvents = useMemo(() => {
    const evts = [...calendar.events]
    if (selectedSlotEvent) evts.push(selectedSlotEvent)
    return evts
  }, [calendar.events, selectedSlotEvent])

  // Create lesson mutation
  const createMutation = useMutation({
    mutationFn: (data: {
      scheduledDate: string
      startTime: string
      endTime: string
      teacherId: string
      roomId?: number
    }) =>
      lessonsApi.create({
        courseId: courseId!,
        teacherId: data.teacherId,
        roomId: data.roomId,
        scheduledDate: data.scheduledDate,
        startTime: `${data.startTime}:00`,
        endTime: `${data.endTime}:00`,
        notes: 'Extra lesson',
      }),
    onSuccess: () => {
      toast({
        title: 'Lesson added',
        description: 'The extra lesson has been added.',
      })
      queryClient.invalidateQueries({ queryKey: ['course', courseId, 'lessons'] })
      navigate(`/courses/${courseId}`)
    },
    onError: () => {
      toast({
        title: 'Error',
        description: 'Failed to add the lesson.',
        variant: 'destructive',
      })
    },
  })

  const handleSubmit = () => {
    if (!calendar.selectedSlot || !course) return

    const teacherId =
      calendar.filterTeacher !== 'all' ? calendar.filterTeacher : course.teacherId
    const roomId =
      calendar.filterRoom !== 'all'
        ? Number.parseInt(calendar.filterRoom)
        : course.roomId ?? undefined

    createMutation.mutate({
      scheduledDate: calendar.selectedSlot.date,
      startTime: calendar.selectedSlot.startTime,
      endTime: calendar.selectedSlot.endTime,
      teacherId,
      roomId,
    })
  }

  if (isCourseLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!course) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Course not found</p>
        <Button asChild className="mt-4">
          <Link to="/courses">Back to Courses</Link>
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
        <h1 className="text-2xl font-bold">Add Extra Lesson</h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
        {/* Left panel: Summary + Filters + Submit */}
        <div className="space-y-4">
          <CourseSummaryMini course={course} />

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
              <h3 className="font-medium text-sm mb-2">Selected Timeslot</h3>
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
            disabled={!calendar.selectedSlot || createMutation.isPending}
            onClick={handleSubmit}
          >
            {createMutation.isPending ? 'Adding...' : 'Add Extra Lesson'}
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
