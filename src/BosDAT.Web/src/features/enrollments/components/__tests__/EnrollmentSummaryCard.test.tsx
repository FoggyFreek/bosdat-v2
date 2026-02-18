import { describe, it, expect} from 'vitest'
import { render, screen } from '@/test/utils'
import { EnrollmentSummaryCard } from '../EnrollmentSummaryCard'

describe('EnrollmentSummaryCard', () => {
  it('renders title', () => {
    render(<EnrollmentSummaryCard title="Test Card" />)

    expect(screen.getByText('Test Card')).toBeInTheDocument()
  })

  it('renders course type with label', () => {
    render(
      <EnrollmentSummaryCard
        title="Details"
        courseTypeName="Piano Beginner"
        courseTypeLabel="Individual"
      />
    )

    expect(screen.getByText('enrollments.summary.courseType')).toBeInTheDocument()
    expect(screen.getByText(/Piano Beginner/)).toBeInTheDocument()
    expect(screen.getByText('(Individual)')).toBeInTheDocument()
  })

  it('renders course type without label', () => {
    render(
      <EnrollmentSummaryCard title="Details" courseTypeName="Piano Beginner" />
    )

    expect(screen.getByText(/Piano Beginner/)).toBeInTheDocument()
    expect(screen.queryByText(/\(/)).not.toBeInTheDocument()
  })

  it('renders teacher name', () => {
    render(<EnrollmentSummaryCard title="Details" teacherName="Jane Smith" />)

    expect(screen.getByText('enrollments.summary.teacher')).toBeInTheDocument()
    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
  })

  it('renders start date with day of week', () => {
    render(
      <EnrollmentSummaryCard
        title="Details"
        startDate="1-9-2025"
        dayOfWeek="Monday"
      />
    )

    expect(screen.getByText('enrollments.summary.startDate')).toBeInTheDocument()
    expect(screen.getByText(/1-9-2025 \(Monday\)/)).toBeInTheDocument()
  })

  it('renders end date when present', () => {
    render(<EnrollmentSummaryCard title="Details" endDate="30-6-2026" />)

    expect(screen.getByText('enrollments.summary.endDate')).toBeInTheDocument()
    expect(screen.getByText('30-6-2026')).toBeInTheDocument()
  })

  it('renders frequency', () => {
    render(<EnrollmentSummaryCard title="Details" frequency="Once per week" />)

    expect(screen.getByText('enrollments.summary.frequency')).toBeInTheDocument()
    expect(screen.getByText('Once per week')).toBeInTheDocument()
  })

  it('renders trial badge when isTrial is true', () => {
    render(<EnrollmentSummaryCard title="Details" isTrial />)

    expect(screen.getByText('enrollments.summary.type')).toBeInTheDocument()
    expect(screen.getByText('enrollments.summary.trialLesson')).toBeInTheDocument()
  })

  it('does not render trial badge when isTrial is falsy', () => {
    render(<EnrollmentSummaryCard title="Details" frequency="Weekly" />)

    expect(screen.queryByText('enrollments.summary.trialLesson')).not.toBeInTheDocument()
  })

  it('renders max students', () => {
    render(<EnrollmentSummaryCard title="Details" maxStudents={5} />)

    expect(screen.getByText('enrollments.summary.maxStudents')).toBeInTheDocument()
    expect(screen.getByText('5')).toBeInTheDocument()
  })

  it('renders day of week as standalone row when no startDate', () => {
    render(<EnrollmentSummaryCard title="Details" dayOfWeek="Wednesday" />)

    expect(screen.getByText('enrollments.summary.day')).toBeInTheDocument()
    expect(screen.getByText('Wednesday')).toBeInTheDocument()
  })

  it('renders time with start and end', () => {
    render(
      <EnrollmentSummaryCard
        title="Details"
        startTime="10:00"
        endTime="10:45"
      />
    )

    expect(screen.getByText('enrollments.summary.time')).toBeInTheDocument()
    expect(screen.getByText('10:00 – 10:45')).toBeInTheDocument()
  })

  it('renders room name', () => {
    render(<EnrollmentSummaryCard title="Details" roomName="Room A" />)

    expect(screen.getByText('enrollments.summary.room')).toBeInTheDocument()
    expect(screen.getByText('Room A')).toBeInTheDocument()
  })

  it('renders children', () => {
    render(
      <EnrollmentSummaryCard title="Students">
        <p>Child content</p>
      </EnrollmentSummaryCard>
    )

    expect(screen.getByText('Child content')).toBeInTheDocument()
  })

  it('applies custom className', () => {
    const { container } = render(
      <EnrollmentSummaryCard title="Test" className="custom-class" />
    )

    expect(container.firstChild).toHaveClass('custom-class')
  })

  it('does not render field rows when no props provided', () => {
    render(<EnrollmentSummaryCard title="Empty" />)

    expect(screen.getByText('Empty')).toBeInTheDocument()
    expect(screen.queryByText('enrollments.summary.courseType')).not.toBeInTheDocument()
    expect(screen.queryByText('enrollments.summary.teacher')).not.toBeInTheDocument()
    expect(screen.queryByText('enrollments.summary.room')).not.toBeInTheDocument()
  })
})
