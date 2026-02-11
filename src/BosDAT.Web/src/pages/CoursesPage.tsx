import { Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useCoursesData } from '@/features/courses/hooks/useCoursesData'
import { CourseListItem } from '@/features/courses/components/CourseListItem'

export function CoursesPage() {
  const { t } = useTranslation()
  const { dayGroups, showLoading, isError, courses } = useCoursesData()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('courses.title')}</h1>
          <p className="text-muted-foreground">{t('courses.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/enrollments/new">
            <Plus className="h-4 w-4 mr-2" />
            {t('courses.addCourse')}
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
            <p className="text-destructive">{t('courses.loadFailed')}</p>
          </CardContent>
        </Card>
      )}

      {!showLoading && !isError && courses.length === 0 && (
        <Card>
          <CardContent className="py-8 text-center">
            <p className="text-muted-foreground">{t('courses.noCoursesFound')}</p>
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
