import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { NoteCard } from '../NoteCard'
import type { LessonNote } from '../../types'

const makeNote = (overrides?: Partial<LessonNote>): LessonNote => ({
  id: 'note-1',
  lessonId: 'lesson-1',
  content: '',
  lessonDate: '2024-01-15',
  attachments: [],
  createdAt: '2024-01-15T09:00:00',
  updatedAt: '2024-01-15T09:00:00',
  ...overrides,
})

const lexicalContent = (text: string) =>
  JSON.stringify({
    root: {
      children: [{ type: 'paragraph', children: [{ text, type: 'text' }] }],
    },
  })

describe('NoteCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders lesson date label', () => {
    render(<NoteCard note={makeNote()} isSelected={false} onClick={vi.fn()} />)
    // t() returns the key in tests; key contains 'lessonDate'
    expect(screen.getByText(/lessons\.notes\.lessonDate/)).toBeInTheDocument()
  })

  it('renders no-content placeholder when content is empty', () => {
    render(<NoteCard note={makeNote({ content: '' })} isSelected={false} onClick={vi.fn()} />)
    expect(screen.getByText('lessons.notes.noContent')).toBeInTheDocument()
  })

  it('renders text preview extracted from Lexical JSON', () => {
    const note = makeNote({ content: lexicalContent('Hello world') })
    render(<NoteCard note={note} isSelected={false} onClick={vi.fn()} />)
    expect(screen.getByText('Hello world')).toBeInTheDocument()
  })

  it('renders no-content placeholder when content is invalid JSON', () => {
    const note = makeNote({ content: 'not-json' })
    render(<NoteCard note={note} isSelected={false} onClick={vi.fn()} />)
    expect(screen.getByText('lessons.notes.noContent')).toBeInTheDocument()
  })

  it('renders attachment count when note has attachments', () => {
    const note = makeNote({
      attachments: [
        { id: 'att-1', fileName: 'file.pdf', contentType: 'application/pdf', fileSize: 1024, url: '/files/file.pdf' },
        { id: 'att-2', fileName: 'img.png', contentType: 'image/png', fileSize: 2048, url: '/files/img.png' },
      ],
    })
    render(<NoteCard note={note} isSelected={false} onClick={vi.fn()} />)
    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('does not render attachment badge when no attachments', () => {
    render(<NoteCard note={makeNote()} isSelected={false} onClick={vi.fn()} />)
    // No number rendered
    expect(screen.queryByText('0')).not.toBeInTheDocument()
  })

  it('applies selected border class when isSelected is true', () => {
    const { container } = render(<NoteCard note={makeNote()} isSelected={true} onClick={vi.fn()} />)
    expect(container.firstChild).toHaveClass('border-amber-500')
  })

  it('does not apply selected border class when isSelected is false', () => {
    const { container } = render(<NoteCard note={makeNote()} isSelected={false} onClick={vi.fn()} />)
    expect(container.firstChild).not.toHaveClass('border-amber-500')
  })

  it('calls onClick when card is clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<NoteCard note={makeNote()} isSelected={false} onClick={onClick} />)

    await user.click(screen.getByRole('button'))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('truncates long preview text at 80 chars', () => {
    const longText = 'A'.repeat(100)
    const note = makeNote({ content: lexicalContent(longText) })
    render(<NoteCard note={note} isSelected={false} onClick={vi.fn()} />)
    const preview = screen.getByText(/A+…/)
    expect(preview.textContent?.length).toBeLessThanOrEqual(82) // 80 + ellipsis
  })
})
