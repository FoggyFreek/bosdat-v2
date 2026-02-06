import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import { getStatusColor } from '@/features/courses/hooks/useCoursesData'
import { WeekParityBadge } from './WeekParityBadge'
import type { Course } from '@/features/courses/types'

interface CourseSummaryCardProps {
  course: Course
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{children}</span>
    </div>
  )
}

export function CourseSummaryCard({ course }: CourseSummaryCardProps) {
  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">Course Details</h3>
      <div className="grid gap-2 text-sm">
        <Row label="Instrument:">{course.instrumentName}</Row>
        <Row label="Course Type:">{course.courseTypeName}</Row>
        <Row label="Teacher:">{course.teacherName}</Row>
        <Row label="Day:">{course.dayOfWeek}</Row>
        <Row label="Time:">{formatTime(course.startTime)} – {formatTime(course.endTime)}</Row>
        <Row label="Start Date:">{formatDate(course.startDate)}</Row>
        {course.endDate && <Row label="End Date:">{formatDate(course.endDate)}</Row>}
        <Row label="Frequency:">
          <span className="flex items-center gap-2">
            {course.frequency}
            <WeekParityBadge course={course} />
          </span>
        </Row>
        {course.roomName && <Row label="Room:">{course.roomName}</Row>}
        <Row label="Status:">
          <span
            className={cn(
              'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
              getStatusColor(course.status)
            )}
          >
            {course.status}
          </span>
        </Row>
        {course.isTrial && <Row label="Type:"><span className="text-amber-600">Trial</span></Row>}
        {course.isWorkshop && <Row label="Workshop:">Yes</Row>}
        {course.notes && <Row label="Notes:"><span className="text-right max-w-[60%]">{course.notes}</span></Row>}
      </div>
    </div>
  )
}
