import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { TeacherForm } from '@/components/TeacherForm'
import { teachersApi } from '@/services/api'
import type { Teacher, CreateTeacher } from '@/types'
import { AxiosError } from 'axios'

interface ApiErrorResponse {
  message?: string
  errors?: Record<string, string[]>
}

export function TeacherFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEditMode = !!id
  const [error, setError] = useState<string>()

  const { data: teacher, isLoading } = useQuery<Teacher>({
    queryKey: ['teacher', id],
    queryFn: () => teachersApi.getById(id!),
    enabled: isEditMode,
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateTeacher) => teachersApi.create(data),
    onError: (err: AxiosError<ApiErrorResponse>) => {
      const message =
        err.response?.data?.message ||
        (err.response?.data?.errors
          ? Object.values(err.response.data.errors).flat().join(', ')
          : 'Failed to create teacher')
      setError(message)
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateTeacher) => teachersApi.update(id!, data),
    onError: (err: AxiosError<ApiErrorResponse>) => {
      const message =
        err.response?.data?.message ||
        (err.response?.data?.errors
          ? Object.values(err.response.data.errors).flat().join(', ')
          : 'Failed to update teacher')
      setError(message)
    },
  })

  const handleSubmit = async (data: CreateTeacher): Promise<{ id: string }> => {
    setError(undefined)
    if (isEditMode) {
      await updateMutation.mutateAsync(data)
      return { id: id! }
    } else {
      const result = await createMutation.mutateAsync(data)
      return result
    }
  }

  if (isEditMode && isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (isEditMode && !teacher && !isLoading) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Teacher not found</p>
        <Button asChild className="mt-4">
          <Link to="/teachers">Back to Teachers</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to={isEditMode ? `/teachers/${id}` : '/teachers'}>
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <h1 className="text-3xl font-bold">
          {isEditMode ? 'Edit Teacher' : 'New Teacher'}
        </h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {isEditMode ? 'Edit Teacher Details' : 'Teacher Details'}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <TeacherForm
            teacher={teacher}
            onSubmit={handleSubmit}
            isSubmitting={createMutation.isPending || updateMutation.isPending}
            error={error}
          />
        </CardContent>
      </Card>
    </div>
  )
}
