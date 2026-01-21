import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StudentFormPage } from '../StudentFormPage'
import { studentsApi } from '@/services/api'
import type { Student } from '@/types'

vi.mock('@/services/api', () => ({
  studentsApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    checkDuplicates: vi.fn(),
  },
}))

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

const mockStudent: Student = {
  id: '123',
  firstName: 'John',
  lastName: 'Doe',
  fullName: 'John Doe',
  email: 'john@example.com',
  phone: '123-456-7890',
  status: 'Active',
  autoDebit: false,
  createdAt: '2024-01-01',
  updatedAt: '2024-01-01',
}

const createQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

interface RenderOptions {
  route?: string
  path?: string
}

const renderWithProviders = ({ route = '/students/new', path = '/students/new' }: RenderOptions = {}) => {
  const queryClient = createQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <Routes>
          <Route path={path} element={<StudentFormPage />} />
          <Route path="/students/:id" element={<div>Student Detail Page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('StudentFormPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Default: no duplicates found
    vi.mocked(studentsApi.checkDuplicates).mockResolvedValue({
      hasDuplicates: false,
      duplicates: [],
    })
  })

  describe('Create Mode', () => {
    it('renders create form with correct title', async () => {
      renderWithProviders()

      expect(screen.getByRole('heading', { name: /new student/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /create student/i })).toBeInTheDocument()
    })

    it('creates student and navigates to detail page on success', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.create).mockResolvedValue({ id: '456' })

      renderWithProviders()

      await user.type(screen.getByLabelText(/first name/i), 'Jane')
      await user.type(screen.getByLabelText(/last name/i), 'Smith')
      // Use the input id directly to avoid matching billing contact email
      await user.type(document.getElementById('email')!, 'jane@example.com')
      await user.click(screen.getByRole('button', { name: /create student/i }))

      await waitFor(() => {
        expect(studentsApi.create).toHaveBeenCalledWith(expect.objectContaining({
          firstName: 'Jane',
          lastName: 'Smith',
          email: 'jane@example.com',
        }))
      })

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/students/456')
      })
    })

    it('displays API error on create failure', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.create).mockRejectedValue({
        response: {
          data: {
            message: 'Email already exists',
          },
        },
      })

      renderWithProviders()

      await user.type(screen.getByLabelText(/first name/i), 'Jane')
      await user.type(screen.getByLabelText(/last name/i), 'Smith')
      await user.type(document.getElementById('email')!, 'existing@example.com')
      await user.click(screen.getByRole('button', { name: /create student/i }))

      await waitFor(() => {
        expect(screen.getByText(/email already exists/i)).toBeInTheDocument()
      })
    })

    it('displays validation errors from API', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.create).mockRejectedValue({
        response: {
          data: {
            errors: {
              Email: ['Invalid email format', 'Email is required'],
            },
          },
        },
      })

      renderWithProviders()

      await user.type(screen.getByLabelText(/first name/i), 'Jane')
      await user.type(screen.getByLabelText(/last name/i), 'Smith')
      await user.type(document.getElementById('email')!, 'test@example.com')
      await user.click(screen.getByRole('button', { name: /create student/i }))

      await waitFor(() => {
        expect(screen.getByText(/invalid email format, email is required/i)).toBeInTheDocument()
      })
    })
  })

  describe('Edit Mode', () => {
    it('shows loading state while fetching student', async () => {
      let resolvePromise: (value: Student) => void
      vi.mocked(studentsApi.getById).mockImplementation(
        () => new Promise((resolve) => { resolvePromise = resolve })
      )

      renderWithProviders({
        route: '/students/123/edit',
        path: '/students/:id/edit',
      })

      // Check for loading spinner
      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()

      // Resolve and cleanup
      resolvePromise!(mockStudent)
    })

    it('renders edit form with student data', async () => {
      vi.mocked(studentsApi.getById).mockResolvedValue(mockStudent)

      renderWithProviders({
        route: '/students/123/edit',
        path: '/students/:id/edit',
      })

      await waitFor(() => {
        expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
      }, { timeout: 3000 })

      expect(screen.getByRole('heading', { level: 1, name: /edit student/i })).toBeInTheDocument()
      expect(screen.getByLabelText(/last name/i)).toHaveValue('Doe')
      expect(document.getElementById('email')).toHaveValue('john@example.com')
      expect(document.getElementById('phone')).toHaveValue('123-456-7890')
      expect(screen.getByRole('button', { name: /save changes/i })).toBeInTheDocument()
    })

    it('updates student and navigates to detail page on success', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.getById).mockResolvedValue(mockStudent)
      vi.mocked(studentsApi.update).mockResolvedValue({ ...mockStudent, firstName: 'Johnny' })

      renderWithProviders({
        route: '/students/123/edit',
        path: '/students/:id/edit',
      })

      await waitFor(() => {
        expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
      })

      const firstNameInput = screen.getByLabelText(/first name/i)
      await user.clear(firstNameInput)
      await user.type(firstNameInput, 'Johnny')
      await user.click(screen.getByRole('button', { name: /save changes/i }))

      await waitFor(() => {
        expect(studentsApi.update).toHaveBeenCalledWith('123', expect.objectContaining({
          firstName: 'Johnny',
          lastName: 'Doe',
          email: 'john@example.com',
        }))
      })

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/students/123')
      })
    })

    it('shows not found message when student does not exist', async () => {
      vi.mocked(studentsApi.getById).mockRejectedValue(new Error('Not found'))

      renderWithProviders({
        route: '/students/999/edit',
        path: '/students/:id/edit',
      })

      await waitFor(() => {
        expect(screen.getByText(/student not found/i)).toBeInTheDocument()
      })

      expect(screen.getByRole('link', { name: /back to students/i })).toBeInTheDocument()
    })

    it('displays API error on update failure', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.getById).mockResolvedValue(mockStudent)
      vi.mocked(studentsApi.update).mockRejectedValue({
        response: {
          data: {
            message: 'Email already in use',
          },
        },
      })

      renderWithProviders({
        route: '/students/123/edit',
        path: '/students/:id/edit',
      })

      await waitFor(() => {
        expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
      })

      const emailInput = document.getElementById('email')!
      await user.clear(emailInput)
      await user.type(emailInput, 'taken@example.com')
      await user.click(screen.getByRole('button', { name: /save changes/i }))

      await waitFor(() => {
        expect(screen.getByText(/email already in use/i)).toBeInTheDocument()
      })
    })
  })

  describe('Navigation', () => {
    it('has back link to students list in create mode', () => {
      renderWithProviders()

      const backLink = screen.getByRole('link', { name: '' })
      expect(backLink).toHaveAttribute('href', '/students')
    })

    it('has back link to student detail in edit mode', async () => {
      vi.mocked(studentsApi.getById).mockResolvedValue(mockStudent)

      renderWithProviders({
        route: '/students/123/edit',
        path: '/students/:id/edit',
      })

      await waitFor(() => {
        expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
      })

      const backLink = screen.getByRole('link', { name: '' })
      expect(backLink).toHaveAttribute('href', '/students/123')
    })
  })
})
