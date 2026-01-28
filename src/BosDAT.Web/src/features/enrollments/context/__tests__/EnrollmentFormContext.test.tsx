import { describe, it, expect } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { ReactNode } from 'react'
import {
  EnrollmentFormProvider,
  useEnrollmentForm,
} from '../EnrollmentFormContext'
import { initialStep1Data, initialStep2Data } from '../../types'
import type { EnrollmentGroupMember } from '../../types'

const wrapper = ({ children }: { children: ReactNode }) => (
  <EnrollmentFormProvider>{children}</EnrollmentFormProvider>
)

describe('EnrollmentFormContext', () => {
  it('provides initial state with empty step1 data', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    expect(result.current.formData.step1).toEqual(initialStep1Data)
    expect(result.current.currentStep).toBe(0)
  })

  it('updates step1 data correctly', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.updateStep1({ courseTypeId: 'ct-123' })
    })

    expect(result.current.formData.step1.courseTypeId).toBe('ct-123')
    expect(result.current.formData.step1.teacherId).toBeNull()
  })

  it('preserves other step1 fields when updating', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.updateStep1({ courseTypeId: 'ct-123' })
    })
    act(() => {
      result.current.updateStep1({ teacherId: 't-456' })
    })

    expect(result.current.formData.step1.courseTypeId).toBe('ct-123')
    expect(result.current.formData.step1.teacherId).toBe('t-456')
  })

  it('sets current step correctly', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.setCurrentStep(2)
    })

    expect(result.current.currentStep).toBe(2)
  })

  it('persists data across step navigation', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.updateStep1({
        courseTypeId: 'ct-123',
        teacherId: 't-456',
        startDate: '2024-01-15',
      })
    })

    act(() => {
      result.current.setCurrentStep(1)
    })

    act(() => {
      result.current.setCurrentStep(0)
    })

    expect(result.current.formData.step1.courseTypeId).toBe('ct-123')
    expect(result.current.formData.step1.teacherId).toBe('t-456')
    expect(result.current.formData.step1.startDate).toBe('2024-01-15')
  })

  it('resets form data to initial state', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.updateStep1({
        courseTypeId: 'ct-123',
        teacherId: 't-456',
      })
      result.current.setCurrentStep(2)
    })

    act(() => {
      result.current.resetForm()
    })

    expect(result.current.formData.step1).toEqual(initialStep1Data)
    expect(result.current.currentStep).toBe(0)
  })

  it('validates step1 completion correctly - incomplete', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    expect(result.current.isStep1Valid()).toBe(false)

    act(() => {
      result.current.updateStep1({ courseTypeId: 'ct-123' })
    })

    expect(result.current.isStep1Valid()).toBe(false)
  })

  it('validates step1 completion correctly - complete', () => {
    const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

    act(() => {
      result.current.updateStep1({
        courseTypeId: 'ct-123',
        teacherId: 't-456',
        startDate: '2024-01-15',
      })
    })

    expect(result.current.isStep1Valid()).toBe(true)
  })

  it('throws error when used outside provider', () => {
    const consoleError = console.error
    console.error = () => {}

    expect(() => {
      renderHook(() => useEnrollmentForm())
    }).toThrow('useEnrollmentForm must be used within an EnrollmentFormProvider')

    console.error = consoleError
  })

  // Step 2 Tests
  describe('Step 2: Student Selection', () => {
    const createTestMember = (overrides: Partial<EnrollmentGroupMember> = {}): EnrollmentGroupMember => ({
      studentId: 'student-1',
      studentName: 'John Doe',
      enrolledAt: '2024-01-15',
      discountType: 'None',
      discountPercentage: 0,
      note: '',
      isEligibleForCourseDiscount: false,
      ...overrides,
    })

    it('provides initial step2 state with empty students array', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

      expect(result.current.formData.step2).toEqual(initialStep2Data)
      expect(result.current.formData.step2.students).toEqual([])
    })

    it('adds student to enrollment group', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
      const newMember = createTestMember()

      act(() => {
        result.current.addStudent(newMember)
      })

      expect(result.current.formData.step2.students).toHaveLength(1)
      expect(result.current.formData.step2.students[0].studentId).toBe('student-1')
    })

    it('adds multiple students to enrollment group', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
      const member1 = createTestMember({ studentId: 'student-1', studentName: 'John Doe' })
      const member2 = createTestMember({ studentId: 'student-2', studentName: 'Jane Doe' })

      act(() => {
        result.current.addStudent(member1)
      })
      act(() => {
        result.current.addStudent(member2)
      })

      expect(result.current.formData.step2.students).toHaveLength(2)
    })

    it('removes student from enrollment group', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
      const member1 = createTestMember({ studentId: 'student-1' })
      const member2 = createTestMember({ studentId: 'student-2' })

      act(() => {
        result.current.addStudent(member1)
        result.current.addStudent(member2)
      })

      act(() => {
        result.current.removeStudent('student-1')
      })

      expect(result.current.formData.step2.students).toHaveLength(1)
      expect(result.current.formData.step2.students[0].studentId).toBe('student-2')
    })

    it('updates student in enrollment group', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
      const member = createTestMember()

      act(() => {
        result.current.addStudent(member)
      })

      act(() => {
        result.current.updateStudent('student-1', { discountType: 'Family', discountPercentage: 10 })
      })

      expect(result.current.formData.step2.students[0].discountType).toBe('Family')
      expect(result.current.formData.step2.students[0].discountPercentage).toBe(10)
    })

    it('updates step2 data entirely', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
      const newData = {
        students: [
          createTestMember({ studentId: 'student-1' }),
          createTestMember({ studentId: 'student-2' }),
        ],
      }

      act(() => {
        result.current.updateStep2(newData)
      })

      expect(result.current.formData.step2.students).toHaveLength(2)
    })

    describe('isStep2Valid', () => {
      it('returns invalid when no students selected', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('At least one student must be selected')
      })

      it('returns invalid when Individual course has multiple students', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1' }))
          result.current.addStudent(createTestMember({ studentId: 'student-2' }))
        })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('Individual courses require exactly one student')
      })

      it('returns invalid when exceeding maxStudents for Group course', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1' }))
          result.current.addStudent(createTestMember({ studentId: 'student-2' }))
          result.current.addStudent(createTestMember({ studentId: 'student-3' }))
        })

        const validation = result.current.isStep2Valid('Group', 2)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('Maximum 2 students allowed for this course type')
      })

      it('returns invalid when no student starts on course date', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1', enrolledAt: '2024-01-20' }))
        })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('At least one student must start on the course start date')
      })

      it('returns invalid when student enrolledAt is before course start', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1', enrolledAt: '2024-01-10' }))
        })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('No student can have an enrollment date before the course start date')
      })

      it('returns invalid when mixing Family and Course discounts', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({
            studentId: 'student-1',
            enrolledAt: '2024-01-15',
            discountType: 'Family',
          }))
          result.current.addStudent(createTestMember({
            studentId: 'student-2',
            enrolledAt: '2024-01-15',
            discountType: 'Course',
          }))
        })

        const validation = result.current.isStep2Valid('Group', 6)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('Family and course discounts cannot be combined in the same enrollment')
      })

      it('returns valid for correct Individual course setup', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({
            studentId: 'student-1',
            enrolledAt: '2024-01-15',
          }))
        })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(true)
        expect(validation.errors).toHaveLength(0)
      })

      it('returns valid for correct Group course setup', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({
            studentId: 'student-1',
            enrolledAt: '2024-01-15',
            discountType: 'Family',
          }))
          result.current.addStudent(createTestMember({
            studentId: 'student-2',
            enrolledAt: '2024-01-20',
            discountType: 'Family',
          }))
        })

        const validation = result.current.isStep2Valid('Group', 6)

        expect(validation.isValid).toBe(true)
        expect(validation.errors).toHaveLength(0)
      })
    })

    it('preserves step2 data after reset', () => {
      const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

      act(() => {
        result.current.addStudent(createTestMember())
      })

      act(() => {
        result.current.resetForm()
      })

      expect(result.current.formData.step2.students).toHaveLength(0)
    })
  })
})
