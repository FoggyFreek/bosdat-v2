import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import { CoursesPage } from '../CoursesPage'
import { coursesApi } from '@/features/courses/api'
import type { CourseList } from '@/features/courses/types'

vi.mock('@/features/courses/api', () => ({
  coursesApi: {
    getSummary: vi.fn(),
  },
}))

describe('CoursesPage', () => {
  const mockCourses: CourseList[] = [
    {
      id: 'course-1',
      teacherName: 'John Doe',
      courseTypeName: 'Individual',
      instrumentName: 'Piano',
      roomName: 'Room A',
      dayOfWeek: 'Monday',
      startTime: '10:00:00',
      endTime: '11:00:00',
      frequency: 'Weekly',
      weekParity: 'All',
      status: 'Active',
      enrollmentCount: 3,
    },
    {
      id: 'course-2',
      teacherName: 'Jane Smith',
      courseTypeName: 'Group',
      instrumentName: 'Guitar',
      dayOfWeek: 'Monday',
      startTime: '09:00:00',
      endTime: '10:00:00',
      frequency: 'Biweekly',
      weekParity: 'Odd',
      status: 'Paused',
      enrollmentCount: 5,
    },
    {
      id: 'course-3',
      teacherName: 'John Doe',
      courseTypeName: 'Individual',
      instrumentName: 'Drums',
      roomName: 'Room B',
      dayOfWeek: 'Wednesday',
      startTime: '14:00:00',
      endTime: '15:00:00',
      frequency: 'Weekly',
      weekParity: 'All',
      status: 'Completed',
      enrollmentCount: 1,
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(coursesApi.getSummary).mockResolvedValue(mockCourses)
  })

  describe('Rendering', () => {
    it('renders the page title and subtitle', async () => {
      render(<CoursesPage />)

      expect(screen.getByRole('heading', { name: 'courses.title' })).toBeInTheDocument()
      expect(screen.getByText('courses.subtitle')).toBeInTheDocument()
    })

    it('renders the Add Course button linking to enrollments', () => {
      render(<CoursesPage />)

      const link = screen.getByRole('link', { name: 'courses.addCourse' })
      expect(link).toHaveAttribute('href', '/enrollments/new')
    })

    it('shows loading spinner initially', () => {
      vi.mocked(coursesApi.getSummary).mockImplementation(() => new Promise(() => {}))

      render(<CoursesPage />)

      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('shows empty state when no courses exist', async () => {
      vi.mocked(coursesApi.getSummary).mockResolvedValue([])

      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.noCoursesFound')).toBeInTheDocument()
      })
    })

    it('shows error state when API fails', async () => {
      vi.mocked(coursesApi.getSummary).mockRejectedValue(new Error('API Error'))

      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.loadFailed')).toBeInTheDocument()
      })
    })
  })

  describe('Course Grouping', () => {
    it('groups courses by day of week', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Monday')).toBeInTheDocument()
        expect(screen.getByText('Wednesday')).toBeInTheDocument()
      })
    })

    it('does not render days with no courses', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Monday')).toBeInTheDocument()
      })

      expect(screen.queryByText('Tuesday')).not.toBeInTheDocument()
      expect(screen.queryByText('Thursday')).not.toBeInTheDocument()
      expect(screen.queryByText('Friday')).not.toBeInTheDocument()
    })

    it('sorts courses within a day by start time', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Guitar')).toBeInTheDocument()
      })

      // Guitar (09:00) should appear before Piano (10:00) within Monday
      const guitar = screen.getByText('Guitar')
      const piano = screen.getByText('Piano')
      expect(guitar.compareDocumentPosition(piano) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    })
  })

  describe('Course Display', () => {
    it('displays course instrument name', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Piano')).toBeInTheDocument()
        expect(screen.getByText('Guitar')).toBeInTheDocument()
        expect(screen.getByText('Drums')).toBeInTheDocument()
      })
    })

    it('displays teacher and course type', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        const individualEntries = screen.getAllByText('John Doe - Individual')
        expect(individualEntries).toHaveLength(2)
        expect(screen.getByText('Jane Smith - Group')).toBeInTheDocument()
      })
    })

    it('displays room name when available', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Room A')).toBeInTheDocument()
        expect(screen.getByText('Room B')).toBeInTheDocument()
      })
    })

    it('displays formatted time slots', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        const tenOClock = screen.getAllByText('10:00')
        expect(tenOClock.length).toBeGreaterThanOrEqual(1)
        expect(screen.getByText('11:00')).toBeInTheDocument()
        expect(screen.getByText('14:00')).toBeInTheDocument()
      })
    })

    it('displays enrollment count', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        const enrolledElements = screen.getAllByText((_content, element) => {
          return !!element?.textContent?.includes('courses.list.enrolled')
        })
        expect(enrolledElements.length).toBeGreaterThanOrEqual(2)

        // Check that the enrollment counts are displayed
        expect(enrolledElements.some(el => el.textContent?.includes('3'))).toBeTruthy()
        expect(enrolledElements.some(el => el.textContent?.includes('5'))).toBeTruthy()
      })
    })

    it('displays course status badges', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.status.active')).toBeInTheDocument()
        expect(screen.getByText('courses.status.paused')).toBeInTheDocument()
        expect(screen.getByText('courses.status.completed')).toBeInTheDocument()
      })
    })

    it('displays week parity badge for biweekly courses', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.parity.odd')).toBeInTheDocument()
      })
    })

    it('does not display week parity badge for weekly courses', async () => {
      vi.mocked(coursesApi.getSummary).mockResolvedValue([mockCourses[0]])

      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Piano')).toBeInTheDocument()
      })

      expect(screen.queryByText('courses.parity.odd')).not.toBeInTheDocument()
      expect(screen.queryByText('courses.parity.even')).not.toBeInTheDocument()
    })

    it('links each course to its detail page', async () => {
      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('Piano')).toBeInTheDocument()
      })

      const courseLinks = screen.getAllByRole('link').filter(
        link => link.getAttribute('href')?.startsWith('/courses/')
      )
      expect(courseLinks).toHaveLength(3)
      expect(courseLinks[0]).toHaveAttribute('href', '/courses/course-2')
      expect(courseLinks[1]).toHaveAttribute('href', '/courses/course-1')
    })
  })

  describe('Edge Cases', () => {
    it('handles null API response gracefully', async () => {
      vi.mocked(coursesApi.getSummary).mockResolvedValue(null as unknown as CourseList[])

      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.noCoursesFound')).toBeInTheDocument()
      })
    })

    it('handles non-array API response gracefully', async () => {
      vi.mocked(coursesApi.getSummary).mockResolvedValue({} as unknown as CourseList[])

      render(<CoursesPage />)

      await waitFor(() => {
        expect(screen.getByText('courses.noCoursesFound')).toBeInTheDocument()
      })
    })
  })
})
