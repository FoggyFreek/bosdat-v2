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

    expect(screen.getByText('courses.lessonHistory.recentTitle')).toBeInTheDocument()
    expect(screen.getByText('lessons.status.scheduled')).toBeInTheDocument()
    expect(screen.getByText('lessons.status.cancelled')).toBeInTheDocument()
    // Table header columns
    expect(screen.getByText('courses.lessonHistory.table.date')).toBeInTheDocument()
    expect(screen.getByText('courses.lessonHistory.table.time')).toBeInTheDocument()
    expect(screen.getByText('common.entities.student')).toBeInTheDocument()
    expect(screen.getByText('courses.lessonHistory.table.status')).toBeInTheDocument()
  })

  it('shows "No lessons found" when lessons array is empty', () => {
    render(<CourseLessonHistoryCard {...defaultProps} lessons={[]} />)

    expect(screen.getByText('courses.lessonHistory.noLessons')).toBeInTheDocument()
  })

  it('shows filtered count header when date filters are active', () => {
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        filters={{ startDate: '2026-01-01' }}
      />
    )

    expect(screen.getByText('courses.lessonHistory.results')).toBeInTheDocument()
  })

  it('toggles the filter panel when Filter button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    expect(screen.queryByLabelText('courses.lessonHistory.from')).not.toBeInTheDocument()

    fireEvent.click(screen.getByText('common.actions.filter'))

    expect(screen.getByLabelText('courses.lessonHistory.from')).toBeInTheDocument()
    expect(screen.getByLabelText('courses.lessonHistory.to')).toBeInTheDocument()
  })

  it('calls onFiltersChange when date inputs change', () => {
    const onFiltersChange = vi.fn()
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        onFiltersChange={onFiltersChange}
      />
    )

    fireEvent.click(screen.getByText('common.actions.filter'))
    fireEvent.change(screen.getByLabelText('courses.lessonHistory.from'), {
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

    fireEvent.click(screen.getByText('courses.lessonHistory.clear'))

    expect(onFiltersChange).toHaveBeenCalledWith({
      startDate: undefined,
      endDate: undefined,
    })
  })

  it('shows enabled move and cancel buttons for scheduled lessons', () => {
    render(<CourseLessonHistoryCard {...defaultProps} lessons={[baseLesson]} />)

    const moveButton = screen.getByTitle('lessons.moveLesson')
    const cancelButton = screen.getByTitle('lessons.cancelLesson')

    expect(moveButton).not.toBeDisabled()
    expect(cancelButton).not.toBeDisabled()
  })

  it('shows disabled move and cancel buttons for non-scheduled lessons', () => {
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        lessons={[cancelledLesson]}
      />
    )

    const moveButton = screen.getByTitle('lessons.moveLesson')
    const cancelButton = screen.getByTitle('lessons.cancelLesson')

    expect(moveButton).toBeDisabled()
    expect(cancelButton).toBeDisabled()
  })

  it('navigates to move lesson page when move button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} lessons={[baseLesson]} />)

    fireEvent.click(screen.getByTitle('lessons.moveLesson'))

    expect(mockNavigate).toHaveBeenCalledWith(
      '/courses/course-1/lessons/lesson-1/move'
    )
  })

  it('calls onCancelLesson when cancel button is clicked', () => {
    const onCancelLesson = vi.fn()
    render(
      <CourseLessonHistoryCard
        {...defaultProps}
        lessons={[baseLesson]}
        onCancelLesson={onCancelLesson}
      />
    )

    fireEvent.click(screen.getByTitle('lessons.cancelLesson'))

    expect(onCancelLesson).toHaveBeenCalledWith(baseLesson)
  })

  it('navigates to add lesson page when Add lesson button is clicked', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    fireEvent.click(screen.getByText('lessons.addLesson'))

    expect(mockNavigate).toHaveBeenCalledWith('/courses/course-1/add-lesson')
  })

  it('renders Actions column header', () => {
    render(<CourseLessonHistoryCard {...defaultProps} />)

    expect(screen.getByText('courses.lessonHistory.table.actions')).toBeInTheDocument()
  })
})
