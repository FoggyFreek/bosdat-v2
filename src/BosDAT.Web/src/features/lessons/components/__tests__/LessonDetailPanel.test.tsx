import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { LessonDetailPanel } from '../LessonDetailPanel'
import type { Lesson } from '@/features/lessons/types'

vi.mock('@/features/lessons/api', () => ({
  lessonsApi: {
    getById: vi.fn(),
    getAll: vi.fn(),
    getByStudent: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    updateStatus: vi.fn(),
    delete: vi.fn(),
    generate: vi.fn(),
    generateBulk: vi.fn(),
  },
}))

vi.mock('@/features/course-tasks/api', () => ({
  courseTasksApi: {
    getByCourse: vi.fn(),
    create: vi.fn(),
    delete: vi.fn(),
  },
}))

vi.mock('@/features/lesson-notes/api', () => ({
  lessonNotesApi: {
    getByCourse: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    addAttachment: vi.fn(),
    deleteAttachment: vi.fn(),
  },
}))

vi.mock('@/components/LexicalEditor', () => ({
  LexicalEditor: () => <div data-testid="lexical-editor" />,
}))

import { lessonsApi } from '@/features/lessons/api'
import { courseTasksApi } from '@/features/course-tasks/api'
import { lessonNotesApi } from '@/features/lesson-notes/api'

const mockGetById = vi.mocked(lessonsApi.getById)
const mockGetTasksByCourse = vi.mocked(courseTasksApi.getByCourse)
const mockGetNotesByCourse = vi.mocked(lessonNotesApi.getByCourse)

const makeLesson = (overrides?: Partial<Lesson>): Lesson => ({
  id: 'lesson-1',
  courseId: 'course-1',
  teacherId: 'teacher-1',
  teacherName: 'John Doe',
  studentId: 'student-1',
  studentName: 'Alice Johnson',
  courseTypeName: 'Individual',
  instrumentName: 'Piano',
  scheduledDate: '2024-01-15',
  startTime: '09:00',
  endTime: '10:00',
  status: 'Scheduled',
  isInvoiced: false,
  isPaidToTeacher: false,
  createdAt: '2024-01-01T00:00:00',
  updatedAt: '2024-01-01T00:00:00',
  ...overrides,
})

describe('LessonDetailPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockGetById.mockResolvedValue(makeLesson())
    mockGetTasksByCourse.mockResolvedValue([])
    mockGetNotesByCourse.mockResolvedValue([])
  })

  it('renders panel header title', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    expect(screen.getByText('lessons.detail.title')).toBeInTheDocument()
  })

  it('renders close button', () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    const closeBtn = screen.getByRole('button')
    expect(closeBtn).toBeInTheDocument()
  })

  it('calls onClose when close button clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(<LessonDetailPanel lessonId="lesson-1" onClose={onClose} />)
    await user.click(screen.getByRole('button'))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('shows loading spinner while fetching', () => {
    mockGetById.mockImplementation(() => new Promise(() => {}))
    const { container } = render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    expect(container.querySelector('.animate-spin')).toBeInTheDocument()
  })

  it('shows error message when fetch fails', async () => {
    mockGetById.mockRejectedValue(new Error('Not found'))
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('lessons.lessonNotFound')).toBeInTheDocument()
    })
  })

  it('renders lesson instrument and course type', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('Piano – Individual')).toBeInTheDocument()
    })
  })

  it('renders lesson status badge', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('lessons.status.scheduled')).toBeInTheDocument()
    })
  })

  it('renders time display', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText(/09:00/)).toBeInTheDocument()
    })
  })

  it('renders student name', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })
  })

  it('renders teacher name', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
  })

  it('renders room when provided', async () => {
    mockGetById.mockResolvedValue(makeLesson({ roomName: 'Room A' }))
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      expect(screen.getByText('Room A')).toBeInTheDocument()
    })
  })

  it('does not render room section when roomName is absent', async () => {
    mockGetById.mockResolvedValue(makeLesson({ roomName: undefined }))
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      // Lesson title renders — confirms lesson loaded
      expect(screen.getByText('Piano – Individual')).toBeInTheDocument()
    })
    // No room icon row should be present
    const mapPinIcons = document.querySelectorAll('svg.lucide-map-pin')
    expect(mapPinIcons).toHaveLength(0)
  })

  it('renders link to full lesson page', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      const link = screen.getByRole('link', { name: /lessons\.detail\.openFull/ })
      expect(link).toHaveAttribute('href', '/lessons/lesson-1')
    })
  })

  it('renders link to course page', async () => {
    render(<LessonDetailPanel lessonId="lesson-1" onClose={vi.fn()} />)
    await waitFor(() => {
      const courseLink = screen.getByRole('link', { name: 'Individual' })
      expect(courseLink).toHaveAttribute('href', '/courses/course-1')
    })
  })
})
