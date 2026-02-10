import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ArrowRightLeft, Ban, Filter, X } from 'lucide-react'
import type { Lesson, LessonStatus } from '@/features/lessons/types'
import type { LessonFilters } from '@/features/courses/hooks/useCourseDetailData'

const LESSON_STATUS_COLORS: Record<LessonStatus, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Completed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-amber-100 text-amber-800',
}

interface CourseLessonHistoryCardProps {
  lessons: Lesson[]
  courseId: string
  filters: LessonFilters
  onFiltersChange: (filters: LessonFilters) => void
  onCancelLesson: (lesson: Lesson) => void
}

export function CourseLessonHistoryCard({
  lessons,
  courseId,
  filters,
  onFiltersChange,
  onCancelLesson,
}: CourseLessonHistoryCardProps) {
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
          {hasActiveFilters ? `Lessons (${lessons.length} results)` : 'Recent Lessons'}
        </h3>
        <div className="flex items-center gap-2">
          <Button
            variant={showFilters ? 'secondary' : 'ghost'}
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="h-4 w-4 mr-1" />
            Filter
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate(`/courses/${courseId}/add-lesson`)}
          >
            Add lesson
          </Button>
        </div>
      </div>

      {showFilters && (
        <div className="flex flex-wrap items-end gap-3 mb-4 p-3 rounded-md bg-background border">
          <div className="flex flex-col gap-1">
            <Label htmlFor="filter-start-date" className="text-xs">
              From
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
              To
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
              Clear
            </Button>
          )}
        </div>
      )}

      {lessons.length === 0 && (
        <p className="text-sm text-muted-foreground">No lessons found</p>
      )}
      {lessons.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-2 font-medium text-muted-foreground">Date</th>
                <th className="pb-2 font-medium text-muted-foreground">Time</th>
                <th className="pb-2 font-medium text-muted-foreground">Student</th>
                <th className="pb-2 font-medium text-muted-foreground">Status</th>
                <th className="pb-2 font-medium text-muted-foreground text-right">Invoiced</th>
                <th className="pb-2 font-medium text-muted-foreground text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {lessons.map((lesson) => (
                <tr key={lesson.id}>
                  <td className="py-2">{formatDate(lesson.scheduledDate)}</td>
                  <td className="py-2">
                    {formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}
                  </td>
                  <td className="py-2">{lesson.studentName ?? '—'}</td>
                  <td className="py-2">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                        LESSON_STATUS_COLORS[lesson.status]
                      )}
                    >
                      {lesson.status}
                    </span>
                  </td>
                  <td className="py-2 text-right">
                    {lesson.isInvoiced ? (
                      <span className="text-green-600">Yes</span>
                    ) : (
                      <span className="text-muted-foreground">No</span>
                    )}
                  </td>
                  <td className="py-2 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 px-2"
                        title="Move lesson"
                        disabled={lesson.status !== 'Scheduled'}
                        onClick={() =>
                          navigate(`/courses/${courseId}/lessons/${lesson.id}/move`)
                        }
                      >
                        <ArrowRightLeft className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 px-2 text-destructive hover:text-destructive"
                        title="Cancel lesson"
                        disabled={lesson.status !== 'Scheduled'}
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
