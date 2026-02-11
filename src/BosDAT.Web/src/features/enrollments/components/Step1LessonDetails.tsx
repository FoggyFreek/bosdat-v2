import { useEffect, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { teachersApi } from '@/features/teachers/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { getDayNameFromNumber } from '@/lib/datetime-helpers'
import type { CourseType } from '@/features/course-types/types'
import { courseTypeCategoryTranslations } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import type { RecurrenceType } from '../types'

interface Step1LessonDetailsProps {
  courseTypes: CourseType[]
}

export const Step1LessonDetails = ({ courseTypes }: Step1LessonDetailsProps) => {
  const { t } = useTranslation()
  const { formData, updateStep1 } = useEnrollmentForm()
  const { step1 } = formData

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
  const isTrail = step1.recurrence === 'Trial'
  const isIndividualOrGroup = selectedCourseType?.type === 'Individual' || selectedCourseType?.type === 'Group'
  const isEndDateRequired = isWorkshop
  const isEndDateDisabled = isTrail

  const dayOfWeek = useMemo(() => {
    if (!step1.startDate) return null
    const date = new Date(step1.startDate)
    return getDayNameFromNumber(date.getDay())
  }, [step1.startDate])

  // Auto-set end date when Trail recurrence is selected
  useEffect(() => {
    if (isTrail && step1.startDate) {
      updateStep1({ endDate: step1.startDate })
    }
  }, [isTrail, step1.startDate, updateStep1])

  // Clear teacher when course type changes
  useEffect(() => {
    if (step1.courseTypeId && step1.teacherId) {
      const teacherStillValid = filteredTeachers.some((t) => t.id === step1.teacherId)
      if (!teacherStillValid) {
        updateStep1({ teacherId: null })
      }
    }
  }, [step1.courseTypeId, step1.teacherId, filteredTeachers, updateStep1])

  // Reset recurrence to Weekly when switching to Workshop (Trail not allowed for Workshop)
  useEffect(() => {
    if (isWorkshop && isTrail) {
      updateStep1({ recurrence: 'Weekly' })
    }
  }, [isWorkshop, isTrail, updateStep1])

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

  const handleRecurrenceChange = (value: RecurrenceType) => {
    updateStep1({ recurrence: value })
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2">
        {/* Course Type */}
        <div className="space-y-2">
          <Label htmlFor="courseType">{t('enrollments.step1.courseType')}</Label>
          <Select
            value={step1.courseTypeId || ''}
            onValueChange={handleCourseTypeChange}
          >
            <SelectTrigger id="courseType" aria-label={t('enrollments.step1.courseType')}>
              <SelectValue placeholder={t('enrollments.step1.selectCourseType')} />
            </SelectTrigger>
            <SelectContent>
              {courseTypes.map((ct) => (
                <SelectItem key={ct.id} value={ct.id}>
                  {ct.name} ({t(courseTypeCategoryTranslations[ct.type])})
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Teacher */}
        <div className="space-y-2">
          <Label htmlFor="teacher">{t('enrollments.step1.teacher')}</Label>
          <Select
            value={step1.teacherId || ''}
            onValueChange={handleTeacherChange}
            disabled={!step1.courseTypeId}>
            <SelectTrigger id="teacher" aria-label={t('enrollments.step1.teacher')}>
              <SelectValue placeholder={!step1.courseTypeId ? t('enrollments.step1.selectCourseTypeFirst') : t('enrollments.step1.selectTeacher')} />
            </SelectTrigger>
            <SelectContent>
              {filteredTeachers.map((b) => (
                <SelectItem key={b.id} value={b.id}>
                  {b.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {/* Start Date */}
        <div className="space-y-2">
          <Label htmlFor="startDate">{t('enrollments.step1.startDate')}</Label>
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
            {t('enrollments.step1.endDate')} {isEndDateRequired && <span className="text-destructive">*</span>}
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
              {t('enrollments.step1.endDateAutoSet')}
            </p>
          )}
        </div>
      </div>

      {/* Recurrence Options */}
      <div className="space-y-2">
        <Label>{t('enrollments.step1.recurrence')}</Label>

        <div className="flex gap-4">
          {isIndividualOrGroup && (<label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="recurrence"
              value="Trail"
              checked={step1.recurrence === 'Trial'}
              onChange={() => handleRecurrenceChange('Trial')}
              className="h-4 w-4"
              disabled={isWorkshop}
            />
            <span>{t('enrollments.step1.trial')}</span>
          </label>)}
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="radio"
              name="recurrence"
              value="Weekly"
              checked={step1.recurrence === 'Weekly'}
              onChange={() => handleRecurrenceChange('Weekly')}
              className="h-4 w-4"
            />
            <span>{t('enrollments.step1.weekly')}</span>
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
            <span>{t('enrollments.step1.biweekly')}</span>
          </label>
        </div>
      </div>
    </div>
  )
}
