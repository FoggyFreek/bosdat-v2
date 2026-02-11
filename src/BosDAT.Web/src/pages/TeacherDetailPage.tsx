import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Mail, Phone, MapPin, Music } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useTranslation } from 'react-i18next'
import { teachersApi } from '@/features/teachers/api'
import { useAuth } from '@/context/AuthContext'
import type { Teacher } from '@/features/teachers/types'
import type { CourseList } from '@/features/courses/types'
import { formatCurrency } from '@/lib/utils'
import { TeacherAvailabilitySection } from '@/features/teachers/components/TeacherAvailabilitySection'
import { CourseListItem } from '@/features/courses/components/CourseListItem'

const FINANCIAL_ADMIN_ROLE = 'FinancialAdmin'

export function TeacherDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()

  const canViewHourlyRate = user?.roles.includes(FINANCIAL_ADMIN_ROLE) || user?.roles.includes('Admin')

  const { data: teacher, isLoading } = useQuery<Teacher>({
    queryKey: ['teacher', id],
    queryFn: () => teachersApi.getById(id!),
    enabled: !!id,
  })

  const { data: coursesData } = useQuery<{ teacher: Teacher; courses: CourseList[] }>({
    queryKey: ['teacher', id, 'courses'],
    queryFn: () => teachersApi.getWithCourses(id!),
    enabled: !!id,
  })

  const courses = coursesData?.courses ?? []

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!teacher) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">{t('teachers.noTeachersFound')}</p>
        <Button asChild className="mt-4">
          <Link to="/teachers">{t('common.actions.back')}</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/teachers">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{teacher.fullName}</h1>
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
              teacher.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
            }`}
          >

            {teacher.isActive ? t('common.status.active') : t('common.status.inactive')}
          </span>
        </div>
        <Button asChild>
          <Link to={`/teachers/${id}/edit`}>{t('common.actions.edit')} {t('common.entities.teacher')}</Link>
        </Button>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('students.profile.contactInfo')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <Mail className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">{t('teachers.form.email')}</p>
                <p>{teacher.email}</p>
              </div>
            </div>
            {teacher.phone && (
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('teachers.form.phone')}</p>
                  <p>{teacher.phone}</p>
                </div>
              </div>
            )}
            {teacher.address && (
              <div className="flex items-center gap-3">
                <MapPin className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('teachers.form.address')}</p>
                  <p>
                    {teacher.address}
                    <br />
                    {teacher.postalCode} {teacher.city}
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('teachers.teacherDetails')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">{t('teachers.form.role')}</p>
              <p>{teacher.role}</p>
            </div>
            {canViewHourlyRate && (
              <div>
                <p className="text-sm text-muted-foreground">{t('teachers.form.hourlyRate')}</p>
                <p>{formatCurrency(teacher.hourlyRate)}</p>
              </div>
            )}
            <div className="flex items-center gap-3">
              <Music className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">{t('teachers.sections.instruments')}</p>
                <div className="flex flex-wrap gap-1 mt-1">
                  {teacher.instruments.length > 0 ? (
                    teacher.instruments.map((instrument) => (
                      <span
                        key={instrument.id}
                        className="inline-flex items-center rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary"
                      >
                        {instrument.name}
                      </span>
                    ))
                  ) : (
                    <p>{t('teachers.noInstrumentsAvailable')}</p>
                  )}
                </div>
              </div>
            </div>
            {teacher.notes && (
              <div>
                <p className="text-sm text-muted-foreground">{t('teachers.form.notes')}</p>
                <p>{teacher.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <TeacherAvailabilitySection teacherId={id!} />

      <Card>
        <CardHeader>
          <CardTitle>{t('teachers.sections.courses')}</CardTitle>
        </CardHeader>
        <CardContent>
          {courses.length === 0 && (
            <p className="text-muted-foreground">{t('courses.noCoursesFound')}</p>
          )}
          {courses.length > 0 && (
            <div className="divide-y">
              {courses
                .toSorted((a, b) => a.startTime.localeCompare(b.startTime))
                .map((course) => (
                  <CourseListItem key={course.id} course={course} />
                ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
