import { cn } from '@/lib/utils'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import type { Lesson, LessonStatus } from '@/features/lessons/types'

const LESSON_STATUS_COLORS: Record<LessonStatus, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Completed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-amber-100 text-amber-800',
}

interface CourseLessonHistoryCardProps {
  lessons: Lesson[]
}

export function CourseLessonHistoryCard({ lessons }: CourseLessonHistoryCardProps) {
  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">Recent Lessons</h3>
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
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
