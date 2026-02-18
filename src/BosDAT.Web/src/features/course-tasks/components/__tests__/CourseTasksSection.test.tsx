import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CourseTasksSection } from '../CourseTasksSection'

vi.mock('@/features/course-tasks/api', () => ({
  courseTasksApi: {
    getByCourse: vi.fn(),
    create: vi.fn(),
    delete: vi.fn(),
  },
}))

import { courseTasksApi } from '@/features/course-tasks/api'
const mockGetByCourse = vi.mocked(courseTasksApi.getByCourse)
const mockCreate = vi.mocked(courseTasksApi.create)
const mockDelete = vi.mocked(courseTasksApi.delete)

const mockTasks = [
  { id: 'task-1', courseId: 'course-1', title: 'Scales practice', createdAt: '2024-01-15T09:00:00' },
  { id: 'task-2', courseId: 'course-1', title: 'Sight reading', createdAt: '2024-01-15T10:00:00' },
]

describe('CourseTasksSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockGetByCourse.mockResolvedValue(mockTasks)
    mockCreate.mockResolvedValue({ id: 'task-3', courseId: 'course-1', title: 'New task', createdAt: '2024-01-16T09:00:00' })
    mockDelete.mockResolvedValue(undefined as never)
  })

  it('renders section heading', async () => {
    render(<CourseTasksSection courseId="course-1" />)
    expect(screen.getByText('lessons.tasks.title')).toBeInTheDocument()
  })

  it('shows loading skeleton initially', () => {
    mockGetByCourse.mockImplementation(() => new Promise(() => {}))
    const { container } = render(<CourseTasksSection courseId="course-1" />)
    const skeletons = container.querySelectorAll('.animate-pulse')
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('renders task list after load', async () => {
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => {
      expect(screen.getByText('Scales practice')).toBeInTheDocument()
      expect(screen.getByText('Sight reading')).toBeInTheDocument()
    })
  })

  it('renders empty state when no tasks', async () => {
    mockGetByCourse.mockResolvedValue([])
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => {
      expect(screen.getByText('lessons.tasks.empty')).toBeInTheDocument()
    })
  })

  it('renders add input with placeholder', async () => {
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => {
      expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument()
    })
  })

  it('add button is disabled when input is empty', async () => {
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => {
      const addButton = screen.getByRole('button', { name: 'common.actions.add' })
      expect(addButton).toBeDisabled()
    })
  })

  it('add button becomes enabled when input has text', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument())

    await user.type(screen.getByPlaceholderText('lessons.tasks.placeholder'), 'New task')
    const addButton = screen.getByRole('button', { name: 'common.actions.add' })
    expect(addButton).not.toBeDisabled()
  })

  it('creates task when add button clicked', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument())

    await user.type(screen.getByPlaceholderText('lessons.tasks.placeholder'), 'New task')
    await user.click(screen.getByRole('button', { name: 'common.actions.add' }))

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith('course-1', { title: 'New task' })
    })
  })

  it('creates task when Enter key pressed', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument())

    await user.type(screen.getByPlaceholderText('lessons.tasks.placeholder'), 'New task{Enter}')

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith('course-1', { title: 'New task' })
    })
  })

  it('does not create task when input is blank', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument())

    await user.type(screen.getByPlaceholderText('lessons.tasks.placeholder'), '   {Enter}')
    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('invalidates query after successful create', async () => {
    const user = userEvent.setup()
    // After create resolves, getByCourse is called again for refetch
    const updatedTasks = [...mockTasks, { id: 'task-3', courseId: 'course-1', title: 'New task', createdAt: '2024-01-16T09:00:00' }]
    mockGetByCourse.mockResolvedValueOnce(mockTasks).mockResolvedValue(updatedTasks)

    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByPlaceholderText('lessons.tasks.placeholder')).toBeInTheDocument())

    await user.type(screen.getByPlaceholderText('lessons.tasks.placeholder'), 'New task')
    await user.click(screen.getByRole('button', { name: 'common.actions.add' }))

    await waitFor(() => {
      expect(mockGetByCourse).toHaveBeenCalledTimes(2)
    })
  })

  it('deletes task via circle button', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByText('Scales practice')).toBeInTheDocument())

    const markDoneButtons = screen.getAllByRole('button', { name: 'lessons.tasks.markDone' })
    await user.click(markDoneButtons[0])

    await waitFor(() => {
      expect(mockDelete).toHaveBeenCalledWith('course-1', 'task-1')
    })
  })

  it('deletes task via trash button', async () => {
    const user = userEvent.setup()
    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByText('Scales practice')).toBeInTheDocument())

    const deleteButtons = screen.getAllByRole('button', { name: 'common.actions.delete' })
    await user.click(deleteButtons[0])

    await waitFor(() => {
      expect(mockDelete).toHaveBeenCalledWith('course-1', 'task-1')
    })
  })

  it('invalidates query after successful delete', async () => {
    const user = userEvent.setup()
    mockGetByCourse.mockResolvedValueOnce(mockTasks).mockResolvedValue([mockTasks[1]])

    render(<CourseTasksSection courseId="course-1" />)
    await waitFor(() => expect(screen.getByText('Scales practice')).toBeInTheDocument())

    const markDoneButtons = screen.getAllByRole('button', { name: 'lessons.tasks.markDone' })
    await user.click(markDoneButtons[0])

    await waitFor(() => {
      expect(mockGetByCourse).toHaveBeenCalledTimes(2)
    })
  })
})
