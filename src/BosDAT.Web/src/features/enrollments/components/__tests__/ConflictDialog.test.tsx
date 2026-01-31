import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { ConflictDialog } from '../ConflictDialog'
import type { ConflictingCourse } from '../../types'

describe('ConflictDialog', () => {
  const mockConflicts: ConflictingCourse[] = [
    {
      courseId: '1',
      courseName: 'Piano Basics',
      dayOfWeek: 'Monday',
      timeSlot: '10:00 - 11:30',
      frequency: 'Weekly',
      weekParity: undefined,
    },
    {
      courseId: '2',
      courseName: 'Guitar Advanced',
      dayOfWeek: 'Monday',
      timeSlot: '10:30 - 12:00',
      frequency: 'Biweekly',
      weekParity: 'Odd',
    },
  ]

  it('renders when open', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />
    )

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Schedule Conflict Detected')).toBeInTheDocument()
  })

  it('does not render when closed', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={false} conflicts={mockConflicts} onClose={onClose} />
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('displays all conflicting courses', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />
    )

    expect(screen.getByText('Piano Basics')).toBeInTheDocument()
    expect(screen.getByText('Guitar Advanced')).toBeInTheDocument()
  })

  it('displays course details for each conflict', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />
    )

    // Check for specific conflict details
    expect(screen.getByText('Monday 10:00 - 11:30')).toBeInTheDocument()
    expect(screen.getAllByText(/Weekly/i).length).toBeGreaterThan(0)
  })

  it('displays week parity when present', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />
    )

    expect(screen.getByText(/Odd Weeks/i)).toBeInTheDocument()
  })

  it('calls onClose when close button clicked', async () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />
    )

    const closeButton = screen.getByRole('button', {
      name: /choose different course/i,
    })
    closeButton.click()

    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('displays empty state when no conflicts', () => {
    const onClose = vi.fn()
    render(<ConflictDialog open={true} conflicts={[]} onClose={onClose} />)

    expect(screen.getByText('Schedule Conflict Detected')).toBeInTheDocument()
    expect(screen.queryByText('Piano Basics')).not.toBeInTheDocument()
  })
})
