import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils'
import { CourseEnrollmentsCard } from '../CourseEnrollmentsCard'
import type { Enrollment } from '@/features/enrollments/types'

const mockEnrollments: Enrollment[] = [
  {
    id: 'e1',
    studentId: 's1',
    studentName: 'Alice Johnson',
    courseId: 'c1',
    enrolledAt: '2025-09-01',
    discountPercent: 0,
    discountType: 'None',
    status: 'Active',
    invoicingPreference: 'Monthly',
  },
  {
    id: 'e2',
    studentId: 's2',
    studentName: 'Bob Williams',
    courseId: 'c1',
    enrolledAt: '2025-09-01',
    discountPercent: 10,
    discountType: 'Family',
    status: 'Trail',
    invoicingPreference: 'Quarterly',
  },
]

describe('CourseEnrollmentsCard', () => {
  it('renders title with enrollment count', () => {
    render(<CourseEnrollmentsCard enrollments={mockEnrollments} />)

    expect(screen.getByText('Enrolled Students (2)')).toBeInTheDocument()
  })

  it('shows empty state when no enrollments', () => {
    render(<CourseEnrollmentsCard enrollments={[]} />)

    expect(screen.getByText('Enrolled Students (0)')).toBeInTheDocument()
    expect(screen.getByText('No students enrolled')).toBeInTheDocument()
  })

  it('renders student names', () => {
    render(<CourseEnrollmentsCard enrollments={mockEnrollments} />)

    expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    expect(screen.getByText('Bob Williams')).toBeInTheDocument()
  })

  it('renders discount info when discount > 0', () => {
    render(<CourseEnrollmentsCard enrollments={mockEnrollments} />)

    expect(screen.getByText(/10% discount \(Family\)/)).toBeInTheDocument()
  })

  it('does not render discount info when discount is 0', () => {
    render(<CourseEnrollmentsCard enrollments={[mockEnrollments[0]]} />)

    expect(screen.queryByText(/discount/)).not.toBeInTheDocument()
  })

  it('renders status badges', () => {
    render(<CourseEnrollmentsCard enrollments={mockEnrollments} />)

    expect(screen.getByText('Active')).toBeInTheDocument()
    expect(screen.getByText('Trail')).toBeInTheDocument()
  })
})
