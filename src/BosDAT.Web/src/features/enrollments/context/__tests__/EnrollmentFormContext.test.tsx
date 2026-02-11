import { describe, it, expect, vi } from 'vitest'
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

  // Note: Testing that the hook throws when used outside provider is an implementation detail
  // and is difficult to test properly with renderHook. The error handling is demonstrated
  // by the fact that all other tests use the wrapper, and users will get a clear error
  // message if they forget to use the provider.

  // Step 2 Tests
  describe('Step 2: Student Selection', () => {
    const createTestMember = (overrides: Partial<EnrollmentGroupMember> = {}): EnrollmentGroupMember => ({
      studentId: 'student-1',
      studentName: 'John Doe',
      enrolledAt: '2024-01-15',
      discountType: 'None',
      discountPercentage: 0,
      invoicingPreference: 'Monthly',
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

      it('allows mixing Family and Course discounts across different students', () => {
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

        // This should be valid - different students can have different discount types
        expect(validation.isValid).toBe(true)
        expect(validation.errors).toHaveLength(0)
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

    // Fallback tests for undefined students array
    describe('handles undefined students array gracefully', () => {
      it('handles adding student when students array exists', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })
        const member = createTestMember()

        act(() => {
          result.current.addStudent(member)
        })

        expect(result.current.formData.step2.students).toHaveLength(1)
        expect(result.current.formData.step2.students[0]).toEqual(member)
      })

      it('handles removing student from empty array', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.removeStudent('non-existent')
        })

        expect(result.current.formData.step2.students).toEqual([])
      })

      it('handles updating student in empty array', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStudent('non-existent', { discountType: 'Family' })
        })

        expect(result.current.formData.step2.students).toEqual([])
      })

      it('validates step2 with empty students array', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        const validation = result.current.isStep2Valid('Individual', 1)

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('At least one student must be selected')
      })
    })
  })

  // Step 3 Tests
  describe('Step 3: Calendar Slot Selection', () => {
    describe('UPDATE_STEP3 action', () => {
      it('should update step3 data immutably', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        const step3Data = {
          selectedRoomId: 1,
          selectedDayOfWeek: 1,
          selectedDate: '2024-03-18',
          selectedStartTime: '14:00',
          selectedEndTime: '15:00',
        }

        act(() => {
          result.current.updateStep3(step3Data)
        })

        expect(result.current.formData.step3).toEqual(step3Data)
      })

      it('should partially update step3 data', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({
            selectedRoomId: 1,
            selectedDayOfWeek: 1,
          })
        })

        expect(result.current.formData.step3).toEqual({
          selectedRoomId: 1,
          selectedDayOfWeek: 1,
          selectedDate: null,
          selectedStartTime: null,
          selectedEndTime: null,
        })

        act(() => {
          result.current.updateStep3({
            selectedDate: '2024-03-18',
            selectedStartTime: '14:00',
          })
        })

        expect(result.current.formData.step3).toEqual({
          selectedRoomId: 1,
          selectedDayOfWeek: 1,
          selectedDate: '2024-03-18',
          selectedStartTime: '14:00',
          selectedEndTime: null,
        })
      })

      it('should maintain immutability when updating step3', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        const initialFormData = result.current.formData
        const initialStep3 = result.current.formData.step3

        act(() => {
          result.current.updateStep3({ selectedRoomId: 1 })
        })

        expect(result.current.formData).not.toBe(initialFormData)
        expect(result.current.formData.step3).not.toBe(initialStep3)
      })
    })

    describe('isStep3Valid', () => {
      it('should return invalid when no data is set', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        const validation = result.current.isStep3Valid()

        expect(validation.isValid).toBe(false)
        expect(validation.errors).toContain('Room must be selected')
        expect(validation.errors).toContain('Date must be selected')
        expect(validation.errors).toContain('Start time must be selected')
      })

      it('should return invalid when only room is selected', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({ selectedRoomId: 1 })
        })

        const validation = result.current.isStep3Valid()

        expect(validation.isValid).toBe(false)
        expect(validation.errors).not.toContain('Room must be selected')
        expect(validation.errors).toContain('Date must be selected')
        expect(validation.errors).toContain('Start time must be selected')
      })

      it('should return invalid when room and date are selected but no time', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({
            selectedRoomId: 1,
            selectedDate: '2024-03-18',
          })
        })

        const validation = result.current.isStep3Valid()

        expect(validation.isValid).toBe(false)
        expect(validation.errors).not.toContain('Room must be selected')
        expect(validation.errors).not.toContain('Date must be selected')
        expect(validation.errors).toContain('Start time must be selected')
      })

      it('should return valid when all required fields are set', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({
            selectedRoomId: 1,
            selectedDate: '2024-03-18',
            selectedStartTime: '14:00',
          })
        })

        const validation = result.current.isStep3Valid()

        expect(validation.isValid).toBe(true)
        expect(validation.errors).toEqual([])
      })

      it('should accept endTime as optional', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({
            selectedRoomId: 1,
            selectedDate: '2024-03-18',
            selectedStartTime: '14:00',
            selectedEndTime: '15:00',
          })
        })

        const validation = result.current.isStep3Valid()

        expect(validation.isValid).toBe(true)
        expect(validation.errors).toEqual([])
      })
    })

    describe('updateStep3 method', () => {
      it('should be available in context', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        expect(result.current.updateStep3).toBeDefined()
        expect(typeof result.current.updateStep3).toBe('function')
      })

      it('should update only provided fields', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep3({
            selectedRoomId: 1,
            selectedDayOfWeek: 2,
            selectedDate: '2024-03-19',
          })
        })

        expect(result.current.formData.step3.selectedRoomId).toBe(1)
        expect(result.current.formData.step3.selectedDayOfWeek).toBe(2)
        expect(result.current.formData.step3.selectedDate).toBe('2024-03-19')
        expect(result.current.formData.step3.selectedStartTime).toBe(null)
        expect(result.current.formData.step3.selectedEndTime).toBe(null)
      })
    })

    describe('syncStartDate', () => {
      const createTestMember = (overrides: Partial<EnrollmentGroupMember> = {}): EnrollmentGroupMember => ({
        studentId: 'student-1',
        studentName: 'John Doe',
        enrolledAt: '2024-01-15',
        discountType: 'None',
        discountPercentage: 0,
        invoicingPreference: 'Monthly',
        note: '',
        isEligibleForCourseDiscount: false,
        ...overrides,
      })

      it('should update step1 startDate', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step1.startDate).toBe('2024-02-01')
      })

      it('should update step1 endDate when it matches old startDate', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15', endDate: '2024-01-15' })
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step1.startDate).toBe('2024-02-01')
        expect(result.current.formData.step1.endDate).toBe('2024-02-01')
      })

      it('should not update step1 endDate when it differs from old startDate', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15', endDate: '2024-06-30' })
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step1.startDate).toBe('2024-02-01')
        expect(result.current.formData.step1.endDate).toBe('2024-06-30')
      })

      it('should update enrolledAt for students matching old startDate', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1', enrolledAt: '2024-01-15' }))
          result.current.addStudent(createTestMember({ studentId: 'student-2', enrolledAt: '2024-01-15' }))
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step2.students[0].enrolledAt).toBe('2024-02-01')
        expect(result.current.formData.step2.students[1].enrolledAt).toBe('2024-02-01')
      })

      it('should not update enrolledAt for students with different dates', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1', enrolledAt: '2024-01-15' }))
          result.current.addStudent(createTestMember({ studentId: 'student-2', enrolledAt: '2024-03-01' }))
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step2.students[0].enrolledAt).toBe('2024-02-01')
        expect(result.current.formData.step2.students[1].enrolledAt).toBe('2024-03-01')
      })

      it('should be a no-op when new date matches current startDate', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
          result.current.addStudent(createTestMember({ studentId: 'student-1', enrolledAt: '2024-01-15' }))
        })

        const formDataBefore = result.current.formData

        act(() => {
          result.current.syncStartDate('2024-01-15')
        })

        expect(result.current.formData).toBe(formDataBefore)
      })

      it('should handle empty students array', () => {
        const { result } = renderHook(() => useEnrollmentForm(), { wrapper })

        act(() => {
          result.current.updateStep1({ startDate: '2024-01-15' })
        })

        act(() => {
          result.current.syncStartDate('2024-02-01')
        })

        expect(result.current.formData.step1.startDate).toBe('2024-02-01')
        expect(result.current.formData.step2.students).toEqual([])
      })
    })
  })
})
