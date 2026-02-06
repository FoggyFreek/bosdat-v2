import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface EnrollmentSummaryCardProps {
  title: string
  courseTypeName?: string
  courseTypeLabel?: string
  teacherName?: string
  startDate?: string
  dayOfWeek?: string
  endDate?: string
  isTrial?: boolean
  frequency?: string
  maxStudents?: number
  startTime?: string
  endTime?: string
  roomName?: string
  className?: string
  children?: ReactNode
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{children}</span>
    </div>
  )
}

export function EnrollmentSummaryCard({
  title,
  courseTypeName,
  courseTypeLabel,
  teacherName,
  startDate,
  dayOfWeek,
  endDate,
  isTrial,
  frequency,
  maxStudents,
  startTime,
  endTime,
  roomName,
  className,
  children,
}: EnrollmentSummaryCardProps) {
  const hasFields =
    courseTypeName || teacherName || startDate || dayOfWeek || endDate ||
    isTrial || frequency || maxStudents || startTime || roomName

  return (
    <div className={cn('rounded-lg border bg-muted/50 p-4', className)}>
      <h3 className="font-medium mb-3 text-sm">{title}</h3>
      {hasFields && (
        <div className="grid gap-2 text-sm">
          {courseTypeName && (
            <Row label="Course Type:">
              {courseTypeName}{' '}
              {courseTypeLabel && (
                <span className="text-muted-foreground">({courseTypeLabel})</span>
              )}
            </Row>
          )}
          {teacherName && <Row label="Teacher:">{teacherName}</Row>}
          {startDate && (
            <Row label="Start Date:">
              {startDate}{dayOfWeek && ` (${dayOfWeek})`}
            </Row>
          )}
          {endDate && <Row label="End Date:">{endDate}</Row>}
          {frequency && <Row label="Frequency:">{frequency}</Row>}
          {isTrial && (
            <Row label="Type:"><span className="text-amber-600">Trial Lesson</span></Row>
          )}
          {maxStudents !== undefined && (
            <Row label="Max Students:">{String(maxStudents)}</Row>
          )}
          {dayOfWeek && !startDate && <Row label="Day:">{dayOfWeek}</Row>}
          {startTime && (
            <Row label="Time:">{startTime}{endTime && ` – ${endTime}`}</Row>
          )}
          {roomName && <Row label="Room:">{roomName}</Row>}
        </div>
      )}
      {children}
    </div>
  )
}
