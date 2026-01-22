import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { StudentForm } from '@/components/StudentForm'
import { studentsApi } from '@/services/api'
import type { Student, CreateStudent } from '@/features/students/types'
import { AxiosError } from 'axios'

interface ApiErrorResponse {
  message?: string
  errors?: Record<string, string[]>
}

export function StudentFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEditMode = !!id
  const [error, setError] = useState<string>()

  const { data: student, isLoading } = useQuery<Student>({
    queryKey: ['student', id],
    queryFn: () => studentsApi.getById(id!),
    enabled: isEditMode,
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateStudent) => studentsApi.create(data),
    onError: (err: AxiosError<ApiErrorResponse>) => {
      const message =
        err.response?.data?.message ||
        (err.response?.data?.errors
          ? Object.values(err.response.data.errors).flat().join(', ')
          : 'Failed to create student')
      setError(message)
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateStudent) => studentsApi.update(id!, data),
    onError: (err: AxiosError<ApiErrorResponse>) => {
      const message =
        err.response?.data?.message ||
        (err.response?.data?.errors
          ? Object.values(err.response.data.errors).flat().join(', ')
          : 'Failed to update student')
      setError(message)
    },
  })

  const handleSubmit = async (data: CreateStudent): Promise<{ id: string }> => {
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

  if (isEditMode && !student && !isLoading) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Student not found</p>
        <Button asChild className="mt-4">
          <Link to="/students">Back to Students</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to={isEditMode ? `/students/${id}` : '/students'}>
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <h1 className="text-3xl font-bold">
          {isEditMode ? 'Edit Student' : 'New Student'}
        </h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {isEditMode ? 'Edit Student Details' : 'Student Details'}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <StudentForm
            student={student}
            onSubmit={handleSubmit}
            isSubmitting={createMutation.isPending || updateMutation.isPending}
            error={error}
          />
        </CardContent>
      </Card>
    </div>
  )
}
