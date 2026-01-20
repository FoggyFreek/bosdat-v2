import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { StudentForm } from '../StudentForm'
import type { Student } from '@/types'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

describe('StudentForm', () => {
  const mockOnSubmit = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders empty form in create mode', () => {
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    expect(screen.getByLabelText(/first name/i)).toHaveValue('')
    expect(screen.getByLabelText(/last name/i)).toHaveValue('')
    expect(screen.getByLabelText(/email/i)).toHaveValue('')
    expect(screen.getByLabelText(/phone/i)).toHaveValue('')
    expect(screen.getByRole('button', { name: /create student/i })).toBeInTheDocument()
  })

  it('renders form with student data in edit mode', () => {
    const student: Student = {
      id: '1',
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

    render(
      <StudentForm student={student} onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    expect(screen.getByLabelText(/first name/i)).toHaveValue('John')
    expect(screen.getByLabelText(/last name/i)).toHaveValue('Doe')
    expect(screen.getByLabelText(/email/i)).toHaveValue('john@example.com')
    expect(screen.getByLabelText(/phone/i)).toHaveValue('123-456-7890')
    expect(screen.getByRole('button', { name: /save changes/i })).toBeInTheDocument()
  })

  it('shows validation errors for empty required fields', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: /create student/i }))

    await waitFor(() => {
      expect(screen.getByText(/first name is required/i)).toBeInTheDocument()
      expect(screen.getByText(/last name is required/i)).toBeInTheDocument()
      expect(screen.getByText(/email is required/i)).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('shows validation error for invalid email format', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(screen.getByLabelText(/email/i), 'invalid-email')
    await user.click(screen.getByRole('button', { name: /create student/i }))

    await waitFor(() => {
      expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('clears validation error when field is corrected', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: /create student/i }))

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

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(screen.getByLabelText(/email/i), 'john@example.com')
    await user.type(screen.getByLabelText(/phone/i), '123-456-7890')
    await user.click(screen.getByRole('button', { name: /create student/i }))

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        phone: '123-456-7890',
        status: 'Active',
      })
    })
  })

  it('submits form without optional phone field', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(screen.getByLabelText(/email/i), 'john@example.com')
    await user.click(screen.getByRole('button', { name: /create student/i }))

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        phone: undefined,
        status: 'Active',
      })
    })
  })

  it('navigates to student detail page after successful submission', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(screen.getByLabelText(/first name/i), 'John')
    await user.type(screen.getByLabelText(/last name/i), 'Doe')
    await user.type(screen.getByLabelText(/email/i), 'john@example.com')
    await user.click(screen.getByRole('button', { name: /create student/i }))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/students/123')
    })
  })

  it('displays error message when provided', () => {
    render(
      <StudentForm
        onSubmit={mockOnSubmit}
        isSubmitting={false}
        error="Email already exists"
      />
    )

    expect(screen.getByText(/email already exists/i)).toBeInTheDocument()
  })

  it('disables submit button when submitting', () => {
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={true} />
    )

    expect(screen.getByRole('button', { name: /creating/i })).toBeDisabled()
  })

  it('shows saving text in edit mode when submitting', () => {
    const student: Student = {
      id: '1',
      firstName: 'John',
      lastName: 'Doe',
      fullName: 'John Doe',
      email: 'john@example.com',
      status: 'Active',
      autoDebit: false,
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
    }

    render(
      <StudentForm student={student} onSubmit={mockOnSubmit} isSubmitting={true} />
    )

    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled()
  })

  it('navigates back when cancel button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(mockNavigate).toHaveBeenCalledWith(-1)
  })
})
