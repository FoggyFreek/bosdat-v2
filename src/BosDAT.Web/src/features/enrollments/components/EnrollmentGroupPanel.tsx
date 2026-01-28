import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { EnrollmentGroupMemberCard } from './EnrollmentGroupMemberCard'
import type { EnrollmentGroupMember } from '../types'

interface EnrollmentGroupPanelProps {
  courseStartDate: string
  familyDiscountPercent: number
  courseDiscountPercent: number
  maxStudents: number
}

export const EnrollmentGroupPanel = ({
  courseStartDate,
  familyDiscountPercent,
  courseDiscountPercent,
  maxStudents,
}: EnrollmentGroupPanelProps) => {
  const { formData, removeStudent, updateStudent } = useEnrollmentForm()
  const { students } = formData.step2

  const handleUpdate = (
    studentId: string,
    updates: Partial<EnrollmentGroupMember>
  ) => {
    updateStudent(studentId, updates)
  }

  const handleRemove = (studentId: string) => {
    removeStudent(studentId)
  }

  if (students.length === 0) {
    return (
      <div className="flex-1 flex items-center justify-center border rounded-lg bg-muted/30 p-8">
        <div className="text-center text-muted-foreground">
          <svg
            className="mx-auto h-12 w-12 mb-4 opacity-50"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1}
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z"
            />
          </svg>
          <p className="text-sm">No students selected</p>
          <p className="text-xs mt-1">Search and add students from the left panel</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 space-y-3 overflow-y-auto">
      <div className="flex items-center justify-between mb-2">
        <h3 className="font-medium text-sm">
          Selected Students ({students.length}/{maxStudents})
        </h3>
      </div>
      <div className="space-y-3">
        {students.map((member) => (
          <EnrollmentGroupMemberCard
            key={member.studentId}
            member={member}
            courseStartDate={courseStartDate}
            familyDiscountPercent={familyDiscountPercent}
            courseDiscountPercent={courseDiscountPercent}
            onUpdate={(updates) => handleUpdate(member.studentId, updates)}
            onRemove={() => handleRemove(member.studentId)}
          />
        ))}
      </div>
    </div>
  )
}
