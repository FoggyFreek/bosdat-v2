import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils'
import { CourseSummaryCard } from '../CourseSummaryCard'
import type { Course } from '@/features/courses/types'

const baseCourse: Course = {
  id: '1',
  teacherId: 't1',
  teacherName: 'John Doe',
  courseTypeId: 1,
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

    expect(screen.getByText('Course Details')).toBeInTheDocument()
    expect(screen.getByText('Piano')).toBeInTheDocument()
    expect(screen.getByText('Piano Beginner')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('Monday')).toBeInTheDocument()
    expect(screen.getByText('Active')).toBeInTheDocument()
  })

  it('renders frequency with WeekParityBadge', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, frequency: 'Biweekly', weekParity: 'Odd' }} />)

    expect(screen.getByText('Biweekly')).toBeInTheDocument()
    expect(screen.getByText('Odd Weeks')).toBeInTheDocument()
  })

  it('renders end date when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, endDate: '2026-06-30' }} />)

    expect(screen.getByText('End Date:')).toBeInTheDocument()
  })

  it('does not render end date when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('End Date:')).not.toBeInTheDocument()
  })

  it('renders room when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, roomId: 1, roomName: 'Room A' }} />)

    expect(screen.getByText('Room:')).toBeInTheDocument()
    expect(screen.getByText('Room A')).toBeInTheDocument()
  })

  it('does not render room when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('Room:')).not.toBeInTheDocument()
  })

  it('renders trial badge when isTrial is true', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, isTrial: true }} />)

    expect(screen.getByText('Trial')).toBeInTheDocument()
  })

  it('does not render trial badge when isTrial is false', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('Trial')).not.toBeInTheDocument()
  })

  it('renders workshop when isWorkshop is true', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, isWorkshop: true }} />)

    expect(screen.getByText('Workshop:')).toBeInTheDocument()
    expect(screen.getByText('Yes')).toBeInTheDocument()
  })

  it('does not render workshop when isWorkshop is false', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('Workshop:')).not.toBeInTheDocument()
  })

  it('renders notes when present', () => {
    render(<CourseSummaryCard course={{ ...baseCourse, notes: 'Bring your own keyboard' }} />)

    expect(screen.getByText('Notes:')).toBeInTheDocument()
    expect(screen.getByText('Bring your own keyboard')).toBeInTheDocument()
  })

  it('does not render notes when absent', () => {
    render(<CourseSummaryCard course={baseCourse} />)

    expect(screen.queryByText('Notes:')).not.toBeInTheDocument()
  })
})
