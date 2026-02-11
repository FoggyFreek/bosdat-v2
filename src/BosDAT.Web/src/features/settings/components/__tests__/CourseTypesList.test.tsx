import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { CourseTypesList, type CourseTypesListProps } from '../CourseTypesList'
import type { CourseType } from '@/features/course-types/types'

const mockCourseTypes: CourseType[] = [
  {
    id: '1',
    name: 'Piano 30min',
    instrumentId: 1,
    instrumentName: 'Piano',
    durationMinutes: 30,
    type: 'Individual',
    maxStudents: 1,
    isActive: true,
    activeCourseCount: 0,
    hasTeachersForCourseType: true,
    currentPricing: {
      id: 'p1',
      courseTypeId: '1',
      priceAdult: 45.0,
      priceChild: 40.5,
      validFrom: '2024-01-01',
      validUntil: null,
      isCurrent: true,
      createdAt: '2024-01-01',
    },
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
  {
    id: '2',
    name: 'Guitar 45min',
    instrumentId: 2,
    instrumentName: 'Guitar',
    durationMinutes: 45,
    type: 'Individual',
    maxStudents: 1,
    isActive: true,
    activeCourseCount: 0,
    hasTeachersForCourseType: true,
    currentPricing: {
      id: 'p2',
      courseTypeId: '2',
      priceAdult: 50.0,
      priceChild: 45.0,
      validFrom: '2024-01-01',
      validUntil: null,
      isCurrent: true,
      createdAt: '2024-01-01',
    },
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
]

const defaultProps: CourseTypesListProps = {
  courseTypes: mockCourseTypes,
  isLoading: false,
  onEdit: vi.fn(),
  onArchive: vi.fn(),
  onReactivate: vi.fn(),
  isArchiving: false,
  isReactivating: false,
}

describe('CourseTypesList', () => {
  describe('loading state', () => {
    it('renders loading spinner when isLoading is true', () => {
      render(<CourseTypesList {...defaultProps} isLoading={true} />)

      // Check for the spinner (animated element)
      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('does not render course types when loading', () => {
      render(<CourseTypesList {...defaultProps} isLoading={true} />)

      expect(screen.queryByText('Piano 30min')).not.toBeInTheDocument()
      expect(screen.queryByText('Guitar 45min')).not.toBeInTheDocument()
    })
  })

  describe('empty state', () => {
    it('renders empty message when courseTypes is empty', () => {
      render(<CourseTypesList {...defaultProps} courseTypes={[]} />)

      expect(screen.getByText('No course types configured')).toBeInTheDocument()
    })
  })

  describe('list rendering', () => {
    it('renders all course types', () => {
      render(<CourseTypesList {...defaultProps} />)

      expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      expect(screen.getByText('Guitar 45min')).toBeInTheDocument()
    })

    it('renders each course type with proper structure', () => {
      render(<CourseTypesList {...defaultProps} />)

      expect(screen.getByText(/Piano - 30 min - settings\.courseTypes\.types\.Individual/)).toBeInTheDocument()
      expect(screen.getByText(/Guitar - 45 min - settings\.courseTypes\.types\.Individual/)).toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('calls onEdit with correct course type when edit is clicked', () => {
      const onEdit = vi.fn()
      render(<CourseTypesList {...defaultProps} onEdit={onEdit} />)

      const editButtons = screen.getAllByTitle('Edit')
      fireEvent.click(editButtons[0])

      expect(onEdit).toHaveBeenCalledWith(mockCourseTypes[0])
    })

    it('calls onArchive with correct id when archive is clicked', () => {
      const onArchive = vi.fn()
      render(<CourseTypesList {...defaultProps} onArchive={onArchive} />)

      const archiveButtons = screen.getAllByTitle('Archive')
      fireEvent.click(archiveButtons[0])

      expect(onArchive).toHaveBeenCalledWith('1')
    })

    it('calls onReactivate with correct id when reactivate is clicked', () => {
      const onReactivate = vi.fn()
      const inactiveCourseTypes: CourseType[] = [
        { ...mockCourseTypes[0], isActive: false },
      ]
      render(
        <CourseTypesList
          {...defaultProps}
          courseTypes={inactiveCourseTypes}
          onReactivate={onReactivate}
        />
      )

      fireEvent.click(screen.getByTitle('Reactivate'))
      expect(onReactivate).toHaveBeenCalledWith('1')
    })
  })

  describe('button states', () => {
    it('passes isArchiving to all list items', () => {
      render(<CourseTypesList {...defaultProps} isArchiving={true} />)

      const archiveButtons = screen.getAllByTitle('Archive')
      archiveButtons.forEach((button) => {
        expect(button).toBeDisabled()
      })
    })

    it('passes isReactivating to all list items', () => {
      const inactiveCourseTypes: CourseType[] = [
        { ...mockCourseTypes[0], isActive: false },
        { ...mockCourseTypes[1], isActive: false },
      ]
      render(
        <CourseTypesList
          {...defaultProps}
          courseTypes={inactiveCourseTypes}
          isReactivating={true}
        />
      )

      const reactivateButtons = screen.getAllByTitle('Reactivate')
      reactivateButtons.forEach((button) => {
        expect(button).toBeDisabled()
      })
    })
  })
})
