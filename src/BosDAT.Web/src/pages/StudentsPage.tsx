import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus, Search, ChevronRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentsApi } from '@/features/students/api'
import type { StudentList } from '@/features/students/types'
import { cn } from '@/lib/utils'

export function StudentsPage() {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')

  const { data: students = [], isLoading } = useQuery<StudentList[]>({
    queryKey: ['students', search],
    queryFn: () => studentsApi.getAll({ search: search || undefined }),
  })

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800'
      case 'Trial':
        return 'bg-yellow-100 text-yellow-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('students.title')}</h1>
          <p className="text-muted-foreground">{t('students.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/students/new">
            <Plus className="h-4 w-4 mr-2" />
            {t('students.addStudent')}
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('students.allStudents')}</CardTitle>
          <div className="relative max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder={t('students.searchPlaceholder')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10"
            />
          </div>
        </CardHeader>
        <CardContent>
          {isLoading && (
            <div className="flex items-center justify-center py-8">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          )}

          {!isLoading && students.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">{t('students.noStudentsFound')}</p>
            </div>
          )}

          {!isLoading && students.length > 0 && (
            <div className="divide-y">
              {students.map((student) => (
                <Link
                  key={student.id}
                  to={`/students/${student.id}`}
                  className="flex items-center justify-between py-4 hover:bg-muted/50 -mx-6 px-6 transition-colors"
                >
                  <div className="flex items-center gap-4">
                    <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center">
                      <span className="text-sm font-medium text-primary">
                        {student.fullName
                          .split(' ')
                          .map((n) => n[0])
                          .join('')
                          .toUpperCase()
                          .slice(0, 2)}
                      </span>
                    </div>
                    <div>
                      <p className="font-medium">{student.fullName}</p>
                      <p className="text-sm text-muted-foreground">{student.email}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                        getStatusColor(student.status)
                      )}
                    >
                      {student.status}
                    </span>
                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                  </div>
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
