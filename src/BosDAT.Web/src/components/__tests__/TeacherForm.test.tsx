import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { TeacherForm } from '../TeacherForm'
import type { Teacher } from '@/features/teachers/types'
import type { Instrument } from '@/features/instruments/types'
import type { User } from '@/features/auth/types'

import { instrumentsApi } from '@/services/api'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

vi.mock('@/services/api', () => ({
  instrumentsApi: {
    getAll: vi.fn(),
  },
}))

const mockUser: User = {
  id: 'user-1',
  email: 'admin@example.com',
  firstName: 'Admin',
  lastName: 'User',
  roles: ['Admin'],
}

const mockUserFinancialAdmin: User = {
  id: 'user-2',
  email: 'financial@example.com',
  firstName: 'Financial',
  lastName: 'Admin',
  roles: ['FinancialAdmin'],
}

const mockUserRegular: User = {
  id: 'user-3',
  email: 'teacher@example.com',
  firstName: 'Regular',
  lastName: 'User',
  roles: ['Teacher'],
}

let currentMockUser: User | null = mockUser

vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: currentMockUser,
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    logout: vi.fn(),
  }),
}))

const mockInstruments: Instrument[] = [
  { id: 1, name: 'Piano', category: 'String', isActive: true },
  { id: 2, name: 'Guitar', category: 'String', isActive: true },
  { id: 3, name: 'Drums', category: 'Percussion', isActive: true },
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
      <BrowserRouter>{children}</BrowserRouter>
    </QueryClientProvider>
  )
}

describe('TeacherForm', () => {
  const mockOnSubmit = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    currentMockUser = mockUser
    vi.mocked(instrumentsApi.getAll).mockResolvedValue(mockInstruments)
  })

  it('renders empty form in create mode', async () => {
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    expect(screen.getByLabelText(/first name/i)).toHaveValue('')
    expect(screen.getByLabelText(/last name/i)).toHaveValue('')
    expect(document.getElementById('email')).toHaveValue('')
    expect(document.getElementById('phone')).toHaveValue('')
    expect(screen.getByRole('button', { name: /create teacher/i })).toBeInTheDocument()
  })

  it('renders form with teacher data in edit mode', async () => {
    const teacher: Teacher = {
      id: '1',
      firstName: 'John',
      lastName: 'Doe',
      prefix: 'van',
      fullName: 'John van Doe',
      email: 'john@example.com',
      phone: '123-456-7890',
      address: '123 Main St',
      postalCode: '1234 AB',
      city: 'Amsterdam',
      hourlyRate: 50,
      role: 'Teacher',
      isActive: true,
      instruments: [{ id: 1, name: 'Piano', category: 'String', isActive: true }],
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
    }

    render(
      <TeacherForm teacher={teacher} onSubmit={mockOnSubmit} isSubmitting={false} />,
      { wrapper: createWrapper() }
    )

    await waitFor(() => {
      expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
    })
    expect(screen.getByLabelText(/last name/i)).toHaveValue('Doe')
    expect(screen.getByLabelText(/prefix/i)).toHaveValue('van')
    expect(document.getElementById('email')).toHaveValue('john@example.com')
    expect(document.getElementById('phone')).toHaveValue('123-456-7890')
    expect(screen.getByRole('button', { name: /update teacher/i })).toBeInTheDocument()
  })

  it('shows validation errors for empty required fields', async () => {
    const user = userEvent.setup()
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.click(screen.getByRole('button', { name: /create teacher/i }))

    await waitFor(() => {
      expect(screen.getByText(/first name is required/i)).toBeInTheDocument()
      expect(screen.getByText(/last name is required/i)).toBeInTheDocument()
      expect(screen.getByText(/email is required/i)).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('shows validation error for invalid email format', async () => {
    const user = userEvent.setup()
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(document.getElementById('email')!, 'invalid-email')
    await user.click(screen.getByRole('button', { name: /create teacher/i }))

    await waitFor(() => {
      expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('clears validation error when field is corrected', async () => {
    const user = userEvent.setup()
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.click(screen.getByRole('button', { name: /create teacher/i }))

    await waitFor(() => {
      expect(screen.getByText(/first name is required/i)).toBeInTheDocument()
    })

    await user.type(screen.getByLabelText(/first name/i), 'John')

    await waitFor(() => {
      expect(screen.queryByText(/first name is required/i)).not.toBeInTheDocument()
    })
  })

  it('submits form with valid data', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(document.getElementById('email')!, 'john@example.com')
    await user.type(document.getElementById('phone')!, '123-456-7890')
    await user.click(screen.getByRole('button', { name: /create teacher/i }))

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          firstName: 'John',
          lastName: 'Doe',
          email: 'john@example.com',
          phone: '123-456-7890',
        })
      )
    })
  })

  it('navigates to teacher detail page after successful submission', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(document.getElementById('email')!, 'john@example.com')
    await user.click(screen.getByRole('button', { name: /create teacher/i }))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/teachers/123')
    })
  })

  it('displays error message when provided', () => {
    render(
      <TeacherForm
        onSubmit={mockOnSubmit}
        isSubmitting={false}
        error="Email already exists"
      />,
      { wrapper: createWrapper() }
    )

    expect(screen.getByText(/email already exists/i)).toBeInTheDocument()
  })

  it('disables submit button when submitting', () => {
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={true} />, {
      wrapper: createWrapper(),
    })

    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled()
  })

  it('navigates to teachers list when cancel button is clicked', async () => {
    const user = userEvent.setup()
    render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
      wrapper: createWrapper(),
    })

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(mockNavigate).toHaveBeenCalledWith('/teachers')
  })

  describe('Instrument Selection', () => {
    it('displays available instruments as checkboxes', async () => {
      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByLabelText('Piano')).toBeInTheDocument()
        expect(screen.getByLabelText('Guitar')).toBeInTheDocument()
        expect(screen.getByLabelText('Drums')).toBeInTheDocument()
      })
    })

    it('allows selecting multiple instruments', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockResolvedValue({ id: '123' })

      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByLabelText('Piano')).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/first name/i), 'John')
      await user.type(screen.getByLabelText(/last name/i), 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await user.click(screen.getByLabelText('Piano'))
      await user.click(screen.getByLabelText('Guitar'))

      await user.click(screen.getByRole('button', { name: /create teacher/i }))

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            instrumentIds: expect.arrayContaining([1, 2]),
          })
        )
      })
    })

    it('pre-selects instruments in edit mode', async () => {
      const teacher: Teacher = {
        id: '1',
        firstName: 'John',
        lastName: 'Doe',
        fullName: 'John Doe',
        email: 'john@example.com',
        hourlyRate: 50,
        role: 'Teacher',
        isActive: true,
        instruments: [{ id: 1, name: 'Piano', category: 'String', isActive: true }],
        createdAt: '2024-01-01',
        updatedAt: '2024-01-01',
      }

      render(
        <TeacherForm teacher={teacher} onSubmit={mockOnSubmit} isSubmitting={false} />,
        { wrapper: createWrapper() }
      )

      await waitFor(() => {
        expect(screen.getByLabelText('Piano')).toBeChecked()
        expect(screen.getByLabelText('Guitar')).not.toBeChecked()
      })
    })
  })

  describe('Role-Based Hourly Rate Visibility', () => {
    it('shows hourly rate field for Admin role', async () => {
      currentMockUser = mockUser // Admin role

      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText(/financial information/i)).toBeInTheDocument()
        expect(screen.getByLabelText(/hourly rate/i)).toBeInTheDocument()
      })
    })

    it('shows hourly rate field for FinancialAdmin role', async () => {
      currentMockUser = mockUserFinancialAdmin

      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByText(/financial information/i)).toBeInTheDocument()
        expect(screen.getByLabelText(/hourly rate/i)).toBeInTheDocument()
      })
    })

    it('hides hourly rate field for regular user without financial permissions', async () => {
      currentMockUser = mockUserRegular

      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.queryByText(/financial information/i)).not.toBeInTheDocument()
        expect(screen.queryByLabelText(/hourly rate/i)).not.toBeInTheDocument()
      })
    })

    it('validates hourly rate when visible', async () => {
      currentMockUser = mockUserFinancialAdmin
      const user = userEvent.setup()

      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      await waitFor(() => {
        expect(screen.getByLabelText(/hourly rate/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/first name/i), 'John')
      await user.type(screen.getByLabelText(/last name/i), 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')
      await user.type(screen.getByLabelText(/hourly rate/i), '-50')
      await user.click(screen.getByRole('button', { name: /create teacher/i }))

      await waitFor(() => {
        expect(screen.getByText(/please enter a valid hourly rate/i)).toBeInTheDocument()
      })
    })
  })

  describe('Edit Mode', () => {
    it('shows active status checkbox in edit mode', async () => {
      const teacher: Teacher = {
        id: '1',
        firstName: 'John',
        lastName: 'Doe',
        fullName: 'John Doe',
        email: 'john@example.com',
        hourlyRate: 50,
        role: 'Teacher',
        isActive: true,
        instruments: [],
        createdAt: '2024-01-01',
        updatedAt: '2024-01-01',
      }

      render(
        <TeacherForm teacher={teacher} onSubmit={mockOnSubmit} isSubmitting={false} />,
        { wrapper: createWrapper() }
      )

      await waitFor(() => {
        expect(screen.getByLabelText(/active/i)).toBeInTheDocument()
        expect(screen.getByLabelText(/active/i)).toBeChecked()
      })
    })

    it('hides active status checkbox in create mode', () => {
      render(<TeacherForm onSubmit={mockOnSubmit} isSubmitting={false} />, {
        wrapper: createWrapper(),
      })

      expect(screen.queryByLabelText(/^active$/i)).not.toBeInTheDocument()
    })
  })
})
