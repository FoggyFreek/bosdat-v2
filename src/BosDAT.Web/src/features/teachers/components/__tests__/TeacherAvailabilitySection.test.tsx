import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { TeacherAvailabilitySection } from '../TeacherAvailabilitySection'
import type { TeacherAvailability } from '@/features/teachers/types'
import type { User } from '@/features/auth/types'

import { teachersApi } from '@/services/api'

vi.mock('@/services/api', () => ({
  teachersApi: {
    getAvailability: vi.fn(),
    updateAvailability: vi.fn(),
  },
}))

const mockUserAdmin: User = {
  id: 'user-1',
  email: 'admin@example.com',
  firstName: 'Admin',
  lastName: 'User',
  roles: ['Admin'],
}

const mockUserTeacher: User = {
  id: 'user-2',
  email: 'teacher@example.com',
  firstName: 'Teacher',
  lastName: 'User',
  roles: ['Teacher'],
}

const mockUserStudent: User = {
  id: 'user-3',
  email: 'student@example.com',
  firstName: 'Student',
  lastName: 'User',
  roles: ['Student'],
}

let currentMockUser: User | null = mockUserAdmin

vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: currentMockUser,
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    logout: vi.fn(),
  }),
}))

const mockAvailability: TeacherAvailability[] = [
  { id: '1', dayOfWeek: 1, fromTime: '09:00:00', untilTime: '17:00:00' }, // Monday
  { id: '2', dayOfWeek: 2, fromTime: '10:00:00', untilTime: '18:00:00' }, // Tuesday
  { id: '3', dayOfWeek: 3, fromTime: '00:00:00', untilTime: '00:00:00' }, // Wednesday - unavailable
]

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  )
}

describe('TeacherAvailabilitySection', () => {
  const teacherId = 'teacher-123'

  beforeEach(() => {
    vi.clearAllMocks()
    currentMockUser = mockUserAdmin
    vi.mocked(teachersApi.getAvailability).mockResolvedValue(mockAvailability)
    vi.mocked(teachersApi.updateAvailability).mockResolvedValue(mockAvailability)
  })

  describe('Display', () => {
    it('renders loading state initially', async () => {
      vi.mocked(teachersApi.getAvailability).mockImplementation(
        () => new Promise(() => {}) // Never resolves
      )

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      expect(screen.getByRole('heading', { name: /availability/i })).toBeInTheDocument()
    })

    it('displays all 7 days of the week', async () => {
      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText('Monday')).toBeInTheDocument()
      })

      expect(screen.getByText('Monday')).toBeInTheDocument()
      expect(screen.getByText('Tuesday')).toBeInTheDocument()
      expect(screen.getByText('Wednesday')).toBeInTheDocument()
      expect(screen.getByText('Thursday')).toBeInTheDocument()
      expect(screen.getByText('Friday')).toBeInTheDocument()
      expect(screen.getByText('Saturday')).toBeInTheDocument()
      expect(screen.getByText('Sunday')).toBeInTheDocument()
    })

    it('displays availability times correctly', async () => {
      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText('09:00 - 17:00')).toBeInTheDocument()
      })

      expect(screen.getByText('10:00 - 18:00')).toBeInTheDocument()
    })

    it('displays unavailable days correctly', async () => {
      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText('Unavailable')).toBeInTheDocument()
      })
    })

    it('shows "Not set" for days without availability', async () => {
      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        const notSetElements = screen.getAllByText('Not set')
        expect(notSetElements.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Role-based Access', () => {
    it('shows edit button for Admin role', async () => {
      currentMockUser = mockUserAdmin

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })
    })

    it('shows edit button for Teacher role', async () => {
      currentMockUser = mockUserTeacher

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })
    })

    it('hides edit button for other roles', async () => {
      currentMockUser = mockUserStudent

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText('Monday')).toBeInTheDocument()
      })

      expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument()
    })
  })

  describe('Edit Mode', () => {
    it('enters edit mode when edit button is clicked', async () => {
      const user = userEvent.setup()

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument()
    })

    it('exits edit mode when cancel button is clicked', async () => {
      const user = userEvent.setup()

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))
      await user.click(screen.getByRole('button', { name: /cancel/i }))

      expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /cancel/i })).not.toBeInTheDocument()
    })

    it('shows time inputs in edit mode', async () => {
      const user = userEvent.setup()

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      // Time inputs render as input[type="time"]
      const timeInputs = document.querySelectorAll('input[type="time"]')
      expect(timeInputs.length).toBeGreaterThan(0)
    })

    it('shows "Set Unavailable" buttons in edit mode', async () => {
      const user = userEvent.setup()

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      const unavailableButtons = screen.getAllByRole('button', { name: /set unavailable/i })
      expect(unavailableButtons.length).toBeGreaterThan(0)
    })

    it('shows "Set Available" button for unavailable days', async () => {
      const user = userEvent.setup()

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      expect(screen.getByRole('button', { name: /set available/i })).toBeInTheDocument()
    })
  })

  describe('Validation', () => {
    it('shows error message for invalid time range', async () => {
      const user = userEvent.setup()
      vi.mocked(teachersApi.getAvailability).mockResolvedValue([
        { id: '1', dayOfWeek: 1, fromTime: '09:00:00', untilTime: '17:00:00' },
      ])

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      // Find the Monday row and change end time to be less than 1 hour after start
      const timeInputs = document.querySelectorAll('input[type="time"]')
      // Clear and type invalid end time (less than 1 hour after start)
      await user.clear(timeInputs[1]) // End time for Monday
      await user.type(timeInputs[1], '09:30')

      await waitFor(() => {
        expect(screen.getByText(/end time must be at least 1 hour/i)).toBeInTheDocument()
      })
    })

    it('disables save button when validation errors exist', async () => {
      const user = userEvent.setup()
      vi.mocked(teachersApi.getAvailability).mockResolvedValue([
        { id: '1', dayOfWeek: 1, fromTime: '09:00:00', untilTime: '17:00:00' },
      ])

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))

      // Create invalid time range
      const timeInputs = document.querySelectorAll('input[type="time"]')
      await user.clear(timeInputs[1])
      await user.type(timeInputs[1], '09:30')

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /save/i })).toBeDisabled()
      })
    })
  })

  describe('Save Functionality', () => {
    it('calls updateAvailability on save', async () => {
      const user = userEvent.setup()
      vi.mocked(teachersApi.getAvailability).mockResolvedValue([])
      vi.mocked(teachersApi.updateAvailability).mockResolvedValue([])

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))
      await user.click(screen.getByRole('button', { name: /save/i }))

      await waitFor(() => {
        expect(teachersApi.updateAvailability).toHaveBeenCalledWith(teacherId, expect.any(Array))
      })
    })

    it('exits edit mode after successful save', async () => {
      const user = userEvent.setup()
      vi.mocked(teachersApi.getAvailability).mockResolvedValue([])
      vi.mocked(teachersApi.updateAvailability).mockResolvedValue([])

      render(<TeacherAvailabilitySection teacherId={teacherId} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /edit/i }))
      await user.click(screen.getByRole('button', { name: /save/i }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
      })
    })
  })
})
