import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import { LessonDetailPage } from '../LessonDetailPage'
import type { Lesson } from '@/features/lessons/types'

// Mock react-router params
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return {
    ...actual,
    useParams: vi.fn(() => ({ lessonId: 'lesson-1' })),
  }
})

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

describe('LessonDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockGetById.mockResolvedValue(makeLesson())
    mockGetTasksByCourse.mockResolvedValue([])
    mockGetNotesByCourse.mockResolvedValue([])
  })

  it('shows loading spinner initially', () => {
    mockGetById.mockImplementation(() => new Promise(() => {}))
    const { container } = render(<LessonDetailPage />)
    expect(container.querySelector('.animate-spin')).toBeInTheDocument()
  })

  it('shows error state when fetch fails', async () => {
    mockGetById.mockRejectedValue(new Error('Not found'))
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('lessons.lessonNotFound')).toBeInTheDocument()
    })
  })

  it('shows back-to-courses button in error state', async () => {
    mockGetById.mockRejectedValue(new Error('Not found'))
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByRole('link', { name: 'lessons.actions.backToCourses' })).toBeInTheDocument()
    })
  })

  it('renders lesson instrument and course type in header', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Piano – Individual/ })).toBeInTheDocument()
    })
  })

  it('renders lesson scheduled date', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      // formatted date shown below heading
      const dateText = screen.getByText(/2024/)
      expect(dateText).toBeInTheDocument()
    })
  })

  it('renders status badge', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('lessons.status.scheduled')).toBeInTheDocument()
    })
  })

  it('renders time range', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText(/09:00/)).toBeInTheDocument()
    })
  })

  it('renders room when present', async () => {
    mockGetById.mockResolvedValue(makeLesson({ roomName: 'Room B' }))
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('Room B')).toBeInTheDocument()
    })
  })

  it('renders student name', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })
  })

  it('renders teacher name', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
  })

  it('renders back link to course', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      // Back button links to the course
      const backBtn = document.querySelector('a[href="/courses/course-1"]')
      expect(backBtn).toBeInTheDocument()
    })
  })

  it('renders course link with course type name', async () => {
    render(<LessonDetailPage />)
    await waitFor(() => {
      const courseLink = screen.getByRole('link', { name: 'Individual' })
      expect(courseLink).toHaveAttribute('href', '/courses/course-1')
    })
  })

  it('renders completed status with correct badge', async () => {
    mockGetById.mockResolvedValue(makeLesson({ status: 'Completed' }))
    render(<LessonDetailPage />)
    await waitFor(() => {
      expect(screen.getByText('lessons.status.completed')).toBeInTheDocument()
    })
  })
})
