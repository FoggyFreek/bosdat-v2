import { createContext, useContext, useReducer, ReactNode, useCallback, useMemo } from 'react'
import type {
  EnrollmentFormData,
  Step1LessonDetailsData,
  Step2StudentSelectionData,
  EnrollmentGroupMember,
} from '../types'
import {
  initialEnrollmentFormData,
} from '../types'
import type { CourseTypeCategory } from '@/features/course-types/types'

type Action =
  | { type: 'UPDATE_STEP1'; payload: Partial<Step1LessonDetailsData> }
  | { type: 'UPDATE_STEP2'; payload: Step2StudentSelectionData }
  | { type: 'ADD_STUDENT'; payload: EnrollmentGroupMember }
  | { type: 'REMOVE_STUDENT'; payload: string }
  | { type: 'UPDATE_STUDENT'; payload: { studentId: string; updates: Partial<EnrollmentGroupMember> } }
  | { type: 'SET_STEP'; payload: number }
  | { type: 'RESET' }

interface EnrollmentFormState {
  formData: EnrollmentFormData
  currentStep: number
}

const initialState: EnrollmentFormState = {
  formData: initialEnrollmentFormData,
  currentStep: 0,
}

const enrollmentFormReducer = (
  state: EnrollmentFormState,
  action: Action
): EnrollmentFormState => {
  switch (action.type) {
    case 'UPDATE_STEP1':
      return {
        ...state,
        formData: {
          ...state.formData,
          step1: {
            ...state.formData.step1,
            ...action.payload,
          },
        },
      }
    case 'UPDATE_STEP2':
      return {
        ...state,
        formData: {
          ...state.formData,
          step2: action.payload,
        },
      }
    case 'ADD_STUDENT':
      return {
        ...state,
        formData: {
          ...state.formData,
          step2: {
            ...state.formData.step2,
            students: [...state.formData.step2.students, action.payload],
          },
        },
      }
    case 'REMOVE_STUDENT':
      return {
        ...state,
        formData: {
          ...state.formData,
          step2: {
            ...state.formData.step2,
            students: state.formData.step2.students.filter(
              (s) => s.studentId !== action.payload
            ),
          },
        },
      }
    case 'UPDATE_STUDENT':
      return {
        ...state,
        formData: {
          ...state.formData,
          step2: {
            ...state.formData.step2,
            students: state.formData.step2.students.map((s) =>
              s.studentId === action.payload.studentId
                ? { ...s, ...action.payload.updates }
                : s
            ),
          },
        },
      }
    case 'SET_STEP':
      return {
        ...state,
        currentStep: action.payload,
      }
    case 'RESET':
      return initialState
    default:
      return state
  }
}

interface Step2ValidationResult {
  isValid: boolean
  errors: string[]
}

interface EnrollmentFormContextType {
  formData: EnrollmentFormData
  currentStep: number
  updateStep1: (data: Partial<Step1LessonDetailsData>) => void
  updateStep2: (data: Step2StudentSelectionData) => void
  addStudent: (student: EnrollmentGroupMember) => void
  removeStudent: (studentId: string) => void
  updateStudent: (studentId: string, updates: Partial<EnrollmentGroupMember>) => void
  setCurrentStep: (step: number) => void
  resetForm: () => void
  isStep1Valid: () => boolean
  isStep2Valid: (courseTypeCategory: CourseTypeCategory, maxStudents: number) => Step2ValidationResult
}

const EnrollmentFormContext = createContext<EnrollmentFormContextType | undefined>(
  undefined
)

interface EnrollmentFormProviderProps {
  children: ReactNode
}

export const EnrollmentFormProvider = ({ children }: EnrollmentFormProviderProps) => {
  const [state, dispatch] = useReducer(enrollmentFormReducer, initialState)

  const updateStep1 = useCallback((data: Partial<Step1LessonDetailsData>) => {
    dispatch({ type: 'UPDATE_STEP1', payload: data })
  }, [])

  const updateStep2 = useCallback((data: Step2StudentSelectionData) => {
    dispatch({ type: 'UPDATE_STEP2', payload: data })
  }, [])

  const addStudent = useCallback((student: EnrollmentGroupMember) => {
    dispatch({ type: 'ADD_STUDENT', payload: student })
  }, [])

  const removeStudent = useCallback((studentId: string) => {
    dispatch({ type: 'REMOVE_STUDENT', payload: studentId })
  }, [])

  const updateStudent = useCallback(
    (studentId: string, updates: Partial<EnrollmentGroupMember>) => {
      dispatch({ type: 'UPDATE_STUDENT', payload: { studentId, updates } })
    },
    []
  )

  const setCurrentStep = useCallback((step: number) => {
    dispatch({ type: 'SET_STEP', payload: step })
  }, [])

  const resetForm = useCallback(() => {
    dispatch({ type: 'RESET' })
  }, [])

  const isStep1Valid = useCallback(() => {
    const { step1 } = state.formData
    return (
      step1.courseTypeId !== null &&
      step1.teacherId !== null &&
      step1.startDate !== null
    )
  }, [state.formData])

  const isStep2Valid = useCallback(
    (courseTypeCategory: CourseTypeCategory, maxStudents: number): Step2ValidationResult => {
      const { step2, step1 } = state.formData
      const { students } = step2
      const errors: string[] = []

      // Rule 1: At least one student must be selected
      if (students.length === 0) {
        errors.push('At least one student must be selected')
        return { isValid: false, errors }
      }

      // Rule 2: Individual course type: exactly one student
      if (courseTypeCategory === 'Individual' && students.length !== 1) {
        errors.push('Individual courses require exactly one student')
      }

      // Rule 3: Group/Workshop: 1 to maxStudents
      if (
        (courseTypeCategory === 'Group' || courseTypeCategory === 'Workshop') &&
        students.length > maxStudents
      ) {
        errors.push(`Maximum ${maxStudents} students allowed for this course type`)
      }

      // Rule 4 & 5: Date validation
      const courseStartDate = step1.startDate
      if (courseStartDate) {
        // At least one student must have enrolledAt equal to course start date
        const hasStudentAtStart = students.some(
          (s) => s.enrolledAt === courseStartDate
        )
        if (!hasStudentAtStart) {
          errors.push('At least one student must start on the course start date')
        }

        // No student can have enrolledAt before course start date
        const studentsBeforeStart = students.filter(
          (s) => new Date(s.enrolledAt) < new Date(courseStartDate)
        )
        if (studentsBeforeStart.length > 0) {
          errors.push('No student can have an enrollment date before the course start date')
        }
      }

      return { isValid: errors.length === 0, errors }
    },
    [state.formData]
  )

  const value = useMemo(
    () => ({
      formData: state.formData,
      currentStep: state.currentStep,
      updateStep1,
      updateStep2,
      addStudent,
      removeStudent,
      updateStudent,
      setCurrentStep,
      resetForm,
      isStep1Valid,
      isStep2Valid,
    }),
    [
      state.formData,
      state.currentStep,
      updateStep1,
      updateStep2,
      addStudent,
      removeStudent,
      updateStudent,
      setCurrentStep,
      resetForm,
      isStep1Valid,
      isStep2Valid,
    ]
  )

  return (
    <EnrollmentFormContext.Provider value={value}>
      {children}
    </EnrollmentFormContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useEnrollmentForm = (): EnrollmentFormContextType => {
  const context = useContext(EnrollmentFormContext)
  if (context === undefined) {
    throw new Error('useEnrollmentForm must be used within an EnrollmentFormProvider')
  }
  return context
}
