import { useState, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Plus, Loader2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { studentsApi } from '@/features/students/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import type { StudentList } from '@/features/students/types'
import type { EnrollmentGroupMember } from '../types'

const DISPLAY_NAME = 'StudentSearchPanel'

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
  const { t } = useTranslation()
  const [searchTerm, setSearchTerm] = useState('')
  const { formData, addStudent } = useEnrollmentForm()
  const selectedStudents = useMemo(() => formData.step2.students ?? [], [formData.step2.students])

  const { data: allStudents = [], isLoading } = useQuery<StudentList[]>({
    queryKey: ['students', 'search', searchTerm],
    queryFn: () => studentsApi.getAll({ search: searchTerm || undefined }),
    enabled: searchTerm.length >= 2,
    staleTime: 30000,
  })

  const selectedStudentIds = useMemo(
    () => new Set((selectedStudents || []).map((s) => s.studentId)),
    [selectedStudents]
  )

  const filteredStudents = useMemo(() => {
    return allStudents
      .filter((s) => !selectedStudentIds.has(s.id))
      .slice(0, 10)
  }, [allStudents, selectedStudentIds])

  const canAddMore = selectedStudents.length < maxStudents
  const showResults = searchTerm.length >= 2

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
        invoicingPreference: 'Monthly',
        note: '',
        isEligibleForCourseDiscount,
      }

      addStudent(newMember)
      setSearchTerm('')
    },
    [canAddMore, courseStartDate, addStudent]
  )

  const renderHeader = () => (
    <div className="flex items-center justify-between">
      <h3 className="font-medium text-sm">{t('enrollments.step2.searchStudents')}</h3>
      <Button
        onClick={onAddNewStudent}
        size="sm"
        variant="outline"
      >
        <Plus className="h-4 w-4 mr-1" />
        {t('enrollments.step2.newStudent')}
      </Button>
    </div>
  )

  const renderSearchInput = () => (
    <Input
      onChange={handleSearchChange}
      placeholder={t('enrollments.step2.searchPlaceholder')}
      type="search"
      value={searchTerm}
    />
  )

  const renderSearchHint = () => (
    <p className="text-xs text-muted-foreground">
      {searchTerm.length < 2
        ? t('enrollments.step2.searchHint')
        : t('enrollments.step2.resultsFound', { count: filteredStudents.length })}
    </p>
  )

  const renderMaxStudentsWarning = () => (
    <div className="rounded-md bg-amber-50 p-3 text-sm text-amber-800">
      {t('enrollments.step2.maxStudentsReached', { max: maxStudents })}
    </div>
  )

  const renderLoadingState = () => (
    <div className="flex items-center justify-center py-8">
      <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
    </div>
  )

  const renderEmptyState = () => (
    <div className="text-center py-8 text-muted-foreground text-sm">
      <p>{t('enrollments.step2.noStudentsFound')}</p>
      <Button
        className="mt-2"
        onClick={onAddNewStudent}
        size="sm"
        variant="link"
      >
        {t('enrollments.step2.createNewStudent')}
      </Button>
    </div>
  )

  const renderStudentItem = (student: StudentList) => (
    <button
      key={student.id}
      className="w-full text-left p-3 rounded-md border hover:bg-accent transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      disabled={!canAddMore}
      onClick={() => handleSelectStudent(student)}
      type="button"
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
  )

  const renderStudentsList = () => (
    <>
      {filteredStudents.map((student) => renderStudentItem(student))}
    </>
  )

  const renderSearchResults = () => (
    <div className="space-y-2 max-h-[300px] overflow-y-auto">
      {isLoading && renderLoadingState()}
      {!isLoading && filteredStudents.length === 0 && renderEmptyState()}
      {!isLoading && filteredStudents.length > 0 && renderStudentsList()}
    </div>
  )

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        {renderHeader()}
        {renderSearchInput()}
        {renderSearchHint()}
      </div>

      {!canAddMore && renderMaxStudentsWarning()}
      {showResults && renderSearchResults()}
    </div>
  )
}

StudentSearchPanel.displayName = DISPLAY_NAME
