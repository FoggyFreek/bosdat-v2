import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { CalendarTimeSlot } from '../CalendarTimeSlot'
import { userEvent } from '@testing-library/user-event'
import type { CalendarGridItem } from '../../types'

describe('CalendarTimeSlot', () => {
  const mockItem: CalendarGridItem = {
    id: 'item-1',
    type: 'lesson',
    courseType: 'Individual',
    title: 'Piano Lesson',
    startTime: '14:00',
    endTime: '15:00',
    teacherName: 'Jane Smith',
    studentNames: ['John Doe'],
    frequency: 'Weekly',
    isFuture: false,
  }

  const defaultProps = {
    time: '14:00',
    onSelect: vi.fn(),
  }

  describe('Empty slot rendering', () => {
    it('should render empty slot when no item provided', () => {
      render(<CalendarTimeSlot {...defaultProps} />)

      const slot = screen.getByRole('button')
      expect(slot).toBeInTheDocument()
      expect(slot).toHaveClass('bg-white')
    })

    it('should show time for empty slot', () => {
      render(<CalendarTimeSlot {...defaultProps} time="14:00" />)

      expect(screen.getByText('14:00')).toBeInTheDocument()
    })

    it('should be selectable when isSelectable is true', () => {
      render(<CalendarTimeSlot {...defaultProps} isSelectable />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('cursor-pointer')
      expect(slot).toHaveClass('hover:bg-slate-100')
    })

    it('should not be selectable when isSelectable is false', () => {
      render(<CalendarTimeSlot {...defaultProps} isSelectable={false} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('cursor-not-allowed')
    })

    it('should show holiday indicator when isHoliday is true', () => {
      render(<CalendarTimeSlot {...defaultProps} isHoliday />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-amber-50')
    })
  })

  describe('Occupied slot rendering', () => {
    it('should render occupied slot with item', () => {
      render(<CalendarTimeSlot {...defaultProps} item={mockItem} />)

      expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
    })

    it('should display student initials', () => {
      render(<CalendarTimeSlot {...defaultProps} item={mockItem} />)

      expect(screen.getByText('JD')).toBeInTheDocument()
    })

    it('should display multiple student initials', () => {
      const item = {
        ...mockItem,
        studentNames: ['Alice Johnson', 'Bob Smith', 'Carol White'],
      }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      expect(screen.getByText('AJ')).toBeInTheDocument()
      expect(screen.getByText('BS')).toBeInTheDocument()
      expect(screen.getByText('CW')).toBeInTheDocument()
    })

    it('should show +N badge for more than 3 students', () => {
      const item = {
        ...mockItem,
        studentNames: ['Alice Johnson', 'Bob Smith', 'Carol White', 'David Lee', 'Eve Brown'],
      }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      expect(screen.getByText('AJ')).toBeInTheDocument()
      expect(screen.getByText('BS')).toBeInTheDocument()
      expect(screen.getByText('CW')).toBeInTheDocument()
      expect(screen.getByText('+2')).toBeInTheDocument()
    })

    it('should display frequency badge', () => {
      render(<CalendarTimeSlot {...defaultProps} item={mockItem} />)

      expect(screen.getByText('Weekly')).toBeInTheDocument()
    })
  })

  describe('Color coding', () => {
    it('should use blue for Individual courses', () => {
      const item = { ...mockItem, courseType: 'Individual' as const }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-blue-100')
    })

    it('should use green for Group courses', () => {
      const item = { ...mockItem, courseType: 'Group' as const }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-green-100')
    })

    it('should use amber for Workshop courses', () => {
      const item = { ...mockItem, courseType: 'Workshop' as const }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-amber-100')
    })

    it('should use purple for Trail courses', () => {
      const item = { ...mockItem, courseType: 'Trail' as const }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-purple-100')
    })

    it('should use lighter shade for future courses', () => {
      const item = { ...mockItem, isFuture: true }
      render(<CalendarTimeSlot {...defaultProps} item={item} />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('bg-blue-50')
    })
  })

  describe('Interaction', () => {
    it('should call onSelect when empty slot is clicked', async () => {
      const user = userEvent.setup()
      const onSelect = vi.fn()
      render(<CalendarTimeSlot {...defaultProps} onSelect={onSelect} isSelectable />)

      await user.click(screen.getByRole('button'))

      expect(onSelect).toHaveBeenCalledWith('14:00')
    })

    it('should not call onSelect when occupied slot is clicked', async () => {
      const user = userEvent.setup()
      const onSelect = vi.fn()
      render(<CalendarTimeSlot {...defaultProps} item={mockItem} onSelect={onSelect} />)

      await user.click(screen.getByRole('button'))

      expect(onSelect).not.toHaveBeenCalled()
    })

    it('should not call onSelect when not selectable', async () => {
      const user = userEvent.setup()
      const onSelect = vi.fn()
      render(<CalendarTimeSlot {...defaultProps} onSelect={onSelect} isSelectable={false} />)

      await user.click(screen.getByRole('button'))

      expect(onSelect).not.toHaveBeenCalled()
    })

    it('should highlight when selected', () => {
      render(<CalendarTimeSlot {...defaultProps} isSelected />)

      const slot = screen.getByRole('button')
      expect(slot).toHaveClass('ring-2')
      expect(slot).toHaveClass('ring-primary')
    })
  })

  describe('Keyboard accessibility', () => {
    it('should handle Enter key', async () => {
      const user = userEvent.setup()
      const onSelect = vi.fn()
      render(<CalendarTimeSlot {...defaultProps} onSelect={onSelect} isSelectable />)

      const slot = screen.getByRole('button')
      slot.focus()
      await user.keyboard('{Enter}')

      expect(onSelect).toHaveBeenCalled()
    })

    it('should handle Space key', async () => {
      const user = userEvent.setup()
      const onSelect = vi.fn()
      render(<CalendarTimeSlot {...defaultProps} onSelect={onSelect} isSelectable />)

      const slot = screen.getByRole('button')
      slot.focus()
      await user.keyboard(' ')

      expect(onSelect).toHaveBeenCalled()
    })
  })
})
