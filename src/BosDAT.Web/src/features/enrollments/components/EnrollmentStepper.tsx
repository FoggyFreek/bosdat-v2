import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
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
import { courseTypesApi } from '@/services/api'
import type { CourseType } from '@/features/course-types/types'

const DISPLAY_NAME = 'EnrollmentStepper'
const CONTENT_DISPLAY_NAME = 'EnrollmentStepperContent'

const STEPS: StepConfig[] = [
  { title: 'Lesson Details', description: 'Configure course and schedule' },
  { title: 'Students', description: 'Select students to enroll' },
  { title: 'Time Slot', description: 'Choose day and time' },
  { title: 'Confirmation', description: 'Review and confirm' },
]

const getNextButtonLabel = (currentStep: number, totalSteps: number) => {
  return currentStep === totalSteps - 1 ? 'Submit' : 'Next'
}

const EnrollmentStepperContent = () => {
  const {
    currentStep,
    setCurrentStep,
    isStep1Valid,
    isStep2Valid,
    isStep3Valid,
    formData,
  } = useEnrollmentForm()

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

  const handleNext = () => {
    if (currentStep < STEPS.length - 1) {
      setCurrentStep(currentStep + 1)
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

  const renderStep1 = () => <Step1LessonDetails />

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
      <CardTitle>New Enrollment</CardTitle>
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
    <div className="min-h-[300px]">{renderStepContent()}</div>
  )

  const renderPreviousButton = () => (
    <Button
      disabled={currentStep === 0}
      onClick={handlePrevious}
      variant="outline"
    >
      Previous
    </Button>
  )

  const renderNextButton = () => (
    <Button
      disabled={isNextButtonDisabled || isLastStep}
      onClick={handleNext}
    >
      {getNextButtonLabel(currentStep, STEPS.length)}
    </Button>
  )

  const renderNavigationButtons = () => (
    <div className="flex justify-between pt-4 border-t">
      {renderPreviousButton()}
      {renderNextButton()}
    </div>
  )

  return (
    <Card className="w-full max-w-4xl mx-auto">
      {renderHeader()}
      <CardContent className="space-y-8">
        {renderStepper()}
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
