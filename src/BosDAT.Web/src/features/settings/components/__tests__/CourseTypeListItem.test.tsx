import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { CourseTypeListItem, type CourseTypeListItemProps } from '../CourseTypeListItem'
import type { CourseType } from '@/features/course-types/types'

const mockCourseType: CourseType = {
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
}

const defaultProps: CourseTypeListItemProps = {
  courseType: mockCourseType,
  onEdit: vi.fn(),
  onArchive: vi.fn(),
  onReactivate: vi.fn(),
  isArchiving: false,
  isReactivating: false,
}

describe('CourseTypeListItem', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders course type name', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByText('Piano 30min')).toBeInTheDocument()
    })

    it('renders course type details', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByText('Piano - 30 min - Individual')).toBeInTheDocument()
    })

    it('renders max students for non-Individual types', () => {
      const groupCourseType: CourseType = {
        ...mockCourseType,
        type: 'Group',
        maxStudents: 6,
      }
      render(<CourseTypeListItem {...defaultProps} courseType={groupCourseType} />)
      expect(screen.getByText(/max 6/)).toBeInTheDocument()
    })

    it('does not render max students for Individual type', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.queryByText(/max 1/)).not.toBeInTheDocument()
    })

    it('renders pricing information', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByText(/Adult:/)).toBeInTheDocument()
      expect(screen.getByText(/Child:/)).toBeInTheDocument()
    })

    it('renders "No pricing" when currentPricing is null', () => {
      const noPricingCourseType: CourseType = {
        ...mockCourseType,
        currentPricing: null,
      }
      render(<CourseTypeListItem {...defaultProps} courseType={noPricingCourseType} />)
      expect(screen.getByText('No pricing')).toBeInTheDocument()
    })
  })

  describe('status badges', () => {
    it('renders Active badge for active course types', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('renders Archived badge for inactive course types', () => {
      const inactiveCourseType: CourseType = {
        ...mockCourseType,
        isActive: false,
      }
      render(<CourseTypeListItem {...defaultProps} courseType={inactiveCourseType} />)
      expect(screen.getByText('Archived')).toBeInTheDocument()
    })

    it('renders "No teachers" badge when hasTeachersForCourseType is false', () => {
      const noTeachersCourseType: CourseType = {
        ...mockCourseType,
        hasTeachersForCourseType: false,
      }
      render(<CourseTypeListItem {...defaultProps} courseType={noTeachersCourseType} />)
      expect(screen.getByText('No teachers')).toBeInTheDocument()
    })

    it('does not render "No teachers" badge when hasTeachersForCourseType is true', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.queryByText('No teachers')).not.toBeInTheDocument()
    })
  })

  describe('edit button', () => {
    it('calls onEdit when edit button is clicked', () => {
      const onEdit = vi.fn()
      render(<CourseTypeListItem {...defaultProps} onEdit={onEdit} />)

      fireEvent.click(screen.getByTitle('Edit'))
      expect(onEdit).toHaveBeenCalledWith(mockCourseType)
    })
  })

  describe('archive button', () => {
    it('renders archive button for active course types', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByTitle('Archive')).toBeInTheDocument()
    })

    it('calls onArchive when archive button is clicked', () => {
      const onArchive = vi.fn()
      render(<CourseTypeListItem {...defaultProps} onArchive={onArchive} />)

      fireEvent.click(screen.getByTitle('Archive'))
      expect(onArchive).toHaveBeenCalledWith('1')
    })

    it('disables archive button when isArchiving is true', () => {
      render(<CourseTypeListItem {...defaultProps} isArchiving={true} />)
      expect(screen.getByTitle('Archive')).toBeDisabled()
    })

    it('shows warning title when course type has active courses', () => {
      const courseTypeWithActiveCourses: CourseType = {
        ...mockCourseType,
        activeCourseCount: 5,
      }
      render(
        <CourseTypeListItem {...defaultProps} courseType={courseTypeWithActiveCourses} />
      )
      expect(screen.getByTitle('Cannot archive: 5 active courses')).toBeInTheDocument()
    })

    it('shows Archive title when course type has no active courses', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.getByTitle('Archive')).toBeInTheDocument()
    })
  })

  describe('reactivate button', () => {
    it('renders reactivate button for inactive course types', () => {
      const inactiveCourseType: CourseType = {
        ...mockCourseType,
        isActive: false,
      }
      render(<CourseTypeListItem {...defaultProps} courseType={inactiveCourseType} />)
      expect(screen.getByTitle('Reactivate')).toBeInTheDocument()
    })

    it('calls onReactivate when reactivate button is clicked', () => {
      const onReactivate = vi.fn()
      const inactiveCourseType: CourseType = {
        ...mockCourseType,
        isActive: false,
      }
      render(
        <CourseTypeListItem
          {...defaultProps}
          courseType={inactiveCourseType}
          onReactivate={onReactivate}
        />
      )

      fireEvent.click(screen.getByTitle('Reactivate'))
      expect(onReactivate).toHaveBeenCalledWith('1')
    })

    it('disables reactivate button when isReactivating is true', () => {
      const inactiveCourseType: CourseType = {
        ...mockCourseType,
        isActive: false,
      }
      render(
        <CourseTypeListItem
          {...defaultProps}
          courseType={inactiveCourseType}
          isReactivating={true}
        />
      )
      expect(screen.getByTitle('Reactivate')).toBeDisabled()
    })
  })

  describe('pricing history', () => {
    it('renders PricingHistoryCollapsible when pricingHistory has more than 1 entry', () => {
      const courseTypeWithHistory: CourseType = {
        ...mockCourseType,
        pricingHistory: [
          {
            id: 'p1',
            courseTypeId: '1',
            priceAdult: 45.0,
            priceChild: 40.5,
            validFrom: '2024-01-01',
            validUntil: null,
            isCurrent: true,
            createdAt: '2024-01-01',
          },
          {
            id: 'p2',
            courseTypeId: '1',
            priceAdult: 40.0,
            priceChild: 36.0,
            validFrom: '2023-01-01',
            validUntil: '2023-12-31',
            isCurrent: false,
            createdAt: '2023-01-01',
          },
        ],
      }
      render(<CourseTypeListItem {...defaultProps} courseType={courseTypeWithHistory} />)

      // The PricingHistoryCollapsible should be rendered
      expect(screen.getByText(/historic pricing version/i)).toBeInTheDocument()
    })

    it('does not render pricing history when pricingHistory has 1 or fewer entries', () => {
      render(<CourseTypeListItem {...defaultProps} />)
      expect(screen.queryByText(/historic pricing version/i)).not.toBeInTheDocument()
    })
  })
})
