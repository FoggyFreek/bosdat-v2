import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { getStatusColor } from '@/features/courses/hooks/useCoursesData'
import { WeekParityBadge } from './WeekParityBadge'
import type { CourseList, } from '@/features/courses/types'
import { courseStatusTranslations } from '@/features/courses/types'

interface CourseListItemProps {
  readonly course: CourseList
}

export function CourseListItem({ course }: CourseListItemProps) {
  const { t } = useTranslation()

  return (
    <Link
      to={`/courses/${course.id}`}
      className="flex items-center justify-between py-4 hover:bg-muted/50 -mx-6 px-6 transition-colors"
    >
      <div className="flex items-center gap-4">
        <div className="text-center min-w-[60px]">
          <p className="text-sm font-medium">
            {course.startTime.substring(0, 5)}
          </p>
          <p className="text-xs text-muted-foreground">
            {course.endTime.substring(0, 5)}
          </p>
        </div>
        <div>
          <p className="font-medium">{course.instrumentName}</p>
          <p className="text-sm text-muted-foreground">
            {course.teacherName} - {course.courseTypeName}
          </p>
          {course.roomName && (
            <p className="text-xs text-muted-foreground">{course.roomName}</p>
          )}
        </div>
      </div>
      <div className="flex items-center gap-4">
        <div className="text-right">
          <p className="text-sm">{t('courses.list.enrolled')}, {course.enrollmentCount} </p>
        </div>
        <div className="flex items-center gap-2">
          <WeekParityBadge course={course} />
          <span
            className={cn(
              'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
              getStatusColor(course.status)
            )}
          >  {t(courseStatusTranslations[course.status])}
          </span>
        </div>
      </div>
    </Link>
  )
}
