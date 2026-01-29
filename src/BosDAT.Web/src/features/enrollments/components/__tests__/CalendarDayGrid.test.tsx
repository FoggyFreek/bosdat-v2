import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import { CalendarDayGrid } from '../CalendarDayGrid'
import type { CalendarGridItem } from '../../types'

describe('CalendarDayGrid', () => {
  const mockItems: CalendarGridItem[] = [
    {
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
    },
    {
      id: 'item-2',
      type: 'course',
      courseType: 'Group',
      title: 'Guitar Group',
      startTime: '16:00',
      endTime: '17:00',
      teacherName: 'Bob Jones',
      studentNames: ['Alice', 'Bob'],
      frequency: 'Weekly',
      isFuture: false,
    },
  ]

  const defaultProps = {
    items: [],
    selectedTime: null,
    isHoliday: false,
    durationMinutes: 60,
    onTimeSelect: vi.fn(),
  }

  describe('Time slot generation', () => {
    it('should render time slots from 08:00 to 23:00', () => {
      render(<CalendarDayGrid {...defaultProps} />)

      // Check for first and last slots
      expect(screen.getByText('08:00')).toBeInTheDocument()
      expect(screen.getByText('22:50')).toBeInTheDocument()
    })

    it('should generate slots in 10-minute intervals', () => {
      render(<CalendarDayGrid {...defaultProps} />)

      expect(screen.getByText('08:00')).toBeInTheDocument()
      expect(screen.getByText('08:10')).toBeInTheDocument()
      expect(screen.getByText('08:20')).toBeInTheDocument()
      expect(screen.getByText('08:30')).toBeInTheDocument()
    })

    it('should render 90 time slots total', () => {
      const { container } = render(<CalendarDayGrid {...defaultProps} />)

      // Count all buttons (time slots)
      const slots = container.querySelectorAll('button')
      expect(slots.length).toBe(90)
    })
  })

  describe('Occupied slot mapping', () => {
    it('should render occupied slots for items', () => {
      render(<CalendarDayGrid {...defaultProps} items={mockItems} />)

      expect(screen.getByText('Piano Lesson')).toBeInTheDocument()
      expect(screen.getByText('Guitar Group')).toBeInTheDocument()
    })

    it('should map items to their start time slots', () => {
      render(<CalendarDayGrid {...defaultProps} items={mockItems} />)

      // Item at 14:00 should be visible
      const slots = screen.getAllByRole('button')
      const slot14 = slots.find(s => s.textContent?.includes('Piano Lesson'))
      expect(slot14).toBeInTheDocument()
    })

    it('should handle items spanning multiple slots', () => {
      const longItem: CalendarGridItem = {
        id: 'item-long',
        type: 'lesson',
        courseType: 'Individual',
        title: 'Long Lesson',
        startTime: '10:00',
        endTime: '11:30', // 90 minutes
        teacherName: 'Teacher',
        studentNames: ['Student'],
        isFuture: false,
      }

      render(<CalendarDayGrid {...defaultProps} items={[longItem]} />)

      // Item should only appear at start time
      expect(screen.getByText('Long Lesson')).toBeInTheDocument()
    })
  })

  describe('Availability detection', () => {
    it('should mark slots as available when no conflicts', () => {
      render(<CalendarDayGrid {...defaultProps} items={mockItems} />)

      // Slot at 09:00 should be available (no conflicts)
      const slots = screen.getAllByRole('button')
      const slot09 = slots.find(s => s.textContent?.includes('09:00'))
      expect(slot09).not.toHaveClass('cursor-not-allowed')
    })

    it('should mark slots as unavailable during occupied time', () => {
      render(<CalendarDayGrid {...defaultProps} items={mockItems} durationMinutes={60} />)

      // Slot at 14:30 should be unavailable (within 14:00-15:00 lesson)
      const slots = screen.getAllByRole('button')
      const slot1430 = slots.find(s => s.getAttribute('aria-label')?.includes('14:30'))

      // It should either be occupied or unavailable
      const isOccupiedOrUnavailable =
        slot1430?.classList.contains('cursor-not-allowed') ||
        slot1430?.textContent?.includes('Piano Lesson')

      expect(isOccupiedOrUnavailable).toBe(true)
    })

    it('should detect conflicts with existing items', () => {
      render(<CalendarDayGrid {...defaultProps} items={mockItems} durationMinutes={120} />)

      // With 120 minute duration, 14:00 slot should conflict with 14:00-15:00 lesson
      // So it should not be selectable
      const slots = screen.getAllByRole('button')
      const slot14 = slots.find(s => s.textContent?.includes('Piano Lesson'))
      expect(slot14).not.toHaveClass('cursor-pointer')
    })
  })

  describe('Holiday handling', () => {
    it('should mark all slots as unavailable on holidays', () => {
      render(<CalendarDayGrid {...defaultProps} isHoliday />)

      const slots = screen.getAllByRole('button')

      // All empty slots should have holiday styling
      const emptySlots = slots.filter(s => !s.textContent?.includes('Lesson'))
      emptySlots.forEach(slot => {
        expect(slot).toHaveClass('bg-amber-50')
      })
    })

    it('should not allow selection on holidays', () => {
      const onTimeSelect = vi.fn()
      render(<CalendarDayGrid {...defaultProps} isHoliday onTimeSelect={onTimeSelect} />)

      const slots = screen.getAllByRole('button')
      const firstSlot = slots[0]

      firstSlot.click()

      expect(onTimeSelect).not.toHaveBeenCalled()
    })
  })

  describe('Selection handling', () => {
    it('should call onTimeSelect when available slot is clicked', () => {
      const onTimeSelect = vi.fn()
      render(<CalendarDayGrid {...defaultProps} onTimeSelect={onTimeSelect} />)

      const slots = screen.getAllByRole('button')
      const slot09 = slots.find(s => s.textContent === '09:00')

      slot09?.click()

      expect(onTimeSelect).toHaveBeenCalledWith('09:00')
    })

    it('should highlight selected time slot', () => {
      render(<CalendarDayGrid {...defaultProps} selectedTime="10:00" />)

      const slots = screen.getAllByRole('button')
      const slot10 = slots.find(s => s.textContent === '10:00')

      expect(slot10).toHaveClass('ring-2')
      expect(slot10).toHaveClass('ring-primary')
    })

    it('should not select occupied slots', () => {
      const onTimeSelect = vi.fn()
      render(<CalendarDayGrid {...defaultProps} items={mockItems} onTimeSelect={onTimeSelect} />)

      const occupiedSlot = screen.getByText('Piano Lesson').closest('button')
      occupiedSlot?.click()

      expect(onTimeSelect).not.toHaveBeenCalled()
    })
  })

  describe('Rendering optimization', () => {
    it('should render scrollable container', () => {
      const { container } = render(<CalendarDayGrid {...defaultProps} />)

      const scrollContainer = container.querySelector('[class*="overflow"]')
      expect(scrollContainer).toBeInTheDocument()
    })
  })
})
