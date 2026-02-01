import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { StudentForm } from '@/features/students/components/StudentForm'
import { studentsApi } from '@/services/api'
import type { CreateStudent, Student } from '@/features/students/types'

interface AddStudentModalProps {
  readonly open: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly onStudentCreated: (student: Student) => void
}

export const AddStudentModal = ({
  open,
  onOpenChange,
  onStudentCreated,
}: AddStudentModalProps) => {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string>()

  const createMutation = useMutation({
    mutationFn: (data: CreateStudent) => studentsApi.create(data),
    onSuccess: (newStudent: Student) => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
      onStudentCreated(newStudent)
      onOpenChange(false)
    },
    onError: (err: Error & { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to create student')
    },
  })

  const handleSubmit = async (data: CreateStudent) => {
    setError(undefined)
    return createMutation.mutateAsync(data)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add New Student</DialogTitle>
        </DialogHeader>
        <StudentForm
          onSubmit={handleSubmit}
          isSubmitting={createMutation.isPending}
          error={error}
          onSuccess={onStudentCreated}
        />
      </DialogContent>
    </Dialog>
  )
}
