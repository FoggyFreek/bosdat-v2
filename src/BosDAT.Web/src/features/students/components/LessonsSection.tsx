import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { lessonsApi } from '@/services/api'
import type { Lesson } from '@/features/lessons/types'
import { formatDate, formatTime, cn } from '@/lib/utils'

interface LessonsSectionProps {
  studentId: string
}

const ITEMS_PER_PAGE = 10

export function LessonsSection({ studentId }: LessonsSectionProps) {
  const [currentPage, setCurrentPage] = useState(1)

  const { data: lessons = [] } = useQuery<Lesson[]>({
    queryKey: ['lessons', 'student', studentId],
    queryFn: () => lessonsApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  // Sort lessons by date descending (most recent first)
  const sortedLessons = useMemo(() => {
    return [...lessons].sort((a, b) => {
      const dateA = new Date(a.scheduledDate)
      const dateB = new Date(b.scheduledDate)
      return dateB.getTime() - dateA.getTime()
    })
  }, [lessons])

  const totalPages = Math.ceil(sortedLessons.length / ITEMS_PER_PAGE)
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE
  const paginatedLessons = sortedLessons.slice(startIndex, startIndex + ITEMS_PER_PAGE)

  const handlePreviousPage = () => {
    setCurrentPage((prev) => Math.max(1, prev - 1))
  }

  const handleNextPage = () => {
    setCurrentPage((prev) => Math.min(totalPages, prev + 1))
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Lessons</h2>

      <Card>
        <CardHeader>
          <CardTitle>Lesson History</CardTitle>
        </CardHeader>
        <CardContent>
          {sortedLessons.length === 0 ? (
            <p className="text-muted-foreground">No lessons yet</p>
          ) : (
            <>
              <div className="divide-y">
                {paginatedLessons.map((lesson) => (
                  <div key={lesson.id} className="flex items-center justify-between py-3">
                    <div>
                      <p className="font-medium">{lesson.instrumentName}</p>
                      <p className="text-sm text-muted-foreground">
                        {formatDate(lesson.scheduledDate)} - {formatTime(lesson.startTime)} to {formatTime(lesson.endTime)}
                      </p>
                      <p className="text-xs text-muted-foreground">{lesson.teacherName}</p>
                    </div>
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                        lesson.status === 'Scheduled' && 'bg-blue-100 text-blue-800',
                        lesson.status === 'Completed' && 'bg-green-100 text-green-800',
                        lesson.status === 'Cancelled' && 'bg-red-100 text-red-800',
                        lesson.status === 'NoShow' && 'bg-orange-100 text-orange-800'
                      )}
                    >
                      {lesson.status}
                    </span>
                  </div>
                ))}
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between pt-4 border-t mt-4">
                  <p className="text-sm text-muted-foreground">
                    Showing {startIndex + 1}-{Math.min(startIndex + ITEMS_PER_PAGE, sortedLessons.length)} of {sortedLessons.length} lessons
                  </p>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handlePreviousPage}
                      disabled={currentPage === 1}
                    >
                      <ChevronLeft className="h-4 w-4" />
                      Previous
                    </Button>
                    <span className="text-sm text-muted-foreground">
                      Page {currentPage} of {totalPages}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleNextPage}
                      disabled={currentPage === totalPages}
                    >
                      Next
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
