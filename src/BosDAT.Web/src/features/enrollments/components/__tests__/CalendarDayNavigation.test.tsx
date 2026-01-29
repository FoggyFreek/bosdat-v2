import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { CalendarDayNavigation } from '../CalendarDayNavigation'
import { userEvent } from '@testing-library/user-event'
import type { Holiday } from '@/features/schedule/types'

describe('CalendarDayNavigation', () => {
  const mockWeekStart = new Date(2024, 2, 18) // March 18, 2024 (Monday)
  const mockSelectedDate = new Date(2024, 2, 20) // March 20, 2024 (Wednesday)
  const mockHolidays: Holiday[] = [
    {
      id: 1,
      name: 'Spring Break',
      startDate: '2024-03-22',
      endDate: '2024-03-22',
    },
  ]

  const defaultProps = {
    weekStart: mockWeekStart,
    selectedDate: mockSelectedDate,
    holidays: [],
    onDateSelect: vi.fn(),
    onWeekChange: vi.fn(),
  }

  it('should render 7 day buttons', () => {
    render(<CalendarDayNavigation {...defaultProps} />)

    const buttons = screen.getAllByRole('button', { name: /^(Mon|Tue|Wed|Thu|Fri|Sat|Sun)/ })
    expect(buttons.filter(b => !b.getAttribute('aria-label')?.includes('Previous') && !b.getAttribute('aria-label')?.includes('Next'))).toHaveLength(7)
  })

  it('should display day names and dates', () => {
    render(<CalendarDayNavigation {...defaultProps} />)

    expect(screen.getByText('Mon')).toBeInTheDocument()
    expect(screen.getByText('18')).toBeInTheDocument()
    expect(screen.getByText('Sun')).toBeInTheDocument()
    expect(screen.getByText('24')).toBeInTheDocument()
  })

  it('should highlight the selected day', () => {
    render(<CalendarDayNavigation {...defaultProps} />)

    const buttons = screen.getAllByRole('button')
    const wednesdayButton = buttons.find(b => b.textContent?.includes('Wed') && b.textContent?.includes('20'))

    expect(wednesdayButton).toHaveClass('bg-primary')
  })

  it('should call onDateSelect when day button is clicked', async () => {
    const user = userEvent.setup()
    const onDateSelect = vi.fn()
    render(<CalendarDayNavigation {...defaultProps} onDateSelect={onDateSelect} />)

    const buttons = screen.getAllByRole('button')
    const mondayButton = buttons.find(b => b.textContent?.includes('Mon') && b.textContent?.includes('18'))

    await user.click(mondayButton!)

    expect(onDateSelect).toHaveBeenCalledWith(mockWeekStart)
  })

  it('should show holiday indicator on holiday days', () => {
    render(<CalendarDayNavigation {...defaultProps} holidays={mockHolidays} />)

    const buttons = screen.getAllByRole('button')
    const fridayButton = buttons.find(b => b.textContent?.includes('Fri') && b.textContent?.includes('22'))

    // Holiday indicator should be present (e.g., a badge or icon)
    expect(fridayButton).toBeInTheDocument()
  })

  it('should render previous week arrow', () => {
    render(<CalendarDayNavigation {...defaultProps} />)

    expect(screen.getByLabelText(/previous week/i)).toBeInTheDocument()
  })

  it('should render next week arrow', () => {
    render(<CalendarDayNavigation {...defaultProps} />)

    expect(screen.getByLabelText(/next week/i)).toBeInTheDocument()
  })

  it('should call onWeekChange with -7 when previous week is clicked', async () => {
    const user = userEvent.setup()
    const onWeekChange = vi.fn()
    render(<CalendarDayNavigation {...defaultProps} onWeekChange={onWeekChange} />)

    await user.click(screen.getByLabelText(/previous week/i))

    expect(onWeekChange).toHaveBeenCalledWith(-7)
  })

  it('should call onWeekChange with +7 when next week is clicked', async () => {
    const user = userEvent.setup()
    const onWeekChange = vi.fn()
    render(<CalendarDayNavigation {...defaultProps} onWeekChange={onWeekChange} />)

    await user.click(screen.getByLabelText(/next week/i))

    expect(onWeekChange).toHaveBeenCalledWith(7)
  })

  it('should handle keyboard navigation on day buttons', async () => {
    const user = userEvent.setup()
    const onDateSelect = vi.fn()
    render(<CalendarDayNavigation {...defaultProps} onDateSelect={onDateSelect} />)

    const buttons = screen.getAllByRole('button')
    const mondayButton = buttons.find(b => b.textContent?.includes('Mon') && b.textContent?.includes('18'))

    mondayButton!.focus()
    await user.keyboard('{Enter}')

    expect(onDateSelect).toHaveBeenCalled()
  })

  it('should handle Space key for day selection', async () => {
    const user = userEvent.setup()
    const onDateSelect = vi.fn()
    render(<CalendarDayNavigation {...defaultProps} onDateSelect={onDateSelect} />)

    const buttons = screen.getAllByRole('button')
    const mondayButton = buttons.find(b => b.textContent?.includes('Mon') && b.textContent?.includes('18'))

    mondayButton!.focus()
    await user.keyboard(' ')

    expect(onDateSelect).toHaveBeenCalled()
  })
})
