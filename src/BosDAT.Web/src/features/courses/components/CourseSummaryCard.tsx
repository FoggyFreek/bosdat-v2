import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { formatDate, formatTime, dayOfWeekTranslations } from '@/lib/datetime-helpers'
import { getStatusColor } from '@/features/courses/hooks/useCoursesData'
import { WeekParityBadge } from './WeekParityBadge'
import type { Course } from '@/features/courses/types'
import { courseFrequencyTranslations, courseStatusTranslations } from '@/features/courses/types'

interface CourseSummaryCardProps {
  readonly course: Course
}

function Row({ label, children }: { readonly label: string; readonly children: React.ReactNode }) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{children}</span>
    </div>
  )
}

export function CourseSummaryCard({ course }: CourseSummaryCardProps) {
  const { t } = useTranslation()

  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">{t('courses.sections.summary')}</h3>
      <div className="grid gap-2 text-sm">
        <Row label={`${t('common.entities.instrument')}:`}>{course.instrumentName}</Row>
        <Row label={`${t('courses.summary.courseType')}:`}>{course.courseTypeName}</Row>
        <Row label={`${t('common.entities.teacher')}:`}>{course.teacherName}</Row>
        <Row label={`${t('courses.summary.day')}:`}>{t(dayOfWeekTranslations[course.dayOfWeek])}</Row>
        <Row label={`${t('courses.summary.time')}:`}>{formatTime(course.startTime)} – {formatTime(course.endTime)}</Row>
        <Row label={`${t('courses.summary.startDate')}:`}>{formatDate(course.startDate)}</Row>
        {course.endDate && <Row label={`${t('courses.summary.endDate')}:`}>{formatDate(course.endDate)}</Row>}
        <Row label={`${t('courses.summary.frequency')}:`}>
          <span className="flex items-center gap-2">
            {t(courseFrequencyTranslations[course.frequency])}
            <WeekParityBadge course={course} />
          </span>
        </Row>
        {course.roomName && <Row label={`${t('common.entities.room')}:`}>{course.roomName}</Row>}
        <Row label={`${t('courses.summary.status')}:`}>
          <span
            className={cn(
              'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
              getStatusColor(course.status)
            )}
          >
            {t(courseStatusTranslations[course.status])}
          </span>
        </Row>
        {course.isTrial && <Row label={`${t('courses.summary.type')}:`}><span className="text-amber-600">{t('common.status.trial')}</span></Row>}
        {course.isWorkshop && <Row label={`${t('courses.summary.workshop')}:`}>{t('common.form.yes')}</Row>}
        {course.notes && <Row label={`${t('courses.summary.notes')}:`}><span className="text-right max-w-[60%]">{course.notes}</span></Row>}
      </div>
    </div>
  )
}
