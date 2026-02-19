import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Stepper, type StepConfig } from '@/components/ui/stepper'
import {
  EnrollmentFormProvider,
  useEnrollmentForm,
} from '../context/EnrollmentFormContext'
import { Step1LessonDetails } from './Step1LessonDetails'
import { Step2StudentSelection } from './Step2StudentSelection'
import { Step3CalendarSlotSelection } from './Step3CalendarSlotSelection'
import { Step4Summary } from './Step4Summary'
import { courseTypesApi } from '@/features/course-types/api'
import type { CourseType } from '@/features/course-types/types'
import type { CourseFrequency } from '@/features/courses/types'
import type { WeekParity } from '@/lib/datetime-helpers'
import { coursesApi } from '@/features/courses/api'
import { schedulingApi } from '@/features/settings/api'
import { getDayNameFromNumber, getWeekParity, type DayOfWeek } from '@/lib/datetime-helpers'
import { useToast } from '@/hooks/use-toast'
import { enrollmentsApi } from '../api'

const DISPLAY_NAME = 'EnrollmentStepper'
const CONTENT_DISPLAY_NAME = 'EnrollmentStepperContent'

const EnrollmentStepperContent = () => {
  const { t } = useTranslation()
  const {
    currentStep,
    setCurrentStep,
    isStep1Valid,
    isStep2Valid,
    isStep3Valid,
    formData,
  } = useEnrollmentForm()

  const navigate = useNavigate()
  const { toast } = useToast()

  const STEPS: StepConfig[] = useMemo(() => [
    { title: t('enrollments.stepper.lessonDetails'), description: t('enrollments.stepper.lessonDetailsDesc') },
    { title: t('enrollments.stepper.studentSelection'), description: t('enrollments.stepper.studentSelectionDesc') },
    { title: t('enrollments.stepper.calendarSlot'), description: t('enrollments.stepper.calendarSlotDesc') },
    { title: t('enrollments.stepper.summary'), description: t('enrollments.stepper.summaryDesc') },
  ], [t])

  const getNextButtonLabel = (currentStep: number, totalSteps: number) => {
    return currentStep === totalSteps - 1 ? t('enrollments.actions.submit') : t('enrollments.actions.next')
  }

  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  const selectedCourseType = useMemo(
    () => courseTypes.find((ct) => ct.id === formData.step1.courseTypeId),
    [courseTypes, formData.step1.courseTypeId]
  )

  const isNextButtonDisabled = useMemo(() => {
    if (currentStep === 0) {
      return !isStep1Valid()
    }
    if (currentStep === 1) {
      if (!selectedCourseType) return true
      const validationResult = isStep2Valid(
        selectedCourseType.type,
        selectedCourseType.maxStudents
      )
      return !validationResult.isValid
    }
    if (currentStep === 2) {
      const validationResult = isStep3Valid()
      return !validationResult.isValid
    }
    return false
  }, [currentStep, isStep1Valid, isStep2Valid, isStep3Valid, selectedCourseType])

  const isLastStep = currentStep === STEPS.length - 1
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleNext = async () => {
    if (!isLastStep) {
      setCurrentStep(currentStep + 1)
      return
    }

    setIsSubmitting(true)
    try {
      const weeklyOrOnce: CourseFrequency = formData.step1.recurrence === 'Weekly' ? 'Weekly' : 'Once'
      const frequency: CourseFrequency =
        formData.step1.recurrence === 'Biweekly' ? 'Biweekly' : weeklyOrOnce

      const weekParity: WeekParity =
        formData.step1.recurrence === 'Biweekly'
          ? getWeekParity(new Date(formData.step1.startDate!))
          : 'All'

      const dayOfWeek = getDayNameFromNumber(formData.step3.selectedDayOfWeek!) as DayOfWeek

      const course = await coursesApi.create({
        courseTypeId: formData.step1.courseTypeId!,
        teacherId: formData.step1.teacherId!,
        startDate: formData.step1.startDate!,
        roomId: formData.step3.selectedRoomId!,
        endDate: formData.step1.endDate ?? undefined,
        startTime: formData.step3.selectedStartTime!,
        endTime: formData.step3.selectedEndTime!,
        isTrial: formData.step1.recurrence === 'Trial',
        frequency,
        weekParity,
        dayOfWeek,
      })

      for (const student of formData.step2.students) {
        await enrollmentsApi.create({
          studentId: student.studentId,
          courseId: course.id,
          discountPercent: student.discountPercentage,
          discountType: student.discountType,
          invoicingPreference: student.invoicingPreference,
          notes: student.note || undefined,
        })
      }

      await schedulingApi.runSingle(course.id)

      toast({
        title: t('enrollments.success.title'),
        description: t('enrollments.success.description'),
      })

      navigate(`/courses/${course.id}`)
    } catch (error) {
      const message = error instanceof Error ? error.message : t('enrollments.error.unexpectedError')
      toast({
        variant: 'destructive',
        title: t('enrollments.error.title'),
        description: message,
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const handlePrevious = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1)
    }
  }

  const handleStepClick = (step: number) => {
    setCurrentStep(step)
  }

  const renderStep1 = () => <Step1LessonDetails courseTypes={courseTypes} />

  const renderStep2 = () => <Step2StudentSelection />

  const renderStep3 = () => {
    if (!selectedCourseType) return null
    return (
      <Step3CalendarSlotSelection
        durationMinutes={selectedCourseType.durationMinutes}
        teacherId={formData.step1.teacherId!}
      />
    )
  }

  const renderStep4 = () => <Step4Summary />

  const renderStepContent = () => {
    switch (currentStep) {
      case 0:
        return renderStep1()
      case 1:
        return renderStep2()
      case 2:
        return renderStep3()
      case 3:
        return renderStep4()
      default:
        return null
    }
  }

  const renderHeader = () => (
    <CardHeader>
      <CardTitle>{t('enrollments.newEnrollment')}</CardTitle>
    </CardHeader>
  )

  const renderStepper = () => (
    <Stepper
      currentStep={currentStep}
      onStepChange={handleStepClick}
      steps={STEPS}
    />
  )

  const renderStepContainer = () => (
    <div className="flex-1 overflow-y-auto min-h-0">{renderStepContent()}</div>
  )

  const renderPreviousButton = () => (
    <Button
      disabled={currentStep === 0}
      onClick={handlePrevious}
      variant="outline"
    >
      {t('enrollments.actions.previous')}
    </Button>
  )

  const renderNextButton = () => (
    <Button
      disabled={isNextButtonDisabled || isSubmitting}
      onClick={handleNext}
    >
      {isSubmitting ? t('enrollments.actions.submitting') : getNextButtonLabel(currentStep, STEPS.length)}
    </Button>
  )

  const renderNavigationButtons = () => (
    <div className="flex justify-between pt-4 border-t shrink-0">
      {renderPreviousButton()}
      {renderNextButton()}
    </div>
  )

  return (
    <Card className="w-full h-full flex flex-col">
      {renderHeader()}
      <CardContent className="flex-1 flex flex-col gap-8 overflow-hidden">
        <div className="max-w-xl mx-auto shrink-0">
          {renderStepper()}
        </div>
        {renderStepContainer()}
        {renderNavigationButtons()}
      </CardContent>
    </Card>
  )
}

EnrollmentStepperContent.displayName = CONTENT_DISPLAY_NAME

export const EnrollmentStepper = () => {
  return (
    <EnrollmentFormProvider>
      <EnrollmentStepperContent />
    </EnrollmentFormProvider>
  )
}

EnrollmentStepper.displayName = DISPLAY_NAME
