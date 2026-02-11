import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { CourseSummaryCard } from '../CourseSummaryCard'
import type { Course } from '@/features/courses/types'

const baseCourse: Course = {
  id: '1',
  teacherId: 't1',
  teacherName: 'John Doe',
  courseTypeId: 'ct-1',
  courseTypeName: 'Piano Beginner',
  instrumentName: 'Piano',
  dayOfWeek: 'Monday',
  startTime: '10:00',
  endTime: '10:45',
  frequency: 'Weekly',
  weekParity: 'All',
  startDate: '2025-09-01',
  status: 'Active',
  isWorkshop: false,
  isTrial: false,
  enrollmentCount: 0,
  enrollments: [],
  createdAt: '2025-01-01',
  updatedAt: '2025-01-01',
}

describe('CourseSummaryCard', () => {
  it('renders title and required fields', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.getByText('courses.sections.summary')).toBeInTheDocument()
    expect(screen.getByText('Piano')).toBeInTheDocument()
    expect(screen.getByText('Piano Beginner')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('common.time.days.monday')).toBeInTheDocument()
    expect(screen.getByText('courses.status.active')).toBeInTheDocument()
  })

  it('renders frequency with WeekParityBadge', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, frequency: 'Biweekly', weekParity: 'Odd' }} />)

    expect(screen.getByText('courses.frequency.biweekly')).toBeInTheDocument()
    expect(screen.getByText('courses.parity.odd')).toBeInTheDocument()
  })

  it('renders end date when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, endDate: '2026-06-30' }} />)

    expect(screen.getByText(/courses\.summary\.endDate/)).toBeInTheDocument()
  })

  it('does not render end date when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText(/courses\.summary\.endDate/)).not.toBeInTheDocument()
  })

  it('renders room when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, roomId: 1, roomName: 'Room A' }} />)

    expect(screen.getByText(/common\.entities\.room/)).toBeInTheDocument()
    expect(screen.getByText('Room A')).toBeInTheDocument()
  })

  it('does not render room when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText(/common\.entities\.room/)).not.toBeInTheDocument()
  })

  it('renders trial badge when isTrial is true', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, isTrial: true }} />)

    expect(screen.getByText('common.status.trial')).toBeInTheDocument()
  })

  it('does not render trial badge when isTrial is false', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('common.status.trial')).not.toBeInTheDocument()
  })

  it('renders workshop when isWorkshop is true', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, isWorkshop: true }} />)

    expect(screen.getByText(/courses\.summary\.workshop/)).toBeInTheDocument()
    expect(screen.getByText('common.form.yes')).toBeInTheDocument()
  })

  it('does not render workshop when isWorkshop is false', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText(/courses\.summary\.workshop/)).not.toBeInTheDocument()
  })

  it('renders notes when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, notes: 'Bring your own keyboard' }} />)

    expect(screen.getByText(/courses\.summary\.notes/)).toBeInTheDocument()
    expect(screen.getByText('Bring your own keyboard')).toBeInTheDocument()
  })

  it('does not render notes when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText(/courses\.summary\.notes/)).not.toBeInTheDocument()
  })
})
