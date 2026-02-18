import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ArrowRightLeft, Ban, Filter, X } from 'lucide-react'
import type { Lesson, LessonStatus } from '@/features/lessons/types'
import { lessonStatusTranslations } from '@/features/lessons/types'
import type { LessonFilters } from '@/features/courses/hooks/useCourseDetailData'

const LESSON_STATUS_COLORS: Record<LessonStatus, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Completed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-amber-100 text-amber-800',
}

interface CourseLessonHistoryCardProps {
  lessons: Lesson[]
  courseId?: string
  filters: LessonFilters
  onFiltersChange: (filters: LessonFilters) => void
  onCancelLesson: (lesson: Lesson) => void
  showCourseName?: boolean
}

export function CourseLessonHistoryCard({
  lessons,
  courseId,
  filters,
  onFiltersChange,
  onCancelLesson,
  showCourseName = false,
}: CourseLessonHistoryCardProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [showFilters, setShowFilters] = useState(
    !!filters.startDate || !!filters.endDate
  )

  const hasActiveFilters = !!filters.startDate || !!filters.endDate

  const handleClearFilters = () => {
    onFiltersChange({ startDate: undefined, endDate: undefined })
  }

  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-medium text-sm">
          {hasActiveFilters
            ? t('courses.lessonHistory.results', { count: lessons.length })
            : t('courses.lessonHistory.recentTitle')}
        </h3>
        <div className="flex items-center gap-2">
          <Button
            variant={showFilters ? 'secondary' : 'ghost'}
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="h-4 w-4 mr-1" />
            {t('common.actions.filter')}
          </Button>
          {courseId && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate(`/courses/${courseId}/add-lesson`)}
            >
              {t('lessons.addLesson')}
            </Button>
          )}
        </div>
      </div>

      {showFilters && (
        <div className="flex flex-wrap items-end gap-3 mb-4 p-3 rounded-md bg-background border">
          <div className="flex flex-col gap-1">
            <Label htmlFor="filter-start-date" className="text-xs">
              {t('courses.lessonHistory.from')}
            </Label>
            <Input
              id="filter-start-date"
              type="date"
              className="w-[160px] h-8 text-sm"
              value={filters.startDate ?? ''}
              onChange={(e) =>
                onFiltersChange({ ...filters, startDate: e.target.value || undefined })
              }
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor="filter-end-date" className="text-xs">
              {t('courses.lessonHistory.to')}
            </Label>
            <Input
              id="filter-end-date"
              type="date"
              className="w-[160px] h-8 text-sm"
              value={filters.endDate ?? ''}
              onChange={(e) =>
                onFiltersChange({ ...filters, endDate: e.target.value || undefined })
              }
            />
          </div>
          {hasActiveFilters && (
            <Button variant="ghost" size="sm" onClick={handleClearFilters}>
              <X className="h-4 w-4 mr-1" />
              {t('courses.lessonHistory.clear')}
            </Button>
          )}
        </div>
      )}

      {lessons.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('courses.lessonHistory.noLessons')}</p>
      )}
      {lessons.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-2 font-medium text-muted-foreground">{t('courses.lessonHistory.table.date')}</th>
                <th className="pb-2 font-medium text-muted-foreground">{t('courses.lessonHistory.table.time')}</th>
                {showCourseName && <th className="pb-2 font-medium text-muted-foreground">{t('common.entities.course')}</th>}
                {!showCourseName && <th className="pb-2 font-medium text-muted-foreground">{t('common.entities.student')}</th>}
                <th className="pb-2 font-medium text-muted-foreground">{t('courses.lessonHistory.table.status')}</th>
                <th className="pb-2 font-medium text-muted-foreground text-right">{t('courses.lessonHistory.table.invoiced')}</th>
                <th className="pb-2 font-medium text-muted-foreground text-right">{t('courses.lessonHistory.table.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {lessons.map((lesson) => (
                <tr key={lesson.id}>
                  <td className="py-2">
                    <Link
                      to={`/lessons/${lesson.id}`}
                      className="hover:underline text-primary"
                    >
                      {formatDate(lesson.scheduledDate)}
                    </Link>
                  </td>
                  <td className="py-2">
                    {formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}
                  </td>
                  {showCourseName && (
                    <td className="py-2">
                      {lesson.instrumentName} – {lesson.courseTypeName}
                    </td>
                  )}
                  {!showCourseName && <td className="py-2">{lesson.studentName ?? '—'}</td>}
                  <td className="py-2">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                        LESSON_STATUS_COLORS[lesson.status]
                      )}
                    >
                      {t(lessonStatusTranslations[lesson.status])}
                    </span>
                  </td>
                  <td className="py-2 text-right">
                    {lesson.isInvoiced ? (
                      <span className="text-green-600">{t('common.form.yes')}</span>
                    ) : (
                      <span className="text-muted-foreground">{t('common.form.no')}</span>
                    )}
                  </td>
                  <td className="py-2 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 px-2"
                        title={t('lessons.moveLesson')}
                        disabled={lesson.isInvoiced || lesson.status !== 'Scheduled'}
                        onClick={() =>
                          navigate(`/courses/${courseId ?? lesson.courseId}/lessons/${lesson.id}/move`)
                        }
                      >
                        <ArrowRightLeft className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 px-2 text-destructive hover:text-destructive"
                        title={t('lessons.cancelLesson')}
                        disabled={lesson.isInvoiced || lesson.status !== 'Scheduled'}
                        onClick={() => onCancelLesson(lesson)}
                      >
                        <Ban className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
