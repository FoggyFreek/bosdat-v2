import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Clock, ExternalLink, MapPin, User, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { lessonsApi } from '@/features/lessons/api'
import { lessonStatusTranslations } from '@/features/lessons/types'
import { CourseTasksSection } from '@/features/course-tasks/components/CourseTasksSection'
import { LessonNotesSection } from '@/features/lesson-notes/components/LessonNotesSection'
import type { Lesson } from '@/features/lessons/types'
import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'

const STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Completed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-amber-100 text-amber-800',
}

function AvatarInitials({ name }: Readonly<{ name: string }>) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(w => w[0].toUpperCase())
    .join('')

  return (
    <div className="h-7 w-7 rounded-full bg-primary/10 text-primary text-xs font-medium flex items-center justify-center shrink-0">
      {initials}
    </div>
  )
}

interface LessonDetailPanelProps {
  lessonId: string
  onClose: () => void
}

export function LessonDetailPanel({ lessonId, onClose }: Readonly<LessonDetailPanelProps>) {
  const { t } = useTranslation()

  const { data: lesson, isLoading, isError } = useQuery<Lesson>({
    queryKey: ['lessons', lessonId],
    queryFn: () => lessonsApi.getById(lessonId),
  })

  return (
    <div className="flex flex-col h-full border-l bg-white overflow-y-auto">
      {/* Panel Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b shrink-0">
        <span className="text-sm font-medium text-muted-foreground">
          {t('lessons.detail.title')}
        </span>
        <Button variant="ghost" size="icon" className="h-7 w-7" onClick={onClose}>
          <X className="h-4 w-4" />
        </Button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-8 flex-1">
          <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {isError && (
        <div className="px-4 py-8 text-center text-sm text-muted-foreground">
          {t('lessons.lessonNotFound')}
        </div>
      )}

      {lesson && (
        <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
          {/* Title row */}
          <div className="flex items-start gap-2">
            <div className="flex-1 min-w-0">
              <h2 className="text-base font-semibold truncate">
                {lesson.instrumentName} – {lesson.courseTypeName}
              </h2>
              <p className="text-xs text-muted-foreground">{formatDate(lesson.scheduledDate)}</p>
            </div>
            <div className="flex items-center gap-1.5 shrink-0">
              <span
                className={cn(
                  'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                  STATUS_COLORS[lesson.status] ?? 'bg-muted text-muted-foreground'
                )}
              >
                {t(lessonStatusTranslations[lesson.status])}
              </span>
              <Button variant="ghost" size="icon" className="h-6 w-6" asChild title={t('lessons.detail.openFull')}>
                <Link to={`/lessons/${lesson.id}`}>
                  <ExternalLink className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </div>
          </div>

          {/* Meta */}
          <div className="rounded-lg border p-3 space-y-2">
            <div className="flex items-center gap-2 text-xs">
              <Clock className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
              <span>{formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}</span>
            </div>

            {lesson.roomName && (
              <div className="flex items-center gap-2 text-xs">
                <MapPin className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                <span>{lesson.roomName}</span>
              </div>
            )}

            {(lesson.studentName || lesson.teacherName) && (
              <div className="flex items-start gap-2 text-xs">
                <User className="h-3.5 w-3.5 text-muted-foreground shrink-0 mt-0.5" />
                <div className="flex flex-wrap gap-1.5">
                  {lesson.studentName && (
                    <div className="flex items-center gap-1">
                      <AvatarInitials name={lesson.studentName} />
                      <span>{lesson.studentName}</span>
                    </div>
                  )}
                  {lesson.teacherName && (
                    <div className="flex items-center gap-1">
                      <AvatarInitials name={lesson.teacherName} />
                      <span>{lesson.teacherName}</span>
                    </div>
                  )}
                </div>
              </div>
            )}

            <div className="flex items-center gap-2">
              <Badge variant="outline" className="text-xs">
                {t('common.entities.course')}
              </Badge>
              <Link
                to={`/courses/${lesson.courseId}`}
                className="text-xs text-primary hover:underline"
              >
                {lesson.courseTypeName}
              </Link>
            </div>
          </div>

          {/* Tasks */}
          <CourseTasksSection courseId={lesson.courseId} />

          {/* Notes */}
          <LessonNotesSection
            lessonId={lesson.id}
            lessonDate={lesson.scheduledDate}
          />
        </div>
      )}
    </div>
  )
}
