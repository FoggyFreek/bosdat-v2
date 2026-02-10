import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@/test/utils'
import { CancelLessonDialog } from '../CancelLessonDialog'
import type { Lesson } from '@/features/lessons/types'

const mockToast = vi.fn()
vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({ toast: mockToast }),
}))

const mockUpdateStatus = vi.fn()
vi.mock('@/features/lessons/api', () => ({
  lessonsApi: {
    updateStatus: (...args: unknown[]) => mockUpdateStatus(...args),
  },
}))

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
  studentName: 'Alice',
  createdAt: '2026-01-01',
  updatedAt: '2026-01-01',
}

describe('CancelLessonDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockUpdateStatus.mockResolvedValue({})
  })

  it('does not render when lesson is null', () => {
    render(
      <CancelLessonDialog lesson={null} courseId="course-1" onClose={vi.fn()} />
    )

    expect(screen.queryByText('Cancel Lesson')).not.toBeInTheDocument()
  })

  it('renders dialog with lesson details when lesson is provided', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    expect(screen.getByText('Cancel Lesson')).toBeInTheDocument()
    expect(screen.getByText(/Alice/)).toBeInTheDocument()
  })

  it('renders reason textarea and action buttons', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    expect(screen.getByLabelText(/Reason for cancellation/)).toBeInTheDocument()
    expect(screen.getByText('Cancel')).toBeInTheDocument()
    expect(screen.getByText('No-Show')).toBeInTheDocument()
  })

  it('disables action buttons when reason is empty', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    const cancelButton = screen.getByText('Cancel').closest('button')!
    const noShowButton = screen.getByText('No-Show').closest('button')!

    expect(cancelButton).toBeDisabled()
    expect(noShowButton).toBeDisabled()
  })

  it('enables action buttons when reason is entered', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    fireEvent.change(screen.getByLabelText(/Reason for cancellation/), {
      target: { value: 'Student is sick' },
    })

    const cancelButton = screen.getByText('Cancel').closest('button')!
    const noShowButton = screen.getByText('No-Show').closest('button')!

    expect(cancelButton).not.toBeDisabled()
    expect(noShowButton).not.toBeDisabled()
  })

  it('submits cancellation with Cancelled status', async () => {
    const onClose = vi.fn()
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={onClose} />
    )

    fireEvent.change(screen.getByLabelText(/Reason for cancellation/), {
      target: { value: 'Student sick' },
    })

    fireEvent.click(screen.getByText('Cancel'))

    await waitFor(() => {
      expect(mockUpdateStatus).toHaveBeenCalledWith('lesson-1', {
        status: 'Cancelled',
        cancellationReason: 'Student sick',
      })
    })
  })

  it('submits cancellation with NoShow status', async () => {
    const onClose = vi.fn()
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={onClose} />
    )

    fireEvent.change(screen.getByLabelText(/Reason for cancellation/), {
      target: { value: 'Student did not show up' },
    })

    fireEvent.click(screen.getByText('No-Show'))

    await waitFor(() => {
      expect(mockUpdateStatus).toHaveBeenCalledWith('lesson-1', {
        status: 'NoShow',
        cancellationReason: 'Student did not show up',
      })
    })
  })

  it('shows close button that calls onClose', () => {
    const onClose = vi.fn()
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={onClose} />
    )

    // The dialog footer has a Close button; the X button also has a sr-only "Close"
    const closeButtons = screen.getAllByText('Close')
    const footerClose = closeButtons.find(
      (el) => el.closest('div')?.className.includes('flex-col-reverse')
    )!
    fireEvent.click(footerClose)

    expect(onClose).toHaveBeenCalled()
  })

  it('shows not invoiced label under Cancel button', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    expect(screen.getByText('Not invoiced')).toBeInTheDocument()
  })

  it('shows invoiced label under No-Show button', () => {
    render(
      <CancelLessonDialog lesson={baseLesson} courseId="course-1" onClose={vi.fn()} />
    )

    expect(screen.getByText('Invoiced')).toBeInTheDocument()
  })
})
