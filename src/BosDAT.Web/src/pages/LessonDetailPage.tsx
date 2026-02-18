import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, Clock, MapPin, User } from 'lucide-react'
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

function AvatarInitials({ name }: { name: string }) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(w => w[0].toUpperCase())
    .join('')

  return (
    <div className="h-8 w-8 rounded-full bg-primary/10 text-primary text-xs font-medium flex items-center justify-center shrink-0">
      {initials}
    </div>
  )
}

export function LessonDetailPage() {
  const { lessonId } = useParams<{ lessonId: string }>()
  const { t } = useTranslation()

  const { data: lesson, isLoading, isError } = useQuery<Lesson>({
    queryKey: ['lessons', lessonId],
    queryFn: () => lessonsApi.getById(lessonId!),
    enabled: !!lessonId,
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (isError || !lesson) {
    return (
      <div className="text-center py-8 space-y-4">
        <p className="text-muted-foreground">{t('lessons.lessonNotFound')}</p>
        <Button asChild variant="outline">
          <Link to="/courses">{t('lessons.actions.backToCourses')}</Link>
        </Button>
      </div>
    )
  }

  const participants: string[] = []
  if (lesson.studentName) participants.push(lesson.studentName)
  if (lesson.teacherName) participants.push(lesson.teacherName)

  return (
    <div className="max-w-2xl mx-auto space-y-6 pb-8">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" asChild>
          <Link to={`/courses/${lesson.courseId}`}>
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1 min-w-0">
          <h1 className="text-xl font-semibold truncate">
            {lesson.instrumentName} – {lesson.courseTypeName}
          </h1>
          <p className="text-sm text-muted-foreground">{formatDate(lesson.scheduledDate)}</p>
        </div>
        <span
          className={cn(
            'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
            STATUS_COLORS[lesson.status] ?? 'bg-muted text-muted-foreground'
          )}
        >
          {t(lessonStatusTranslations[lesson.status])}
        </span>
      </div>

      {/* Meta */}
      <div className="rounded-lg border p-4 space-y-3">
        <div className="flex items-center gap-2 text-sm">
          <Clock className="h-4 w-4 text-muted-foreground shrink-0" />
          <span>
            {formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}
          </span>
        </div>

        {lesson.roomName && (
          <div className="flex items-center gap-2 text-sm">
            <MapPin className="h-4 w-4 text-muted-foreground shrink-0" />
            <span>{lesson.roomName}</span>
          </div>
        )}

        {participants.length > 0 && (
          <div className="flex items-start gap-2 text-sm">
            <User className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5" />
            <div className="flex flex-wrap gap-1.5">
              {participants.map(name => (
                <div key={name} className="flex items-center gap-1.5">
                  <AvatarInitials name={name} />
                  <span>{name}</span>
                </div>
              ))}
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
  )
}
