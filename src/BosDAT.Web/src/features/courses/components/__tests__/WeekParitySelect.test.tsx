import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { WeekParitySelect } from '../WeekParitySelect'

describe('WeekParitySelect', () => {
  it('renders select trigger', () => {
    const onChange = vi.fn()
    render(<WeekParitySelect value="All" onChange={onChange} disabled={false} />)

    const trigger = screen.getByRole('combobox')
    expect(trigger).toBeInTheDocument()
  })

  it('displays current value in trigger', () => {
    const onChange = vi.fn()
    render(<WeekParitySelect value="Odd" onChange={onChange} disabled={false} />)

    expect(screen.getByText('Odd Weeks Only (1, 3, 5...)')).toBeInTheDocument()
  })

  it('displays All value in trigger', () => {
    const onChange = vi.fn()
    render(<WeekParitySelect value="All" onChange={onChange} disabled={false} />)

    expect(screen.getByText('All Weeks (Every Week)')).toBeInTheDocument()
  })

  it('is disabled when disabled prop is true', () => {
    const onChange = vi.fn()
    render(<WeekParitySelect value="All" onChange={onChange} disabled={true} />)

    const trigger = screen.getByRole('combobox')
    expect(trigger).toBeDisabled()
  })

  it('shows helper text when provided', () => {
    const onChange = vi.fn()
    render(
      <WeekParitySelect
        value="All"
        onChange={onChange}
        disabled={false}
        helperText="Select parity for biweekly courses"
      />
    )

    expect(screen.getByText('Select parity for biweekly courses')).toBeInTheDocument()
  })

  it('shows warning when has53WeekYearWarning is true', () => {
    const onChange = vi.fn()
    render(
      <WeekParitySelect
        value="Odd"
        onChange={onChange}
        disabled={false}
        has53WeekYearWarning={true}
      />
    )

    expect(screen.getByText(/53 ISO weeks/i)).toBeInTheDocument()
    expect(screen.getByText(/7-day gap/i)).toBeInTheDocument()
  })

  it('does not show warning when has53WeekYearWarning is false', () => {
    const onChange = vi.fn()
    render(
      <WeekParitySelect
        value="Odd"
        onChange={onChange}
        disabled={false}
        has53WeekYearWarning={false}
      />
    )

    expect(screen.queryByText(/53 ISO weeks/i)).not.toBeInTheDocument()
  })

  it('does not show warning for All parity even with has53WeekYearWarning', () => {
    const onChange = vi.fn()
    render(
      <WeekParitySelect
        value="All"
        onChange={onChange}
        disabled={false}
        has53WeekYearWarning={true}
      />
    )

    expect(screen.queryByText(/53 ISO weeks/i)).not.toBeInTheDocument()
  })
})
