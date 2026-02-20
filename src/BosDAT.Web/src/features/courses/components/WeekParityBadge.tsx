import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import type { CourseList } from '@/features/courses/types'
import { weekParityTranslations } from '@/features/courses/types'

interface WeekParityBadgeProps {
  readonly course: CourseList
}

export function WeekParityBadge({ course }: WeekParityBadgeProps) {
  const { t } = useTranslation()

  if (course.frequency !== 'Biweekly' || !course.weekParity || course.weekParity === 'All') {
    return null
  }

  const variant = course.weekParity === 'Odd' ? 'default' : 'secondary'
  return (
    <Badge variant={variant} className="text-xs">
      {t(weekParityTranslations[course.weekParity])}
    </Badge>
  )
}
