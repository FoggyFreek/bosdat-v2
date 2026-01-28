import { useState, useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { studentsApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import type { StudentList } from '@/features/students/types'
import type { EnrollmentGroupMember } from '../types'

const getStatusBadgeVariant = (status: string) => {
  if (status === 'Active') return 'default'
  if (status === 'Trial') return 'secondary'
  return 'outline'
}

interface StudentSearchPanelProps {
  courseStartDate: string
  maxStudents: number
  onAddNewStudent: () => void
}

export const StudentSearchPanel = ({
  courseStartDate,
  maxStudents,
  onAddNewStudent,
}: StudentSearchPanelProps) => {
  const [searchTerm, setSearchTerm] = useState('')
  const { formData, addStudent } = useEnrollmentForm()
  const { students: selectedStudents } = formData.step2

  const { data: allStudents = [], isLoading } = useQuery<StudentList[]>({
    queryKey: ['students', 'search', searchTerm],
    queryFn: () => studentsApi.getAll({ search: searchTerm || undefined }),
    enabled: searchTerm.length >= 2,
    staleTime: 30000,
  })

  const selectedStudentIds = useMemo(
    () => new Set(selectedStudents.map((s) => s.studentId)),
    [selectedStudents]
  )

  const filteredStudents = useMemo(() => {
    return allStudents
      .filter((s) => !selectedStudentIds.has(s.id))
      .slice(0, 10)
  }, [allStudents, selectedStudentIds])

  const canAddMore = selectedStudents.length < maxStudents

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value)
  }

  const handleSelectStudent = useCallback(
    async (student: StudentList) => {
      if (!canAddMore) return

      // Check if student has active enrollments for course discount eligibility
      let isEligibleForCourseDiscount = false
      try {
        isEligibleForCourseDiscount = await studentsApi.hasActiveEnrollments(student.id)
      } catch {
        // If check fails, default to not eligible
      }

      const newMember: EnrollmentGroupMember = {
        studentId: student.id,
        studentName: student.fullName,
        enrolledAt: courseStartDate,
        discountType: 'None',
        discountPercentage: 0,
        note: '',
        isEligibleForCourseDiscount,
      }

      addStudent(newMember)
      setSearchTerm('')
    },
    [canAddMore, courseStartDate, addStudent]
  )

  const showResults = searchTerm.length >= 2

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="font-medium text-sm">Search Students</h3>
          <Button variant="outline" size="sm" onClick={onAddNewStudent}>
            <svg
              className="h-4 w-4 mr-1"
              fill="none"
              viewBox="0 0 24 24"
              strokeWidth={1.5}
              stroke="currentColor"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            New Student
          </Button>
        </div>
        <Input
          type="search"
          placeholder="Search by name or email..."
          value={searchTerm}
          onChange={handleSearchChange}
        />
        <p className="text-xs text-muted-foreground">
          {searchTerm.length < 2
            ? 'Enter at least 2 characters to search'
            : `${filteredStudents.length} result${filteredStudents.length === 1 ? '' : 's'} found`}
        </p>
      </div>

      {!canAddMore && (
        <div className="rounded-md bg-amber-50 p-3 text-sm text-amber-800">
          Maximum number of students ({maxStudents}) reached for this course type.
        </div>
      )}

      {showResults && (
        <div className="space-y-2 max-h-[300px] overflow-y-auto">
          {isLoading && (
            <div className="flex items-center justify-center py-8">
              <svg
                className="h-5 w-5 animate-spin text-muted-foreground"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                />
              </svg>
            </div>
          )}

          {!isLoading && filteredStudents.length === 0 && (
            <div className="text-center py-8 text-muted-foreground text-sm">
              <p>No students found</p>
              <Button
                variant="link"
                size="sm"
                onClick={onAddNewStudent}
                className="mt-2"
              >
                Create a new student
              </Button>
            </div>
          )}

          {!isLoading && filteredStudents.length > 0 && (
            <>
              {filteredStudents.map((student) => (
                <button
                  key={student.id}
                  type="button"
                  className="w-full text-left p-3 rounded-md border hover:bg-accent transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  onClick={() => handleSelectStudent(student)}
                  disabled={!canAddMore}
                >
                  <div className="flex items-center justify-between gap-2">
                    <div className="min-w-0 flex-1">
                      <p className="font-medium truncate">{student.fullName}</p>
                      <p className="text-sm text-muted-foreground truncate">
                        {student.email}
                      </p>
                    </div>
                    <Badge variant={getStatusBadgeVariant(student.status)}>
                      {student.status}
                    </Badge>
                  </div>
                </button>
              ))}
            </>
          )}
        </div>
      )}
    </div>
  )
}
