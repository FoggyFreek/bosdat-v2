import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus, Search, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { teachersApi } from '@/features/teachers/api'
import type { TeacherList } from '@/features/teachers/types'
import { cn } from '@/lib/utils'

export function TeachersPage() {
  const [search, setSearch] = useState('')

  const { data: teachers = [], isLoading } = useQuery<TeacherList[]>({
    queryKey: ['teachers'],
    queryFn: () => teachersApi.getAll(),
  })

  const filteredTeachers = teachers.filter(
    (t) =>
      t.fullName.toLowerCase().includes(search.toLowerCase()) ||
      t.email.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Teachers</h1>
          <p className="text-muted-foreground">Manage your music school teachers</p>
        </div>
        <Button asChild>
          <Link to="/teachers/new">
            <Plus className="h-4 w-4 mr-2" />
            Add Teacher
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Teachers</CardTitle>
          <div className="relative max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search teachers..."
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

          {!isLoading && filteredTeachers.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">No teachers found</p>
            </div>
          )}

          {!isLoading && filteredTeachers.length > 0 && (
            <div className="divide-y">
              {filteredTeachers.map((teacher) => (
                <Link
                  key={teacher.id}
                  to={`/teachers/${teacher.id}`}
                  className="flex items-center justify-between py-4 hover:bg-muted/50 -mx-6 px-6 transition-colors"
                >
                  <div className="flex items-center gap-4">
                    <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center">
                      <span className="text-sm font-medium text-primary">
                        {teacher.fullName
                          .split(' ')
                          .map((n) => n[0])
                          .join('')
                          .toUpperCase()
                          .slice(0, 2)}
                      </span>
                    </div>
                    <div>
                      <p className="font-medium">{teacher.fullName}</p>
                      <p className="text-sm text-muted-foreground">{teacher.email}</p>
                      {teacher.instruments.length > 0 && (
                        <p className="text-xs text-muted-foreground">
                          {teacher.instruments.join(', ')}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                        teacher.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-gray-100 text-gray-800'
                      )}
                    >
                      {teacher.isActive ? 'Active' : 'Inactive'}
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
