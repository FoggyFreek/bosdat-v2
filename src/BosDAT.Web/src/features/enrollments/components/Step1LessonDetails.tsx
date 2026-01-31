import { useEffect, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { courseTypesApi, teachersApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { getDayName } from '@/lib/utils'
import { WeekParitySelect } from '@/features/courses/components/WeekParitySelect'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import type { RecurrenceType } from '../types'
import type { WeekParity } from '@/features/courses/types'

export const Step1LessonDetails = () => {
  const { formData, updateStep1 } = useEnrollmentForm()
  const { step1 } = formData

  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  // Fetch teachers filtered by courseTypeId - only when a courseType is selected
  const { data: filteredTeachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active', 'courseType', step1.courseTypeId],
    queryFn: () => teachersApi.getAll({ activeOnly: true, courseTypeId: step1.courseTypeId! }),
    enabled: !!step1.courseTypeId,
  })

  const selectedCourseType = useMemo(() => {
    return courseTypes.find((ct) => ct.id === step1.courseTypeId)
  }, [courseTypes, step1.courseTypeId])

  const isWorkshop = selectedCourseType?.type === 'Workshop'
  const showTrialToggle = selectedCourseType && !isWorkshop
  const isEndDateRequired = isWorkshop
  const isEndDateDisabled = step1.isTrial

  const dayOfWeek = useMemo(() => {
    if (!step1.startDate) return null
    const date = new Date(step1.startDate)
    return getDayName(date.getDay())
  }, [step1.startDate])

  // Auto-set end date when trial is enabled
  useEffect(() => {
    if (step1.isTrial && step1.startDate) {
      updateStep1({ endDate: step1.startDate })
    }
  }, [step1.isTrial, step1.startDate, updateStep1])

  // Clear teacher when course type changes
  useEffect(() => {
    if (step1.courseTypeId && step1.teacherId) {
      const teacherStillValid = filteredTeachers.some((t) => t.id === step1.teacherId)
      if (!teacherStillValid) {
        updateStep1({ teacherId: null })
      }
    }
  }, [step1.courseTypeId, step1.teacherId, filteredTeachers, updateStep1])

  // Reset trial when switching to Workshop
  useEffect(() => {
    if (isWorkshop && step1.isTrial) {
      updateStep1({ isTrial: false })
    }
  }, [isWorkshop, step1.isTrial, updateStep1])

  const handleCourseTypeChange = (value: string) => {
    updateStep1({ courseTypeId: value })
  }

  const handleTeacherChange = (value: string) => {
    updateStep1({ teacherId: value })
  }

  const handleStartDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    updateStep1({ startDate: e.target.value || null })
  }

  const handleEndDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    updateStep1({ endDate: e.target.value || null })
  }

  const handleTrialChange = (checked: boolean) => {
    updateStep1({ isTrial: checked })
  }

  const handleRecurrenceChange = (value: RecurrenceType) => {
    updateStep1({ recurrence: value })
  }

  const handleWeekParityChange = (value: WeekParity) => {
    updateStep1({ weekParity: value })
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2">
        {/* Course Type */}
        <div className="space-y-2">
          <Label htmlFor="courseType">Course Type</Label>
          <Select
            value={step1.courseTypeId || ''}
            onValueChange={handleCourseTypeChange}
          >
            <SelectTrigger id="courseType" aria-label="Course Type">
              <SelectValue placeholder="Select a course type" />
            </SelectTrigger>
            <SelectContent>
              {courseTypes.map((ct) => (
                <SelectItem key={ct.id} value={ct.id}>
                  {ct.name} ({ct.type})
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Teacher */}
        <div className="space-y-2">
          <Label htmlFor="teacher">Teacher</Label>
          <Select
            value={step1.teacherId || ''}
            onValueChange={handleTeacherChange}
            disabled={!step1.courseTypeId}
          >
            <SelectTrigger id="teacher" aria-label="Teacher">
              <SelectValue placeholder={!step1.courseTypeId ? 'Select a course type first' : 'Select a teacher'} />
            </SelectTrigger>
            <SelectContent>
              {filteredTeachers.map((t) => (
                <SelectItem key={t.id} value={t.id}>
                  {t.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {/* Start Date */}
        <div className="space-y-2">
          <Label htmlFor="startDate">Start Date</Label>
          <div className="flex items-center gap-2">
            <Input
              id="startDate"
              type="date"
              value={step1.startDate || ''}
              onChange={handleStartDateChange}
            />
            {dayOfWeek && (
              <span className="text-sm text-muted-foreground whitespace-nowrap">
                {dayOfWeek}
              </span>
            )}
          </div>
        </div>

        {/* End Date */}
        <div className="space-y-2">
          <Label htmlFor="endDate">
            End Date {isEndDateRequired && <span className="text-destructive">*</span>}
          </Label>
          <Input
            id="endDate"
            type="date"
            value={step1.endDate || ''}
            onChange={handleEndDateChange}
            disabled={isEndDateDisabled}
            required={isEndDateRequired}
            min={step1.startDate || undefined}
          />
          {isEndDateDisabled && (
            <p className="text-xs text-muted-foreground">
              End date is set to start date for trial lessons
            </p>
          )}
        </div>
      </div>

      {/* Trial Toggle - only for Individual/Group */}
      {showTrialToggle && (
        <div className="flex items-center space-x-2">
          <Checkbox
            id="trial"
            checked={step1.isTrial}
            onCheckedChange={handleTrialChange}
          />
          <Label htmlFor="trial" className="cursor-pointer">
            Trial Lesson (single occurrence)
          </Label>
        </div>
      )}

      {/* Recurrence Options */}
      <div className="space-y-2">
        <Label>Recurrence</Label>
        <div className="flex gap-4">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="recurrence"
              value="Weekly"
              checked={step1.recurrence === 'Weekly'}
              onChange={() => handleRecurrenceChange('Weekly')}
              className="h-4 w-4"
            />
            <span>Weekly</span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="recurrence"
              value="Biweekly"
              checked={step1.recurrence === 'Biweekly'}
              onChange={() => handleRecurrenceChange('Biweekly')}
              className="h-4 w-4"
            />
            <span>Bi-weekly</span>
          </label>
        </div>
      </div>

      {/* Week Parity - only for Biweekly */}
      {step1.recurrence === 'Biweekly' && (
        <WeekParitySelect
          value={step1.weekParity}
          onChange={handleWeekParityChange}
          disabled={false}
          helperText="Select which weeks this biweekly course occurs in"
        />
      )}
    </div>
  )
}
