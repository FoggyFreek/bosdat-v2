import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { ViewSelector } from '../ViewSelector'
import type { CalendarView } from '../types'

describe('ViewSelector', () => {
  const mockOnViewChange = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders all three view options', () => {
    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    expect(screen.getByRole('tab', { name: /calendar.views.week/i })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /calendar.views.day/i })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /calendar.views.list/i })).toBeInTheDocument()
  })

  it('renders the tablist role with correct aria-label', () => {
    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    expect(screen.getByRole('tablist', { name: /calendar.views.label/i })).toBeInTheDocument()
  })

  it('marks the active view as selected', () => {
    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    const weekTab = screen.getByRole('tab', { name: /calendar.views.week/i })
    expect(weekTab).toHaveAttribute('aria-selected', 'true')

    const dayTab = screen.getByRole('tab', { name: /calendar.views.day/i })
    expect(dayTab).toHaveAttribute('aria-selected', 'false')

    const listTab = screen.getByRole('tab', { name: /calendar.views.list/i })
    expect(listTab).toHaveAttribute('aria-selected', 'false')
  })

  it('marks day view as selected when view is day', () => {
    render(<ViewSelector view="day" onViewChange={mockOnViewChange} />)

    expect(screen.getByRole('tab', { name: /calendar.views.day/i })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: /calendar.views.week/i })).toHaveAttribute('aria-selected', 'false')
  })

  it('calls onViewChange when a different view is clicked', async () => {
    const user = userEvent.setup()

    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    const dayTab = screen.getByRole('tab', { name: /calendar.views.day/i })
    await user.click(dayTab)

    expect(mockOnViewChange).toHaveBeenCalledWith('day')
  })

  it('calls onViewChange with list when list tab is clicked', async () => {
    const user = userEvent.setup()

    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    const listTab = screen.getByRole('tab', { name: /calendar.views.list/i })
    await user.click(listTab)

    expect(mockOnViewChange).toHaveBeenCalledWith('list')
  })

  it('calls onViewChange when clicking the already selected view', async () => {
    const user = userEvent.setup()

    render(<ViewSelector view="week" onViewChange={mockOnViewChange} />)

    const weekTab = screen.getByRole('tab', { name: /calendar.views.week/i })
    await user.click(weekTab)

    expect(mockOnViewChange).toHaveBeenCalledWith('week')
  })

  it.each<CalendarView>(['week', 'day', 'list'])(
    'applies active styling to %s view when selected',
    (viewValue) => {
      render(<ViewSelector view={viewValue} onViewChange={mockOnViewChange} />)

      const tabs = screen.getAllByRole('tab')
      tabs.forEach((tab) => {
        if (tab.getAttribute('aria-selected') === 'true') {
          expect(tab.className).toContain('bg-white')
        } else {
          expect(tab.className).not.toContain('bg-white')
        }
      })
    }
  )
})
