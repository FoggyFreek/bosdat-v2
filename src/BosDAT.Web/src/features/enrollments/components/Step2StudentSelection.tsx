import { useState, useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { courseTypesApi, settingsApi, studentsApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { Step2Summary } from './Step2Summary'
import { StudentSearchPanel } from './StudentSearchPanel'
import { EnrollmentGroupPanel } from './EnrollmentGroupPanel'
import { AddStudentModal } from './AddStudentModal'
import type { CourseType } from '@/features/course-types/types'
import type { Student } from '@/features/students/types'
import type { EnrollmentGroupMember } from '../types'

export const Step2StudentSelection = () => {
  const { formData, addStudent, isStep2Valid } = useEnrollmentForm()
  const { step1 } = formData
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)

  // Fetch course type for validation
  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  const selectedCourseType = useMemo(
    () => courseTypes.find((ct) => ct.id === step1.courseTypeId),
    [courseTypes, step1.courseTypeId]
  )

  // Fetch discount settings
  const { data: familyDiscountSetting } = useQuery({
    queryKey: ['settings', 'family_discount_percent'],
    queryFn: () => settingsApi.getByKey('family_discount_percent'),
  })

  const { data: courseDiscountSetting } = useQuery({
    queryKey: ['settings', 'course_discount_percent'],
    queryFn: () => settingsApi.getByKey('course_discount_percent'),
  })

  const familyDiscountPercent = parseFloat(familyDiscountSetting?.value || '10')
  const courseDiscountPercent = parseFloat(courseDiscountSetting?.value || '10')

  const courseStartDate = step1.startDate || ''
  const maxStudents = selectedCourseType?.maxStudents || 1

  // Validation
  const validationResult = useMemo(() => {
    if (!selectedCourseType) {
      return { isValid: false, errors: ['Course type not selected'] }
    }
    return isStep2Valid(selectedCourseType.type, maxStudents)
  }, [selectedCourseType, maxStudents, isStep2Valid])

  const handleAddNewStudent = () => {
    setIsAddModalOpen(true)
  }

  const handleStudentCreated = useCallback(
    async (student: Student) => {
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
      setIsAddModalOpen(false)
    },
    [courseStartDate, addStudent]
  )

  return (
    <div className="space-y-6">
      {/* Step 1 Summary */}
      <Step2Summary />

      {/* Validation Errors */}
      {!validationResult.isValid && validationResult.errors.length > 0 && (
        <Alert variant="destructive">
          <AlertDescription>
            <ul className="list-disc list-inside space-y-1">
              {validationResult.errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      {/* Main Content - Split View */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Left Panel: Student Search */}
        <div className="space-y-4">
          <StudentSearchPanel
            courseStartDate={courseStartDate}
            maxStudents={maxStudents}
            onAddNewStudent={handleAddNewStudent}
          />
        </div>

        {/* Right Panel: Selected Students */}
        <div className="space-y-4">
          <EnrollmentGroupPanel
            courseStartDate={courseStartDate}
            familyDiscountPercent={familyDiscountPercent}
            courseDiscountPercent={courseDiscountPercent}
            maxStudents={maxStudents}
          />
        </div>
      </div>

      {/* Add Student Modal */}
      <AddStudentModal
        open={isAddModalOpen}
        onOpenChange={setIsAddModalOpen}
        onStudentCreated={handleStudentCreated}
      />
    </div>
  )
}
