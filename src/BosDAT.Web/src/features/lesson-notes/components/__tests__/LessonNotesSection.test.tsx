import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { LessonNotesSection } from '../LessonNotesSection'
import type { LessonNote } from '../../types'

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

// Mock LexicalEditor to avoid Lexical setup in tests
vi.mock('@/components/LexicalEditor', () => ({
  LexicalEditor: ({ placeholder, onChange }: { placeholder?: string; onChange?: (v: string) => void }) => (
    <div data-testid="lexical-editor">
      <span>{placeholder}</span>
      <textarea data-testid="editor-textarea" onChange={e => onChange?.(e.target.value)} />
    </div>
  ),
}))

import { lessonNotesApi } from '@/features/lesson-notes/api'
const mockGetByCourse = vi.mocked(lessonNotesApi.getByCourse)

const makeNote = (id: string, date: string): LessonNote => ({
  id,
  lessonId: 'lesson-1',
  content: JSON.stringify({ root: { children: [{ type: 'paragraph', children: [{ text: `Content of ${id}`, type: 'text' }] }] } }),
  lessonDate: date,
  attachments: [],
  createdAt: `${date}T09:00:00`,
  updatedAt: `${date}T09:00:00`,
})

describe('LessonNotesSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockGetByCourse.mockResolvedValue([makeNote('note-1', '2024-01-15'), makeNote('note-2', '2024-01-22')])
  })

  it('renders section heading', async () => {
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getAllByText('lessons.notes.title').length).toBeGreaterThan(0))
  })

  it('shows loading skeletons initially', () => {
    mockGetByCourse.mockImplementation(() => new Promise(() => {}))
    const { container } = render(<LessonNotesSection lessonId="lesson-1" />)
    const skeletons = container.querySelectorAll('.animate-pulse')
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('renders note cards after load', async () => {
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => {
      expect(screen.getByText('Content of note-1')).toBeInTheDocument()
      expect(screen.getByText('Content of note-2')).toBeInTheDocument()
    })
  })

  it('renders empty state message when no notes', async () => {
    mockGetByCourse.mockResolvedValue([])
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => {
      expect(screen.getByText('lessons.notes.empty')).toBeInTheDocument()
    })
  })

  it('renders add note button', async () => {
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => {
      expect(screen.getByText('lessons.notes.addNote')).toBeInTheDocument()
    })
  })

  it('clicking add note opens editor in new note mode', async () => {
    const user = userEvent.setup()
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getByText('lessons.notes.addNote')).toBeInTheDocument())

    await user.click(screen.getByText('lessons.notes.addNote'))

    // NoteEditorView is shown — it renders a back button and lexical editor
    expect(screen.getByTestId('lexical-editor')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'common.actions.back' })).toBeInTheDocument()
  })

  it('clicking a note card opens the editor', async () => {
    const user = userEvent.setup()
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getByText('Content of note-1')).toBeInTheDocument())

    const noteButton = screen.getByText('Content of note-1').closest('button')!
    await user.click(noteButton)

    // Editor is now shown
    expect(screen.getByTestId('lexical-editor')).toBeInTheDocument()
  })

  it('back button in editor returns to grid view', async () => {
    const user = userEvent.setup()
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getByText('lessons.notes.addNote')).toBeInTheDocument())

    // Open editor
    await user.click(screen.getByText('lessons.notes.addNote'))
    expect(screen.getByTestId('lexical-editor')).toBeInTheDocument()

    // Go back
    await user.click(screen.getByRole('button', { name: 'common.actions.back' }))
    expect(screen.queryByTestId('lexical-editor')).not.toBeInTheDocument()
    expect(screen.getByText('lessons.notes.addNote')).toBeInTheDocument()
  })

  it('renders grid view by default', async () => {
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getByRole('button', { name: 'lessons.notes.viewGrid' })).toBeInTheDocument())

    const gridButton = screen.getByRole('button', { name: 'lessons.notes.viewGrid' })
    expect(gridButton.className).toContain('secondary')
  })

  it('toggles to list view when list button is clicked', async () => {
    const user = userEvent.setup()
    render(<LessonNotesSection lessonId="lesson-1" />)
    await waitFor(() => expect(screen.getByRole('button', { name: 'lessons.notes.viewList' })).toBeInTheDocument())

    await user.click(screen.getByRole('button', { name: 'lessons.notes.viewList' }))
    const listButton = screen.getByRole('button', { name: 'lessons.notes.viewList' })
    expect(listButton.className).toContain('secondary')
  })
})
