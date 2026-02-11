import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'
import type { Enrollment } from '@/features/enrollments/types'
import { enrollmentStatusTranslations } from '@/features/enrollments/types'

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
  const { t } = useTranslation()

  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">
        {t('enrollments.title', { count: enrollments.length })}
      </h3>
      {enrollments.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('enrollments.noStudents')}</p>
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
                  {t('enrollments.enrolledAt')} {formatDate(enrollment.enrolledAt)}
                  {enrollment.discountPercent > 0 && (
                    <> &middot; {t('enrollments.discount', {
                      percent: enrollment.discountPercent,
                      type: enrollment.discountType
                    })}</>
                  )}
                </p>
              </div>
              <span
                className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                  ENROLLMENT_STATUS_COLORS[enrollment.status] ?? 'bg-gray-100 text-gray-800'
                )}
              >
                {t(enrollmentStatusTranslations[enrollment.status])}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
