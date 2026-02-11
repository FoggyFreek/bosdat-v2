import { useQuery } from '@tanstack/react-query'
import { Users, GraduationCap, Music } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentsApi } from '@/features/students/api'
import { teachersApi } from '@/features/teachers/api'
import { coursesApi } from '@/features/courses/api'
import type { StudentList } from '@/features/students/types'
import type { TeacherList } from '@/features/teachers/types'

const getStatusBadgeClass = (status: string) => {
  if (status === 'Active') {
    return 'bg-green-100 text-green-800'
  }
  if (status === 'Trial') {
    return 'bg-yellow-100 text-yellow-800'
  }
  return 'bg-gray-100 text-gray-800'
}

export function DashboardPage() {
  const { t } = useTranslation()

  const { data: students = [] } = useQuery<StudentList[]>({
    queryKey: ['students'],
    queryFn: () => studentsApi.getAll(),
  })

  const { data: teachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers'],
    queryFn: () => teachersApi.getAll(),
  })

  const { data: courses = 0 } = useQuery<number>({
    queryKey: ['courses', 'count', { status: 'Active' }],
    queryFn: () => coursesApi.getCount(
      { status: 'Active' }
    ),
  })

  const activeStudents = students.filter((s) => s.status === 'Active').length
  const activeTeachers = teachers.filter((t) => t.isActive).length
  const activeCourses = courses

  const stats = [
    {
      name: t('dashboard.stats.activeStudents'),
      value: activeStudents,
      icon: Users,
      description: t('dashboard.stats.totalStudents', { count: students.length }),
    },
    {
      name: t('dashboard.stats.activeTeachers'),
      value: activeTeachers,
      icon: GraduationCap,
      description: t('dashboard.stats.totalTeachers', { count: teachers.length }),
    },
    {
      name: t('dashboard.stats.activeCourses'),
      value: activeCourses,
      icon: Music,
      description: t('dashboard.stats.totalCourses', { count: activeCourses }),
    }
  ]

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">{t('dashboard.subtitle')}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.name}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">{stat.name}</CardTitle>
              <stat.icon className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground">{stat.description}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.recentStudents.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {students.slice(0, 5).map((student) => (
                <div key={student.id} className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">{student.fullName}</p>
                    <p className="text-sm text-muted-foreground">{student.email}</p>
                  </div>
                  <span
                    className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusBadgeClass(student.status)}`}
                  >
                    {student.status}
                  </span>
                </div>
              ))}
              {students.length === 0 && (
                <p className="text-sm text-muted-foreground">{t('dashboard.recentStudents.noStudents')}</p>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.todaysSchedule.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
                <p className="text-sm text-muted-foreground">{t('dashboard.todaysSchedule.comingSoon')}</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
