import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import { getStatusColor } from '@/features/courses/hooks/useCoursesData'
import { SummaryCard, type SummaryItem } from '@/components/SummaryCard'
import { WeekParityBadge } from './WeekParityBadge'
import type { Course } from '@/features/courses/types'

interface CourseSummaryCardProps {
  course: Course
}

export function CourseSummaryCard({ course }: CourseSummaryCardProps) {
  const items: SummaryItem[] = [
    { label: 'Instrument:', value: course.instrumentName },
    { label: 'Course Type:', value: course.courseTypeName },
    { label: 'Teacher:', value: course.teacherName },
    { label: 'Day:', value: course.dayOfWeek },
    {
      label: 'Time:',
      value: `${formatTime(course.startTime)} – ${formatTime(course.endTime)}`,
    },
    { label: 'Start Date:', value: formatDate(course.startDate) },
    ...(course.endDate
      ? [{ label: 'End Date:', value: formatDate(course.endDate) }]
      : []),
    {
      label: 'Frequency:',
      value: (
        <span className="flex items-center gap-2">
          {course.frequency}
          <WeekParityBadge course={course} />
        </span>
      ),
    },
    ...(course.roomName
      ? [{ label: 'Room:', value: course.roomName }]
      : []),
    {
      label: 'Status:',
      value: (
        <span
          className={cn(
            'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
            getStatusColor(course.status)
          )}
        >
          {course.status}
        </span>
      ),
    },
    ...(course.isTrial
      ? [{ label: 'Type:', value: <span className="text-amber-600">Trial</span> }]
      : []),
    ...(course.isWorkshop
      ? [{ label: 'Workshop:', value: 'Yes' }]
      : []),
    ...(course.notes
      ? [{ label: 'Notes:', value: <span className="text-right max-w-[60%]">{course.notes}</span> }]
      : []),
  ]

  return <SummaryCard title="Course Details" items={items} />
}
