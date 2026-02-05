import { cn } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'
import { SummaryCard } from '@/components/SummaryCard'
import type { Enrollment } from '@/features/enrollments/types'

const ENROLLMENT_STATUS_COLORS: Record<string, string> = {
  Active: 'bg-green-100 text-green-800',
  Trail: 'bg-amber-100 text-amber-800',
  Withdrawn: 'bg-red-100 text-red-800',
  Completed: 'bg-blue-100 text-blue-800',
  Suspended: 'bg-gray-100 text-gray-800',
}

interface CourseEnrollmentsCardProps {
  enrollments: Enrollment[]
}

export function CourseEnrollmentsCard({ enrollments }: CourseEnrollmentsCardProps) {
  return (
    <SummaryCard title={`Enrolled Students (${enrollments.length})`}>
      {enrollments.length === 0 && (
        <p className="text-sm text-muted-foreground">No students enrolled</p>
      )}
      {enrollments.length > 0 && (
        <div className="space-y-3">
          {enrollments.map((enrollment) => (
            <div
              key={enrollment.id}
              className="flex items-center justify-between text-sm"
            >
              <div>
                <p className="font-medium">{enrollment.studentName}</p>
                <p className="text-xs text-muted-foreground">
                  Enrolled {formatDate(enrollment.enrolledAt)}
                  {enrollment.discountPercent > 0 && (
                    <> &middot; {enrollment.discountPercent}% discount ({enrollment.discountType})</>
                  )}
                </p>
              </div>
              <span
                className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                  ENROLLMENT_STATUS_COLORS[enrollment.status] ?? 'bg-gray-100 text-gray-800'
                )}
              >
                {enrollment.status}
              </span>
            </div>
          ))}
        </div>
      )}
    </SummaryCard>
  )
}
