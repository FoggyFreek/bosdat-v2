import { Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useCoursesData } from '@/features/courses/hooks/useCoursesData'
import { CourseListItem } from '@/features/courses/components/CourseListItem'

export function CoursesPage() {
  const { dayGroups, showLoading, isError, courses } = useCoursesData()

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

      {showLoading && (
        <div className="flex items-center justify-center py-8">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {!showLoading && isError && (
        <Card>
          <CardContent className="py-8 text-center">
            <p className="text-destructive">Failed to load courses. Please try again later.</p>
          </CardContent>
        </Card>
      )}

      {!showLoading && !isError && courses.length === 0 && (
        <Card>
          <CardContent className="py-8 text-center">
            <p className="text-muted-foreground">No courses found</p>
          </CardContent>
        </Card>
      )}

      {!showLoading && !isError && courses.length > 0 && (
        <div className="grid gap-6">
          {dayGroups.map((group) => (
            <Card key={group.day}>
              <CardHeader>
                <CardTitle>{group.dayName}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="divide-y">
                  {group.courses
                    .toSorted((a, b) => a.startTime.localeCompare(b.startTime))
                    .map((course) => (
                      <CourseListItem key={course.id} course={course} />
                    ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
