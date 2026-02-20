import { useTranslation } from 'react-i18next'
import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Music, Trash2, ArrowUpCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { enrollmentsApi } from '@/features/enrollments/api'
import { coursesApi } from '@/features/courses/api'
import type { StudentEnrollment } from '@/features/students/types'
import type { CourseList } from '@/features/courses/types'
import { enrollmentStatusTranslations } from '@/features/enrollments/types'
import { cn } from '@/lib/utils'
import { formatTime } from '@/lib/datetime-helpers'

interface EnrollmentsSectionProps {
  readonly studentId: string
}

export function EnrollmentsSection({ studentId }: EnrollmentsSectionProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [showEnrollForm, setShowEnrollForm] = useState(false)
  const [selectedCourse, setSelectedCourse] = useState('')

  const { data: enrollments = [] } = useQuery<StudentEnrollment[]>({
    queryKey: ['enrollments', 'student', studentId],
    queryFn: () => enrollmentsApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const { data: courses = [] } = useQuery<CourseList[]>({
    queryKey: ['courses', 'active'],
    queryFn: () => coursesApi.getAll({ status: 'Active' }),
    enabled: showEnrollForm,
  })

  const enrollMutation = useMutation({
    mutationFn: (courseId: string) =>
      enrollmentsApi.create({
        studentId,
        courseId,
        invoicingPreference: 'Monthly'
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments', 'student', studentId] })
      setShowEnrollForm(false)
      setSelectedCourse('')
    },
  })

  const withdrawMutation = useMutation({
    mutationFn: (enrollmentId: string) => enrollmentsApi.delete(enrollmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments', 'student', studentId] })
    },
  })

  const promoteMutation = useMutation({
    mutationFn: (enrollmentId: string) => enrollmentsApi.promoteFromTrail(enrollmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student', studentId, 'registration-fee'] })
    },
  })

  const handleEnroll = () => {
    if (selectedCourse) {
      enrollMutation.mutate(selectedCourse)
    }
  }

  // Filter out courses the student is already enrolled in
  const availableCourses = courses.filter(
    (course) => !enrollments.some((e) => e.courseId === course.id)
  )

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">{t('students.sections.enrollments')}</h2>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>{t('students.enrollments.courseEnrollments')}</CardTitle>
          <Button size="sm" onClick={() => setShowEnrollForm(!showEnrollForm)}>
            <Plus className="h-4 w-4 mr-2" />
            {t('students.enrollments.enrollInCourse')}
          </Button>
        </CardHeader>
        <CardContent>
          {showEnrollForm && (
            <div className="flex gap-2 mb-4 p-4 bg-muted/50 rounded-lg">
              <Select value={selectedCourse} onValueChange={setSelectedCourse}>
                <SelectTrigger className="flex-1">
                  <SelectValue placeholder={t('students.enrollments.selectCourse')} />
                </SelectTrigger>
                <SelectContent>
                  {availableCourses.length === 0 ? (
                    <SelectItem value="" disabled>
                      {t('students.enrollments.noAvailableCourses')}
                    </SelectItem>
                  ) : (
                    availableCourses.map((course) => (
                      <SelectItem key={course.id} value={course.id}>
                        {course.instrumentName} - {course.teacherName} ({course.dayOfWeek} {formatTime(course.startTime)})
                      </SelectItem>
                    ))
                  )}
                </SelectContent>
              </Select>
              <Button onClick={handleEnroll} disabled={!selectedCourse || enrollMutation.isPending}>
                {enrollMutation.isPending ? t('students.enrollments.enrolling') : t('students.enrollments.enroll')}
              </Button>
              <Button variant="outline" onClick={() => setShowEnrollForm(false)}>
                {t('common.actions.cancel')}
              </Button>
            </div>
          )}

          {enrollments.length === 0 ? (
            <p className="text-muted-foreground">{t('students.enrollments.noEnrollments')}</p>
          ) : (
            <div className="divide-y">
              {enrollments.map((enrollment) => (
                <div key={enrollment.id} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <Music className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">{enrollment.instrumentName}</p>
                      <p className="text-sm text-muted-foreground">
                        {enrollment.teacherName} - {enrollment.dayOfWeek} {formatTime(enrollment.startTime)}
                      </p>
                      {enrollment.roomName && (
                        <p className="text-xs text-muted-foreground">{enrollment.roomName}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                        enrollment.status === 'Active' && 'bg-green-100 text-green-800',
                        enrollment.status === 'Trail' && 'bg-yellow-100 text-yellow-800',
                        enrollment.status !== 'Active' && enrollment.status !== 'Trail' && 'bg-gray-100 text-gray-800'
                      )}
                    >
                      {t(enrollmentStatusTranslations[enrollment.status])}
                    </span>
                    {enrollment.status === 'Trail' && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                        onClick={() => promoteMutation.mutate(enrollment.id)}
                        disabled={promoteMutation.isPending}
                      >
                        <ArrowUpCircle className="h-4 w-4 mr-1" />
                        {t('students.enrollments.promote')}
                      </Button>
                    )}
                    {(enrollment.status === 'Active' || enrollment.status === 'Trail') && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50"
                        onClick={() => withdrawMutation.mutate(enrollment.id)}
                        disabled={withdrawMutation.isPending}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
