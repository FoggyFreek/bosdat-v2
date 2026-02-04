import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { coursesApi } from '@/services/api'
import { getDayNameFromNumber } from '@/lib/iso-helpers'
import type { CourseList } from '@/features/courses/types'
import { cn, getDayName } from '@/lib/utils'

export function CoursesPage() {
  const { data: courses = [], isLoading } = useQuery<CourseList[]>({
    queryKey: ['courses'],
    queryFn: () => coursesApi.getSummary(),
  })

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800'
      case 'Paused':
        return 'bg-yellow-100 text-yellow-800'
      case 'Completed':
        return 'bg-blue-100 text-blue-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getWeekParityBadge = (course: CourseList) => {
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

  // Group courses by day
  const coursesByDay = courses.reduce((acc, course) => {
    const day = course.dayOfWeek
    if (!acc[day]) {
      acc[day] = []
    }
    acc[day].push(course)
    return acc
  }, {} as Record<string, CourseList[]>)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Courses</h1>
          <p className="text-muted-foreground">Manage your recurring courses</p>
        </div>
        <Button asChild>
          <Link to="/enrollments/new">
            <Plus className="h-4 w-4 mr-2" />
            Add Course
          </Link>
        </Button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-8">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {!isLoading && courses.length === 0 && (
        <Card>
          <CardContent className="py-8 text-center">
            <p className="text-muted-foreground">No courses found</p>
          </CardContent>
        </Card>
      )}

      {!isLoading && courses.length > 0 && (
        <div className="grid gap-6">
          {[1, 2, 3, 4, 5, 6, 0].map((day: number) => {
            const dayCourses = coursesByDay[getDayNameFromNumber(day)]  || []
            if (dayCourses.length === 0) return null

            return (
              <Card key={day}>
                <CardHeader>
                  <CardTitle>{getDayName(day)}</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="divide-y">
                    {dayCourses
                      .toSorted((a, b) => a.startTime.localeCompare(b.startTime))
                      .map((course) => (
                        <Link
                          key={course.id}
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
                              <p className="text-sm">{course.enrollmentCount} enrolled</p>
                            </div>
                            <div className="flex items-center gap-2">
                              {getWeekParityBadge(course)}
                              <span
                                className={cn(
                                  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                                  getStatusColor(course.status)
                                )}
                              >
                                {course.status}
                              </span>
                            </div>
                          </div>
                        </Link>
                      ))}
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
