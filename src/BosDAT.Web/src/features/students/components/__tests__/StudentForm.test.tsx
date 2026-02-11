import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { StudentForm } from '../StudentForm'
import type { Student, DuplicateCheckResult } from '@/features/students/types'
import { studentsApi } from '@/features/students/api'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

vi.mock('@/features/students/api', () => ({
  studentsApi: {
    checkDuplicates: vi.fn(),
  },
}))

describe('StudentForm', () => {
  const mockOnSubmit = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    // Default: no duplicates found
    vi.mocked(studentsApi.checkDuplicates).mockResolvedValue({
      hasDuplicates: false,
      duplicates: [],
    })
  })

  it('renders empty form in create mode', () => {
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    expect(document.getElementById('firstName')).toHaveValue('')
    expect(document.getElementById('lastName')).toHaveValue('')
    expect(document.getElementById('email')).toHaveValue('')
    expect(document.getElementById('phone')).toHaveValue('')
    expect(screen.getByRole('button', { name: 'students.actions.createStudent' })).toBeInTheDocument()
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

    expect(document.getElementById('firstName')).toHaveValue('John')
    expect(document.getElementById('lastName')).toHaveValue('Doe')
    // Use input ID to avoid matching contact email
    expect(document.getElementById('email')).toHaveValue('john@example.com')
    expect(document.getElementById('phone')).toHaveValue('123-456-7890')
    expect(screen.getByRole('button', { name: 'students.actions.saveChanges' })).toBeInTheDocument()
  })

  it('shows validation errors for empty required fields', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

    await waitFor(() => {
      expect(screen.getByText('students.validation.firstNameRequired')).toBeInTheDocument()
      expect(screen.getByText('students.validation.lastNameRequired')).toBeInTheDocument()
      expect(screen.getByText('students.validation.emailRequired')).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('shows validation error for invalid email format', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(document.getElementById('firstName')!, 'John')
    await user.type(document.getElementById('lastName')!, 'Doe')
    await user.type(document.getElementById('email')!, 'invalid-email')
    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

    await waitFor(() => {
      expect(screen.getByText('students.validation.emailInvalid')).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('clears validation error when field is corrected', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

    await waitFor(() => {
      expect(screen.getByText('students.validation.firstNameRequired')).toBeInTheDocument()
    })

    await user.type(document.getElementById('firstName')!, 'John')

    await waitFor(() => {
      expect(screen.queryByText('students.validation.firstNameRequired')).not.toBeInTheDocument()
    })
  })

  it('submits form with valid data', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(document.getElementById('firstName')!, 'John')
    await user.type(document.getElementById('lastName')!, 'Doe')
    await user.type(document.getElementById('email')!, 'john@example.com')
    await user.type(document.getElementById('phone')!, '123-456-7890')
    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(expect.objectContaining({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        phone: '123-456-7890',
      }))
    })
  })

  it('submits form without optional phone field', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(document.getElementById('firstName')!, 'John')
    await user.type(document.getElementById('lastName')!, 'Doe')
    await user.type(document.getElementById('email')!, 'john@example.com')
    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(expect.objectContaining({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
      }))
    })
  })

  it('navigates to student detail page after successful submission', async () => {
    const user = userEvent.setup()
    mockOnSubmit.mockResolvedValue({ id: '123' })

    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.type(document.getElementById('firstName')!, 'John')
    await user.type(document.getElementById('lastName')!, 'Doe')
    await user.type(document.getElementById('email')!, 'john@example.com')
    await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

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

    expect(screen.getByRole('button', { name: 'students.actions.creating' })).toBeDisabled()
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

    expect(screen.getByRole('button', { name: 'students.actions.saving' })).toBeDisabled()
  })

  it('navigates back when cancel button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />
    )

    await user.click(screen.getByRole('button', { name: 'common.actions.cancel' }))

    expect(mockNavigate).toHaveBeenCalledWith(-1)
  })

  describe('Duplicate Detection', () => {
    const duplicateResult: DuplicateCheckResult = {
      hasDuplicates: true,
      duplicates: [
        {
          id: 'dup-1',
          fullName: 'John Doe',
          email: 'john@example.com',
          phone: '123-456-7890',
          status: 'Active',
          confidenceScore: 80,
          matchReason: 'Exact email match',
        },
      ],
    }

    it('shows duplicate warning when potential duplicates are found', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.checkDuplicates).mockResolvedValue(duplicateResult)

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await waitFor(() => {
        expect(screen.getByText('students.duplicate.title')).toBeInTheDocument()
        expect(screen.getByText('John Doe')).toBeInTheDocument()
        expect(screen.getByText('john@example.com')).toBeInTheDocument()
        expect(screen.getByText('students.duplicate.match')).toBeInTheDocument()
      })
    })

    it('disables submit button when duplicates exist and not acknowledged', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.checkDuplicates).mockResolvedValue(duplicateResult)

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await waitFor(() => {
        expect(screen.getByText('students.duplicate.title')).toBeInTheDocument()
      })

      expect(screen.getByRole('button', { name: 'students.actions.createStudent' })).toBeDisabled()
    })

    it('enables submit button after acknowledging duplicates', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.checkDuplicates).mockResolvedValue(duplicateResult)
      mockOnSubmit.mockResolvedValue({ id: '123' })

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await waitFor(() => {
        expect(screen.getByText('students.duplicate.title')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'students.duplicate.acknowledge' }))

      await waitFor(() => {
        expect(screen.getByText('students.duplicate.acknowledged')).toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'students.actions.createStudent' })).not.toBeDisabled()
      })
    })

    it('allows form submission after acknowledging duplicates', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.checkDuplicates).mockResolvedValue(duplicateResult)
      mockOnSubmit.mockResolvedValue({ id: '123' })

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await waitFor(() => {
        expect(screen.getByText('students.duplicate.title')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'students.duplicate.acknowledge' }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'students.actions.createStudent' })).not.toBeDisabled()
      })

      await user.click(screen.getByRole('button', { name: 'students.actions.createStudent' }))

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalled()
      })
    })

    it('shows link to view potential duplicate student', async () => {
      const user = userEvent.setup()
      vi.mocked(studentsApi.checkDuplicates).mockResolvedValue(duplicateResult)

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      await waitFor(() => {
        const link = screen.getByRole('link', { name: 'John Doe' })
        expect(link).toHaveAttribute('href', '/students/dup-1')
      })
    })

    it('shows checking indicator while checking for duplicates', async () => {
      const user = userEvent.setup()
      // Create a promise we can control
      let resolveCheck: (value: DuplicateCheckResult) => void
      const checkPromise = new Promise<DuplicateCheckResult>((resolve) => {
        resolveCheck = resolve
      })
      vi.mocked(studentsApi.checkDuplicates).mockReturnValue(checkPromise)

      render(<StudentForm onSubmit={mockOnSubmit} isSubmitting={false} />)

      await user.type(document.getElementById('firstName')!, 'John')
      await user.type(document.getElementById('lastName')!, 'Doe')
      await user.type(document.getElementById('email')!, 'john@example.com')

      // Wait for debounce and check
      await waitFor(
        () => {
          expect(screen.getByText('students.duplicate.checking')).toBeInTheDocument()
        },
        { timeout: 1000 }
      )

      // Resolve the check
      resolveCheck!({ hasDuplicates: false, duplicates: [] })

      await waitFor(() => {
        expect(screen.queryByText('students.duplicate.checking')).not.toBeInTheDocument()
      })
    })

    it('excludes current student when checking duplicates in edit mode', async () => {
      const user = userEvent.setup()
      const student: Student = {
        id: 'student-123',
        firstName: 'John',
        lastName: 'Doe',
        fullName: 'John Doe',
        email: 'john@example.com',
        status: 'Active',
        autoDebit: false,
        createdAt: '2024-01-01',
        updatedAt: '2024-01-01',
      }

      render(<StudentForm student={student} onSubmit={mockOnSubmit} isSubmitting={false} />)

      // Trigger a change to invoke duplicate check
      const firstNameInput = document.getElementById('firstName')!
      await user.clear(firstNameInput)
      await user.type(firstNameInput, 'Johnny')

      await waitFor(() => {
        expect(studentsApi.checkDuplicates).toHaveBeenCalledWith(
          expect.objectContaining({
            excludeId: 'student-123',
          })
        )
      })
    })
  })
})
