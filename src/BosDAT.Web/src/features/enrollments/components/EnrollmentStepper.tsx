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
import { StepPlaceholder } from './StepPlaceholder'
import { courseTypesApi } from '@/services/api'
import type { CourseType } from '@/features/course-types/types'

const STEPS: StepConfig[] = [
  { title: 'Lesson Details', description: 'Configure course and schedule' },
  { title: 'Students', description: 'Select students to enroll' },
  { title: 'Pricing', description: 'Review pricing details' },
  { title: 'Confirmation', description: 'Review and confirm' },
]

const EnrollmentStepperContent = () => {
  const { currentStep, setCurrentStep, isStep1Valid, isStep2Valid, formData } = useEnrollmentForm()

  // Fetch course types for validation
  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  const selectedCourseType = useMemo(
    () => courseTypes.find((ct) => ct.id === formData.step1.courseTypeId),
    [courseTypes, formData.step1.courseTypeId]
  )

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

  const isNextDisabled = () => {
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
    return false
  }

  const renderStepContent = () => {
    switch (currentStep) {
      case 0:
        return <Step1LessonDetails />
      case 1:
        return <Step2StudentSelection />
      case 2:
        return <StepPlaceholder title="Pricing" />
      case 3:
        return <StepPlaceholder title="Confirmation" />
      default:
        return null
    }
  }

  return (
    <Card className="w-full max-w-4xl mx-auto">
      <CardHeader>
        <CardTitle>New Enrollment</CardTitle>
      </CardHeader>
      <CardContent className="space-y-8">
        <Stepper
          steps={STEPS}
          currentStep={currentStep}
          onStepChange={handleStepClick}
        />

        <div className="min-h-[300px]">{renderStepContent()}</div>

        <div className="flex justify-between pt-4 border-t">
          <Button
            variant="outline"
            onClick={handlePrevious}
            disabled={currentStep === 0}
          >
            Previous
          </Button>
          <Button
            onClick={handleNext}
            disabled={isNextDisabled() || currentStep === STEPS.length - 1}
          >
            {currentStep === STEPS.length - 1 ? 'Submit' : 'Next'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}

export const EnrollmentStepper = () => {
  return (
    <EnrollmentFormProvider>
      <EnrollmentStepperContent />
    </EnrollmentFormProvider>
  )
}
