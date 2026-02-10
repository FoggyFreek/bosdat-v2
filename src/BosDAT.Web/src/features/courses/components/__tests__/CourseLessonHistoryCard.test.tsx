import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { CourseLessonHistoryCard } from '../CourseLessonHistoryCard'
import type { Lesson } from '@/features/lessons/types'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

const baseLesson: Lesson = {
  id: 'lesson-1',
  courseId: 'course-1',
  teacherId: 'teacher-1',
  teacherName: 'Jane Smith',
  courseTypeName: 'Piano Beginner',
  instrumentName: 'Piano',
  scheduledDate: '2026-02-10',
  startTime: '10:00:00',
  endTime: '10:45:00',
  status: 'Scheduled',
  isInvoiced: false,
  isPaidToTeacher: false,
  createdAt: '2026-01-01',
  updatedAt: '2026-01-01',
}

const cancelledLesson: Lesson = {
  ...baseLesson,
  id: 'lesson-2',
  scheduledDate: '2026-02-03',
  status: 'Cancelled',
  cancellationReason: 'Student sick',
}

describe('CourseLessonHistoryCard', () => {
  const defaultProps = {
    lessons: [baseLesson, cancelledLesson],
    courseId: 'course-1',
    filters: {},
    onFiltersChange: vi.fn(),
    onCancelLesson: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the lessons table with correct data', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    expect(screen.getByText('Recent Lessons')).toBeInTheDocument()
    expect(screen.getByText('Scheduled')).toBeInTheDocument()
    expect(screen.getByText('Cancelled')).toBeInTheDocument()
    // Table header columns
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Time')).toBeInTheDocument()
    expect(screen.getByText('Student')).toBeInTheDocument()
    expect(screen.getByText('Status')).toBeInTheDocument()
  })

  it('shows "No lessons found" when lessons array is empty', () => {
    render(<CourseLessonHistoryCard {...defaultProps} lessons={[]} />)

    expect(screen.getByText('No lessons found')).toBeInTheDocument()
  })

  it('shows filtered count header when date filters are active', () => {
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        filters={{ startDate: '2026-01-01' }}
      />
    )

    expect(screen.getByText('Lessons (2 results)')).toBeInTheDocument()
  })

  it('toggles the filter panel when Filter button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    expect(screen.queryByLabelText('From')).not.toBeInTheDocument()

    fireEvent.click(screen.getByText('Filter'))

    expect(screen.getByLabelText('From')).toBeInTheDocument()
    expect(screen.getByLabelText('To')).toBeInTheDocument()
  })

  it('calls onFiltersChange when date inputs change', () => {
    const onFiltersChange = vi.fn()
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        onFiltersChange={onFiltersChange}
      />
    )

    fireEvent.click(screen.getByText('Filter'))
    fireEvent.change(screen.getByLabelText('From'), {
      target: { value: '2026-01-01' },
    })

    expect(onFiltersChange).toHaveBeenCalledWith({
      startDate: '2026-01-01',
      endDate: undefined,
    })
  })

  it('shows Clear button when filters are active and clears them on click', () => {
    const onFiltersChange = vi.fn()
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        filters={{ startDate: '2026-01-01', endDate: '2026-02-01' }}
        onFiltersChange={onFiltersChange}
      />
    )

    fireEvent.click(screen.getByText('Clear'))

    expect(onFiltersChange).toHaveBeenCalledWith({
      startDate: undefined,
      endDate: undefined,
    })
  })

  it('shows move and cancel action buttons for scheduled lessons', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    const moveButtons = screen.getAllByTitle('Move lesson')
    const cancelButtons = screen.getAllByTitle('Cancel lesson')

    // Only the scheduled lesson should have action buttons
    expect(moveButtons).toHaveLength(1)
    expect(cancelButtons).toHaveLength(1)
  })

  it('does not show action buttons for cancelled lessons', () => {
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        lessons={[cancelledLesson]}
      />
    )

    expect(screen.queryByTitle('Move lesson')).not.toBeInTheDocument()
    expect(screen.queryByTitle('Cancel lesson')).not.toBeInTheDocument()
  })

  it('navigates to move lesson page when move button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    fireEvent.click(screen.getByTitle('Move lesson'))

    expect(mockNavigate).toHaveBeenCalledWith(
      '/courses/course-1/lessons/lesson-1/move'
    )
  })

  it('calls onCancelLesson when cancel button is clicked', () => {
    const onCancelLesson = vi.fn()
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        onCancelLesson={onCancelLesson}
      />
    )

    fireEvent.click(screen.getByTitle('Cancel lesson'))

    expect(onCancelLesson).toHaveBeenCalledWith(baseLesson)
  })

  it('navigates to add lesson page when Add lesson button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    fireEvent.click(screen.getByText('Add lesson'))

    expect(mockNavigate).toHaveBeenCalledWith('/courses/course-1/add-lesson')
  })

  it('renders Actions column header', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    expect(screen.getByText('Actions')).toBeInTheDocument()
  })
})
