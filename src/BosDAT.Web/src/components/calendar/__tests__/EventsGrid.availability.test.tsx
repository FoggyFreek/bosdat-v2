import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { EventsGrid } from '../EventsGrid'
import type { DayAvailability } from '../types'

const defaultProps = {
  hours: [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18],
  events: [],
  hourHeight: 100,
  minHour: 8,
  maxHour: 18,
  dayStartTime: 9,
  dayEndTime: 17,
  dates: [
    new Date('2024-01-15'), // Monday
    new Date('2024-01-16'), // Tuesday
    new Date('2024-01-17'), // Wednesday
    new Date('2024-01-18'), // Thursday
    new Date('2024-01-19'), // Friday
    new Date('2024-01-20'), // Saturday
    new Date('2024-01-21'), // Sunday
  ],
}

describe('EventsGrid - Availability Overlays', () => {
  describe('Without availability prop', () => {
    it('renders grid without availability overlays when prop is undefined', () => {
      render(<EventsGrid {...defaultProps} />)

      // Should render the grid
      expect(screen.getByRole('grid')).toBeInTheDocument()

      // No availability-specific overlays (only general day start/end overlays)
      const overlays = document.querySelectorAll('[aria-label^="Unavailable on"]')
      expect(overlays.length).toBe(0)
    })

    it('renders grid without availability overlays when prop is empty array', () => {
      render(<EventsGrid {...defaultProps} availability={[]} />)

      expect(screen.getByRole('grid')).toBeInTheDocument()

      const overlays = document.querySelectorAll('[aria-label^="Unavailable on"]')
      expect(overlays.length).toBe(0)
    })
  })

  describe('Full day unavailability', () => {
    it('renders full-day overlay for completely unavailable day', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 0, untilTime: 0 }, // Monday unavailable
      ]

      render(<EventsGrid {...defaultProps} availability={availability} />)

      const overlay = screen.getByLabelText('Unavailable on Monday')
      expect(overlay).toBeInTheDocument()
      // Height is calculated as (dayEndTime - dayStartTime) * hourHeight = (17 - 9) * 100 = 800px
      expect(overlay).toHaveStyle({ height: '800px' })
    })

    it('renders overlays for multiple unavailable days', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Sunday', fromTime: 0, untilTime: 0 }, // Sunday unavailable
        { dayOfWeek: 'Saturday', fromTime: 0, untilTime: 0 }, // Saturday unavailable
      ]

      render(<EventsGrid {...defaultProps} availability={availability} />)

      expect(screen.getByLabelText('Unavailable on Sunday')).toBeInTheDocument()
      expect(screen.getByLabelText('Unavailable on Saturday')).toBeInTheDocument()
    })
  })

  describe('Partial day availability', () => {
    it('renders overlays before and after teacher availability', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 10, untilTime: 16 }, // Monday 10:00-16:00
      ]

      const { container } = render(<EventsGrid {...defaultProps} availability={availability} />)

      // Should have overlays for before 10:00 and after 16:00
      // Check for overlay elements with z-2 class (availability overlays)
      const availabilityOverlays = container.querySelectorAll('.z-2')
      expect(availabilityOverlays.length).toBe(2) // before and after
    })

    it('does not render before-overlay when teacher starts at grid start', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 8, untilTime: 16 }, // Monday 8:00-16:00 (starts at minHour)
      ]

      const { container } = render(<EventsGrid {...defaultProps} availability={availability} />)

      // Should only have one overlay (after 16:00)
      const availabilityOverlays = container.querySelectorAll('.z-2')
      expect(availabilityOverlays.length).toBe(1)
    })

    it('does not render after-overlay when teacher ends at grid end', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 10, untilTime: 19 }, // Monday 10:00-19:00 (ends after maxHour)
      ]

      const { container } = render(<EventsGrid {...defaultProps} availability={availability} />)

      // Should only have one overlay (before 10:00)
      const availabilityOverlays = container.querySelectorAll('.z-2')
      expect(availabilityOverlays.length).toBe(1)
    })
  })

  describe('Column positioning', () => {
    it('positions Monday availability in correct column', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 0, untilTime: 0 }, // Monday -> column 0
      ]

      render(<EventsGrid {...defaultProps} availability={availability} />)

      const overlay = screen.getByLabelText('Unavailable on Monday')
      expect(overlay).toHaveStyle({ left: '0%' })
    })

    it('positions Sunday availability in correct column', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Sunday', fromTime: 0, untilTime: 0 }, // Sunday -> column 6
      ]

      render(<EventsGrid {...defaultProps} availability={availability} />)

      const overlay = screen.getByLabelText('Unavailable on Sunday')
      // Sunday should be in the last column (6/7 * 100 = ~85.7%)
      const style = window.getComputedStyle(overlay)
      expect(style.left).toMatch(/85/)
    })
  })

  describe('Mixed availability', () => {
    it('handles mix of unavailable and partially available days', () => {
      const availability: DayAvailability[] = [
        { dayOfWeek: 'Monday', fromTime: 9, untilTime: 17 },    // Monday normal
        { dayOfWeek: 'Tuesday', fromTime: 0, untilTime: 0 },    // Tuesday unavailable
        { dayOfWeek: 'Wednesday', fromTime: 10, untilTime: 15 }, // Wednesday shorter
      ]

      render(<EventsGrid {...defaultProps} availability={availability} />)

      // Tuesday should show full day unavailable
      expect(screen.getByLabelText('Unavailable on Tuesday')).toBeInTheDocument()

      // Grid should still be interactive
      expect(screen.getByRole('grid')).toBeInTheDocument()
    })
  })
})
