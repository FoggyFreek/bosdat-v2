import { Badge } from '@/components/ui/badge'
import type { CourseList } from '@/features/courses/types'

interface WeekParityBadgeProps {
  course: CourseList
}

export function WeekParityBadge({ course }: WeekParityBadgeProps) {
  if (course.frequency !== 'Biweekly' || !course.weekParity || course.weekParity === 'All') {
    return null
  }

  const variant = course.weekParity === 'Odd' ? 'default' : 'secondary'
  return (
    <Badge variant={variant} className="text-xs">
      {course.weekParity} Weeks
    </Badge>
  )
}
