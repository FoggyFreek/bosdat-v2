import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { NoteEditorView } from '../NoteEditorView'
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

vi.mock('@/components/LexicalEditor', () => ({
  LexicalEditor: ({ placeholder, onChange }: { placeholder?: string; onChange?: (v: string) => void }) => (
    <div data-testid="lexical-editor">
      <span>{placeholder}</span>
      <textarea data-testid="editor-textarea" onChange={e => onChange?.(e.target.value)} />
    </div>
  ),
}))

import { lessonNotesApi } from '@/features/lesson-notes/api'
const mockCreate = vi.mocked(lessonNotesApi.create)
const mockUpdate = vi.mocked(lessonNotesApi.update)
const mockDelete = vi.mocked(lessonNotesApi.delete)
const mockDeleteAttachment = vi.mocked(lessonNotesApi.deleteAttachment)
const mockAddAttachment = vi.mocked(lessonNotesApi.addAttachment)

const makeNote = (overrides?: Partial<LessonNote>): LessonNote => ({
  id: 'note-1',
  lessonId: 'lesson-1',
  content: '{"root":{"children":[]}}',
  lessonDate: '2024-01-15',
  attachments: [],
  createdAt: '2024-01-15T09:00:00',
  updatedAt: '2024-01-15T09:00:00',
  ...overrides,
})

const defaultProps = {
  lessonId: 'lesson-1',
  lessonDate: '2024-01-15',
  queryKey: ['lesson-notes', 'lesson-1'],
  onBack: vi.fn(),
}

describe('NoteEditorView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockCreate.mockResolvedValue(makeNote())
    mockUpdate.mockResolvedValue(makeNote())
    mockDelete.mockResolvedValue(undefined as never)
    mockDeleteAttachment.mockResolvedValue(undefined as never)
    mockAddAttachment.mockResolvedValue({
      id: 'att-new', fileName: 'file.pdf', contentType: 'application/pdf', fileSize: 1024, url: '/files/file.pdf'
    })
  })

  it('renders back button', () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    expect(screen.getByRole('button', { name: 'common.actions.back' })).toBeInTheDocument()
  })

  it('calls onBack when back button clicked', async () => {
    const user = userEvent.setup()
    const onBack = vi.fn()
    render(<NoteEditorView note={null} {...defaultProps} onBack={onBack} />)
    await user.click(screen.getByRole('button', { name: 'common.actions.back' }))
    expect(onBack).toHaveBeenCalledOnce()
  })

  it('renders save button', () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    expect(screen.getByRole('button', { name: 'common.actions.save' })).toBeInTheDocument()
  })

  it('renders the lexical editor', () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    expect(screen.getByTestId('lexical-editor')).toBeInTheDocument()
  })

  it('does NOT show delete button for new note (note=null)', () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    expect(screen.queryByRole('button', { name: 'lessons.notes.deleteNote' })).not.toBeInTheDocument()
  })

  it('shows delete button for existing note', () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    expect(screen.getByRole('button', { name: 'lessons.notes.deleteNote' })).toBeInTheDocument()
  })

  it('shows attachment section for existing note', () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    expect(screen.getByText('lessons.notes.attachments')).toBeInTheDocument()
    expect(screen.getByText('lessons.notes.addAttachment')).toBeInTheDocument()
  })

  it('hides attachment section for new note', () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    expect(screen.queryByText('lessons.notes.attachments')).not.toBeInTheDocument()
  })

  it('calls create API when saving a new note', async () => {
    const user = userEvent.setup()
    render(<NoteEditorView note={null} {...defaultProps} />)

    await user.click(screen.getByRole('button', { name: 'common.actions.save' }))

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith('lesson-1', '')
    })
  })

  it('calls update API when saving an existing note', async () => {
    const user = userEvent.setup()
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)

    await user.click(screen.getByRole('button', { name: 'common.actions.save' }))

    await waitFor(() => {
      expect(mockUpdate).toHaveBeenCalledWith('lesson-1', 'note-1', expect.any(String))
    })
  })

  it('calls onBack after creating a new note', async () => {
    const user = userEvent.setup()
    const onBack = vi.fn()
    render(<NoteEditorView note={null} {...defaultProps} onBack={onBack} />)

    await user.click(screen.getByRole('button', { name: 'common.actions.save' }))

    await waitFor(() => {
      expect(onBack).toHaveBeenCalledOnce()
    })
  })

  it('does NOT call onBack after updating an existing note (stays in editor)', async () => {
    const user = userEvent.setup()
    const onBack = vi.fn()
    render(<NoteEditorView note={makeNote()} {...defaultProps} onBack={onBack} />)

    await user.click(screen.getByRole('button', { name: 'common.actions.save' }))

    await waitFor(() => {
      expect(mockUpdate).toHaveBeenCalled()
    })
    expect(onBack).not.toHaveBeenCalled()
  })

  it('calls delete API and onBack when delete button clicked', async () => {
    const user = userEvent.setup()
    const onBack = vi.fn()
    render(<NoteEditorView note={makeNote()} {...defaultProps} onBack={onBack} />)

    await user.click(screen.getByRole('button', { name: 'lessons.notes.deleteNote' }))

    await waitFor(() => {
      expect(mockDelete).toHaveBeenCalledWith('lesson-1', 'note-1')
      expect(onBack).toHaveBeenCalledOnce()
    })
  })

  it('renders attachment items when note has attachments', () => {
    const note = makeNote({
      attachments: [
        { id: 'att-1', fileName: 'report.pdf', contentType: 'application/pdf', fileSize: 1024, url: '/files/report.pdf' },
      ],
    })
    render(<NoteEditorView note={note} {...defaultProps} />)
    expect(screen.getByText('report.pdf')).toBeInTheDocument()
  })

  it('renders image icon for image attachments', () => {
    const note = makeNote({
      attachments: [
        { id: 'att-1', fileName: 'photo.png', contentType: 'image/png', fileSize: 2048, url: '/files/photo.png' },
      ],
    })
    const { container } = render(<NoteEditorView note={note} {...defaultProps} />)
    // Image icon rendered (lucide-image class)
    expect(container.querySelector('svg.lucide-image')).toBeInTheDocument()
  })

  it('calls deleteAttachment when attachment delete button clicked', async () => {
    const user = userEvent.setup()
    const note = makeNote({
      attachments: [
        { id: 'att-1', fileName: 'report.pdf', contentType: 'application/pdf', fileSize: 1024, url: '/files/report.pdf' },
      ],
    })
    render(<NoteEditorView note={note} {...defaultProps} />)

    const deleteAttBtn = screen.getByRole('button', { name: 'Delete attachment' })
    await user.click(deleteAttBtn)

    await waitFor(() => {
      expect(mockDeleteAttachment).toHaveBeenCalledWith('lesson-1', 'note-1', 'att-1')
    })
  })

  it('renders drag-drop upload zone for existing note', () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    expect(screen.getByRole('button', { name: 'lessons.notes.uploadHint' })).toBeInTheDocument()
  })

  it('shows formatted lesson date in header', () => {
    render(<NoteEditorView note={null} {...defaultProps} lessonDate="2024-01-15" />)
    // The t() mock returns 'lessons.notes.lessonDate' key
    expect(screen.getByText(/lessons\.notes\.lessonDate/)).toBeInTheDocument()
  })

  it('drag over upload zone sets dragging state (border color changes)', () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    const dropZone = screen.getByRole('button', { name: 'lessons.notes.uploadHint' })
    fireEvent.dragOver(dropZone, { preventDefault: () => {} })
    expect(dropZone.className).toContain('border-primary')
  })

  it('drag leave upload zone clears dragging state', () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    const dropZone = screen.getByRole('button', { name: 'lessons.notes.uploadHint' })
    fireEvent.dragOver(dropZone, { preventDefault: () => {} })
    fireEvent.dragLeave(dropZone)
    expect(dropZone.className).not.toContain('border-primary')
  })

  it('drop on upload zone triggers upload for valid file', async () => {
    render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    const dropZone = screen.getByRole('button', { name: 'lessons.notes.uploadHint' })
    const file = new File(['content'], 'dropped.pdf', { type: 'application/pdf' })
    fireEvent.drop(dropZone, {
      preventDefault: () => {},
      dataTransfer: { files: [file] },
    })

    await waitFor(() => {
      expect(mockAddAttachment).toHaveBeenCalledWith('lesson-1', 'note-1', file)
    })
  })

  it('file input onChange triggers upload', async () => {
    const { container } = render(<NoteEditorView note={makeNote()} {...defaultProps} />)
    const fileInput = container.querySelector('input[type="file"]')!
    const file = new File(['data'], 'upload.pdf', { type: 'application/pdf' })
    Object.defineProperty(fileInput, 'files', { value: [file], configurable: true })
    fireEvent.change(fileInput)

    await waitFor(() => {
      expect(mockAddAttachment).toHaveBeenCalledWith('lesson-1', 'note-1', file)
    })
  })

  it('does not upload files when note is null (new note)', async () => {
    render(<NoteEditorView note={null} {...defaultProps} />)
    // The attachment section and upload zone are not shown for new notes — confirmed by the test
    expect(screen.queryByRole('button', { name: 'lessons.notes.uploadHint' })).not.toBeInTheDocument()
    expect(mockAddAttachment).not.toHaveBeenCalled()
  })
})
