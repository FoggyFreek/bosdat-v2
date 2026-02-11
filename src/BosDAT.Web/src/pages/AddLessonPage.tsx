import { useMemo } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import type { CalendarEvent } from '@/components/calendar/types'
import { useToast } from '@/hooks/use-toast'
import { lessonsApi } from '@/features/lessons/api'
import { coursesApi } from '@/features/courses/api'
import { useScheduleCalendarData } from '@/features/lessons/hooks/useScheduleCalendarData'
import { formatTime, getDurationMinutes } from '@/lib/datetime-helpers'
import type { Course } from '@/features/courses/types'
import { LessonSchedulerLayout } from '@/components/LessonSchedulerLayout'

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

export function AddLessonPage() {
  const { id: courseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const queryClient = useQueryClient()

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

  const createMutation = useMutation({
    mutationFn: async(data: {
      scheduledDate: string
      startTime: string
      endTime: string
      teacherId: string
      roomId?: number
    }) => {
        if (!course?.enrollments?.length) return
        
        return Promise.all(
        course.enrollments.map((enrollment) => 
          lessonsApi.create({
            courseId: courseId!,
            teacherId: data.teacherId,
            studentId: enrollment.studentId,
            roomId: data.roomId,
            scheduledDate: data.scheduledDate,
            startTime: `${data.startTime}:00`,
            endTime: `${data.endTime}:00`,
            notes: 'Extra lesson',
          })
        )
      )
    },
    onSuccess: () => {
      toast({
        title: 'Lessons for all attendees were added',
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
    <LessonSchedulerLayout
      title="Add Extra Lesson"
      backTo={`/courses/${courseId}`}
      summaryCard={<CourseSummaryMini course={course} />}
      calendar={calendar}
      allEvents={allEvents}
      slotLabel="Selected Timeslot"
      submitLabel="Add Extra Lesson"
      pendingLabel="Adding..."
      isPending={createMutation.isPending}
      onSubmit={handleSubmit}
    />
  )
}
